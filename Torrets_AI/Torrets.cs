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

    [Header("Model Facing Offset")]
    [Tooltip("Use this to correct model's facing direction if it's visually off (e.g., 90° yaw).")]
    public float modelFacingOffsetY = 0f; // adjust per prefab (try 90 or -90)

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
    public float muzzleFlashDuration = 0.08f;

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
        DisableAllMuzzleFlash();
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag(playerTag)?.transform;
    }

    void Update()
    {
        // 🧠 If player is dead → turret immediately stops everything.
        if (PlayerHealth.IsPlayerDead)
        {
            if (isActive)
                ForceShutdown();
            return;
        }

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

        // apply facing offset fix
        target *= Quaternion.Euler(0f, modelFacingOffsetY, 0f);

        Vector3 tE = target.eulerAngles;
        Vector3 cE = transform.eulerAngles;

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
            if (PlayerHealth.IsPlayerDead)
            {
                ForceShutdown();
                yield break;
            }

            EnableAllAnticipation();
            if (audioSource && anticipationSFX)
                audioSource.PlayOneShot(anticipationSFX);

            yield return new WaitForSeconds(anticipationDuration);

            for (int i = 0; i < muzzlePoints.Length && state == TurretState.Engaging; i++)
            {
                if (PlayerHealth.IsPlayerDead)
                {
                    ForceShutdown();
                    yield break;
                }

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

        Vector3 toPlayerWorld = (player.position - muzzle.position).normalized;
        Vector3 toPlayerLocal = muzzle.InverseTransformDirection(toPlayerWorld);

        Vector3 chosenLocalAxis = projectileForward switch
        {
            ProjectileForward.Forward => Vector3.forward,
            ProjectileForward.Up => Vector3.up,
            ProjectileForward.Right => Vector3.right,
            ProjectileForward.Custom => projectileCustomForward.normalized,
            _ => Vector3.forward
        };

        Quaternion localRot = Quaternion.FromToRotation(chosenLocalAxis, toPlayerLocal);
        Quaternion finalRot = muzzle.rotation * localRot;

        GameObject proj = Instantiate(projectilePrefab, muzzle.position, finalRot);

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

    // 🚨 NEW: Immediately stop everything when player dies
    void ForceShutdown()
    {
        isActive = false;

        if (firingRoutine != null)
        {
            StopCoroutine(firingRoutine);
            firingRoutine = null;
        }

        DisableAllMuzzleFlash();
        EnableAllAnticipation();
        state = TurretState.Idle;

        // Optionally: stop audio abruptly to avoid lingering SFX
        if (audioSource && audioSource.isPlaying)
            audioSource.Stop();

        // Optional: disable all visual effects
        foreach (var fx in anticipationVFXs)
            if (fx) fx.SetActive(false);
    }
}
