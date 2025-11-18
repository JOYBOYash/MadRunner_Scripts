using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 3;
    private int currentHealth;

    private bool isDead = false;
    private Rigidbody rb;

    // 🔥 Global access
    public static bool IsPlayerDead { get; private set; } = false;

    // 🧩 Dynamically linked references
    private ScoreDataSO scoreData;
    private GameOverUIManager gameOverUIManager;
    private PlayerController playerController;
    private Animator animator;
    private ScoreTracker scoreManager;
    private AudioSource audioSource;
    private AudioClip hitClip;
    private PlayerHealthUI playerHealthUI;
    private ScreenShake screenShake;
    private ScreenFlash screenFlash;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();
        animator = GetComponentInChildren<Animator>();
        scoreManager = FindFirstObjectByType<ScoreTracker>();

        IsPlayerDead = false;
    }

    void Start()
    {
        // ⚙️ When player spawns, auto-register with GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayer(this);
        }
        else
        {
            Debug.LogWarning("⚠️ No GameManager found in scene. PlayerHealth will operate standalone.");
        }

        currentHealth = maxHealth;

        // Initialize UI if available
        playerHealthUI?.ResetHealthUI();
    }

    // 🎯 This gets called by GameManager.RegisterPlayer()
    public void AssignManagerReferences(
        ScoreDataSO scoreData,
        GameOverUIManager uiManager,
        PlayerHealthUI healthUI,
        ScreenShake shake,
        ScreenFlash flash,
        AudioSource source,
        AudioClip clip)
    {
        this.scoreData = scoreData;
        this.gameOverUIManager = uiManager;
        this.playerHealthUI = healthUI;
        this.screenShake = shake;
        this.screenFlash = flash;
        this.audioSource = source;
        this.hitClip = clip;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        // 💢 Feedback
        screenShake?.Shake(0.3f, 0.3f);
        screenFlash?.Flash(Color.red, 0.3f);
        playerHealthUI?.UpdateHealthUI(currentHealth, maxHealth);

        if (currentHealth > 0)
        {
            animator?.SetTrigger("Stumbles");
            audioSource?.PlayOneShot(hitClip);
            Debug.Log($"🩸 Player hit! HP: {currentHealth}");
        }

        if (rb != null)
        {
            Vector3 knockDir = -transform.forward; // or custom knockback
            rb.AddForce(knockDir * 5f, ForceMode.Impulse);
        }

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        IsPlayerDead = true;
        Debug.Log("💀 Player has died!");

        scoreManager?.StopTracking();
        scoreData?.FinalizeScore();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        animator?.SetBool("isDead", true);
        animator?.ResetTrigger("Stumbles");
        animator?.ResetTrigger("Revives");
        animator?.SetTrigger("Dies");

        playerController.enabled = false;
        scoreManager.enabled = false;

        gameOverUIManager?.ShowGameOver();

        if (Time.timeScale != 1)
            Time.timeScale = 1f;
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        IsPlayerDead = false;

        rb.isKinematic = false;

        animator?.SetBool("isDead", false);
        animator?.SetTrigger("Revives");

        playerController.enabled = true;
        scoreManager.enabled = true;

        playerHealthUI?.ResetHealthUI();
    }
}
