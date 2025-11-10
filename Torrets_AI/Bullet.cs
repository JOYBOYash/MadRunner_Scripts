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

    private void Start()
    {
        // Determine movement axis relative to the projectile's local transform
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
}
