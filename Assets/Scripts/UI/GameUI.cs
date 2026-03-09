using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour
{
    [Header("References")]
    public GameObject pausePanel;

    public Button pauseButton;
    public Button resumeButton;
    public Button restartButton;
    public Button mainMenuButton;

    public TextMeshProUGUI scoreText;

    [Header("Player")]
    public Transform player;

    private bool isPaused = false;
    private float highestY;
    private int scorePenalty;

    void Start()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f;

        // Подключаем кнопки
        pauseButton.onClick.AddListener(TogglePause);
        resumeButton.onClick.AddListener(ResumeGame);
        restartButton.onClick.AddListener(RestartGame);
        mainMenuButton.onClick.AddListener(GoToMainMenu);

        highestY = player.position.y;
        scorePenalty = 0;
    }

    void Update()
    {
        UpdateScore();
    }

    void UpdateScore()
    {
        if (player.position.y > highestY)
            highestY = player.position.y;

        int baseScore = Mathf.FloorToInt(highestY * 10f);
        int finalScore = Mathf.Max(0, baseScore - scorePenalty);

        scoreText.text = "" + finalScore;
    }

    public int GetCurrentScore()
    {
        int baseScore = Mathf.FloorToInt(highestY * 10f);
        return Mathf.Max(0, baseScore - scorePenalty);
    }

    void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    void PauseGame(bool showResumeButton = true)
    {
        isPaused = true;
        if (resumeButton != null)
            resumeButton.gameObject.SetActive(showResumeButton);
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    /// <param name="showResumeButton">false = игра окончена (смерть), кнопка Continue скрыта; true = обычная пауза</param>
    public void ShowPauseMenu(bool showResumeButton = true)
    {
        PauseGame(showResumeButton);
    }

    void ResumeGame()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void AddScorePenalty(int amount)
    {
        if (amount <= 0) return;

        scorePenalty += amount;
    }
}