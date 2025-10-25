using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Game State")]
    public bool gameStarted = false;
    public bool gameOver = false;
    public float gameTime = 0f;

    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private Image blockingImage;
    [SerializeField] private TMP_Text gameTimerText;

    [SerializeField] private float initialWaitTime = 2f;
    [SerializeField] private float countdownDuration = 1f; 
    [SerializeField] private float goDisplayTime = 0.5f;

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

        // Initialize UI
        if (gameTimerText != null)
        {
            UpdateGameTimerUI();
        }

        // Start countdown sequence
        StartCoroutine(StartGameCountdown());
    }

    void Update()
    {
        if (timerRunning)
        {
            gameTime += Time.deltaTime;
            UpdateGameTimerUI();
        }
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

        // Hide UI elements
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }

        if (blockingImage != null)
        {
            blockingImage.gameObject.SetActive(false);
        }

        // Start the game
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
        gameOver = true;
        timerRunning = false;

        if (pacStudent != null)
        {
            pacStudent.SetMovementEnabled(false);
        }

        Debug.Log("Game Ended");
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