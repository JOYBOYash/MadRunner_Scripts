using UnityEngine;

[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class SlimeAI_3D : MonoBehaviour
{
    [Header("Detection & Movement")]
    public float moveSpeed = 3f;
    public float detectionRadius = 8f;
    public float stoppingDistance = 1.5f;
    public float rotationSpeed = 5f;

    [Header("Attack Settings")]
    public int damage = 1;
    public float attackCooldown = 1.5f;

    [Header("Visual Feedback (optional)")]
    public float bounceAmplitude = 0.1f;
    public float bounceFrequency = 3f;

    private Transform player;
    private Rigidbody rb;
    private float lastAttackTime;
    private Vector3 baseScale;
    private bool isDead = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.isKinematic = false;

        Collider col = GetComponent<Collider>();
        col.isTrigger = true; // 👈 MAKE SLIME A TRIGGER-BASED DAMAGE AREA

        baseScale = transform.localScale;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (!player)
        {
            Debug.LogWarning("⚠️ SlimeAI: Player not found! Make sure your player has the 'Player' tag.");
        }
    }

    void Update()
    {
        if (isDead || !player) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Bounce animation for slime life
        float bounce = Mathf.Sin(Time.time * bounceFrequency) * bounceAmplitude;
        transform.localScale = baseScale + new Vector3(0, bounce, 0);

        if (distance <= detectionRadius)
        {
            MoveTowardPlayer(distance);
        }
    }

    void MoveTowardPlayer(float distance)
    {
        Vector3 direction = (player.position - transform.position).normalized;

        // Smoothly rotate toward player
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);

        // Move if not within attack range
        if (distance > stoppingDistance)
        {
            Vector3 newPos = transform.position + transform.forward * moveSpeed * Time.deltaTime;
            rb.MovePosition(newPos);
        }
    }

    // ✅ Trigger-based attack (more reliable for player detection)
    private void OnTriggerStay(Collider other)
    {
        if (isDead) return;
        if (Time.time - lastAttackTime < attackCooldown) return;

        if (other.CompareTag("Player"))
        {
            lastAttackTime = Time.time;

            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                Debug.Log($"🩸 Slime dealt {damage} damage to player!");
            }
        }
    }

    public void Die()
    {
        isDead = true;
        rb.isKinematic = true;
        GetComponent<Collider>().enabled = false;
        Destroy(gameObject, 0.3f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
