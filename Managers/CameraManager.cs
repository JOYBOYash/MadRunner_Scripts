using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;

public class CameraManager : NetworkBehaviour
{
    [Header("Camera Prefab Reference")]
    public GameObject cameraPrefab; // 👈 Your camera prefab (with ScreenFlash, SmoothCameraFollowNet, etc.)

    [Header("UI Canvas Detection")]
    public Canvas mainCanvas;

    private GameObject activeCamera;
    private bool isAssigned = false;

    private void Start()
    {
        // Only local player gets a camera
        if (IsOwner)
            StartCoroutine(SetupCameraRoutine());
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
            StartCoroutine(SetupCameraRoutine());
    }

    private IEnumerator SetupCameraRoutine()
    {
        // Wait until everything is loaded (especially Canvas & player)
        yield return new WaitUntil(() => FindObjectOfType<Canvas>() != null);

        // Destroy any leftover camera (e.g. after restart)
        if (activeCamera != null)
        {
            Destroy(activeCamera);
            activeCamera = null;
        }

        // 🧠 Spawn camera prefab for this player
        if (cameraPrefab != null)
        {
            activeCamera = Instantiate(cameraPrefab);
            activeCamera.name = $"PlayerCamera_{OwnerClientId}";
            activeCamera.SetActive(true);

            // Set camera to follow the local player
            SmoothCameraFollow follow = activeCamera.GetComponent<SmoothCameraFollow>();
            if (follow != null)
                follow.target = transform;

            AssignUIToCamera(activeCamera);

            Debug.Log($"🎥 CameraManager: Camera assigned for player {OwnerClientId}");
        }
        else
        {
            Debug.LogWarning("⚠️ CameraManager: No camera prefab assigned!");
        }

        isAssigned = true;
    }

    private void AssignUIToCamera(GameObject cam)
    {
        // Wait until Canvas exists
        if (mainCanvas == null)
            mainCanvas = FindObjectOfType<Canvas>();

        if (mainCanvas == null)
        {
            Debug.LogWarning("⚠️ No Canvas found for Camera UI assignment!");
            return;
        }

        // Assign ScreenFlash
        ScreenFlash flash = cam.GetComponent<ScreenFlash>();
        if (flash != null)
        {
            Image foundFlash = null;
            foreach (var img in mainCanvas.GetComponentsInChildren<Image>(true))
            {
                if (img.name.ToLower().Contains("flash"))
                {
                    foundFlash = img;
                    break;
                }
            }

            if (foundFlash != null)
            {
                flash.flashImage = foundFlash;
                Debug.Log($"✅ Linked FlashImage → {foundFlash.name}");
            }
        }

        // Add more UI assignments as needed (health bar, crosshair, etc.)
    }

    // 🔄 Called on game restart or respawn
    public void ResetCamera()
    {
        isAssigned = false;

        if (activeCamera != null)
        {
            Destroy(activeCamera);
            activeCamera = null;
        }

        StartCoroutine(SetupCameraRoutine());
    }

    private void OnDisable()
    {
        if (IsOwner && activeCamera != null)
        {
            Destroy(activeCamera);
            activeCamera = null;
        }
    }
}
