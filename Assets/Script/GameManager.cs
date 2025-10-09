using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // Singleton instance
    public GameObject gameOverUI;      // Assign the GameOver UI GameObject in the Inspector
    public GameObject gameCanvasUI;    // Assign the Game Canvas UI GameObject in the Inspector

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Keeps GameManager across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }
}