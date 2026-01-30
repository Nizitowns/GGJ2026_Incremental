using System;
using HolenderGames.StatSystem;
using UnityEngine;

public class GameSessionController : MonoBehaviour
{
    public enum SessionState
    {
        Idle, Running, Ended
    }

    [Header("Config")]
    [SerializeField] private GrassGameConfig config;

    [Header("Systems")]
    [SerializeField] private BreakerController breaker;
    [SerializeField] private GrassSpawner spawner;
    [SerializeField] private GrassCutterSystem cutter;

    public SessionState State { get; private set; } = SessionState.Idle;
    public float TimeRemaining
    {
        get; private set;
    }

    public event Action SessionStarted;
    public event Action SessionEnded;

    private void Awake()
    {
        if (!config)
            Debug.LogError($"{name}: Missing GrassGameConfig reference.");
        if (!breaker)
            Debug.LogError($"{name}: Missing BreakerController reference.");
        if (!spawner)
            Debug.LogError($"{name}: Missing GrassSpawner reference.");
        if (!cutter)
            Debug.LogError($"{name}: Missing GrassCutterSystem reference.");

        // Inject config into systems (keeps upgrades easy later)
        if (breaker)
            breaker.SetConfig(config);
        if (spawner)
            spawner.SetConfig(config);
        if (cutter)
            cutter.SetConfig(config);
    }

    private void Start()
    {
        StartSession();
    }

    public void StartSession()
    {
        Debug.Log("StartSession sessionTime=" +
          GameData.Instance.GetStat(config.SessionTimeSecondsStat));
        float sessionSeconds = 10f;

        if (config != null && GameData.Instance != null)
            sessionSeconds = GameData.Instance.GetStat(config.SessionTimeSecondsStat);

        TimeRemaining = sessionSeconds;
        State = SessionState.Running;

        spawner?.ResetSpawner();
        spawner?.SpawnInitial();

        cutter?.Begin(this, breaker, spawner);

        SessionStarted?.Invoke();
    }


    private void Update()
    {
        if (State != SessionState.Running)
            return;

        TimeRemaining -= Time.deltaTime;
        if (TimeRemaining <= 0f)
        {
            EndSession();
        }
    }

    public void EndSession()
    {
        if (State == SessionState.Ended)
            return;

        State = SessionState.Ended;
        TimeRemaining = 0f;

        cutter?.Stop();
        spawner?.Stop();

        SessionEnded?.Invoke();
    }
    public void RestartSession()
    {
        // stop anything still running
        cutter?.Stop();
        spawner?.Stop();

        // IMPORTANT: read latest stats here (not cached)
        StartSession();
    }

    public GrassGameConfig Config => config;

    private void OnDrawGizmosSelected()
    {
        if (!config)
            return;
        var b = config.GetFieldBounds();
        Gizmos.DrawWireCube(b.center, new Vector3(b.size.x, 0.01f, b.size.z));
    }
}
