using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameOverUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject gameOverPanel;
    public TMP_Text scoreText;
    public TMP_Text highScoreText;
    public TMP_Text newHighScoreText;
    public Button restartButton;
    public Button mainMenuButton;
    public Button quitButton;

    [Header("References")]
    public ScoreDataSO scoreData;
    public SceneManagerController sceneManager;

    void Start()
    {
        gameOverPanel.SetActive(false);

        if (restartButton != null)
            restartButton.onClick.AddListener(sceneManager.RestartGame);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(sceneManager.LoadMainMenu);

        if (quitButton != null)
            quitButton.onClick.AddListener(sceneManager.QuitGame);
    }

    public void ShowGameOver()
    {
        gameOverPanel.SetActive(true);

        scoreData.LoadHighScore();

        scoreText.text = $"Score: {Mathf.FloorToInt(scoreData.currentScore)}";
        highScoreText.text = $"High Score: {Mathf.FloorToInt(scoreData.highScore)}";

        if (scoreData.isNewHighScore)
        {
            newHighScoreText.gameObject.SetActive(true);
            StartCoroutine(PopTextEffect(newHighScoreText.transform));
        }
        else
        {
            newHighScoreText.gameObject.SetActive(false);
        }
    }

    private System.Collections.IEnumerator PopTextEffect(Transform t)
    {
        Vector3 originalScale = t.localScale;
        Vector3 targetScale = originalScale * 1.5f;
        float speed = 6f;

        // Pop out
        float timer = 0f;
        while (timer < 0.3f)
        {
            t.localScale = Vector3.Lerp(originalScale, targetScale, timer / 0.3f);
            timer += Time.deltaTime * speed;
            yield return null;
        }

        // Pop back
        timer = 0f;
        while (timer < 0.3f)
        {
            t.localScale = Vector3.Lerp(targetScale, originalScale, timer / 0.3f);
            timer += Time.deltaTime * speed;
            yield return null;
        }

        t.localScale = originalScale;
    }
}
