using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    public string scoreText;
    public int scoreValue;
    public TextMeshProUGUI score;
    public Slider scoreSlider;

    private void Start()
    {
        
    }
    private void Update()
    {
        scoreText = scoreValue.ToString();
        score.text = scoreText;
        scoreSlider.value = scoreValue;
    }

    public void ScoreCounter(int score)
    {
        scoreValue += score;
    }
}
