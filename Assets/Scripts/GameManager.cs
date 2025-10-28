using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Game State")]
    public bool gameStarted = false;
    public bool gameOver = false;
    public float gameTime = 0f;

    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private Image blockingImage;
    [SerializeField] private TMP_Text gameTimerText;
    [SerializeField] private TMP_Text gameOverText;

    [SerializeField] private float initialWaitTime = 2f;
    [SerializeField] private float countdownDuration = 1f;
    [SerializeField] private float goDisplayTime = 0.5f;
    [SerializeField] private float gameOverDisplayTime = 3f;

    private PacStudentController pacStudent;
    private bool timerRunning = false;

    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        pacStudent = FindFirstObjectByType<PacStudentController>();

        if (gameTimerText != null)
        {
            UpdateGameTimerUI();
        }

        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(false);
        }

        StartCoroutine(StartGameCountdown());
    }

    void Update()
    {
        if (timerRunning)
        {
            gameTime += Time.deltaTime;
            UpdateGameTimerUI();
        }

        if (gameStarted && !gameOver)
        {
            CheckGameOverConditions();
        }
    }

    private void CheckGameOverConditions()
    {

    }

    public void TriggerGameOver()
    {
        if (gameOver) return;

        gameOver = true;
        timerRunning = false;

        // Disable movement
        if (pacStudent != null)
        {
            pacStudent.SetMovementEnabled(false);
        }

        // Save high score
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SaveHighScore(gameTime);
        }

        StartCoroutine(ShowGameOverSequence());
    }

    private IEnumerator ShowGameOverSequence()
    {
        if (blockingImage != null)
        {
            blockingImage.gameObject.SetActive(true);
        }

        if (gameOverText != null)
        {
            gameOverText.text = "Game Over";
            gameOverText.gameObject.SetActive(true);
        }

        Debug.Log("Game Over displayed");

        yield return new WaitForSeconds(gameOverDisplayTime);
        SceneManager.LoadScene(0);
    }

    private IEnumerator StartGameCountdown()
    {
        // Show blocking image
        if (blockingImage != null)
        {
            blockingImage.gameObject.SetActive(true);
        }

        // Disable player movement
        if (pacStudent != null)
        {
            pacStudent.SetMovementEnabled(false);
        }

        yield return new WaitForSeconds(initialWaitTime);

        string[] countdownTexts = { "3", "2", "1", "GO!" };

        for (int i = 0; i < countdownTexts.Length; i++)
        {
            string text = countdownTexts[i];

            if (countdownText != null)
            {
                countdownText.text = text;
                countdownText.gameObject.SetActive(true);
                Debug.Log($"Countdown: {text}");
            }

            if (text == "GO!")
            {
                yield return new WaitForSeconds(goDisplayTime);
            }
            else
            {
                yield return new WaitForSeconds(countdownDuration);
            }
        }

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }

        if (blockingImage != null)
        {
            blockingImage.gameObject.SetActive(false);
        }

        StartGame();
    }

    private void StartGame()
    {
        gameStarted = true;
        timerRunning = true;

        // Enable player movement
        if (pacStudent != null)
        {
            pacStudent.SetMovementEnabled(true);
        }

        Debug.Log("Game Started!");
    }

    private void UpdateGameTimerUI()
    {
        if (gameTimerText != null)
        {
            int hours = Mathf.FloorToInt(gameTime / 3600f);
            int minutes = Mathf.FloorToInt((gameTime % 3600f) / 60f);
            int seconds = Mathf.FloorToInt(gameTime % 60f);

            gameTimerText.text = string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
        }
    }

    public void PauseTimer()
    {
        timerRunning = false;
    }

    public void ResumeTimer()
    {
        if (gameStarted && !gameOver)
        {
            timerRunning = true;
        }
    }

    public void EndGame()
    {
        TriggerGameOver();
    }

    public float TotalCountdownTime
    {
        get
        {
            return initialWaitTime + (countdownDuration * 3) + goDisplayTime;
        }
    }

    public bool IsCountdownActive
    {
        get
        {
            return !gameStarted && !gameOver;
        }
    }
}