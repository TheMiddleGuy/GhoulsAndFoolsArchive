using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("References")]
    public BasicMovement[] players;
    public Camera cam;

    [Header("Zoom")]
    [Tooltip("Soft minimum distance to prevent extreme close-ups")]
    public float minZoom = 6f;

    [Tooltip("Extra framing padding (1.0 = exact fit, 1.1 = 10% margin)")]
    public float framingPadding = 1.1f;

    [Header("Camera")]
    [Tooltip("Downward tilt angle in degrees")]
    public float tiltAngle = 60f;
    public float requiredRadius = 5f;

    [Header("Smoothing")]
    [Tooltip("How fast the camera adjusts distance/height")]
    public float zoomSmooth = 12f;

    [Tooltip("How fast the camera moves horizontally")]
    public float positionSmooth = 8f;

    [Header("Framing")]
    [Tooltip("Pushes framing forward to reduce top empty space (world units)")]
    public float forwardBias = 0.5f;

    [Header("Level Fallback")]
    public Transform levelMin;
    public Transform levelMax;
    private Vector3 _posVelocity; // Needed for SmoothDamp
    private float _zoomVelocity;  // Needed for SmoothDamp
    private void OnEnable()
    {
        PlayerSpawner.PlayersSpawned += CollectPlayers;
    }

    private void OnDisable()
    {
        PlayerSpawner.PlayersSpawned -= CollectPlayers;
    }

    void CollectPlayers()
    {
        players = Object.FindObjectsByType<BasicMovement>(FindObjectsSortMode.None);

        if (players.Length == 0)
        {
            Debug.LogWarning("Camera manager: No players found");
        }
    }

    void LateUpdate()
    {
        if (players == null || players.Length == 0 || cam == null)
            return;

        Bounds bounds = ComputeBounds();
        UpdateCameraPosition(cam, bounds);
    }

    // =========================================================
    // BOUNDS STRUCT
    // =========================================================
    public class Bounds
    {
        public Vector3 min;
        public Vector3 max;
    }

    // =========================================================
    // COMPUTE PLAYER BOUNDS (INCLUDING RADIUS)
    // =========================================================
    Bounds ComputeBounds()
    {
        Bounds result = new Bounds();
        bool initialized = false;
        bool anyVisible = false;

        foreach (var p in players)
        {
            if (p == null) continue;

            // 1. Get the PlayerHealth component to check if they are alive
            PlayerHealth health = p.GetComponent<PlayerHealth>();

            // 2. Only track if they are alive AND visible
            // If they are dead, we treat them as if they don't exist for the camera
            if (health != null && !health.IsAlive) continue;
            if (!p.IsVisible()) continue;

            anyVisible = true;

            Vector3 pos = p.transform.position;
            float r = requiredRadius;

            Vector3 min = pos - new Vector3(r, 0f, r);
            Vector3 max = pos + new Vector3(r, 0f, r);

            if (!initialized)
            {
                result.min = min;
                result.max = max;
                initialized = true;
            }
            else
            {
                result.min = Vector3.Min(result.min, min);
                result.max = Vector3.Max(result.max, max);
            }
        }

        // If no players are alive/visible -> show whole level fallback
        if (!anyVisible && levelMin != null && levelMax != null)
        {
            result.min = levelMin.position;
            result.max = levelMax.position;
        }

        return result;
    }

    // =========================================================
    // CAMERA FRAMING USING FRUSTUM MATH
    // =========================================================
    void UpdateCameraPosition(Camera cam, Bounds bounds)
    {
        Vector3 center = (bounds.min + bounds.max) * 0.5f;
        center += Vector3.forward * forwardBias;
        Vector3 size = bounds.max - bounds.min;

        float halfWidth = size.x * 0.5f;
        float halfDepth = size.z * 0.5f;

        float vFovRad = cam.fieldOfView * Mathf.Deg2Rad;
        float hFovRad = 2f * Mathf.Atan(Mathf.Tan(vFovRad * 0.5f) * cam.aspect);

        float distForWidth = halfWidth / Mathf.Tan(hFovRad * 0.5f);
        float distForDepth = halfDepth / Mathf.Tan(vFovRad * 0.5f);

        float targetDistance = Mathf.Max(distForWidth, distForDepth);
        targetDistance *= framingPadding;
        targetDistance = Mathf.Max(targetDistance, minZoom);

        // STUTTER FIX 1: Use SmoothDamp instead of Lerp for Distance
        // We use a local variable to track "ideal" distance or just SmoothDamp directly
        float currentDistance = Vector3.Distance(cam.transform.position, center);
        float smoothDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref _zoomVelocity, 1f / zoomSmooth);

        float rad = tiltAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            0f,
            Mathf.Sin(rad) * smoothDistance,
            -Mathf.Cos(rad) * smoothDistance
        );

        Vector3 desiredPos = center + offset;

        // STUTTER FIX 2: SmoothDamp for Position
        // 1f / positionSmooth converts your "speed" into "smooth time"
        cam.transform.position = Vector3.SmoothDamp(
            cam.transform.position,
            desiredPos,
            ref _posVelocity,
            1f / positionSmooth
        );

        cam.transform.rotation = Quaternion.Euler(tiltAngle, 0f, 0f);
    }
}
