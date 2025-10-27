using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementController : MonoBehaviour
{
    [Header("References")]
    public PlayerInputHandler input;
    public PlayerAnimationController animatorController;

    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float rotationSpeed = 120f;
    public float gravity = 9.8f;

    [Header("Movement Mode")]
    public bool useLocalAxis = true;   // ✅ Toggle between local/global axis
    [Tooltip("If true, character moves based on its facing direction (local axis). If false, uses world axes.")]

    private CharacterController controller;
    private Vector3 moveDirection;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (input == null)
        {
            Debug.LogWarning("No PlayerInputHandler assigned!");
            return;
        }

        HandleRotation();
        HandleMovement();
        HandleActions();
    }

    void HandleRotation()
    {
        // Only rotate character when in Local Axis mode
        if (useLocalAxis)
        {
            float rotationAmount = input.rotationInput * rotationSpeed * Time.deltaTime;
            transform.Rotate(Vector3.up * rotationAmount);
        }
    }

    void HandleMovement()
    {
        Vector3 desiredDirection;

        if (useLocalAxis)
        {
            // 🧭 Local: Move relative to where the player faces
            desiredDirection = transform.forward;
        }
        else
        {
            // 🌍 Global: Always move in +Z world direction (forward) but allow turning via input
            desiredDirection = Vector3.forward;

            // Allow strafing and backward motion in global mode using rotationInput
            desiredDirection += new Vector3(input.rotationInput, 0f, 0f);
        }

        desiredDirection.Normalize();
        moveDirection = desiredDirection * moveSpeed;

        // Keep player grounded
        if (!controller.isGrounded)
            moveDirection.y -= gravity * Time.deltaTime;

        controller.Move(moveDirection * Time.deltaTime);
    }

    void HandleActions()
    {
        if (input.dashPressed)
        {
            animatorController.TriggerDash();
            FindObjectOfType<SmoothCameraFollow>()?.TriggerCinematicEffect();
            input.dashPressed = false;
        }

        if (input.slidePressed)
        {
            animatorController.TriggerSlide();
            FindObjectOfType<SmoothCameraFollow>()?.TriggerCinematicEffect();
            input.slidePressed = false;
        }
    }
}
