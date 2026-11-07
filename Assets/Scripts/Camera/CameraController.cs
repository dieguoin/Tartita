using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public static CameraController instance;

    [Header("Target Tracking")]
    [SerializeField] private Transform target;
    [Tooltip("Target vertical offset relative to camera center (negative = player sits below center).")]
    [SerializeField] private float verticalOffset = -0.5f;
    [Tooltip("Smooth damping time for upward camera movement.")]
    [SerializeField] private float smoothTime = 0.18f;

    [Header("Orthographic View")]
    [SerializeField] private float defaultOrthoSize = 5.0f;
    [SerializeField] private float maxJumpOrthoSize = 5.25f;
    [SerializeField] private float zoomSmoothTime = 0.35f;

    private Camera cam;
    private float currentVelocityY;
    private float currentZoomVelocity;
    private float highestCameraY = 0f;

    public float HighestCameraY => highestCameraY;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(this);
            return;
        }

        cam = GetComponent<Camera>();
        if (cam != null)
        {
            defaultOrthoSize = cam.orthographicSize;
        }
        highestCameraY = transform.position.y;
    }

    private void Start()
    {
        FindPlayerTarget();
    }

    public void FindPlayerTarget()
    {
        if (target == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                target = playerGO.transform;
            }
        }
    }

    public void ResetCamera(float startY = 0f)
    {
        highestCameraY = startY;
        currentVelocityY = 0f;
        transform.position = new Vector3(transform.position.x, startY, transform.position.z);
        if (cam != null)
        {
            cam.orthographicSize = defaultOrthoSize;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            FindPlayerTarget();
            if (target == null) return;
        }

        // Camera only follows Tartita UPWARDS, never downward
        float desiredY = target.position.y - verticalOffset;

        if (desiredY > transform.position.y)
        {
            float newY = Mathf.SmoothDamp(transform.position.y, desiredY, ref currentVelocityY, smoothTime);
            // Strictly enforce monotonic upward progression
            if (newY > highestCameraY)
            {
                highestCameraY = newY;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }
        }

        // Optional subtle dynamic zoom on super leaps
        if (cam != null && cam.orthographic)
        {
            Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
            float speedY = targetRb != null ? Mathf.Max(0f, targetRb.linearVelocity.y) : 0f;
            float targetSize = speedY > 10f ? Mathf.Min(maxJumpOrthoSize, 5.25f) : defaultOrthoSize;
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetSize, ref currentZoomVelocity, zoomSmoothTime);
        }
    }
}
