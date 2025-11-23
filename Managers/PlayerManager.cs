using UnityEngine;
using UnityEngine.UI;

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

    [Header("Player UI Controls")]
    public Joystick joystick;
    public Button dashButton;
    public Button slideButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Called by PlayerHealth.cs or PlayerManager when a new player spawns
    public void RegisterPlayer(PlayerHealth player)
    {
        Debug.Log("🧩 Player registered to GameManager.");

        // Assign runtime references (existing behavior)
        player.AssignManagerReferences(
            scoreData,
            gameOverUIManager,
            playerHealthUI,
            screenShake,
            screenFlash,
            audioSource,
            hitClip
        );

        // Also assign joystick & action buttons to PlayerInputHandler
        AssignInputToPlayer(player.GetComponent<PlayerInputHandler>());
    }

    private void AssignInputToPlayer(PlayerInputHandler inputHandler)
    {
        if (inputHandler == null)
        {
            Debug.LogWarning("⚠ PlayerInputHandler missing on player object!");
            return;
        }

        if (!inputHandler.IsOwner)
        {
            Debug.Log("🔒 Not assigning controls to remote player.");
            return;
        }

        // Assign joystick/buttons from UI
        inputHandler.joystick = joystick != null ? joystick : FindObjectOfType<Joystick>(true);
        inputHandler.dashButton = dashButton != null ? dashButton : GameObject.Find("DashButton")?.GetComponent<Button>();
        inputHandler.slideButton = slideButton != null ? slideButton : GameObject.Find("SlideButton")?.GetComponent<Button>();

        Debug.Log("🎮 Controls assigned successfully to local player!");
    }
}
