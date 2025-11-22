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

            // НОВОЕ: Проверяем начальное поле, чтобы оно не было тупиковым
            CheckForStuckState();
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
        score += amount;
        UpdateUI();
        // Проверяем победу сразу после начисления очков
        CheckWinCondition();
    }


    // --- НОВЫЕ МЕТОДЫ ДЛЯ УПРАВЛЕНИЯ СОСТОЯНИЕМ ИГРЫ ---

    /// <summary>
    /// Проверяет, достиг ли игрок целевого счета.
    /// </summary>
    public void CheckWinCondition()
    {
        // Проверяем победу только в случае, если игра еще не закончилась
        if (!isGameOver && score >= targetScore)
        {
            GameWin();
        }
    }

    /// <summary>
    /// Обрабатывает состояние победы.
    /// </summary>
    public void GameWin()
    {
        isGameOver = true;
        Debug.Log("ПОБЕДА! Вы достигли целевого счета.");
        // if (winScreen != null) winScreen.SetActive(true); // Раскомментируйте, когда добавите UI-панель
    }

    /// <summary>
    /// Обрабатывает состояние поражения (вызывается, когда ходы заканчиваются).
    /// </summary>
    public void GameOver()
    {
        isGameOver = true;
        Debug.Log("ПОРАЖЕНИЕ! Ходы закончились, а цель не достигнута.");
        // if (loseScreen != null) loseScreen.SetActive(true); // Раскомментируйте, когда добавите UI-панель
    }

    
    public void DecreaseMove()
    {
        if (isGameOver) return;
        movesLeft--;
        UpdateUI();

        // 1. Проверка на конец игры (если ходы закончились)
        if (movesLeft <= 0)
        {
            // Если ходы закончились, и мы не достигли цели (GameWin() не сработал ранее)
            if (score < targetScore)
            {
                GameOver();
            }
            return;
        }

        // 2. Проверка на "Безвыходные ситуации"
        if (gridManager != null && !gridManager.CheckForPossibleMoves())
        {
            // Запускаем корутину перемешивания
            StartCoroutine(gridManager.ShuffleGrid());
        }
    }

    void WinGame()
    {
        isGameOver = true;
        Debug.Log("YOU WIN!");
        // Проверка на null, чтобы не вылетело, если экран не назначен
        if (winScreen != null) winScreen.SetActive(true);
    }


    // --- НОВЫЙ МЕТОД: ПРОВЕРКА И ПЕРЕМЕШИВАНИЕ ---
    public void CheckForStuckState()
    {
        // Если игра закончилась, или GridManager не найден, выходим
        if (isGameOver || gridManager == null) return; 
        
        // Если ходов нет, запускаем перемешивание
        if (!gridManager.CheckForPossibleMoves())
        {
            Debug.Log("Нет возможных ходов! Запускается перемешивание.");
            StartCoroutine(gridManager.ShuffleGrid());
        }
    }
    
}