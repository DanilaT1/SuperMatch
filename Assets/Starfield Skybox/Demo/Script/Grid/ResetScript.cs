using UnityEngine;

public class ResetScript : MonoBehaviour
{
    void Start()
    {
        // УДАЛЯЕТ ВСЕ СОХРАНЕНИЯ PLAYERPREFS!
        PlayerPrefs.DeleteAll();
        Debug.Log("Все PlayerPrefs сброшены. Можно запускать игру.");
    }
}