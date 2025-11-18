using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PlayerInputHandler : NetworkBehaviour
{
    [Header("UI References (Auto-assigned at runtime)")]
    public Joystick joystick;
    public Button dashButton;
    public Button slideButton;

    [Header("Keyboard Bindings")]
    public KeyCode moveForwardKey = KeyCode.W;
    public KeyCode moveBackwardKey = KeyCode.S;
    public KeyCode moveLeftKey = KeyCode.A;
    public KeyCode moveRightKey = KeyCode.D;
    public KeyCode dashKey = KeyCode.X;
    public KeyCode slideKey = KeyCode.Z;

    // Outputs consumed by PlayerController
    [HideInInspector] public Vector2 moveInput;
    [HideInInspector] public bool dashPressed;
    [HideInInspector] public bool slidePressed;

    private bool uiBound = false;

    public override void OnNetworkSpawn()
    {
        // ❌ Don’t read input from remote players
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        // ✅ Dynamically find joystick & buttons in the local scene
        TryBindUI();
    }

    void TryBindUI()
    {
        // Find joystick
        if (joystick == null)
        {
            joystick = FindObjectOfType<Joystick>(true);
            if (joystick != null)
                Debug.Log("🎮 Joystick found and assigned dynamically!");
            else
                Debug.LogWarning("⚠️ No Joystick found in scene!");
        }

        // Find buttons
        if (dashButton == null)
        {
            var dash = GameObject.Find("DashButton");
            if (dash != null)
                dashButton = dash.GetComponent<Button>();
        }

        if (slideButton == null)
        {
            var slide = GameObject.Find("SlideButton");
            if (slide != null)
                slideButton = slide.GetComponent<Button>();
        }

        // Hook up UI events (once)
        if (dashButton != null)
        {
            dashButton.onClick.RemoveAllListeners();
            dashButton.onClick.AddListener(() => dashPressed = true);
        }

        if (slideButton != null)
        {
            slideButton.onClick.RemoveAllListeners();
            slideButton.onClick.AddListener(() => slidePressed = true);
        }

        uiBound = true;
    }

    void Update()
    {
        if (!IsOwner) return; // only local player reads input
        if (!uiBound) TryBindUI();

        // ✅ Joystick movement
        if (joystick != null)
        {
            moveInput = new Vector2(joystick.Horizontal, joystick.Vertical);
        }
        else
        {
            // ✅ Keyboard fallback
            float x = 0f, y = 0f;
            if (Input.GetKey(moveLeftKey)) x = -1f;
            if (Input.GetKey(moveRightKey)) x = 1f;
            if (Input.GetKey(moveForwardKey)) y = 1f;
            if (Input.GetKey(moveBackwardKey)) y = -1f;
            moveInput = new Vector2(x, y).normalized;
        }

        // ✅ Button / Key triggers
        if (Input.GetKeyDown(dashKey))
            dashPressed = true;

        if (Input.GetKeyDown(slideKey))
            slidePressed = true;
    }

    public void ResetTriggers()
    {
        dashPressed = false;
        slidePressed = false;
    }
}
