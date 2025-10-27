using UnityEngine;

[CreateAssetMenu(fileName = "ScoreData", menuName = "Game/Score Data", order = 1)]
public class ScoreDataSO : ScriptableObject
{
    [Header("Score Data")]
    public float currentScore = 0f;
    public float highScore = 0f;

    [Header("Multiplier")]
    public float scoreMultiplier = 1f;

    [Header("Runtime Flags")]
    public bool isNewHighScore = false;

    private const string HighScoreKey = "HighScore";

    public void ResetScore()
    {
        currentScore = 0f;
        isNewHighScore = false;
    }

    public void AddScore(float value)
    {
        currentScore += value * scoreMultiplier;
    }

    public void CheckAndUpdateHighScore()
    {
        LoadHighScore();

        if (currentScore > highScore)
        {
            highScore = currentScore;
            isNewHighScore = true;
            SaveHighScore();
            Debug.Log($"🏆 New High Score: {highScore}");
        }
        else
        {
            isNewHighScore = false;
            Debug.Log($"💀 Run ended. Score: {currentScore}, High Score: {highScore}");
        }
    }

    public void SaveHighScore()
    {
        PlayerPrefs.SetFloat(HighScoreKey, highScore);
        PlayerPrefs.Save();
    }

    public void LoadHighScore()
    {
        highScore = PlayerPrefs.GetFloat(HighScoreKey, 0f);
    }
}
