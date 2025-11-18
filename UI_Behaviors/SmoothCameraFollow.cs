using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class SmoothCameraFollow : NetworkBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Camera will automatically find the local player if left empty.")]
    public Transform target;

    [Header("Follow Settings")]
    public Vector3 followOffset = new Vector3(0, 5, -10);
    public float followSmoothness = 5f;

     public GameObject gameObject;

    [Header("Zoom Settings")]
    public float normalZoom = 7f;
    public float zoomedInSize = 5f;
    public float zoomSpeed = 3f;

    [Header("Slow Motion Settings")]
    public float slowMoScale = 0.4f;
    public float slowMoDuration = 0.5f;

    private Camera cam;
    private float targetZoom;
    private bool isZooming = false;
    private bool targetFound = false;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        targetZoom = normalZoom;
    }

    public override void OnNetworkSpawn()
    {
        // Only the OWNER gets the camera
        if (!IsOwner)
        {
            gameObject.SetActive(false);
            return;
        }

        // If target isn't set yet, search for local player
        if (target == null)
            StartCoroutine(FindLocalPlayerRoutine());
    }

    private IEnumerator FindLocalPlayerRoutine()
    {
        float timeout = 5f;
        Debug.Log("🎥 Camera searching for local player...");

        while (timeout > 0)
        {
            if (NetworkManager.Singleton.LocalClient != null &&
                NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                target = NetworkManager.Singleton.LocalClient.PlayerObject.transform;
                targetFound = true;

                Debug.Log("🎉 Camera successfully attached to LOCAL player.");
                yield break;
            }

            timeout -= Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning("⚠️ Camera could not find local player!");
    }

    void LateUpdate()
    {
        if (!IsOwner || !targetFound || target == null)
            return;

        // Smooth follow
        Vector3 desiredPos = target.position + followOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, followSmoothness * Time.deltaTime);

        // Smooth zoom
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, zoomSpeed * Time.deltaTime);
    }

    // ---------------------------------------
    // 🎬 Cinematic Dash / Slide Effect
    // ---------------------------------------
    public void TriggerCinematicEffect()
    {
        if (!isZooming)
            StartCoroutine(CinematicEffectRoutine());
    }

    private IEnumerator CinematicEffectRoutine()
    {
        isZooming = true;

        float originalTimeScale = Time.timeScale;

        // Slow motion
        Time.timeScale = slowMoScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // Zoom in
        targetZoom = zoomedInSize;
        yield return new WaitForSecondsRealtime(slowMoDuration);

        // Restore
        Time.timeScale = originalTimeScale;
        Time.fixedDeltaTime = 0.02f;
        targetZoom = normalZoom;

        isZooming = false;
    }
}
