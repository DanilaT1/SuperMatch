using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    // Эти значения теперь будут задаваться через GameManager, но оставим дефолтные
    public int gridWidth = 8;
    public int gridHeight = 8;
    public float cellSize = 1f;
    public GameObject cellPrefab;

    [Header("Planet Settings")]
    public Sprite[] planetSprites;
    public int planetTypesCount = 6;

    [Header("Selection Settings")]
    public Color selectedColor = Color.white;
    public Color normalColor = Color.gray;

    [Header("Animation Settings")]
    public float swapDuration = 0.3f;
    public float fallDuration = 0.1f;

    private GameObject[,] grid;
    private int[,] planetGrid;
    private GameObject selectedCell = null;
    private Vector2Int selectedCoord;
    
    private bool isProcessing = false; 

    public enum PlanetType
    {
        Lava, Ice, Gas, Crystal, Desert, Ocean
    }

    // --- ВАЖНОЕ ИЗМЕНЕНИЕ: Метод Start убран или пуст ---
    // Мы больше не создаем сетку здесь, мы ждем команды от GameManager
    void Start()
    {
        // Пусто. Ждем вызова InitializeLevel
    }

    // --- НОВЫЙ МЕТОД: Вызывается из GameManager ---
    public void InitializeLevel(int width, int height)
    {
        // 1. Очищаем старое поле, если оно было
        if (grid != null)
        {
            foreach (GameObject obj in grid)
            {
                if (obj != null) Destroy(obj);
            }
        }

        // 2. Применяем новые размеры
        gridWidth = width;
        gridHeight = height;

        // 3. Создаем всё заново
        CreateGrid();
        InitializePlanets();
        UpdateAllVisuals();
    }

    void Update()
    {
        if (!isProcessing)
        {
            HandleInput();
        }
    }

    void CreateGrid()
    {
        grid = new GameObject[gridWidth, gridHeight];
        
        // Вычисляем смещение, чтобы сетка была ровно по центру экрана
        Vector3 gridOffset = new Vector3(-gridWidth * cellSize / 2 + cellSize / 2, -gridHeight * cellSize / 2 + cellSize / 2, 0);

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 cellPosition = new Vector3(x * cellSize, y * cellSize, 0) + gridOffset;
                GameObject cell = Instantiate(cellPrefab, cellPosition, Quaternion.identity, transform);
                cell.name = $"Cell_{x}_{y}";
                grid[x, y] = cell;
            }
        }
    }

    void InitializePlanets()
    {
        planetGrid = new int[gridWidth, gridHeight];
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                int randomPlanetType = GetValidPlanetType(x, y);
                planetGrid[x, y] = randomPlanetType;
            }
        }
    }

    int GetValidPlanetType(int x, int y)
    {
        int maxAttempts = 10;
        int attempts = 0;
        
        while (attempts < maxAttempts)
        {
            int randomType = Random.Range(0, planetTypesCount);
            if (!CreatesInitialMatch(x, y, randomType))
            {
                return randomType;
            }
            attempts++;
        }
        return Random.Range(0, planetTypesCount);
    }

    bool CreatesInitialMatch(int x, int y, int planetType)
    {
        if (x >= 2)
        {
            if (planetGrid[x-1, y] == planetType && planetGrid[x-2, y] == planetType)
                return true;
        }
        if (y >= 2)
        {
            if (planetGrid[x, y-1] == planetType && planetGrid[x, y-2] == planetType)
                return true;
        }
        return false;
    }

    void HandleInput()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));
            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
            
            if (hit.collider != null)
            {
                OnCellClicked(hit.collider.gameObject);
            }
        }
    }

    void OnCellClicked(GameObject cell)
    {
        Vector2Int coord = FindCellCoordinates(cell);
        
        if (selectedCell == null)
        {
            SelectCell(coord);
        }
        else
        {
            Vector2Int firstCoord = selectedCoord;
            
            if (firstCoord == coord) {
                DeselectCell();
                return;
            }

            if (AreCellsNeighbors(firstCoord, coord))
            {
                StartCoroutine(SwapPlanets(firstCoord, coord));
            }
            else 
            {
                DeselectCell();
                SelectCell(coord);
            }
        }
    }

    Vector2Int FindCellCoordinates(GameObject cell)
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] == cell) return new Vector2Int(x, y);
            }
        }
        return new Vector2Int(-1, -1);
    }

    void SelectCell(Vector2Int coord)
    {
        selectedCell = grid[coord.x, coord.y];
        selectedCoord = coord;
        SpriteRenderer sr = selectedCell.GetComponent<SpriteRenderer>();
        sr.color = selectedColor;
    }

    void DeselectCell()
    {
        if (selectedCell != null)
        {
            ResetCellColor(selectedCoord.x, selectedCoord.y, planetGrid[selectedCoord.x, selectedCoord.y]);
            selectedCell = null;
        }
    }

    bool AreCellsNeighbors(Vector2Int coord1, Vector2Int coord2)
    {
        int dx = Mathf.Abs(coord1.x - coord2.x);
        int dy = Mathf.Abs(coord1.y - coord2.y);
        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }

    // --- ГЛАВНАЯ ЛОГИКА ---

    IEnumerator SwapPlanets(Vector2Int coord1, Vector2Int coord2)
    {
        isProcessing = true; 
        DeselectCell(); 

        // 1. Логический обмен
        int tempType = planetGrid[coord1.x, coord1.y];
        planetGrid[coord1.x, coord1.y] = planetGrid[coord2.x, coord2.y];
        planetGrid[coord2.x, coord2.y] = tempType;
        
        // 2. Визуальный обмен
        UpdatePlanetVisual(coord1.x, coord1.y);
        UpdatePlanetVisual(coord2.x, coord2.y);
        
        yield return new WaitForSeconds(swapDuration);
        
        // 3. Проверка совпадений
        HashSet<Vector2Int> matches = FindAllMatches();
        
        if (matches.Count > 0)
        {
            // УСПЕХ: Списываем ход
            if (GameManager.Instance != null)
            {
                GameManager.Instance.DecreaseMove();
            }

            yield return StartCoroutine(ProcessMatches(matches));
        }
        else
        {
            // НЕУДАЧА: Возвращаем всё назад
            tempType = planetGrid[coord1.x, coord1.y];
            planetGrid[coord1.x, coord1.y] = planetGrid[coord2.x, coord2.y];
            planetGrid[coord2.x, coord2.y] = tempType;
            
            // Визуальное возвращение
            UpdatePlanetVisual(coord1.x, coord1.y);
            UpdatePlanetVisual(coord2.x, coord2.y);
            
            isProcessing = false;
            
            // НОВОЕ: Проверяем, не застряла ли игра после неудачного хода
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CheckForStuckState(); 
            }
        }
    }

    IEnumerator ProcessMatches(HashSet<Vector2Int> matches)
    {
        while (matches.Count > 0)
        {
            // Начисляем очки
            if (GameManager.Instance != null) 
            {
                GameManager.Instance.AddScore(matches.Count * 10); 
            }

            // 1. Уничтожаем
            foreach (var coord in matches)
            {
                planetGrid[coord.x, coord.y] = -1; 
                grid[coord.x, coord.y].GetComponent<SpriteRenderer>().sprite = null;
            }
            
            yield return new WaitForSeconds(0.2f);

            // 2. Падение
            yield return StartCoroutine(CollapseGrid());

            // 3. Заполнение
            yield return StartCoroutine(RefillGrid());

            // 4. Повторный поиск
            matches = FindAllMatches();
        }
        // Проверяем, не застряла ли игра после того, как все матчи были обработаны.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CheckForStuckState();
        }

        isProcessing = false;
    }

    HashSet<Vector2Int> FindAllMatches()
    {
        HashSet<Vector2Int> matchedCells = new HashSet<Vector2Int>();

        // Горизонталь
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth - 2; x++)
            {
                int type1 = planetGrid[x, y];
                int type2 = planetGrid[x + 1, y];
                int type3 = planetGrid[x + 2, y];

                if (type1 != -1 && type1 == type2 && type1 == type3)
                {
                    matchedCells.Add(new Vector2Int(x, y));
                    matchedCells.Add(new Vector2Int(x + 1, y));
                    matchedCells.Add(new Vector2Int(x + 2, y));
                }
            }
        }

        // Вертикаль
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight - 2; y++)
            {
                int type1 = planetGrid[x, y];
                int type2 = planetGrid[x, y + 1];
                int type3 = planetGrid[x, y + 2];

                if (type1 != -1 && type1 == type2 && type1 == type3)
                {
                    matchedCells.Add(new Vector2Int(x, y));
                    matchedCells.Add(new Vector2Int(x, y + 1));
                    matchedCells.Add(new Vector2Int(x, y + 2));
                }
            }
        }

        return matchedCells;
    }

    IEnumerator CollapseGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            int emptyCellsCount = 0;
            
            for (int y = 0; y < gridHeight; y++)
            {
                if (planetGrid[x, y] == -1)
                {
                    emptyCellsCount++;
                }
                else if (emptyCellsCount > 0)
                {
                    int newY = y - emptyCellsCount;
                    planetGrid[x, newY] = planetGrid[x, y];
                    planetGrid[x, y] = -1;
                    
                    UpdatePlanetVisual(x, newY);
                    grid[x, y].GetComponent<SpriteRenderer>().sprite = null; 
                }
            }
        }
        
        yield return new WaitForSeconds(fallDuration);
    }

    IEnumerator RefillGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (planetGrid[x, y] == -1)
                {
                    int newType = Random.Range(0, planetTypesCount);
                    planetGrid[x, y] = newType;
                    UpdatePlanetVisual(x, y);
                }
            }
        }
        yield return new WaitForSeconds(fallDuration);
    }

    void UpdateAllVisuals()
    {
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                UpdatePlanetVisual(x, y);
    }

    void UpdatePlanetVisual(int x, int y)
    {
        GameObject cell = grid[x, y];
        SpriteRenderer sr = cell.GetComponent<SpriteRenderer>();
        int planetType = planetGrid[x, y];

        if (planetType == -1)
        {
            sr.sprite = null;
            return;
        }
        
        if (planetSprites != null && planetType < planetSprites.Length && planetSprites[planetType] != null)
        {
            sr.sprite = planetSprites[planetType];
            sr.color = Color.white;
        }
        else
        {
            Color[] planetColors = new Color[]
            {
                Color.red, Color.blue, Color.yellow, 
                Color.magenta, new Color(1f, 0.5f, 0f), Color.cyan
            };
            
            if (planetType < planetColors.Length)
            {
                sr.sprite = null;
                sr.color = planetColors[planetType];
            }
        }
    }


    



    // --- НОВЫЙ МЕТОД: ПРОВЕРКА НА БЕЗВЫХОДНЫЕ СИТУАЦИИ ---

public bool CheckForPossibleMoves()
{
    // Проверяем все ячейки
    for (int x = 0; x < gridWidth; x++)
    {
        for (int y = 0; y < gridHeight; y++)
        {
            // Получаем координаты текущей ячейки
            Vector2Int coord1 = new Vector2Int(x, y);

            // Проверяем четырех соседей (вправо и вверх достаточно, чтобы не дублировать проверки)
            
            // 1. Проверяем соседа справа (x + 1)
            if (x < gridWidth - 1)
            {
                Vector2Int coord2 = new Vector2Int(x + 1, y);
                if (IsSwapPossible(coord1, coord2))
                {
                    return true; // Найден возможный ход!
                }
            }

            // 2. Проверяем соседа сверху (y + 1)
            if (y < gridHeight - 1)
            {
                Vector2Int coord2 = new Vector2Int(x, y + 1);
                if (IsSwapPossible(coord1, coord2))
                {
                    return true; // Найден возможный ход!
                }
            }
        }
    }
    return false; // Ходов не найдено
}

// Вспомогательный метод: проверяет, создаст ли обмен совпадение
private bool IsSwapPossible(Vector2Int coord1, Vector2Int coord2)
{
    // Виртуально меняем местами типы планет
    int type1 = planetGrid[coord1.x, coord1.y];
    int type2 = planetGrid[coord2.x, coord2.y];

    planetGrid[coord1.x, coord1.y] = type2;
    planetGrid[coord2.x, coord2.y] = type1;

    // Проверяем, образуются ли совпадения после виртуального обмена
    bool matchFound = FindMatchesAt(coord1).Count > 0 || FindMatchesAt(coord2).Count > 0;

    // ОБЯЗАТЕЛЬНО возвращаем типы планет на место
    planetGrid[coord1.x, coord1.y] = type1;
    planetGrid[coord2.x, coord2.y] = type2;

    return matchFound;
}

// Вспомогательный метод: ищет совпадения только вокруг одной ячейки (нам нужен новый, быстрый метод)
    HashSet<Vector2Int> FindMatchesAt(Vector2Int coord)
    {
        HashSet<Vector2Int> matches = new HashSet<Vector2Int>();
        int x = coord.x;
        int y = coord.y;
        int type = planetGrid[x, y];

        // Горизонталь
        List<Vector2Int> horizontalMatches = new List<Vector2Int>();
        horizontalMatches.Add(coord);

        // Влево
        for (int i = x - 1; i >= 0 && planetGrid[i, y] == type; i--) horizontalMatches.Add(new Vector2Int(i, y));
        // Вправо
        for (int i = x + 1; i < gridWidth && planetGrid[i, y] == type; i++) horizontalMatches.Add(new Vector2Int(i, y));

        if (horizontalMatches.Count >= 3)
        {
            foreach (var c in horizontalMatches) matches.Add(c);
        }

        // Вертикаль
        List<Vector2Int> verticalMatches = new List<Vector2Int>();
        verticalMatches.Add(coord);

        // Вниз
        for (int i = y - 1; i >= 0 && planetGrid[x, i] == type; i--) verticalMatches.Add(new Vector2Int(x, i));
        // Вверх
        for (int i = y + 1; i < gridHeight && planetGrid[x, i] == type; i++) verticalMatches.Add(new Vector2Int(x, i));

        if (verticalMatches.Count >= 3)
        {
            foreach (var c in verticalMatches) matches.Add(c);
        }

        return matches;
    }


    void ResetCellColor(int x, int y, int planetType)
    {
        UpdatePlanetVisual(x, y);
    }


    // --- НОВЫЙ МЕТОД: ПЕРЕМЕШИВАНИЕ ПОЛЯ ---

    public IEnumerator ShuffleGrid()
    {
        Debug.Log("Поле перемешивается. Нет доступных ходов.");
        
        // 1. Очищаем визуал (для красоты)
        foreach (GameObject cell in grid)
        {
            if (cell != null) cell.GetComponent<SpriteRenderer>().sprite = null;
        }
        yield return new WaitForSeconds(0.5f); // Пауза для эффекта

        // 2. Цикл перемешивания, пока не найдется хотя бы один ход
        int maxAttempts = 100; // Ограничение на всякий случай
        int attempt = 0;

        do
        {
            // Создаем временный список всех типов планет на поле
            List<int> allPlanets = new List<int>();
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    allPlanets.Add(planetGrid[x, y]);
                }
            }

            // Перемешиваем список (алгоритм Фишера-Йетса)
            for (int i = 0; i < allPlanets.Count; i++)
            {
                int temp = allPlanets[i];
                int randomIndex = Random.Range(i, allPlanets.Count);
                allPlanets[i] = allPlanets[randomIndex];
                allPlanets[randomIndex] = temp;
            }

            // Заполняем поле перемешанными планетами
            int index = 0;
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    planetGrid[x, y] = allPlanets[index++];
                }
            }
            
            attempt++;
            
        } while (CheckForPossibleMoves() == false && attempt < maxAttempts);

        // 3. Обновляем визуал, чтобы показать новое поле
        UpdateAllVisuals();
        yield return new WaitForSeconds(0.5f);
        
        // После перемешивания нужно проверить, не создало ли оно новых совпадений
        HashSet<Vector2Int> matches = FindAllMatches();
        if (matches.Count > 0)
        {
            Debug.Log("Перемешивание создало матчи, обрабатываем...");
            yield return StartCoroutine(ProcessMatches(matches));
        }

        isProcessing = false; // Разрешаем ввод после всех операций
    }
}