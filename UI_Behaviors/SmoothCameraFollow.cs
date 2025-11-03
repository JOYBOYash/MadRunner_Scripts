using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class SmoothCameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;                   // Player transform to follow

    [Header("Follow Settings")]
    public Vector3 followOffset = new Vector3(0f, 5f, -10f); // Camera position offset
    public float followSmoothness = 5f;                      // Lerp speed for following

    [Header("Zoom Settings")]
    public float normalZoom = 7f;               // Default orthographic size
    public float zoomedInSize = 5f;             // Zoom size during dash/slide
    public float zoomSpeed = 3f;                // How fast the zoom transitions

    [Header("Slow Motion Settings")]
    public float slowMoScale = 0.4f;            // 0.4 = 40% of normal time
    public float slowMoDuration = 0.5f;         // Duration in real-time seconds

    private Camera cam;
    private bool isZooming = false;
    private float targetZoom;

    void Start()
    {
        cam = GetComponent<Camera>();

        if (!cam.orthographic)
        {
            Debug.LogWarning("SmoothCameraFollow is optimized for Orthographic cameras!");
            cam.orthographic = true;
        }

        targetZoom = normalZoom;

        if (target == null)
            Debug.LogWarning("Camera has no target assigned! Please assign the player transform.");
    }

    void LateUpdate()
    {
        if (target == null) return;

        // FOLLOW PLAYER WITH OFFSET
        Vector3 desiredPosition = target.position + followOffset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, followSmoothness * Time.deltaTime);
        transform.position = smoothedPosition;

        // Maintain orthographic size smoothly
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, zoomSpeed * Time.deltaTime);
    }

    // Public function to trigger Dash/Slide effects
    public void TriggerCinematicEffect()
    {
        if (!isZooming)
            StartCoroutine(CinematicRoutine());
    }

    private IEnumerator CinematicRoutine()
    {
        isZooming = true;

        // Step 1: Slow down time
        float originalTimeScale = Time.timeScale;
        Time.timeScale = slowMoScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale; // Fix physics delta

        // Step 2: Zoom in
        targetZoom = zoomedInSize;
        yield return new WaitForSecondsRealtime(slowMoDuration);

        // Step 3: Restore time and zoom out
        Time.timeScale = originalTimeScale;
        Time.fixedDeltaTime = 0.02f;
        targetZoom = normalZoom;

        isZooming = false;
    }
}
