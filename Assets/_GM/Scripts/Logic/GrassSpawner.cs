using System.Collections.Generic;
using HolenderGames.Currencies;
using HolenderGames.StatSystem;
using UnityEngine;
public class GrassSpawner : MonoBehaviour
{
    [Header("Prefab / Layers")]
    [SerializeField] private GrassPatch grassPrefab;
    [SerializeField] private LayerMask grassLayerMask;
    [SerializeField] private Transform spawnedParent;

    [Header("Config (non-stats)")]
    [SerializeField] private GrassGameConfig config;

    [Header("Stat Keys")]
    [SerializeField] private StatType statStartingGrassHP;
    [SerializeField] private StatType statStartingGrassPatchCount;
    [SerializeField] private StatType statMaxGrassPatches;

    [SerializeField] private StatType statBaseRespawnRatePerSecond;
    [SerializeField] private StatType statRespawnRatePerCutPerSecond;
    [SerializeField] private StatType statBaseTargetPopulation;
    [SerializeField] private StatType statTargetPopulationPerCutPerSecond;
    [SerializeField] private StatType statPressureWindowSeconds;

    private readonly List<GrassPatch> alive = new(512);
    private readonly Queue<GrassPatch> pool = new(512);
    private readonly Queue<float> cutTimestamps = new(256);

    private bool running;
    private float spawnBudget;

    public int AliveCount => alive.Count;
    public LayerMask GrassMask => grassLayerMask;

    public void ResetSpawner()
    {
        Stop();

        for (int i = alive.Count - 1; i >= 0; i--)
            Despawn(alive[i]);

        alive.Clear();
        cutTimestamps.Clear();
        spawnBudget = 0f;
    }

    public void SpawnInitial()
    {
        if (!config || !grassPrefab)
            return;

        running = true;

        int maxPatches = GetMaxGrassPatches();
        int count = Mathf.Clamp(GetStartingGrassPatchCount(), 0, maxPatches);

        for (int i = 0; i < count; i++)
            TrySpawnOne();
    }

    public void Stop() => running = false;

    public void NotifyGrassCut()
    {
        float now = Time.time;
        cutTimestamps.Enqueue(now);
        TrimCuts(now);
    }

    private void Update()
    {
        if (!running || !config || !grassPrefab)
            return;
        if (GameData.Instance == null)
            return;

        float now = Time.time;
        TrimCuts(now);

        float cps = GetCutsPerSecond(now);

        int maxPatches = GetMaxGrassPatches();

        int target = Mathf.Clamp(
            Mathf.RoundToInt(GetBaseTargetPopulation() + GetTargetPopPerCps() * cps),
            0,
            maxPatches
        );

        float respawnRate = GetBaseRespawnRate() + GetRespawnRatePerCps() * cps;

        if (alive.Count < target && alive.Count < maxPatches)
        {
            spawnBudget += respawnRate * Time.deltaTime;

            int toSpawn = Mathf.FloorToInt(spawnBudget);
            if (toSpawn > 0)
            {
                spawnBudget -= toSpawn;

                for (int i = 0; i < toSpawn; i++)
                {
                    if (alive.Count >= target || alive.Count >= maxPatches)
                        break;
                    if (!TrySpawnOne())
                        break;
                }
            }
        }
        else
        {
            spawnBudget = Mathf.Max(0f, spawnBudget - Time.deltaTime);
        }
    }

    private void TrimCuts(float now)
    {
        float window = GetPressureWindowSeconds();
        float cutoff = now - window;

        while (cutTimestamps.Count > 0 && cutTimestamps.Peek() < cutoff)
            cutTimestamps.Dequeue();
    }

    private float GetCutsPerSecond(float now)
    {
        float window = GetPressureWindowSeconds();
        if (window <= 0.0001f)
            return 0f;
        return cutTimestamps.Count / window;
    }

    private bool TrySpawnOne()
    {
        if (!TryGetSpawnPosition(out Vector3 pos))
            return false;

        GrassPatch patch = (pool.Count > 0)
            ? pool.Dequeue()
            : Instantiate(grassPrefab, spawnedParent ? spawnedParent : transform);

        patch.transform.position = pos;

        patch.Cut -= OnGrassPatchCut;
        patch.Cut += OnGrassPatchCut;

        patch.Initialize(GetStartingGrassHP());

        alive.Add(patch);
        return true;
    }

    private void OnGrassPatchCut(GrassPatch patch)
    {
        NotifyGrassCut();
        CurrencyManager.Instance.AddCurrency(CurrencyType.Gold, 1);
        Despawn(patch);
        alive.Remove(patch);
    }
    public void SetConfig(GrassGameConfig cfg) => config = cfg;

    private void Despawn(GrassPatch patch)
    {
        if (!patch)
            return;
        patch.Cut -= OnGrassPatchCut;
        patch.gameObject.SetActive(false);
        pool.Enqueue(patch);
    }

    private bool TryGetSpawnPosition(out Vector3 pos)
    {
        Bounds b = config.GetFieldBounds();

        float r = Mathf.Max(0f, config.SpawnAvoidRadius);

        for (int attempt = 0; attempt < config.SpawnAttemptsPerPatch; attempt++)
        {
            float x = Random.Range(b.min.x, b.max.x);
            float z = Random.Range(b.min.z, b.max.z);
            pos = new Vector3(x, config.SpawnY, z);

            if (r <= 0f)
                return true;

            Collider[] hits = Physics.OverlapSphere(pos, r, grassLayerMask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
                return true;
        }

        pos = default;
        return false;
    }

    // ---- Stat helpers (via GameData) ----
    private float GS(StatType t) => GameData.Instance.GetStat(t);

    private int GetStartingGrassPatchCount() => Mathf.Max(0, Mathf.RoundToInt(GS(statStartingGrassPatchCount)));
    private int GetMaxGrassPatches() => Mathf.Max(0, Mathf.RoundToInt(GS(statMaxGrassPatches)));
    private float GetStartingGrassHP() => Mathf.Max(0.01f, GS(statStartingGrassHP));

    private float GetBaseRespawnRate() => Mathf.Max(0f, GS(statBaseRespawnRatePerSecond));
    private float GetRespawnRatePerCps() => Mathf.Max(0f, GS(statRespawnRatePerCutPerSecond));

    private float GetBaseTargetPopulation() => Mathf.Max(0f, GS(statBaseTargetPopulation));
    private float GetTargetPopPerCps() => Mathf.Max(0f, GS(statTargetPopulationPerCutPerSecond));

    private float GetPressureWindowSeconds() => Mathf.Max(0.1f, GS(statPressureWindowSeconds));
}
