using DamageNumbersPro;
using HolenderGames.Currencies;
using HolenderGames.StatSystem;
using System.Collections.Generic;
using UnityEngine;
public class GrassSpawner : MonoBehaviour
{
    [Header("Damage Numbers Pro (GUI)")]
    [SerializeField] private DamageNumber damageNumberGuiPrefab;
    [SerializeField] private RectTransform damageNumbersRoot; // under your Canvas
    [SerializeField] private Camera worldCamera;              // usually Camera.main
    [SerializeField] private float damageNumberYOffset = 0.2f;
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

    [Header("Beam Grass Stat Keys")]
    [SerializeField] private StatType statBeamSpawnChance;
    [SerializeField] private StatType statBeamDamage;
    [SerializeField] private StatType statBeamWidth;

    [Header("Beam Visual")]
    [SerializeField] private Material beamMaterial;
    [SerializeField] private float beamLifetime = 0.15f;

    private readonly Collider[] beamHits = new Collider[512];
    private readonly HashSet<GrassPatch> nonPlayerKills = new(); // use this to avoid CPS/gold/lightning chaining


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
        patch.Damaged -= OnGrassPatchDamaged;
        patch.Damaged += OnGrassPatchDamaged;
        patch.Initialize(GetStartingGrassHP());

        bool isElectric = Random.value < Mathf.Clamp01(GS(statElectricSpawnChance));
        float beamChance = Mathf.Clamp01(GS(statBeamSpawnChance));
        bool isBeam = Random.value < beamChance;
        patch.SetBeam(isBeam);
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
        bool nonPlayer = nonPlayerKills.Remove(patch);
        if (!nonPlayer)
        {
            NotifyGrassCut();
            CurrencyManager.Instance.AddCurrency(CurrencyType.Gold, 1);

            if (patch.IsBeam)
                TriggerBeam(patch);
            else if (patch.IsElectric)
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
    private void SpawnBeamLine(Vector3 a, Vector3 b, float width)
    {
        var go = new GameObject("BeamLine");
        go.transform.SetParent(spawnedParent ? spawnedParent : transform);

        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, a + Vector3.up * 0.1f);
        lr.SetPosition(1, b + Vector3.up * 0.1f);

        lr.startWidth = width;
        lr.endWidth = width;
        lr.material = beamMaterial;
        lr.useWorldSpace = true;

        Destroy(go, beamLifetime);
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
    private void TriggerBeam(GrassPatch source)
    {
        float dmg = Mathf.Max(0f, GS(statBeamDamage));
        if (dmg <= 0f) return;

        float width = Mathf.Max(0.02f, GS(statBeamWidth));
        Bounds b = config.GetFieldBounds();

        // point to pass through
        Vector3 p = source.transform.position;

        Vector2 d2 = Random.insideUnitCircle.normalized;
        if (d2.sqrMagnitude < 0.0001f) d2 = Vector2.right;

        Vector3 dir = new Vector3(d2.x, 0f, d2.y);

        //if (!TryGetBeamEndpointsThroughBounds(config.GetFieldBounds(), p, dir, out Vector3 a, out Vector3 c))
        //    return;

        //a.y = config.SpawnY;
        //c.y = config.SpawnY;
        // HUGE beam length (so it never ends up short)

        float big = Mathf.Max(b.size.x, b.size.z) * 5f; // crank this up if you want

        Vector3 a = p - dir * big;
        Vector3 c = p + dir * big;

        // now a--c ALWAYS passes through p
        SpawnBeamLine(a, c, width);

        // damage via OverlapBox aligned to beam
        Vector3 seg = c - a;
        float len = seg.magnitude;
        if (len < 0.001f) return;

        Vector3 forward = seg / len;
        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
        Vector3 center = (a + c) * 0.5f;

        Vector3 halfExtents = new Vector3(width * 0.5f, 1f, len * 0.5f);

        int hitCount = Physics.OverlapBoxNonAlloc(
            center, halfExtents, beamHits, rot, grassLayerMask, QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            var col = beamHits[i];
            if (!col) continue;

            GrassPatch gp = col.GetComponent<GrassPatch>() ?? col.GetComponentInParent<GrassPatch>();
            if (!gp || !gp.gameObject.activeInHierarchy) continue;
            if (gp == source) continue;

            nonPlayerKills.Add(gp);
            gp.ApplyDamage(dmg);
        }
    }
    // Returns the 2 points where the line (p + t*dir) intersects the XZ rectangle of bounds.
    private static bool TryGetBeamEndpointsThroughBounds(Bounds b, Vector3 p, Vector3 dir, out Vector3 a, out Vector3 c)
    {
        a = c = default;

        // We only care about XZ
        float minX = b.min.x, maxX = b.max.x;
        float minZ = b.min.z, maxZ = b.max.z;

        // dir must not be ~zero in XZ
        Vector2 d = new Vector2(dir.x, dir.z);
        if (d.sqrMagnitude < 1e-8f) return false;

        float tMin = float.NegativeInfinity;
        float tMax = float.PositiveInfinity;

        // X slab
        if (Mathf.Abs(dir.x) < 1e-6f)
        {
            if (p.x < minX || p.x > maxX) return false; // parallel and outside
        }
        else
        {
            float tx1 = (minX - p.x) / dir.x;
            float tx2 = (maxX - p.x) / dir.x;
            if (tx1 > tx2) (tx1, tx2) = (tx2, tx1);
            tMin = Mathf.Max(tMin, tx1);
            tMax = Mathf.Min(tMax, tx2);
        }

        // Z slab
        if (Mathf.Abs(dir.z) < 1e-6f)
        {
            if (p.z < minZ || p.z > maxZ) return false;
        }
        else
        {
            float tz1 = (minZ - p.z) / dir.z;
            float tz2 = (maxZ - p.z) / dir.z;
            if (tz1 > tz2) (tz1, tz2) = (tz2, tz1);
            tMin = Mathf.Max(tMin, tz1);
            tMax = Mathf.Min(tMax, tz2);
        }

        if (tMax < tMin) return false;

        a = p + dir * tMin;
        c = p + dir * tMax;
        return true;
    }

    //private void TriggerBeam(GrassPatch source)
    //{
    //    float dmg = Mathf.Max(0f, GS(statBeamDamage));
    //    if (dmg <= 0f) return;

    //    float width = Mathf.Max(0.02f, GS(statBeamWidth)); // world units
    //    Bounds b = config.GetFieldBounds();

    //    // Pick a diagonal. You can also randomize which diagonal.
    //    Vector3 a = new Vector3(b.min.x, config.SpawnY, b.min.z);
    //    Vector3 c = new Vector3(b.max.x, config.SpawnY, b.max.z);

    //    // Visual
    //    SpawnBeamLine(a, c, width);

    //    // Damage: OverlapBox along the beam line
    //    Vector3 dir = (c - a);
    //    float len = dir.magnitude;
    //    if (len < 0.001f) return;
    //    dir /= len;

    //    Vector3 center = (a + c) * 0.5f;

    //    // OverlapBox uses half-extents in local space; we align Z with the beam direction
    //    Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
    //    Vector3 halfExtents = new Vector3(width * 0.5f, 1f, len * 0.5f); // y=1f just to catch grass colliders

    //    int hitCount = Physics.OverlapBoxNonAlloc(
    //        center,
    //        halfExtents,
    //        beamHits,
    //        rot,
    //        grassLayerMask,
    //        QueryTriggerInteraction.Ignore
    //    );

    //    for (int i = 0; i < hitCount; i++)
    //    {
    //        Collider col = beamHits[i];
    //        if (!col) continue;

    //        GrassPatch p = col.GetComponent<GrassPatch>() ?? col.GetComponentInParent<GrassPatch>();
    //        if (!p || !p.gameObject.activeInHierarchy) continue;

    //        // Don’t re-hit the source (it’s already dying)
    //        if (p == source) continue;

    //        // Mark as non-player kill BEFORE damage, so Cut event won’t trigger more effects/gold/CPS
    //        nonPlayerKills.Add(p);
    //        p.ApplyDamage(dmg);
    //    }
    //}


    public void SetConfig(GrassGameConfig cfg) => config = cfg;

    private void Despawn(GrassPatch patch)
    {
        if (!patch)
            return;
        patch.Cut -= OnGrassPatchCut;
        patch.Damaged -= OnGrassPatchDamaged;
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

    private void OnGrassPatchDamaged(GrassPatch patch, float dmg)
    {
        if (damageNumberGuiPrefab == null || damageNumbersRoot == null) return;
        if (worldCamera == null) worldCamera = Camera.main;

        Vector3 worldPos = patch.transform.position + Vector3.up * damageNumberYOffset;

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(worldCamera, worldPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            damageNumbersRoot,
            screen,
            null, // Screen Space Overlay canvas => null
            out Vector2 anchoredPos
        );

        damageNumberGuiPrefab.SpawnGUI(damageNumbersRoot, anchoredPos, dmg);
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
