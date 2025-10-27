using UnityEngine;
using UnityEngine.UI;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("References")]
    public Joystick joystick;         // Assign mobile joystick
    public Button dashButton;         // UI button for dash
    public Button slideButton;        // UI button for slide

    [Header("Keyboard Bindings")]
    public KeyCode rotateLeftKey = KeyCode.A;
    public KeyCode rotateRightKey = KeyCode.D;
    public KeyCode dashKey = KeyCode.X;
    public KeyCode slideKey = KeyCode.Z;

    [HideInInspector] public float rotationInput;
    [HideInInspector] public bool dashPressed;
    [HideInInspector] public bool slidePressed;

    void Start()
    {
        if (dashButton != null)
            dashButton.onClick.AddListener(() => dashPressed = true);

        if (slideButton != null)
            slideButton.onClick.AddListener(() => slidePressed = true);
    }

    void Update()
    {
        // Rotation input (from joystick or keyboard)
        if (joystick != null)
            rotationInput = joystick.Horizontal;
        else
        {
            rotationInput = 0f;
            if (Input.GetKey(rotateLeftKey)) rotationInput = -1f;
            if (Input.GetKey(rotateRightKey)) rotationInput = 1f;
        }

        // Dash & Slide keys
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
