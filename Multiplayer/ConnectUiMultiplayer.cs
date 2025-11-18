using System;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class ConnectUiMultiplayer : MonoBehaviour
{
    [Header("UI References")]
    public Button hostButton;
    public Button clientButton;

    [Header("Optional UI Prefabs")]
    public Joystick joystick;
    public Button dashButton;
    public Button slideButton;
    public Camera playerCamera;

    private void Start()
    {
        hostButton.onClick.AddListener(ClickStartHost);
        clientButton.onClick.AddListener(ClickStartClient);

        // Subscribe to player spawn events
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void ClickStartHost()
    {
        if (NetworkManager.Singleton.StartHost())
            Debug.Log("✅ Host started successfully");
        else
            Debug.LogError("❌ Failed to start host!");
    }

    private void ClickStartClient()
    {
        if (NetworkManager.Singleton.StartClient())
            Debug.Log("✅ Client connected successfully");
        else
            Debug.LogError("❌ Failed to connect as client!");
    }

    private void OnClientConnected(ulong clientId)
    {
        // This event fires for all players when anyone connects
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log($"🎮 Local player connected: {clientId}");
            StartCoroutine(AttachLocalPlayerControls());
        }
    }

    private System.Collections.IEnumerator AttachLocalPlayerControls()
    {
        // Wait until the local player object is spawned
        while (NetworkManager.Singleton.LocalClient == null ||
               NetworkManager.Singleton.LocalClient.PlayerObject == null)
        {
            yield return null;
        }

        var playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
        var controller = playerObj.GetComponent<PlayerController>();
        var inputHandler = playerObj.GetComponent<PlayerInputHandler>();

        if (controller == null || inputHandler == null)
        {
            Debug.LogWarning("⚠️ Player components not found on spawned player!");
            yield break;
        }

        // 🔁 Dynamically assign camera & joystick from UI scene
        if (joystick == null) joystick = FindObjectOfType<Joystick>(true);
        if (dashButton == null) dashButton = GameObject.Find("DashButton")?.GetComponent<Button>();
        if (slideButton == null) slideButton = GameObject.Find("SlideButton")?.GetComponent<Button>();

        inputHandler.joystick = joystick;
        inputHandler.dashButton = dashButton;
        inputHandler.slideButton = slideButton;

        // Assign camera
        if (playerCamera == null) playerCamera = Camera.main;
        controller.cameraTransform = playerCamera.transform;

        Debug.Log("🎯 Player input and camera successfully attached!");
    }
}
