using UnityEngine;
using System.Collections;

public class MultiMuzzleShooterWithRange : MonoBehaviour
{
    public enum TurretState { Idle, Patrolling, Engaging }
    private TurretState state = TurretState.Idle;

    [Header("Projectile & Muzzle Setup")]
    public Transform[] muzzlePoints;
    public GameObject projectilePrefab;

    [Header("Effects")]
    public ParticleSystem muzzleVFX;
    public ParticleSystem anticipationVFX;
    public AudioSource audioSource;
    public AudioClip anticipationSFX;
    public AudioClip fireSFX;

    [Header("Rotation Axis Controls")]
    public bool rotateX = false;
    public bool rotateY = true;
    public bool rotateZ = false;

    [Header("Rotation Settings")]
    public float rotationSpeed = 5f;

    [Header("Ranges")]
    public float patrollableRange = 15f;
    public float attackRange = 12f;
    public float lineOfSightCheckRadius = 1f;
    public LayerMask lineOfSightMask;

    [Header("Combat Settings")]
    public float projectileSpeed = 20f;
    public float accuracySpread = 1f;
    public float fireCooldown = 1.2f;
    public float anticipationDelay = 0.35f;

    [Header("Patrol Behavior")]
    public float patrolRotateSpeed = 30f;
    public float patrolSweepAngle = 45f;

    [Header("View Cone Debug")]
    public float viewAngle = 60f;

    private Transform player;
    private bool isFiring = false;
    private float patrolStartYaw;
    private int currentMuzzleIndex = 0;

    void Start()
    {
        patrolStartYaw = transform.eulerAngles.y;
        FindPlayer();
    }

    void Update()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case TurretState.Idle:
                if (dist <= patrollableRange) state = TurretState.Patrolling;
                break;

            case TurretState.Patrolling:
                PatrolMotion();
                if (dist <= attackRange && HasLineOfSight())
                    state = TurretState.Engaging;
                if (dist > patrollableRange)
                    state = TurretState.Idle;
                break;

            case TurretState.Engaging:
                RotateTowardsPlayer();
                if (!isFiring) StartCoroutine(FireSequence());
                if (dist > patrollableRange || !HasLineOfSight())
                    state = TurretState.Patrolling;
                break;
        }
    }

    void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    bool HasLineOfSight()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        return !Physics.SphereCast(transform.position, lineOfSightCheckRadius, dir,
            out RaycastHit hit, attackRange, lineOfSightMask) || hit.transform.CompareTag("Player");
    }

    void RotateTowardsPlayer()
    {
        if (player == null) return;

        Vector3 direction = player.position - transform.position;

        if (!rotateX)
            direction.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        Vector3 e = targetRotation.eulerAngles;
        Vector3 current = transform.rotation.eulerAngles;

        if (!rotateX) e.x = current.x;
        if (!rotateY) e.y = current.y;
        if (!rotateZ) e.z = current.z;

        Quaternion finalRotation = Quaternion.Euler(e);
        transform.rotation = Quaternion.Lerp(transform.rotation, finalRotation, Time.deltaTime * rotationSpeed);
    }

    void PatrolMotion()
    {
        float angle = Mathf.Sin(Time.time * patrolRotateSpeed * 0.1f) * patrolSweepAngle;
        Vector3 baseRot = transform.rotation.eulerAngles;

        if (rotateX) baseRot.x = angle;
        if (rotateY) baseRot.y = patrolStartYaw + angle;
        if (rotateZ) baseRot.z = angle;

        transform.rotation = Quaternion.Euler(baseRot);
    }

    private IEnumerator FireSequence()
    {
        isFiring = true;

        if (audioSource && anticipationSFX)
            audioSource.PlayOneShot(anticipationSFX);

        if (anticipationVFX != null && muzzlePoints.Length > 0)
            Instantiate(anticipationVFX, muzzlePoints[currentMuzzleIndex].position, muzzlePoints[currentMuzzleIndex].rotation);

        yield return new WaitForSeconds(anticipationDelay);

        FireProjectiles();

        yield return new WaitForSeconds(fireCooldown);
        isFiring = false;
    }

    void FireProjectiles()
    {
        if (muzzlePoints.Length == 0) return;

        Transform muzzle = muzzlePoints[currentMuzzleIndex];
        GameObject proj = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);

        Vector3 dir = muzzle.forward + Random.insideUnitSphere * (accuracySpread * 0.01f);
        proj.transform.forward = dir;

        if (proj.TryGetComponent(out Projectile p)) p.speed = projectileSpeed;

        if (muzzleVFX != null)
            Instantiate(muzzleVFX, muzzle.position, muzzle.rotation);

        if (audioSource && fireSFX)
            audioSource.PlayOneShot(fireSFX);

        currentMuzzleIndex = (currentMuzzleIndex + 1) % muzzlePoints.Length;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, patrollableRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // View Cone
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        Vector3 forward = transform.forward;
        Vector3 leftLimit = Quaternion.Euler(0, -viewAngle / 2f, 0) * forward;
        Vector3 rightLimit = Quaternion.Euler(0, viewAngle / 2f, 0) * forward;
        Gizmos.DrawRay(transform.position, leftLimit * attackRange);
        Gizmos.DrawRay(transform.position, rightLimit * attackRange);

        // Muzzle direction debug
        if (muzzlePoints != null)
        {
            Gizmos.color = Color.green;
            foreach (var m in muzzlePoints)
            {
                if (m != null)
                    Gizmos.DrawRay(m.position, m.forward * 1f);
            }
        }

        // LOS line
        if (player != null)
        {
            Gizmos.color = HasLineOfSight() ? Color.green : Color.yellow;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}
