using UnityEngine;
using UnityEngine.UI; // Или TMPro если используешь TextMeshPro
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // Синглтон для удобного доступа

    [Header("Level Settings")]
    public int movesLeft = 20;
    public int score = 0;
    public int targetScore = 1000;

    [Header("UI")]
    // Сюда перетащишь текстовые поля из Canvas
    public Text movesText; 
    public Text scoreText;
    public GameObject winScreen;
    public GameObject loseScreen;

    private bool isGameOver = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        UpdateUI();
        winScreen.SetActive(false);
        loseScreen.SetActive(false);
    }

    // Вызывается из GridManager, когда фишки уничтожаются
    public void AddScore(int amount)
    {
        if (isGameOver) return;

        score += amount;
        UpdateUI();
        CheckWinCondition();
    }

    // Вызывается из GridManager после каждого SwapPlanets (если ход был результативным или просто потрачен)
    public void DecreaseMove()
    {
        if (isGameOver) return;

        movesLeft--;
        UpdateUI();

        if (movesLeft <= 0 && score < targetScore)
        {
            GameOver();
        }
    }

    void CheckWinCondition()
    {
        if (score >= targetScore)
        {
            WinGame();
        }
    }

    void WinGame()
    {
        isGameOver = true;
        Debug.Log("YOU WIN!");
        
        // Расчет звезд
        int stars = 1;
        if (movesLeft >= 5) stars = 3;
        else if (movesLeft >= 2) stars = 2;

        Debug.Log($"Stars received: {stars}");
        winScreen.SetActive(true);
        // Тут можно сохранить прогресс
    }

    void GameOver()
    {
        isGameOver = true;
        Debug.Log("GAME OVER");
        loseScreen.SetActive(true);
    }

    void UpdateUI()
    {
        if (movesText) movesText.text = "Moves: " + movesLeft;
        if (scoreText) scoreText.text = "Score: " + score;
    }
}