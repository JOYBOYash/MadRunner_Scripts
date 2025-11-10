using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 20f;
    public float lifeTime = 5f;
    public int damageAmount = 1; // ❤️ Damage per hit (1 heart)

    [Header("Direction Control")]
    public bool useX = false;
    public bool useY = false;
    public bool useZ = true;
    public bool invertDirection = false;

    [Header("Impact Settings")]
    public GameObject impactVFX;          // Prefab for ground impact effect
    public AudioClip impactSFX;           // Sound for ground hit
    public float impactDestroyDelay = 2f; // How long before destroying VFX
    public LayerMask groundLayer;         // Which layer counts as ground

    [Header("Player Hit Settings")]
    public AudioClip playerHitSFX;        // Optional SFX when hitting player
    public GameObject playerHitVFX;       // Optional player hit effect prefab

    private Vector3 moveDirection;
    private AudioSource audioSource;

    private void Start()
    {
        // Determine movement direction
        if (useX) moveDirection = transform.right;
        else if (useY) moveDirection = transform.up;
        else moveDirection = transform.forward;

        if (invertDirection)
            moveDirection = -moveDirection;

        // Ensure we have an AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
        }

        // Auto-destroy after lifetime expires
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // --- 1️⃣ Player Hit ---
        if (collision.gameObject.CompareTag("Player"))
        {
            HandlePlayerHit(collision.gameObject, collision.contacts[0].point, collision.contacts[0].normal);
            return;
        }

        // --- 2️⃣ Ground Hit ---
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            HandleGroundImpact(collision.contacts[0].point, collision.contacts[0].normal);
            return;
        }

        // --- 3️⃣ Other collisions (optional) ---
        Destroy(gameObject);
    }

    private void HandlePlayerHit(GameObject player, Vector3 hitPoint, Vector3 normal)
    {
        // Try getting PlayerHealth
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damageAmount);
        }

        // Optional visual effect
        if (playerHitVFX != null)
        {
            GameObject vfx = Instantiate(playerHitVFX, hitPoint, Quaternion.LookRotation(normal));
            Destroy(vfx, impactDestroyDelay);
        }

        // Optional hit sound
        if (playerHitSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(playerHitSFX);
        }

        // Destroy projectile immediately after hitting the player
        Destroy(gameObject);
    }

    private void HandleGroundImpact(Vector3 point, Vector3 normal)
    {
        // Visual impact effect
        if (impactVFX != null)
        {
            GameObject impact = Instantiate(impactVFX, point, Quaternion.LookRotation(normal));
            Destroy(impact, impactDestroyDelay);
        }

        // Impact sound
        if (impactSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(impactSFX);
        }

        // Destroy projectile
        Destroy(gameObject);
    }
}
