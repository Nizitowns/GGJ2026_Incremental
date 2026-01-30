using System;
using HolenderGames.StatSystem;
using UnityEngine;

public class GrassCutterSystem : MonoBehaviour
{
    public event Action SnipTick;

    [SerializeField] private int overlapBufferSize = 256;

    private GrassGameConfig config;
    private GameSessionController session;
    private BreakerController breaker;
    private GrassSpawner spawner;

    private Collider[] overlapBuffer;
    private bool running;
    private float nextTickTime;

    public void SetConfig(GrassGameConfig cfg) => config = cfg;

    private void Awake()
    {
        overlapBuffer = new Collider[Mathf.Max(32, overlapBufferSize)];
    }

    public void Begin(GameSessionController sessionController, BreakerController breakerController, GrassSpawner grassSpawner)
    {
        session = sessionController;
        breaker = breakerController;
        spawner = grassSpawner;

        running = true;
        nextTickTime = Time.time; // tick immediately
    }

    public void Stop()
    {
        running = false;
        session = null;
        breaker = null;
        spawner = null;
    }

    private void Update()
    {
        if (!running || config == null || session == null || breaker == null || spawner == null)
            return;
        if (session.State != GameSessionController.SessionState.Running)
            return;
        if (GameData.Instance == null)
            return;

        float now = Time.time;
        if (now < nextTickTime)
            return;

        float tickInterval = Mathf.Max(0.01f, GameData.Instance.GetStat(config.BreakerTickIntervalStat));
        nextTickTime = now + tickInterval;

        DoSnipTick();
    }

    private void DoSnipTick()
    {
        SnipTick?.Invoke(); // pulse UI every tick (move below if you only want pulse-on-hit)

        Vector3 center = breaker.BreakerWorldPos;
        float radius = breaker.Radius; // already stat-driven in your updated BreakerController

        int hitCount = Physics.OverlapSphereNonAlloc(
            center,
            radius,
            overlapBuffer,
            spawner.GrassMask,
            QueryTriggerInteraction.Ignore
        );

        if (hitCount <= 0)
            return;

        float baseDmg = Mathf.Max(0f, GameData.Instance.GetStat(config.BreakerDamageStat));
        float critChance = Mathf.Clamp01(GameData.Instance.GetStat(config.BreakerCritChanceStat));
        float critMult = Mathf.Max(1f, GameData.Instance.GetStat(config.BreakerCritBonusMultiplierStat));

        for (int i = 0; i < hitCount; i++)
        {
            Collider c = overlapBuffer[i];
            if (!c)
                continue;

            GrassPatch patch = c.GetComponent<GrassPatch>() ?? c.GetComponentInParent<GrassPatch>();
            if (!patch)
                continue;

            float dmg = baseDmg;
            if (critChance > 0f && UnityEngine.Random.value < critChance)
                dmg *= critMult;

            patch.ApplyDamage(dmg);
        }
    }
}
