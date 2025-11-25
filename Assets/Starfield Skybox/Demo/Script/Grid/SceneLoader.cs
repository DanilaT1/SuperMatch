using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Имя вашей главной игровой сцены, где висит GameManager
    public string gameSceneName = "Game"; 

    // Метод, который будет вызываться кнопкой
    public void LoadLevel(int levelNumber)
    {
        // 1. Сохраняем выбранный уровень
        PlayerPrefs.SetInt("SelectedLevel", levelNumber);
        
        // 2. Загружаем сцену игры
        SceneManager.LoadScene(gameSceneName);
    }
    
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Выход из игры...");
    }
}