using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class MultiMuzzleTurretAdvanced : MonoBehaviour
{
    public enum TurretState { Idle, Patrolling, Engaging }
    private TurretState state = TurretState.Idle;

    [Header("Player / Ranges")]
    public string playerTag = "Player";
    public float patrollableRange = 18f;
    public float engageRange = 12f;
    public float rotationSpeedDegPerSec = 180f;

    [Header("Muzzles (ordered)")]
    public Transform[] muzzlePoints;

    [Header("VFX (match muzzle order)")]
    public GameObject[] anticipationVFXs;
    public GameObject[] muzzleFlashVFXs;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip anticipationSFX;
    public AudioClip fireSFX;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 25f;

    public enum ProjectileForward { Forward, Up, Right, Custom }
    public ProjectileForward projectileForward = ProjectileForward.Forward;
    public Vector3 projectileCustomForward = Vector3.forward;

    [Header("Timings")]
    public float anticipationDuration = 0.3f;
    public float perMuzzleCooldown = 0.2f;
    public float cycleDelayAfterAllMuzzles = 0.3f;
    public float muzzleFlashDuration = 0.08f; // <<< KEY FIX HERE

    [Header("Patrol")]
    public bool patrolEnabled = true;
    public float patrolSweepAngle = 35f;
    public float patrolSpeed = 1f;
    public bool lockRotationX, lockRotationY, lockRotationZ;

    private Transform player;
    private Coroutine firingRoutine = null;
    private float baseYaw;
    private bool isActive = true;

    void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        baseYaw = transform.eulerAngles.y;

        EnableAllAnticipation();
        DisableAllMuzzleFlash(); // <<< IMPORTANT
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag(playerTag)?.transform;
    }

    void Update()
    {
        if (!isActive) return;

        if (player == null)
            player = GameObject.FindGameObjectWithTag(playerTag)?.transform;

        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist > patrollableRange)
            SetState(TurretState.Idle);
        else if (dist > engageRange)
            SetState(TurretState.Patrolling);
        else
            SetState(TurretState.Engaging);

        if (state == TurretState.Patrolling)
            DoPatrol();
        else if (state == TurretState.Engaging)
            DoRotateToPlayer();
    }

    void SetState(TurretState newState)
    {
        if (newState != TurretState.Engaging && firingRoutine != null)
        {
            StopCoroutine(firingRoutine);
            firingRoutine = null;
            EnableAllAnticipation();
        }

        if (newState == TurretState.Engaging && firingRoutine == null)
            firingRoutine = StartCoroutine(FiringCycle());

        state = newState;
    }

    void DoPatrol()
    {
        if (!patrolEnabled) return;

        float angle = Mathf.Sin(Time.time * patrolSpeed) * patrolSweepAngle;
        Vector3 e = transform.eulerAngles;

        if (!lockRotationX) e.x = angle;
        if (!lockRotationY) e.y = baseYaw + angle;
        if (!lockRotationZ) e.z = angle;

        transform.rotation = Quaternion.Euler(e);
    }

    void DoRotateToPlayer()
    {
        if (player == null) return;

        Vector3 dir = (player.position - transform.position);
        Quaternion target = Quaternion.LookRotation(dir.normalized);
        Vector3 tE = target.eulerAngles, cE = transform.eulerAngles;

        if (lockRotationX) tE.x = cE.x;
        if (lockRotationY) tE.y = cE.y;
        if (lockRotationZ) tE.z = cE.z;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.Euler(tE),
            rotationSpeedDegPerSec * Time.deltaTime
        );
    }

    IEnumerator FiringCycle()
    {
        while (state == TurretState.Engaging)
        {
            EnableAllAnticipation();
            if (audioSource && anticipationSFX)
                audioSource.PlayOneShot(anticipationSFX);

            yield return new WaitForSeconds(anticipationDuration);

            for (int i = 0; i < muzzlePoints.Length && state == TurretState.Engaging; i++)
            {
                DisableAnticipation(i);
                FireMuzzleOnce(i);
                yield return new WaitForSeconds(perMuzzleCooldown);
                EnableAnticipation(i);
            }

            yield return new WaitForSeconds(cycleDelayAfterAllMuzzles);
        }

        EnableAllAnticipation();
        firingRoutine = null;
    }
void FireMuzzleOnce(int index)
{
    Transform muzzle = muzzlePoints[index];

    EnableMuzzleFlash(index);
    StartCoroutine(DisableFlashAfterDelay(index));

    if (audioSource && fireSFX)
        audioSource.PlayOneShot(fireSFX);

    if (player == null) return;

    // 1. Get direction to player in world space
    Vector3 toPlayerWorld = (player.position - muzzle.position).normalized;

    // 2. Convert that direction to muzzle local space
    Vector3 toPlayerLocal = muzzle.InverseTransformDirection(toPlayerWorld);

    // 3. Decide which local axis to use to shoot along
    Vector3 chosenLocalAxis = projectileForward switch
    {
        ProjectileForward.Forward => Vector3.forward,
        ProjectileForward.Up => Vector3.up,
        ProjectileForward.Right => Vector3.right,
        ProjectileForward.Custom => projectileCustomForward.normalized,
        _ => Vector3.forward
    };

    // 4. Align chosen axis to point at player direction in local space
    Quaternion localRot = Quaternion.FromToRotation(chosenLocalAxis, toPlayerLocal);

    // 5. Convert back to world rotation
    Quaternion finalRot = muzzle.rotation * localRot;

    // 6. Spawn projectile
    GameObject proj = Instantiate(projectilePrefab, muzzle.position, finalRot);

    // 7. Set projectile speed
    if (proj.TryGetComponent(out Projectile p))
        p.speed = projectileSpeed;
}
    IEnumerator DisableFlashAfterDelay(int index)
    {
        yield return new WaitForSeconds(muzzleFlashDuration);
        DisableMuzzleFlash(index);
    }

    void EnableAllAnticipation()
    {
        foreach (var fx in anticipationVFXs)
            if (fx) fx.SetActive(true);
    }

    void EnableAnticipation(int i)
    {
        if (i < anticipationVFXs.Length && anticipationVFXs[i])
            anticipationVFXs[i].SetActive(true);
    }

    void DisableAnticipation(int i)
    {
        if (i < anticipationVFXs.Length && anticipationVFXs[i])
            anticipationVFXs[i].SetActive(false);
    }

    void DisableAllMuzzleFlash()
    {
        foreach (var fx in muzzleFlashVFXs)
            if (fx) fx.SetActive(false);
    }

    void EnableMuzzleFlash(int i)
    {
        if (i < muzzleFlashVFXs.Length && muzzleFlashVFXs[i])
            muzzleFlashVFXs[i].SetActive(true);
    }

    void DisableMuzzleFlash(int i)
    {
        if (i < muzzleFlashVFXs.Length && muzzleFlashVFXs[i])
            muzzleFlashVFXs[i].SetActive(false);
    }

    public void SetActive(bool on)
    {
        isActive = on;
        if (!on)
        {
            if (firingRoutine != null) StopCoroutine(firingRoutine);
            firingRoutine = null;
            EnableAllAnticipation();
        }
    }
}
