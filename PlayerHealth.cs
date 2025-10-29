using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 3;
    private int currentHealth;

    [Header("References")]
    public ScoreDataSO scoreData;
    public GameOverUIManager gameOverUIManager;  // Link this in Inspector

    public PlayerController playerController; // Reference to the player movement script

    public Animator animator; // Reference to the Animator component

    public ScoreTracker scoreManager;

    private bool isDead = false;
    private Rigidbody rb;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>(); // Cache Rigidbody once for performance
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"🩸 Player hit! Current HP: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("💀 Player has died!");

        // Update high score only on death
        if (scoreData.currentScore > scoreData.highScore)
        {
            scoreData.highScore = scoreData.currentScore;
            scoreData.isNewHighScore = true;
            scoreData.SaveHighScore();
        }
        else
        {
            scoreData.isNewHighScore = false;
        }

    

        // Show Game Over UI
        if (gameOverUIManager != null)
            gameOverUIManager.ShowGameOver();

        // Stop player motion and disable movement scripts
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;  // Optional: Prevents further physics interactions
        }


        animator.SetTrigger("Dies");


        if (playerController != null)
        {
            playerController.enabled = false;
        }

         if (scoreManager != null)
        {
            scoreManager.enabled = false;
        }


        // Optional: Disable any other movement-related scripts (e.g., if you have multiple)
        // Example: GetComponent<AnotherMovementScript>()?.enabled = false;
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;

        // Re-enable movement on reset
        if (rb != null)
        {
            rb.isKinematic = false;  // Restore physics if set to kinematic
        }

         animator.SetTrigger("Revives");


        if (playerController != null)
        {
            playerController.enabled = true;
        }
        

           if (scoreManager != null)
        {
            scoreManager.enabled = true;
        }


    }
}
