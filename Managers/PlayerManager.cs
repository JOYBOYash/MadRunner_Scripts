using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene References")]
    public ScoreDataSO scoreData;
    public GameOverUIManager gameOverUIManager;
    public PlayerHealthUI playerHealthUI;
    public ScreenShake screenShake;
    public ScreenFlash screenFlash;
    public AudioClip hitClip;
    public AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional: persist between scenes
    }

    // Called automatically when a new player spawns
    public void RegisterPlayer(PlayerHealth player)
    {
        Debug.Log("🧩 Player registered to GameManager.");

        // Assign dynamic runtime references
        player.AssignManagerReferences(
            scoreData,
            gameOverUIManager,
            playerHealthUI,
            screenShake,
            screenFlash,
            audioSource,
            hitClip
        );
    }
}
