using UnityEngine;
using TMPro;

public class ScoreTracker : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public TMP_Text distanceText;
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

    void Start()
    {
        if (!player || !distanceText || !scoreData)
        {
            Debug.LogError("PlayerDistanceTracker: Missing references!");
            enabled = false;
            return;
        }

        scoreData.ResetScore();
        startZ = player.position.z;
        originalScale = distanceText.transform.localScale;
    }

    void Update()
    {
        UpdateDistance();
        SmoothDisplayScore();
    }

    void UpdateDistance()
    {
        float distanceTravelled;

        if (useZAxisOnly)
            distanceTravelled = (player.position.z - startZ) * distanceMultiplier;
        else
            distanceTravelled = Vector3.Distance(new Vector3(player.position.x, 0, player.position.z),
                                                 new Vector3(0, 0, startZ)) * distanceMultiplier;

        distanceTravelled = Mathf.Max(0, distanceTravelled);

        scoreData.AddScore(Time.deltaTime * distanceTravelled * 0.1f);

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
        distanceText.text = $"{Mathf.FloorToInt(displayedScore)} m";

        if (isPopping)
        {
            distanceText.transform.localScale = Vector3.Lerp(
                distanceText.transform.localScale,
                originalScale * popScaleAmount,
                Time.deltaTime * popSpeed
            );

            if (Vector3.Distance(distanceText.transform.localScale, originalScale * popScaleAmount) < 0.05f)
                isPopping = false;
        }
        else
        {
            distanceText.transform.localScale = Vector3.Lerp(
                distanceText.transform.localScale,
                originalScale,
                Time.deltaTime * popSpeed
            );
        }
    }

    void TriggerPopEffect()
    {
        isPopping = true;
        Debug.Log($"🎉 Milestone reached: {lastMilestone * milestoneStep} m!");
    }



}
