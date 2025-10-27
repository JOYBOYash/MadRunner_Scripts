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

        // Wobble bounce effect for slime liveliness
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

        // Smooth rotation toward player
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);

        // Move only if outside attack range
        if (distance > stoppingDistance)
        {
            Vector3 newPos = transform.position + transform.forward * moveSpeed * Time.deltaTime;
            rb.MovePosition(newPos);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        if (isDead) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            lastAttackTime = Time.time;

            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                // Optional: small knockback
                Vector3 knockbackDir = (collision.transform.position - transform.position).normalized * -1f;
                rb.AddForce(knockbackDir * 2f, ForceMode.Impulse);
            }
        }
    }

    public void Die()
    {
        isDead = true;
        rb.isKinematic = true;
        Destroy(gameObject, 0.3f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
