using UnityEngine;

// Эта строчка добавляет пункт в меню создания файлов Unity
[CreateAssetMenu(fileName = "NewLevel", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Настройки поля")]
    public int width = 8;
    public int height = 8;

    [Header("Правила игры")]
    public int movesCount = 5;
    public int targetScore = 160;
    
    // Сюда можно добавить фон для уровня, если захотите
   
    public Sprite backgroundSprite; 
}