using UnityEngine;
using UnityEngine.UI;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("References")]
    public Joystick joystick;         // 🎮 Assign mobile joystick
    public Button dashButton;         // UI button for dash
    public Button slideButton;        // UI button for slide

    [Header("Keyboard Bindings")]
    public KeyCode moveForwardKey = KeyCode.W;
    public KeyCode moveBackwardKey = KeyCode.S;
    public KeyCode moveLeftKey = KeyCode.A;
    public KeyCode moveRightKey = KeyCode.D;
    public KeyCode dashKey = KeyCode.X;
    public KeyCode slideKey = KeyCode.Z;

    // 📦 Outputs consumed by movement controller
    [HideInInspector] public Vector2 moveInput; // (x = horizontal, y = vertical)
    [HideInInspector] public bool dashPressed;
    [HideInInspector] public bool slidePressed;

    void Start()
    {
        // Hook up UI buttons
        if (dashButton != null)
            dashButton.onClick.AddListener(() => dashPressed = true);

        if (slideButton != null)
            slideButton.onClick.AddListener(() => slidePressed = true);
    }

    void Update()
    {
        // ✅ Joystick-based movement
        if (joystick != null)
        {
            moveInput = new Vector2(joystick.Horizontal, joystick.Vertical);
        }
        else
        {
            // ✅ Keyboard fallback (WASD)
            float x = 0f;
            float y = 0f;

            if (Input.GetKey(moveLeftKey)) x = -1f;
            if (Input.GetKey(moveRightKey)) x = 1f;
            if (Input.GetKey(moveForwardKey)) y = 1f;
            if (Input.GetKey(moveBackwardKey)) y = -1f;

            moveInput = new Vector2(x, y).normalized;
        }

        // ✅ Dash & Slide (keyboard)
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
