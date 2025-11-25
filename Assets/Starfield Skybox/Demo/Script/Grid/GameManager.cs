using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // [Header("Текущий Уровень")]
    
    [Header("Настройки Уровней")]
    public LevelData[] allLevels; // Оставьте публичным, чтобы назначать в Инспекторе
    private LevelData currentLevel; // Оставьте приватным, чтобы устанавливать из кода Start()

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
        // 1. Если Instance уже существует и это НЕ мы, уничтожаем дубликат.
        if (Instance != null && Instance != this)
        {
            // Debug.LogWarning("Найден дубликат GameManager. Он будет уничтожен.");
            Destroy(gameObject); 
            return; 
        }
        
        // 2. Мы — первый и единственный экземпляр.
        Instance = this;
        
        // 3. Если вы хотите, чтобы GameManager ПЕРЕНОСИЛСЯ между сценами:
        // DontDestroyOnLoad(gameObject);
        
        // 4. Находим GridManager.
        gridManager = FindObjectOfType<GridManager>();
    }

    void Start()
    {
    // ... (debug логирование, если нужно) ...
    // --- НОВЫЙ КОД ДЛЯ ОТЛАДКИ ---
    Debug.Log($"[DEBUG] GameManager Start: allLevels.Length = {allLevels.Length}");

    int selectedLevelIndex = PlayerPrefs.GetInt("SelectedLevel", 1) - 1; 

    // БЕЗОПАСНЫЙ ЗАПУСК
    if (allLevels.Length == 0)
        {
            Debug.LogError("КРИТИЧЕСКАЯ ОШИБКА: В массиве уровней нет данных! Не могу запустить игру.");
            return; // Выходим из Start, игра не запустится
        }

        // 2. Проверяем, что индекс валиден (0 до Length - 1)
        if (selectedLevelIndex >= 0 && selectedLevelIndex < allLevels.Length)
        {
            // 3. Устанавливаем выбранный уровень
            currentLevel = allLevels[selectedLevelIndex]; 
        }
        else
        {
            // 4. Если индекс неверный (например, нажали кнопку 5, а уровней всего 3), запускаем Уровень 1 (индекс 0)
            Debug.LogWarning($"SelectedLevelIndex ({selectedLevelIndex}) неверен. Запуск первого уровня (0).");
            currentLevel = allLevels[0];
        }
    
    // 5. Запускаем уровень, который мы успешно выбрали
    StartLevel(); 
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
        if (winScreen != null) winScreen.SetActive(true); // Раскомментируйте, когда добавите UI-панель
    }

    /// <summary>
    /// Обрабатывает состояние поражения (вызывается, когда ходы заканчиваются).
    /// </summary>
    public void GameOver()
    {
        isGameOver = true;
        Debug.Log("ПОРАЖЕНИЕ! Ходы закончились, а цель не достигнута.");
        if (loseScreen != null) loseScreen.SetActive(true); // Раскомментируйте, когда добавите UI-панель
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
    // --- МЕТОДЫ ДЛЯ КНОПОК UI ---

    public void RestartLevel()
    {
        // Перезагружаем текущую сцену
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMenu()
    {
        // Загружаем сцену выбора уровня
        SceneManager.LoadScene("LevelSelect");
    }

    public void NextLevel()
    {
        // Получаем индекс текущего уровня из PlayerPrefs
        int currentIdx = PlayerPrefs.GetInt("SelectedLevel", 1);
        int nextIdx = currentIdx + 1;

        // Проверяем, есть ли такой уровень в нашем массиве
        if (nextIdx <= allLevels.Length) 
        {
            PlayerPrefs.SetInt("SelectedLevel", nextIdx);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            // Если уровни кончились, идем в меню
            Debug.Log("Уровни кончились!");
            GoToMenu();
        }
    }
}