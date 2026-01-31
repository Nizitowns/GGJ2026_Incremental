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
        Explosive_HP,
        Explosive_SpawnChance,
        Explosive_BurnDamage,
        Explosive_BurnRadius,
        statElectricSpawnChance,
        statElectricDamage,
        statElectricChains,
        statElectricChainRange,
        statBeamWidth,
        statBeamSpawnChance,
        statBeamDamage
    }

}

