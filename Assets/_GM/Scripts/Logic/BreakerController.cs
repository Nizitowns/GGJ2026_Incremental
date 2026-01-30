using HolenderGames.StatSystem;
using UnityEngine;
using UnityEngine.InputSystem;

public class BreakerController : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask playfieldMask = ~0;

    private GrassGameConfig config;

    public Vector3 BreakerWorldPos
    {
        get; private set;
    }

    // Radius now comes from stats (not config float)
    public float Radius
    {
        get
        {
            if (config == null || GameData.Instance == null)
                return 1f;
            return GameData.Instance.GetStat(config.BreakerRadiusStat);
        }
    }

    public void SetConfig(GrassGameConfig cfg) => config = cfg;

    private void Awake()
    {
        if (!mainCamera)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        UpdateBreakerPosition();
    }

    private void UpdateBreakerPosition()
    {
        if (!mainCamera)
            return;
        if (Mouse.current == null)
            return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mouseScreen);

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, playfieldMask, QueryTriggerInteraction.Ignore))
        {
            BreakerWorldPos = hit.point;
            return;
        }

        // Fallback plane at SpawnY (now via getter)
        float y = (config != null) ? config.SpawnY : 0f;
        Plane plane = new Plane(Vector3.up, new Vector3(0f, y, 0f));
        if (plane.Raycast(ray, out float enter))
            BreakerWorldPos = ray.GetPoint(enter);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(BreakerWorldPos, Radius);
    }
}
