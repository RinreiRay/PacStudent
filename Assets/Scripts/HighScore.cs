using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HighScore : MonoBehaviour
{
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private TMP_Text levelTimeText;


    void Start()
    {
        LoadAndDisplayHighScore();
    }

    private void LoadAndDisplayHighScore()
    {
        int highScore = ScoreManager.GetHighScore();
        float highScoreTime = ScoreManager.GetHighScoreTime();

        if (highScoreText != null)
        {
            highScoreText.text = $"Best Score: {highScore}";
        }

        if (levelTimeText != null)
        {
            if (highScore > 0)
            {
                string formattedTime = ScoreManager.FormatTime(highScoreTime);
                levelTimeText.text = $"Level1 Time: {formattedTime}";
            }
            else
            {
                levelTimeText.text = "Level1 Time: 00:00:00";
            }
        }

        Debug.Log($"Loaded high score: {highScore} with time: {highScoreTime}");
    }

    public void RefreshHighScore()
    {
        LoadAndDisplayHighScore();
    }
}