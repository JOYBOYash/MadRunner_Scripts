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

    private NetworkVariable<Vector3> networkPosition =
        new(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<Quaternion> networkRotation =
        new(writePerm: NetworkVariableWritePermission.Owner);

    private float lerpSpeed = 10f;

    public Transform CameraTransform
    {
        get => cameraTransform;
        set => cameraTransform = value;
    }

    public override void OnNetworkSpawn()
    {
        controller = GetComponent<CharacterController>();
        if (audioManager == null) audioManager = GetComponent<PlayerAudioManager>();

        currentSpeed = config != null ? config.baseSpeed : 5f;
        targetSpeed = currentSpeed;

        if (!IsOwner && input != null) input.enabled = false;

        name = IsOwner ? "LocalPlayer" : $"RemotePlayer_{OwnerClientId}";
    }

    void Update()
    {
        if (!IsSpawned) return;

        if (IsOwner)
        {
            if (PlayerHealth.IsPlayerDead)
            {
                audioManager?.SetRunningState(false);
                return;
            }

            HandleMovementAndRotation();
            HandleActions();
            UpdateSpeedBoost();

            networkPosition.Value = transform.position;
            networkRotation.Value = transform.rotation;
        }
        else
        {
            InterpolateRemotePlayer();
        }
    }

    private void InterpolateRemotePlayer()
    {
        transform.position = Vector3.Lerp(transform.position, networkPosition.Value, Time.deltaTime * lerpSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation.Value, Time.deltaTime * lerpSpeed);
    }

    void HandleMovementAndRotation()
    {
        if (input == null || config == null) return;

        Vector2 joystickInput = input.moveInput;

        // Build 360° steering vector
        Vector3 steeringDir;

        if (useCameraRelativeMovement && cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            steeringDir = (camForward * joystickInput.y + camRight * joystickInput.x).normalized;
        }
        else
        {
            steeringDir = new Vector3(joystickInput.x, 0f, joystickInput.y).normalized;
        }

        // Only rotate if there is some joystick direction
        if (steeringDir.sqrMagnitude > 0.001f)
        {
            Quaternion desiredRot = Quaternion.LookRotation(steeringDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desiredRot,
                config.rotationSmoothness * Time.deltaTime
            );
        }

        // Always move forward relative to current facing direction
        Vector3 move = transform.forward * currentSpeed * Time.deltaTime;

        if (!controller.isGrounded)
            move.y -= config.gravity * Time.deltaTime;

        controller.Move(move);

        audioManager?.SetRunningState(true);
    }

    void HandleActions()
    {
        if (input == null || config == null) return;

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
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 10f);

            if (boostTimer >= boostDuration)
            {
                currentSpeed = config.baseSpeed;
                isSpeedBoosted = false;
            }
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, config.baseSpeed, Time.deltaTime * 2f);
        }
    }
}
