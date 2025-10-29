using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public int currentScore = 0;
    public int pelletScore = 10;
    public int powerPelletScore = 50;
    public int cherryScore = 100;
    public int ghostScore = 300;
    public int totalPellets = 0;
    public int remainingPellets = 0;

    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text scaredText;

    private const string HIGH_SCORE_KEY = "HighScore";
    private const string HIGH_SCORE_TIME_KEY = "HighScoreTime";

    public static ScoreManager Instance { get; private set; }

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
        // Initialize score display
        UpdateScoreUI();
        UpdateScaredUI();
    }

    public void InitializePelletCount(int total)
    {
        totalPellets = total;
        remainingPellets = total;
        Debug.Log($"Total pellets in level: {totalPellets}");
    }

    public void CollectPellet()
    {
        if (remainingPellets > 0)
        {
            remainingPellets--;
            Debug.Log($"Pellets remaining: {remainingPellets}");

            // Check for game over condition
            if (remainingPellets <= 0)
            {
                Debug.Log("All pellets collected! Triggering game over...");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerGameOver();
                }
            }
        }
    }

    public void AddPelletScore()
    {
        AddScore(pelletScore);
        CollectPellet(); // Track pellet collection
    }

    public void AddPowerPelletScore()
    {
        AddScore(powerPelletScore);
        CollectPellet(); // Track pellet collection
    }

    public void AddScore(int points)
    {
        currentScore += points;
        UpdateScoreUI();
        Debug.Log($"Score added: {points}. Total score: {currentScore}");
    }

    public void AddCherryScore()
    {
        AddScore(cherryScore);
    }

    public void AddGhostScore()
    {
        AddScore(ghostScore);
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString("D6"); // 6-digit format with leading zeros
        }
    }

    private void UpdateScaredUI()
    {
        if (scaredText != null)
        {
            scaredText.text = "0";
        }
    }

    public void SaveHighScore(float gameTime)
    {
        int savedHighScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        float savedHighScoreTime = PlayerPrefs.GetFloat(HIGH_SCORE_TIME_KEY, float.MaxValue);

        bool shouldSave = false;

        // Save if current score is higher
        if (currentScore > savedHighScore)
        {
            shouldSave = true;
            Debug.Log($"New high score! {currentScore} > {savedHighScore}");
        }
        // Save if score is same but time is better (lower)
        else if (currentScore == savedHighScore && gameTime < savedHighScoreTime)
        {
            shouldSave = true;
            Debug.Log($"Same score but better time! {gameTime} < {savedHighScoreTime}");
        }

        if (shouldSave)
        {
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, currentScore);
            PlayerPrefs.SetFloat(HIGH_SCORE_TIME_KEY, gameTime);
            PlayerPrefs.Save();
            Debug.Log($"High score saved: {currentScore} with time: {gameTime}");
        }
    }

    public static int GetHighScore()
    {
        return PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
    }

    public static float GetHighScoreTime()
    {
        return PlayerPrefs.GetFloat(HIGH_SCORE_TIME_KEY, 0f);
    }

    public static string FormatTime(float timeInSeconds)
    {
        int hours = Mathf.FloorToInt(timeInSeconds / 3600f);
        int minutes = Mathf.FloorToInt((timeInSeconds % 3600f) / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);

        return string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
    }
}