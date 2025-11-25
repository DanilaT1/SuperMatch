using UnityEngine;

public class TestResetter : MonoBehaviour
{
    void Start()
    {
        // Это будет сбрасывать прогресс при каждом запуске!
        PlayerPrefs.DeleteAll(); 
        Debug.Log("Прогресс сброшен автоматически.");
    }
}