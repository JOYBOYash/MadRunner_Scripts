using UnityEngine;
using TMPro;

public class ScoreTracker : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public TMP_Text scoreText;
    public ScoreDataSO scoreData;

    [Header("Settings")]
    public bool useZAxisOnly = true;
    public float distanceMultiplier = 1f;
    public int milestoneStep = 1000;
    public float popScaleAmount = 1.3f;
    public float popSpeed = 8f;

    private float startZ;
    private float lastMilestone = 0f;
    private Vector3 originalScale;
    private bool isPopping = false;
    private float displayedScore = 0f;
    private bool isTracking = true;

    void Start()
    {
        if (!scoreText || !scoreData)
        {
            Debug.LogError("❌ ScoreTracker: Missing references!");
            enabled = false;
            return;
        }

        scoreData.ResetScore();
        scoreData.LoadHighScore(); // ✅ Ensure we have previous high score loaded
        startZ = player.position.z;
        originalScale = scoreText.transform.localScale;
    }

    void Update()
    {
        if (!isTracking) return;

        UpdateDistance();
        SmoothDisplayScore();
    }

    void UpdateDistance()
    {
        float distanceTravelled = useZAxisOnly
            ? (player.position.z - startZ)
            : Vector3.Distance(new Vector3(player.position.x, 0, player.position.z), new Vector3(0, 0, startZ));

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
        Debug.Log($"🎉 Milestone: {lastMilestone * milestoneStep} m");
    }

    // 🔥 Call this when player dies or game ends
    public void StopTracking()
    {
        if (!isTracking) return;

        isTracking = false;
        scoreData.FinalizeScore(); // ✅ Save and check high score only once
        Debug.Log($"💀 Final Score: {Mathf.FloorToInt(scoreData.currentScore)}");
    }
}
