using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Текущий Уровень")]
    public LevelData currentLevel; // Сюда перетащи файл уровня (Level_1)

    [Header("Game State")]
    public int movesLeft;
    public int score;
    public int targetScore;

    [Header("UI")]
    public Text movesText;
    public Text scoreText;
    public GameObject winScreen;
    public GameObject loseScreen;

    private bool isGameOver = false;
    private GridManager gridManager;

    void Awake()
    {
        Instance = this;
        gridManager = FindObjectOfType<GridManager>();
    }

    void Start()
    {
        if (currentLevel != null)
        {
            StartLevel();
        }
        else
        {
            Debug.LogError("ОШИБКА: Не назначен файл уровня (Current Level) в инспекторе GameManager!");
        }
    }

    void StartLevel()
    {
        // 1. Загружаем данные
        if (currentLevel != null)
        {
            movesLeft = currentLevel.movesCount;
            targetScore = currentLevel.targetScore;
        }
        
        score = 0;
        isGameOver = false;

        // БЕЗОПАСНОЕ СКРЫТИЕ ОКОН (проверяем, есть ли они вообще, чтобы не было ошибок)
        if (winScreen != null) winScreen.SetActive(false);
        if (loseScreen != null) loseScreen.SetActive(false);
        
        UpdateUI();

        // 2. Создаем сетку
        if (gridManager != null && currentLevel != null)
        {
            gridManager.InitializeLevel(currentLevel.width, currentLevel.height);
        }
        else
        {
            Debug.LogError("ОШИБКА: GridManager не найден или LevelData пустой!");
        }
    }

    // ЭТОТ МЕТОД ДОЛЖЕН БЫТЬ ТОЛЬКО ОДИН РАЗ
    void UpdateUI()
    {
        // БЕЗОПАСНОЕ ОБНОВЛЕНИЕ ТЕКСТА
        if (movesText != null) movesText.text = "Moves: " + movesLeft;
        if (scoreText != null) scoreText.text = "Score: " + score;
    }

    public void AddScore(int amount)
    {
        if (isGameOver) return;
        score += amount;
        UpdateUI();
        CheckWinCondition();
    }

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
        // Проверка на null, чтобы не вылетело, если экран не назначен
        if (winScreen != null) winScreen.SetActive(true);
    }

    void GameOver()
    {
        isGameOver = true;
        Debug.Log("GAME OVER");
        // Проверка на null
        if (loseScreen != null) loseScreen.SetActive(true);
    }
}