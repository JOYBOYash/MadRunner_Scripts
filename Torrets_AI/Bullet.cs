using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 20f;
    public float lifeTime = 5f;

    [Header("Forward Direction Control")]
    public bool useX = false;
    public bool useY = false;
    public bool useZ = true;

    public bool invertDirection = false;

    private Vector3 moveDirection;

    [Header("Impact Settings")]
    public GameObject impactVFX;
    public float impactVFXDuration = 1.5f;
    public LayerMask groundLayer;

    private void Start()
    {
        // Determine movement axis relative to projectile orientation
        if (useX)
            moveDirection = transform.right;
        else if (useY)
            moveDirection = transform.up;
        else
            moveDirection = transform.forward; // default

        if (invertDirection)
            moveDirection = -moveDirection;

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if the thing we hit belongs to the ground layer
        if ((groundLayer.value & (1 << collision.gameObject.layer)) != 0)
        {
            SpawnImpact(collision.contacts[0].point, collision.contacts[0].normal);
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Also support trigger ground surfaces if needed
        if ((groundLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            SpawnImpact(transform.position, Vector3.up);
            Destroy(gameObject);
        }
    }

    void SpawnImpact(Vector3 position, Vector3 normal)
    {
        if (impactVFX != null)
        {
            GameObject vfx = Instantiate(impactVFX, position, Quaternion.LookRotation(normal));
            Destroy(vfx, impactVFXDuration);
        }
    }
}
