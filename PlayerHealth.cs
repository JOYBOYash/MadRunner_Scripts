using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 3;
    private int currentHealth;

    [Header("References")]
    public ScoreDataSO scoreData;
    public GameOverUIManager gameOverUIManager;
    public PlayerController playerController;
    public Animator animator;
    public ScoreTracker scoreManager;
    public AudioSource audioSource;
    public AudioClip hitClip;
    public PlayerHealthUI playerHealthUI;   // 🩸 UI Reference
    public ScreenShake screenShake;         // 💥 Screen shake reference
    public ScreenFlash screenFlash;         // ⚡ Screen flash reference

    private bool isDead = false;
    private Rigidbody rb;

    // 🔥 Global flag accessible from anywhere
    public static bool IsPlayerDead { get; private set; } = false;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        IsPlayerDead = false;

        // Initialize UI
        if (playerHealthUI != null)
            playerHealthUI.ResetHealthUI();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        // 💢 Trigger effects
        if (screenShake != null) screenShake.Shake(0.3f, 0.3f);
        if (screenFlash != null) screenFlash.Flash(Color.red, 0.3f);

        // 💔 Update UI hearts
        playerHealthUI?.UpdateHealthUI(currentHealth, maxHealth);


        // 💢 Play stumble animation and sound
        if (currentHealth > 0)
        {
            animator.SetTrigger("Stumbles");
            audioSource?.PlayOneShot(hitClip);
            Debug.Log($"🩸 Player hit! Current HP: {currentHealth}");
        }


        if (rb != null)
        {
            Vector3 knockDir = (rb.transform.position - transform.position).normalized;
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

        // Stop score updates and save final score
        FindFirstObjectByType<ScoreTracker>()?.StopTracking();
        scoreData.FinalizeScore();

        // Stop player movement and physics
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // ⚰️ Animator death state
        animator.SetBool("isDead", true);
        animator.ResetTrigger("Stumbles");
        animator.ResetTrigger("Revives");
        animator.SetTrigger("Dies");

        // Disable controller and scoring
        if (playerController != null)
            playerController.enabled = false;

        if (scoreManager != null)
            scoreManager.enabled = false;

        // Show game over
        gameOverUIManager?.ShowGameOver();

        // Ensure time is normal
        if (Time.timeScale != 1)
        {
            Debug.LogWarning("⚠️ Resetting Time.timeScale to 1 because something paused the game.");
            Time.timeScale = 1f;
        }
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        IsPlayerDead = false;

        rb.isKinematic = false;

        animator.SetBool("isDead", false);
        animator.SetTrigger("Revives");

        playerController.enabled = true;
        scoreManager.enabled = true;

        // 💖 Reset health UI
        playerHealthUI?.ResetHealthUI();
    }
}
