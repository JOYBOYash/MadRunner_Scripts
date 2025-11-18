using UnityEngine;
using TMPro;

public class ScoreTracker : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Automatically found if not assigned.")]
    public Transform player;
    public TMP_Text scoreText;
    public ScoreDataSO scoreData;

    [Header("Settings")]
    public bool useZAxisOnly = true;
    public float distanceMultiplier = 1f;
    public int milestoneStep = 1000;
    public float popScaleAmount = 1.3f;
    public float popSpeed = 8f;
    public float playerSearchInterval = 1.0f; // How often to retry finding player

    private float startZ;
    private float lastMilestone = 0f;
    private Vector3 originalScale;
    private bool isPopping = false;
    private float displayedScore = 0f;
    private bool isTracking = true;

    private float searchTimer = 0f;

    void Start()
    {
        if (!scoreText || !scoreData)
        {
            Debug.LogError("❌ ScoreTracker: Missing essential references (ScoreText or ScoreDataSO)!");
            enabled = false;
            return;
        }

        scoreData.ResetScore();
        scoreData.LoadHighScore();

        originalScale = scoreText.transform.localScale;

        // Try finding the player immediately at startup
        FindPlayer();
    }

    void Update()
    {
        if (!isTracking) return;

        // 🔍 Auto-find player if missing (safe for respawns)
        if (player == null)
        {
            searchTimer += Time.deltaTime;
            if (searchTimer >= playerSearchInterval)
            {
                FindPlayer();
                searchTimer = 0f;
            }
            return;
        }

        UpdateDistance();
        SmoothDisplayScore();
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
            startZ = player.position.z;
            Debug.Log($"✅ ScoreTracker: Player found — tracking from Z = {startZ:F2}");
        }
        else
        {
            Debug.LogWarning("⚠️ ScoreTracker: Player not found yet. Will retry...");
        }
    }

    void UpdateDistance()
    {
        if (player == null) return;

        float distanceTravelled;

        if (useZAxisOnly)
            distanceTravelled = player.position.z - startZ;
        else
            distanceTravelled = Vector3.Distance(
                new Vector3(player.position.x, 0, player.position.z),
                new Vector3(0, 0, startZ)
            );

        distanceTravelled = Mathf.Max(0, distanceTravelled);

        scoreData.AddScore(Time.deltaTime * distanceTravelled * distanceMultiplier);

        float currentMilestone = Mathf.Floor(scoreData.currentScore / milestoneStep);
        if (currentMilestone > lastMilestone)
        {
            lastMilestone = currentMilestone;
            TriggerPopEffect();
        }
    }

    void SmoothDisplayScore()
    {
        displayedScore = Mathf.Lerp(displayedScore, scoreData.currentScore, Time.deltaTime * 5f);
        scoreText.text = $"{Mathf.FloorToInt(displayedScore)} m";

        if (isPopping)
        {
            scoreText.transform.localScale = Vector3.Lerp(
                scoreText.transform.localScale,
                originalScale * popScaleAmount,
                Time.deltaTime * popSpeed
            );

            if (Vector3.Distance(scoreText.transform.localScale, originalScale * popScaleAmount) < 0.05f)
                isPopping = false;
        }
        else
        {
            scoreText.transform.localScale = Vector3.Lerp(
                scoreText.transform.localScale,
                originalScale,
                Time.deltaTime * popSpeed
            );
        }
    }

    void TriggerPopEffect()
    {
        isPopping = true;
        Debug.Log($"🎉 Milestone reached: {lastMilestone * milestoneStep} m");
    }

    // 🧩 External control — called when game over or player dies
    public void StopTracking()
    {
        if (!isTracking) return;

        isTracking = false;
        scoreData.FinalizeScore();
        Debug.Log($"💀 Final Score: {Mathf.FloorToInt(scoreData.currentScore)}");
    }

    // 🧩 Called when new player spawns (from GameManager or PlayerHealth)
    public void RegisterPlayer(Transform newPlayer)
    {
        player = newPlayer;
        startZ = player.position.z;
        isTracking = true;
        Debug.Log($"🔄 ScoreTracker: New player registered at Z={startZ:F2}");
    }
}
