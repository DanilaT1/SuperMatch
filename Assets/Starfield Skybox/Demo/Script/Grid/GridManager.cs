using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Нужно для списков
using UnityEngine.InputSystem;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
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
    public float fallDuration = 0.1f; // Задержка для эффекта падения

    private GameObject[,] grid;
    private int[,] planetGrid;
    private GameObject selectedCell = null;
    private Vector2Int selectedCoord;
    
    // Флаг, чтобы нельзя было кликать, пока идет анимация уничтожения/падения
    private bool isProcessing = false; 

    public enum PlanetType
    {
        Lava, Ice, Gas, Crystal, Desert, Ocean
    }

    void Start()
    {
        CreateGrid();
        InitializePlanets();
        // Убрали CreateTestPlanets, используем UpdateVisuals
        UpdateAllVisuals(); 
    }

    void Update()
    {
        if (!isProcessing) // Блокируем ввод во время анимаций
        {
            HandleInput();
        }
    }

    void CreateGrid()
    {
        grid = new GameObject[gridWidth, gridHeight];
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
            
            // Если кликнули на ту же ячейку - снимаем выделение
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
                // Если кликнули далеко - просто переключаем выделение
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
        isProcessing = true; // Блокируем ввод
        DeselectCell();      // Снимаем выделение сразу

        // 1. Логический обмен местами
        int tempType = planetGrid[coord1.x, coord1.y];
        planetGrid[coord1.x, coord1.y] = planetGrid[coord2.x, coord2.y];
        planetGrid[coord2.x, coord2.y] = tempType;
        
        // 2. Визуальное обновление (картинки меняются местами)
        UpdatePlanetVisual(coord1.x, coord1.y);
        UpdatePlanetVisual(coord2.x, coord2.y);
        
        // Ждем пока пройдет анимация свапа
        yield return new WaitForSeconds(swapDuration);
        
        // 3. Проверяем совпадения
        HashSet<Vector2Int> matches = FindAllMatches();
        
        if (matches.Count > 0)
        {
            // === УСПЕХ: СОВПАДЕНИЕ ЕСТЬ ===
            
            // Списываем ход (только здесь!)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.DecreaseMove();
            }

            // Запускаем процесс уничтожения, падения и начисления очков
            yield return StartCoroutine(ProcessMatches(matches));
        }
        else
        {
            // === НЕУДАЧА: СОВПАДЕНИЙ НЕТ ===
            // Ход НЕ списываем, просто возвращаем всё назад
            
            // Меняем обратно в массиве данных
            tempType = planetGrid[coord1.x, coord1.y];
            planetGrid[coord1.x, coord1.y] = planetGrid[coord2.x, coord2.y];
            planetGrid[coord2.x, coord2.y] = tempType;
            
            // Возвращаем картинки обратно
            UpdatePlanetVisual(coord1.x, coord1.y);
            UpdatePlanetVisual(coord2.x, coord2.y);
            
            isProcessing = false; // Разблокируем ввод, игрок может пробовать снова
        }
    }
    // Основной цикл: Удалить -> Упасть -> Заполнить -> Повторить поиск
    IEnumerator ProcessMatches(HashSet<Vector2Int> matches)
    {
        while (matches.Count > 0)
        {

            // Начисляем очки за количество уничтоженных планет
            // ВАЖНО: Добавь эту проверку и вызов
            if (GameManager.Instance != null) 
            {
                // Например, 10 очков за каждую планету
                GameManager.Instance.AddScore(matches.Count * 10); 
            }
            // 1. Уничтожаем совпадения
            foreach (var coord in matches)
            {
                planetGrid[coord.x, coord.y] = -1; // -1 означает "пусто"
                // Визуально скрываем (можно добавить партиклы здесь)
                grid[coord.x, coord.y].GetComponent<SpriteRenderer>().sprite = null;
            }
            
            yield return new WaitForSeconds(0.2f);

            // 2. Заставляем планеты падать вниз
            yield return StartCoroutine(CollapseGrid());

            // 3. Заполняем пустоты сверху
            yield return StartCoroutine(RefillGrid());

            // 4. Снова ищем совпадения (цепная реакция)
            matches = FindAllMatches();
        }

        isProcessing = false; // Разблокируем ввод, когда всё успокоилось
    }

    // Поиск всех совпадений (горизонталь и вертикаль)
    HashSet<Vector2Int> FindAllMatches()
    {
        HashSet<Vector2Int> matchedCells = new HashSet<Vector2Int>();

        // Горизонтальные
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

        // Вертикальные
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

    // Логика падения
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
                    // Сдвигаем планету вниз на количество пустых клеток под ней
                    int newY = y - emptyCellsCount;
                    planetGrid[x, newY] = planetGrid[x, y];
                    planetGrid[x, y] = -1;
                    
                    // Обновляем визуализацию
                    UpdatePlanetVisual(x, newY);
                    // Очищаем старую ячейку
                    grid[x, y].GetComponent<SpriteRenderer>().sprite = null; 
                }
            }
        }
        
        // Небольшая пауза для визуального эффекта завершения падения
        yield return new WaitForSeconds(fallDuration);
    }

    // Спавн новых планет
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

    // --- ВИЗУАЛИЗАЦИЯ ---

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

        // Если тип -1 (пусто), убираем спрайт
        if (planetType == -1)
        {
            sr.sprite = null;
            return;
        }
        
        if (planetSprites != null && planetType < planetSprites.Length && planetSprites[planetType] != null)
        {
            sr.sprite = planetSprites[planetType];
            sr.color = Color.white; // Сбрасываем цвет на белый, чтобы видеть спрайт
        }
        else
        {
            // Fallback: использование цветов, если нет спрайтов
            Color[] planetColors = new Color[]
            {
                Color.red, Color.blue, Color.yellow, 
                Color.magenta, new Color(1f, 0.5f, 0f), Color.cyan
            };
            
            if (planetType < planetColors.Length)
            {
                sr.sprite = null; // Убираем спрайт, чтобы видеть цвет
                sr.color = planetColors[planetType];
            }
        }
    }

    void ResetCellColor(int x, int y, int planetType)
    {
        // Этот метод теперь просто вызывает UpdatePlanetVisual для упрощения
        UpdatePlanetVisual(x, y);
    }
}