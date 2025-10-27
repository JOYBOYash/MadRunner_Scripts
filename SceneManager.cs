using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerController : MonoBehaviour
{
    public ScoreDataSO scoreData;

    public void RestartGame()
    {
        if (scoreData != null)
            scoreData.ResetScore();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        if (scoreData != null)
            scoreData.SaveHighScore();

        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        if (scoreData != null)
            scoreData.SaveHighScore();

        Debug.Log("🚪 Quitting Game...");
        Application.Quit();
    }
}
