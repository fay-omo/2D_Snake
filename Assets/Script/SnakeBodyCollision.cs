using UnityEngine;

public class SnakeBodyCollision : MonoBehaviour
{
    private void Start()
    {
        // No need to add collider; it’s pre-attached to the prefab and controlled by SnakeMovement
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            Debug.Log($"Collider status for body at: {transform.position}, Enabled: {collider.enabled}");
        }
        else
        {
            Debug.LogWarning($"No BoxCollider2D found on body at: {transform.position}");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Head")) // Assume the head has a "Head" tag
        {
            Debug.Log($"Game Over: Head collided with body at {transform.position}, Head at {other.transform.position}");
            Time.timeScale = 0; // Pause the game
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.gameOverUI != null)
                {
                    GameManager.Instance.gameOverUI.SetActive(true); // Show the game over UI
                }
                if (GameManager.Instance.gameCanvasUI != null)
                {
                    GameManager.Instance.gameCanvasUI.SetActive(true); // Show the game canvas UI
                }
            }
            else
            {
                Debug.LogWarning("GameManager Instance not found!");
            }
        }
    }
}