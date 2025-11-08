using UnityEngine;

public class FacePlayer_YOnly : MonoBehaviour
{
    [Header("Rotation Settings")]
    public Transform player;            // Assign in Inspector or find by tag
    public float rotationSpeed = 5f;    // Higher = quicker turning

    [Header("Allowed Rotation Axes")]
    public bool rotateX = false;
    public bool rotateY = true;   // Default: Only rotate Y
    public bool rotateZ = false;

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    // void Update()
    // {
    //     if (player == null) return;

    //     // Get direction to player but ignore vertical differences
    //     Vector3 direction = player.position - transform.position;
    //     direction.y = 0f; // Prevent tilting up/down

    //     if (direction.sqrMagnitude < 0.001f) return;

    //     Quaternion targetRotation = Quaternion.LookRotation(direction);

    //     // Keep locked axes
    //     Vector3 euler = targetRotation.eulerAngles;
    //     Quaternion current = transform.rotation;
    //     Vector3 currentEuler = current.eulerAngles;

    //     if (!rotateX) euler.x = currentEuler.x;
    //     if (!rotateY) euler.y = currentEuler.y;
    //     if (!rotateZ) euler.z = currentEuler.z;

    //     Quaternion finalRotation = Quaternion.Euler(euler);

    //     // Smooth rotation
    //     transform.rotation = Quaternion.Slerp(current, finalRotation, rotationSpeed * Time.deltaTime);
    // }
}
