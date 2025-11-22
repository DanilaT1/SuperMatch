using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 8;
    public int gridHeight = 8;
    public float cellSize = 1f;
    public GameObject cellPrefab;

    [Header("Planet Settings")]
    public Sprite[] planetSprites; // Массив спрайтов планет
    public int planetTypesCount = 6; // Количество видов планет

    [Header("Selection Settings")]
    public Color selectedColor = Color.white;
    public Color normalColor = Color.gray;

    [Header("Animation Settings")]
    public float swapDuration = 0.3f;

    private GameObject[,] grid;
    private int[,] planetGrid; // Хранит типы планет (0, 1, 2, ...)
    private GameObject selectedCell = null;
    private Vector2Int selectedCoord;

    public enum PlanetType
    {
        Lava,       // 0
        Ice,        // 1  
        Gas,        // 2
        Crystal,    // 3
        Desert,     // 4
        Ocean       // 5
    }

    void Start()
    {
        CreateGrid();
        InitializePlanets();
        CreateTestPlanets(); // Для теста без спрайтов
    }

    void Update()
    {
        HandleInput();
    }

    void CreateGrid()
    {
        grid = new GameObject[gridWidth, gridHeight];

        // Центрируем сетку на экране
        Vector3 gridOffset = new Vector3(-gridWidth * cellSize / 2 + cellSize / 2, -gridHeight * cellSize / 2 + cellSize / 2, 0);

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // Создаем ячейку
                Vector3 cellPosition = new Vector3(x * cellSize, y * cellSize, 0) + gridOffset;
                GameObject cell = Instantiate(cellPrefab, cellPosition, Quaternion.identity, transform);
                
                // Даем ячейке имя для удобства
                cell.name = $"Cell_{x}_{y}";
                
                // Сохраняем ссылку
                grid[x, y] = cell;
            }
        }

        Debug.Log($"Grid created: {gridWidth}x{gridHeight}");
    }

    void InitializePlanets()
    {
        planetGrid = new int[gridWidth, gridHeight];
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // Генерируем случайный тип планеты
                int randomPlanetType = GetValidPlanetType(x, y);
                planetGrid[x, y] = randomPlanetType;
                
                // Устанавливаем спрайт планеты
                SetPlanetSprite(x, y, randomPlanetType);
            }
        }
    }

    int GetValidPlanetType(int x, int y)
    {
        int maxAttempts = 10; // Защита от бесконечного цикла
        int attempts = 0;
        
        while (attempts < maxAttempts)
        {
            int randomType = Random.Range(0, planetTypesCount);
            
            // Проверяем, не создает ли этот тип готовое совпадение
            if (!CreatesInitialMatch(x, y, randomType))
            {
                return randomType;
            }
            
            attempts++;
        }
        
        // Если не удалось найти подходящий тип - возвращаем любой
        return Random.Range(0, planetTypesCount);
    }

    bool CreatesInitialMatch(int x, int y, int planetType)
    {
        // Проверка горизонтальных совпадений (слева)
        if (x >= 2)
        {
            if (planetGrid[x-1, y] == planetType && planetGrid[x-2, y] == planetType)
                return true;
        }
        
        // Проверка вертикальных совпадений (снизу)  
        if (y >= 2)
        {
            if (planetGrid[x, y-1] == planetType && planetGrid[x, y-2] == planetType)
                return true;
        }
        
        return false;
    }

    void SetPlanetSprite(int x, int y, int planetType)
    {
        if (planetSprites != null && planetType < planetSprites.Length && planetSprites[planetType] != null)
        {
            GameObject cell = grid[x, y];
            SpriteRenderer sr = cell.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = planetSprites[planetType];
            }
        }
    }

    // Временный метод для теста - создает цветные планеты
    void CreateTestPlanets()
    {
        // Если нет спрайтов - используем цвета
        if (planetSprites == null || planetSprites.Length == 0 || planetSprites[0] == null)
        {
            Color[] planetColors = new Color[]
            {
                Color.red,      // Lava
                Color.blue,     // Ice
                Color.yellow,   // Gas
                Color.magenta,  // Crystal
                new Color(1f, 0.5f, 0f), // Orange - Desert
                Color.cyan      // Ocean
            };
            
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    GameObject cell = grid[x, y];
                    SpriteRenderer sr = cell.GetComponent<SpriteRenderer>();
                    int planetType = planetGrid[x, y];
                    if (planetType < planetColors.Length)
                    {
                        sr.color = planetColors[planetType];
                    }
                }
            }
            Debug.Log("Using colored planets for testing");
        }
        else
        {
            Debug.Log("Using planet sprites");
        }
    }

    void HandleInput()
    {
    // Способ для новой Input System
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));
            Vector2 worldPos2D = new Vector2(worldPos.x, worldPos.y);
            
            RaycastHit2D hit = Physics2D.Raycast(worldPos2D, Vector2.zero);
            
            if (hit.collider != null)
            {
                GameObject clickedCell = hit.collider.gameObject;
                OnCellClicked(clickedCell);
            }
        }
    }

    void OnCellClicked(GameObject cell)
    {
        // Находим координаты ячейки
        Vector2Int coord = FindCellCoordinates(cell);
        
        if (selectedCell == null)
        {
            // Первый выбор
            SelectCell(coord);
        }
        else
        {
            // Второй выбор - проверяем соседство и делаем свап
            Vector2Int firstCoord = selectedCoord;
            
            if (AreCellsNeighbors(firstCoord, coord))
            {
                // Меняем планеты местами
                StartCoroutine(SwapPlanets(firstCoord, coord));
            }
            
            // Снимаем выделение в любом случае
            DeselectCell();
        }
    }

    Vector2Int FindCellCoordinates(GameObject cell)
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] == cell)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return new Vector2Int(-1, -1);
    }

    void SelectCell(Vector2Int coord)
    {
        selectedCell = grid[coord.x, coord.y];
        selectedCoord = coord;
        
        // Визуальное выделение
        SpriteRenderer sr = selectedCell.GetComponent<SpriteRenderer>();
        sr.color = selectedColor;
        
        Debug.Log($"Selected: {coord}");
    }

    void DeselectCell()
    {
        if (selectedCell != null)
        {
            // Возвращаем нормальный цвет
            SpriteRenderer sr = selectedCell.GetComponent<SpriteRenderer>();
            int planetType = planetGrid[selectedCoord.x, selectedCoord.y];
            ResetCellColor(selectedCoord.x, selectedCoord.y, planetType);
            
            selectedCell = null;
        }
    }

    bool AreCellsNeighbors(Vector2Int coord1, Vector2Int coord2)
    {
        int dx = Mathf.Abs(coord1.x - coord2.x);
        int dy = Mathf.Abs(coord1.y - coord2.y);
        
        // Соседи если разница только по X ИЛИ только по Y равна 1
        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }

    IEnumerator SwapPlanets(Vector2Int coord1, Vector2Int coord2)
    {
        Debug.Log($"Swapping: {coord1} <-> {coord2}");
        
        // Меняем типы планет в массиве
        int tempType = planetGrid[coord1.x, coord1.y];
        planetGrid[coord1.x, coord1.y] = planetGrid[coord2.x, coord2.y];
        planetGrid[coord2.x, coord2.y] = tempType;
        
        // Обновляем спрайты
        UpdatePlanetVisual(coord1.x, coord1.y);
        UpdatePlanetVisual(coord2.x, coord2.y);
        
        // Ждем немного и проверяем совпадения
        yield return new WaitForSeconds(swapDuration);
        
        // Проверяем есть ли совпадения после свапа
        bool hasMatches = CheckMatches();
        
        if (!hasMatches)
        {
            // Если нет совпадений - возвращаем обратно
            yield return StartCoroutine(SwapPlanets(coord1, coord2));
        }
        else
        {
            // Если есть совпадения - уничтожаем их
            yield return StartCoroutine(DestroyMatches());
        }
    }

    void UpdatePlanetVisual(int x, int y)
    {
        GameObject cell = grid[x, y];
        SpriteRenderer sr = cell.GetComponent<SpriteRenderer>();
        int planetType = planetGrid[x, y];
        
        if (planetSprites != null && planetType < planetSprites.Length && planetSprites[planetType] != null)
        {
            sr.sprite = planetSprites[planetType];
        }
        ResetCellColor(x, y, planetType);
    }

    void ResetCellColor(int x, int y, int planetType)
    {
        GameObject cell = grid[x, y];
        SpriteRenderer sr = cell.GetComponent<SpriteRenderer>();
        
        // Если нет спрайтов - используем цветовую схему
        if (planetSprites == null || planetSprites.Length == 0 || planetSprites[planetType] == null)
        {
            Color[] planetColors = new Color[]
            {
                Color.red, Color.blue, Color.yellow, 
                Color.magenta, new Color(1f, 0.5f, 0f), Color.cyan
            };
            
            if (planetType < planetColors.Length)
            {
                sr.color = planetColors[planetType];
            }
        }
        else
        {
            sr.color = Color.white;
        }
    }

    bool CheckMatches()
    {
        // Временная заглушка - всегда возвращает true для теста
        Debug.Log("Checking for matches...");
        return true;
    }

    IEnumerator DestroyMatches()
    {
        Debug.Log("Destroying matches!");
        
        // Временный эффект - мигание
        for (int i = 0; i < 3; i++)
        {
            SetAllPlanetsColor(Color.black);
            yield return new WaitForSeconds(0.1f);
            ResetAllPlanetsColor();
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.Log("Matches destroyed!");
    }

    void SetAllPlanetsColor(Color color)
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                SpriteRenderer sr = grid[x, y].GetComponent<SpriteRenderer>();
                sr.color = color;
            }
        }
    }

    void ResetAllPlanetsColor()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                ResetCellColor(x, y, planetGrid[x, y]);
            }
        }
    }

    // Метод для получения ячейки по координатам
    public GameObject GetCell(int x, int y)
    {
        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
            return grid[x, y];
        return null;
    }

    // Метод для получения типа планеты по координатам
    public int GetPlanetType(int x, int y)
    {
        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
            return planetGrid[x, y];
        return -1;
    }
}