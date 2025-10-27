using UnityEngine;

[CreateAssetMenu(fileName = "PlayerMovementConfig", menuName = "Player/Movement Config")]
public class PlayerMovementConfig : ScriptableObject
{
    [Header("Speed Settings")]
    [Tooltip("Base speed at which player starts moving.")]
    public float baseSpeed = 6f;

    [Tooltip("How much speed increases over time.")]
    public float speedMultiplier = 0.05f;

    [Tooltip("Maximum allowed speed.")]
    public float maxSpeed = 25f;

    [Header("Rotation Settings")]
    [Tooltip("How quickly the player rotates to face direction (smoothness).")]
    public float rotationSmoothness = 8f;

    [Tooltip("Maximum rotation angle per second.")]
    public float maxRotationSpeed = 120f;

    [Header("Gravity Settings")]
    [Tooltip("Gravity applied to player while not grounded.")]
    public float gravity = 9.8f;
}
