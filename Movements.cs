using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("References")]
    public PlayerInputHandler input;
    public PlayerAnimationController animatorController;
    public PlayerMovementConfig config;
    public Transform cameraTransform;
    public PlayerAudioManager audioManager;

    [Header("Settings")]
    public bool useCameraRelativeMovement = true;
    public float inputDeadzone = 0.15f;

    [Header("Dash / Slide Settings")]
    public float dashSpeedMultiplier = 2.5f;
    public float slideSpeedMultiplier = 1.8f;
    public float boostDuration = 0.5f;

    private CharacterController controller;
    private float currentSpeed;
    private Vector3 targetDirection;
    private bool isSpeedBoosted = false;
    private float boostTimer = 0f;
    private float targetSpeed;

    // 🧠 Networked variables for position & rotation sync
    private NetworkVariable<Vector3> networkPosition = new(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<Quaternion> networkRotation = new(writePerm: NetworkVariableWritePermission.Owner);

    // Smooth interpolation for remote clients
    private Vector3 lastReceivedPos;
    private Quaternion lastReceivedRot;
    private float lerpSpeed = 10f;

    public override void OnNetworkSpawn()
    {
        controller = GetComponent<CharacterController>();
        // if (cameraTransform == null && Camera.main != null)
        //     cameraTransform = Camera.main.transform;

        if (audioManager == null)
            audioManager = GetComponent<PlayerAudioManager>();

        currentSpeed = config.baseSpeed;
        targetSpeed = currentSpeed;

        // Disable input for non-owners
        if (!IsOwner)
        {
            if (input != null)
                input.enabled = false;
        }

        // Set color or tag for clarity
        name = IsOwner ? "LocalPlayer" : $"RemotePlayer_{OwnerClientId}";
    }

    void Update()
    {
        if (!IsSpawned)
            return;

        if (IsOwner)
        {
            // Only owner handles movement logic
            if (PlayerHealth.IsPlayerDead)
            {
                animatorController?.animator?.SetFloat("Speed", 0f);
                audioManager?.SetRunningState(false);
                return;
            }

            HandleMovementAndRotation();
            HandleActions();
            UpdateSpeedBoost();

            // Sync position & rotation across network
            networkPosition.Value = transform.position;
            networkRotation.Value = transform.rotation;
        }
        else
        {
            // Smoothly interpolate non-owner positions
            InterpolateRemotePlayer();
        }
    }

    // 🧭 Smooth interpolation for other clients
    private void InterpolateRemotePlayer()
    {
        transform.position = Vector3.Lerp(transform.position, networkPosition.Value, Time.deltaTime * lerpSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation.Value, Time.deltaTime * lerpSpeed);
    }

    // 🏃 Movement + rotation for local player
    void HandleMovementAndRotation()
    {
        if (input == null)
        {
            Debug.LogWarning("⚠ No PlayerInputHandler assigned!");
            return;
        }

        Vector2 moveInput = input.moveInput;
        if (moveInput.magnitude < inputDeadzone)
            moveInput = Vector2.zero;

        // Camera-relative movement
        if (useCameraRelativeMovement && cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            targetDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;
        }
        else
        {
            targetDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        }

        // Rotate player
        if (targetDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, config.rotationSmoothness * Time.deltaTime);
        }

        // Move
        Vector3 move = targetDirection * currentSpeed * Time.deltaTime;
        if (!controller.isGrounded)
            move.y -= config.gravity * Time.deltaTime;

        controller.Move(move);

        // Update animation and sound
        bool isMoving = controller.velocity.magnitude > 0.1f && controller.isGrounded;
        animatorController?.animator?.SetFloat("Speed", controller.velocity.magnitude);
        audioManager?.SetRunningState(isMoving);
    }

    // 🎮 Dash + Slide actions
    void HandleActions()
    {
        if (input.dashPressed)
        {
            TriggerSpeedBoost(dashSpeedMultiplier);
            animatorController?.TriggerDash();
            audioManager?.PlayDash();
            FindFirstObjectByType<SmoothCameraFollow>()?.TriggerCinematicEffect();
            input.dashPressed = false;
        }

        if (input.slidePressed)
        {
            TriggerSpeedBoost(slideSpeedMultiplier);
            animatorController?.TriggerSlide();
            audioManager?.PlaySlide();
            FindFirstObjectByType<SmoothCameraFollow>()?.TriggerCinematicEffect();
            input.slidePressed = false;
        }
    }

    // ⚡ Speed Boost Logic
    void TriggerSpeedBoost(float multiplier)
    {
        targetSpeed = config.baseSpeed * multiplier;
        boostTimer = 0f;
        isSpeedBoosted = true;
    }

    void UpdateSpeedBoost()
    {
        if (isSpeedBoosted)
        {
            boostTimer += Time.deltaTime;
            if (boostTimer < boostDuration)
            {
                currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 10f);
            }
            else
            {
                currentSpeed = Mathf.Lerp(currentSpeed, config.baseSpeed, Time.deltaTime * 5f);
                if (Mathf.Abs(currentSpeed - config.baseSpeed) < 0.1f)
                {
                    currentSpeed = config.baseSpeed;
                    isSpeedBoosted = false;
                }
            }
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, config.baseSpeed, Time.deltaTime * 2f);
        }
    }
}
