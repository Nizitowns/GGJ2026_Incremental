using HolenderGames.StatSystem;
using UnityEngine;

[CreateAssetMenu(menuName = "Grass Game/Config", fileName = "GrassGameConfig")]
public class GrassGameConfig : ScriptableObject
{
    [Header("Stat Keys (UpgradeTree PRO / StatsSystem)")]
    [SerializeField] private StatType sessionTimeSecondsStat;

    [SerializeField] private StatType breakerRadiusStat;
    [SerializeField] private StatType breakerDamageStat;
    [SerializeField] private StatType breakerTickIntervalStat;
    [SerializeField] private StatType breakerCritChanceStat;
    [SerializeField] private StatType breakerCritBonusMultiplierStat;

    [SerializeField] private StatType startingGrassHPStat;
    [SerializeField] private StatType startingGrassPatchCountStat;
    [SerializeField] private StatType maxGrassPatchesStat;

    [SerializeField] private StatType baseRespawnRatePerSecondStat;
    [SerializeField] private StatType respawnRatePerCutPerSecondStat;
    [SerializeField] private StatType baseTargetPopulationStat;
    [SerializeField] private StatType targetPopulationPerCutPerSecondStat;
    [SerializeField] private StatType pressureWindowSecondsStat;

    [Header("World / Spawning (Not upgraded usually)")]
    [SerializeField] private Vector3 fieldCenter = Vector3.zero;
    [SerializeField] private Vector2 fieldSize = new(20f, 20f);
    [SerializeField, Min(0f)] private float spawnY = 0f;

    [Header("Spawn Collision / Spacing (Usually not upgraded)")]
    [SerializeField, Min(0f)] private float m_spawnAvoidRadius = 0.5f;
    [SerializeField, Min(1)] private int m_spawnAttemptsPerPatch = 12;

    // Expose StatType keys via read-only properties (so other scripts can’t change them at runtime)
    public StatType SessionTimeSecondsStat => sessionTimeSecondsStat;

    public StatType BreakerRadiusStat => breakerRadiusStat;
    public StatType BreakerDamageStat => breakerDamageStat;
    public StatType BreakerTickIntervalStat => breakerTickIntervalStat;
    public StatType BreakerCritChanceStat => breakerCritChanceStat;
    public StatType BreakerCritBonusMultiplierStat => breakerCritBonusMultiplierStat;

    public StatType StartingGrassHPStat => startingGrassHPStat;
    public StatType StartingGrassPatchCountStat => startingGrassPatchCountStat;
    public StatType MaxGrassPatchesStat => maxGrassPatchesStat;

    public StatType BaseRespawnRatePerSecondStat => baseRespawnRatePerSecondStat;
    public StatType RespawnRatePerCutPerSecondStat => respawnRatePerCutPerSecondStat;
    public StatType BaseTargetPopulationStat => baseTargetPopulationStat;
    public StatType TargetPopulationPerCutPerSecondStat => targetPopulationPerCutPerSecondStat;
    public StatType PressureWindowSecondsStat => pressureWindowSecondsStat;

    // Non-stat config values
    public Vector3 FieldCenter => fieldCenter;
    public Vector2 FieldSize => fieldSize;
    public float SpawnY => spawnY;

    public float SpawnAvoidRadius => m_spawnAvoidRadius;
    public int SpawnAttemptsPerPatch => m_spawnAttemptsPerPatch;

    public Bounds GetFieldBounds()
    {
        var size3 = new Vector3(fieldSize.x, 0.01f, fieldSize.y);
        return new Bounds(new Vector3(fieldCenter.x, spawnY, fieldCenter.z), size3);
    }
}
