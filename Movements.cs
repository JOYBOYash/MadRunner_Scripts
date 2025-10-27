using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementController : MonoBehaviour
{
    [Header("References")]
    public PlayerInputHandler input;
    public PlayerAnimationController animatorController;
    public PlayerMovementConfig config; // ScriptableObject config
    public Transform cameraTransform;   // Assign your main camera here

    [Header("Movement Mode")]
    public bool useCameraRelativeMovement = true; // toggles between world and camera-relative

    [Header("Rotation / Input")]
    public float inputDeadzone = 0.15f;

    private CharacterController controller;
    private float currentSpeed;
    private float speedTimer = 0f;

    private Vector3 moveDirection;
    private Vector3 targetDirection;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (config == null)
        {
            Debug.LogError("❌ PlayerMovementConfig not assigned!");
            enabled = false;
            return;
        }

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        currentSpeed = config.baseSpeed;
    }

    void Update()
    {
        if (input == null)
        {
            Debug.LogWarning("⚠ No PlayerInputHandler assigned!");
            return;
        }

        HandleMovementAndRotation();
        HandleActions();
    }

    void HandleMovementAndRotation()
    {
        // ✅ Step 1: Get joystick input
        Vector2 moveInput = input.moveInput; // make sure your PlayerInputHandler exposes this as Vector2

        if (moveInput.magnitude < inputDeadzone)
        {
            moveInput = Vector2.zero;
        }

        // ✅ Step 2: Build camera-relative direction
        if (useCameraRelativeMovement && cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            // Flatten to ground plane (no tilt)
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            targetDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;
        }
        else
        {
            // World-space movement (no camera influence)
            targetDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        }

        // ✅ Step 3: Rotate player toward target direction
        if (targetDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                config.rotationSmoothness * Time.deltaTime
            );
        }

        // ✅ Step 4: Gradually increase player’s forward speed over time
        speedTimer += Time.deltaTime;
        currentSpeed = Mathf.Min(config.baseSpeed + speedTimer * config.speedMultiplier, config.maxSpeed);

        // ✅ Step 5: Move player forward continuously
        Vector3 forwardMove = transform.forward * currentSpeed;

        // Apply gravity
        if (!controller.isGrounded)
            forwardMove.y -= config.gravity * Time.deltaTime;

        controller.Move(forwardMove * Time.deltaTime);

        // ✅ Step 6: Update animations
        if (animatorController != null && animatorController.animator != null)
        {
            animatorController.animator.SetFloat("Speed", currentSpeed);
        }
    }

    void HandleActions()
    {
        if (input.dashPressed)
        {
            animatorController.TriggerDash();
            FindFirstObjectByType<SmoothCameraFollow>()?.TriggerCinematicEffect();
            input.dashPressed = false;
        }

        if (input.slidePressed)
        {
            animatorController.TriggerSlide();
            FindFirstObjectByType<SmoothCameraFollow>()?.TriggerCinematicEffect();
            input.slidePressed = false;
        }
    }
}
