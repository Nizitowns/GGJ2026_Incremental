
namespace HolenderGames.StatSystem
{
    /// <summary>
    /// All the types of stats that upgrades can manipulate
    /// Don't change the order, as it will break existing upgrades
    /// </summary>
    public enum StatType
    {
        Wizard_Damage,
        Wizard_AttackSpeed,
        Wizard_Shields,
        Wizard_Health,
        Magician_Damage,

        Magician_Shields,
        Magician_Health,
        UnlockMagician,
        UnlockQuestBoard,
        SessionTime,
        BreakerRadius,
        BreakerDamage,
        BreakerTickInterval,
        BreackerCritChance,
        BreakerCritBonusMultiplier,
        GrassStartingHP,
        GrassStartingPatchCount,
        GrassMaxPatchCount,
        SpawnerBaseRespawnRate,
        SpawnerRespawnRatePerCut,
        SpawnerBaseTargetPopulation,
        SpawnerTargetPopulationPerCut,
        SpawnerPressureWindowSeconds,
        Barrel_HP,
        Barrel_SpawnChance,
        Barrel_BurnDamage,
        Barrel_BurnRadius
    }

}

