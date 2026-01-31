using HolenderGames.Currencies;
using HolenderGames.StatSystem;
using System.Collections.Generic;
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

    [SerializeField] private ExplosivePatch explosivePrefab;
    [SerializeField] private LayerMask explosiveLayerMask;
    private readonly List<ExplosivePatch> aliveExplosives = new(64);
    private readonly Queue<ExplosivePatch> explosivePool = new(64);


    [Header("Explosive Stat Keys")]
    [SerializeField] private StatType statExplosiveHP;              // base 6
    [SerializeField] private StatType statExplosiveSpawnChance;     // base 0.10
    [SerializeField] private StatType statExplosiveBurnRadius;      // AOE range
    [SerializeField] private StatType statExplosiveBurnDamage;      // grassBurnDmg

    [Header("Electric Grass Stat Keys")]
    [SerializeField] private StatType statElectricSpawnChance;
    [SerializeField] private StatType statElectricDamage;
    [SerializeField] private StatType statElectricChains;

    // optional but recommended
    [SerializeField] private StatType statElectricChainRange;

    [Header("Lightning Visual")]
    [SerializeField] private Material lightningMaterial;
    [SerializeField] private float lightningWidth = 0.08f;
    [SerializeField] private float lightningLifetime = 0.12f;

    private readonly HashSet<GrassPatch> chainVisited = new();

    private Collider[] aoeBuffer = new Collider[256];

    public LayerMask CuttableMask => grassLayerMask;

    private readonly List<GrassPatch> alive = new(512);
    private readonly Queue<GrassPatch> pool = new(512);
    private readonly Queue<float> cutTimestamps = new(256);

    private bool running;
    private float spawnBudget;
    private readonly HashSet<GrassPatch> zapped = new();

    public int AliveCount => alive.Count + aliveExplosives.Count;
    public LayerMask GrassMask => grassLayerMask;
    public void ResetSpawner()
    {
        Stop();

        for (int i = alive.Count - 1; i >= 0; i--)
            Despawn(alive[i]);
        alive.Clear();

        for (int i = aliveExplosives.Count - 1; i >= 0; i--)
            DespawnExplosive(aliveExplosives[i]);
        aliveExplosives.Clear();

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
        int totalAlive = alive.Count + aliveExplosives.Count;

        if (totalAlive < target && totalAlive < maxPatches)
        {
            spawnBudget += respawnRate * Time.deltaTime;

            int toSpawn = Mathf.FloorToInt(spawnBudget);
            if (toSpawn > 0)
            {
                spawnBudget -= toSpawn;

                for (int i = 0; i < toSpawn; i++)
                {
                    int totalAliveNow = alive.Count + aliveExplosives.Count;
                    if (totalAliveNow >= target || totalAliveNow >= maxPatches)
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

    //private bool TrySpawnOne()
    //{
    //    if (!TryGetSpawnPosition(out Vector3 pos))
    //        return false;

    //    GrassPatch patch = (pool.Count > 0)
    //        ? pool.Dequeue()
    //        : Instantiate(grassPrefab, spawnedParent ? spawnedParent : transform);

    //    patch.transform.position = pos;

    //    patch.Cut -= OnGrassPatchCut;
    //    patch.Cut += OnGrassPatchCut;

    //    patch.Initialize(GetStartingGrassHP());

    //    alive.Add(patch);
    //    return true;
    //}
    private bool TrySpawnOne()
    {
        if (!TryGetSpawnPosition(out Vector3 pos))
            return false;

        bool spawnExplosive =
            explosivePrefab != null &&
            Random.value < Mathf.Clamp01(GS(statExplosiveSpawnChance));

        if (spawnExplosive)
        {
            ExplosivePatch explosive = (explosivePool.Count > 0)
                ? explosivePool.Dequeue()
                : Instantiate(explosivePrefab, spawnedParent ? spawnedParent : transform);

            explosive.transform.position = pos; // IMPORTANT

            explosive.Exploded -= OnExplosiveDetonated;
            explosive.Exploded += OnExplosiveDetonated;

            explosive.Initialize(Mathf.Max(0.01f, GS(statExplosiveHP)));

            aliveExplosives.Add(explosive);
            return true;
        }

        GrassPatch patch = (pool.Count > 0)
            ? pool.Dequeue()
            : Instantiate(grassPrefab, spawnedParent ? spawnedParent : transform);

        patch.transform.position = pos;

        patch.Cut -= OnGrassPatchCut;
        patch.Cut += OnGrassPatchCut;

        patch.Initialize(GetStartingGrassHP());
        bool isElectric = Random.value < Mathf.Clamp01(GS(statElectricSpawnChance));

        patch.SetElectric(isElectric);

        alive.Add(patch);
        return true;
    }
    //private void OnGrassPatchCut(GrassPatch patch)
    //{
    //    NotifyGrassCut();
    //    CurrencyManager.Instance.AddCurrency(CurrencyType.Gold, 1);

    //    // ⚡ trigger chain lightning BEFORE despawn
    //    if (patch.IsElectric)
    //        TriggerChainLightning(patch);

    //    Despawn(patch);
    //    alive.Remove(patch);
    //}

    private void OnGrassPatchCut(GrassPatch patch)
    {
        bool wasZapped = zapped.Remove(patch);

        if (!wasZapped)
        {
            NotifyGrassCut();
            CurrencyManager.Instance.AddCurrency(CurrencyType.Gold, 1);

            if (patch.IsElectric)
                TriggerChainLightning(patch);
        }

        Despawn(patch);
        alive.Remove(patch);
    }

    private void TriggerChainLightning(GrassPatch source)
    {
        int chains = Mathf.Max(0, Mathf.RoundToInt(GS(statElectricChains)));
        if (chains <= 0) return;

        float dmg = Mathf.Max(0f, GS(statElectricDamage));
        if (dmg <= 0f) return;

        float range = 9999f;
        if (statElectricChainRange != 0)
            range = Mathf.Max(0.01f, GS(statElectricChainRange));

        // Pick targets first (so the alive list changing from kills won’t break selection)
        chainVisited.Clear();
        chainVisited.Add(source);

        var from = source;
        var targets = new List<GrassPatch>(chains);

        for (int step = 0; step < chains; step++)
        {
            GrassPatch next = FindClosestGrass(from.transform.position, range);
            if (next == null) break;

            chainVisited.Add(next);
            targets.Add(next);
            from = next;
        }

        // Apply damage + draw lines
        Vector3 a = source.transform.position;
        for (int i = 0; i < targets.Count; i++)
        {
            GrassPatch t = targets[i];
            if (!t || !t.gameObject.activeInHierarchy) break;

            Vector3 b = t.transform.position;
            SpawnLightningLine(a, b);
            zapped.Add(t);
            t.ApplyDamage(dmg);
            a = b;
        }
    }
    private void SpawnLightningLine(Vector3 a, Vector3 b)
    {
        var go = new GameObject("LightningLine");
        go.transform.SetParent(spawnedParent ? spawnedParent : transform);

        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, a + Vector3.up * 0.1f);
        lr.SetPosition(1, b + Vector3.up * 0.1f);

        lr.startWidth = lightningWidth;
        lr.endWidth = lightningWidth;
        lr.material = lightningMaterial;
        lr.useWorldSpace = true;

        Destroy(go, lightningLifetime);
    }

    private GrassPatch FindClosestGrass(Vector3 from, float range)
    {
        float bestDistSq = range * range;
        GrassPatch best = null;

        for (int i = 0; i < alive.Count; i++)
        {
            GrassPatch p = alive[i];
            if (!p || !p.gameObject.activeInHierarchy) continue;
            if (chainVisited.Contains(p)) continue;

            float dSq = (p.transform.position - from).sqrMagnitude;
            if (dSq < bestDistSq)
            {
                bestDistSq = dSq;
                best = p;
            }
        }

        return best;
    }

    private void OnExplosiveDetonated(ExplosivePatch explosive)
    {
        BurnNearbyGrass(explosive.transform.position);
        CurrencyManager.Instance.AddCurrency(CurrencyType.Gold, 1);
        DespawnExplosive(explosive);
        aliveExplosives.Remove(explosive);
    }


    private void BurnNearbyGrass(Vector3 center)
    {
        float radius = Mathf.Max(0f, GS(statExplosiveBurnRadius));
        float burnDmg = Mathf.Max(0f, GS(statExplosiveBurnDamage));
        if (radius <= 0f || burnDmg <= 0f) return;

        int hitCount = Physics.OverlapSphereNonAlloc(
            center, radius, aoeBuffer, grassLayerMask, QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider c = aoeBuffer[i];
            if (!c) continue;

            GrassPatch grass = c.GetComponent<GrassPatch>() ?? c.GetComponentInParent<GrassPatch>();
            if (!grass) continue;

            grass.ApplyDamage(burnDmg);
        }
    }

    //private void DespawnExplosive(ExplosivePatch explosive)
    //{
    //    explosive.Exploded -= OnExplosiveDetonated;
    //    explosive.gameObject.SetActive(false);
    //    explosivePool.Enqueue(explosive);
    //}


    private void DespawnExplosive(ExplosivePatch explosive)
    {
        if (!explosive) return;
        explosive.Exploded -= OnExplosiveDetonated;
        explosive.gameObject.SetActive(false);
        explosivePool.Enqueue(explosive);
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

            Collider[] hits = Physics.OverlapSphere(pos, r, CuttableMask, QueryTriggerInteraction.Ignore);
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
