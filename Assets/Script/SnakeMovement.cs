using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // Required for UI

public class SnakeMovement : MonoBehaviour
{
    public Vector2 direction = Vector2.zero; // Public for potential future access
    private Vector2 targetPosition;
    public float moveSpeed = 2f; // Reduced for better collision detection
    public float moveInterval = 0.5f; // Time between moves
    private float timer = 0f;
    private bool hasStartedGrowing = false; // Flag to delay initial growth collision

    // Swipe detection variables
    private Vector2 touchStartPos;
    private Vector2 touchEndPos;
    private float minSwipeDistance = 50f; // Minimum distance for a swipe to register

    public List<GameObject> bodyParts = new List<GameObject>();
    public GameObject bodyPrefab; // Assign a sprite prefab for body segments with disabled BoxCollider2D
    public GameObject foodPrefab; // Assign a sprite prefab for food
    public GameObject bigFoodPrefab; // Assign a sprite prefab for big food
    public Vector2 gridSize; // Grid boundaries, set dynamically
    public float gridCellSize = 1f; // Size of each grid cell in world units
    public float bodySpacing = 0.1f; // Keeping original spacing as requested
    private int nextBigFoodThreshold = 0; // Total occupiedSpaces for next big food spawn
    private int lastBigFoodSpawn = 0; // Last occupiedSpaces when big food spawned
    [SerializeField] public float bigFoodLifetime = 6f; // Time in seconds big food lasts (public/serialized)

    // UI Elements
    [SerializeField] private TextMeshProUGUI scoreText; // Assign in Inspector to display score
    [SerializeField] private Slider countdownSlider; // Assign in Inspector to display countdown as slider
    private int score = 0; // Track player score

    void Start()
    {
        // Ensure the head has the "Head" tag
        gameObject.tag = "Head";

        // Calculate grid size based on screen and adjust camera
        CalculateGridSizeAndCamera();

        // Snap initial position to grid and set initial target within bounds
        Vector2 initialPosition = new Vector2(0, 0); // Start at center of grid
        transform.position = initialPosition;
        targetPosition = initialPosition;
        SpawnFood();
        SetNextBigFoodThreshold(); // Set initial random threshold
        UpdateScore(); // Initialize score display
        Debug.Log($"Initial position: {transform.position}, Target: {targetPosition}, Grid Size: {gridSize.x} x {gridSize.y}, Next Big Food Threshold: {nextBigFoodThreshold}");
    }

    void CalculateGridSizeAndCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
            return;
        }

        // Get the full visible height based on the camera's orthographic size
        float visibleHeight = mainCamera.orthographicSize * 2f; // Full height in world units
        float aspectRatio = (float)Screen.width / Screen.height;
        float visibleWidth = visibleHeight * aspectRatio; // Full width based on aspect ratio

        // Set grid size to match the full visible area, rounded up to ensure coverage
        gridSize = new Vector2(
            Mathf.Ceil(visibleWidth / gridCellSize) * gridCellSize, // Round up for full coverage
            Mathf.Ceil(visibleHeight / gridCellSize) * gridCellSize
        );

        // Adjust camera orthographic size to fit the grid
        mainCamera.orthographicSize = gridSize.y / 2f; // Ensure the grid fits vertically

        Debug.Log($"Screen Width: {Screen.width}, Height: {Screen.height}, Aspect Ratio: {aspectRatio}");
        Debug.Log($"Visible Width: {visibleWidth}, Height: {visibleHeight}, Grid Size: {gridSize.x} x {gridSize.y}");
    }

    void Update()
    {
        HandleTouchInput();

        timer += Time.deltaTime;
        if (timer >= moveInterval && hasStartedGrowing) // Only move after initial growth
        {
            MoveSnake();
            timer = 0f;
            Debug.Log($"Moved to target: {targetPosition}, Current Position: {transform.position}, Direction: {direction}");
        }

        // Smooth movement toward target only within the grid
        if (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            Vector3 currentPos = transform.position;
            Vector3 targetPos = new Vector3(targetPosition.x, targetPosition.y, currentPos.z);
            transform.position = Vector3.MoveTowards(currentPos, targetPos, moveSpeed * Time.deltaTime);
            Debug.Log($"Smooth move: Current: {transform.position}, Target: {targetPosition}");
        }
    }

    void SpawnFood()
    {
        int totalGridSpaces = (int)((gridSize.x / gridCellSize) * (gridSize.y / gridCellSize));
        int occupiedSpaces = bodyParts.Count + 1;

        if (occupiedSpaces >= totalGridSpaces)
        {
            Debug.Log("Game Over: No space left for food!");
            Time.timeScale = 0;
            return;
        }

        Vector2 foodPos;
        bool validPosition;
        int maxAttempts = 100;

        do
        {
            validPosition = true;
            float x = Mathf.Round(Random.Range(-gridSize.x / 2, gridSize.x / 2) / gridCellSize) * gridCellSize;
            float y = Mathf.Round(Random.Range(-gridSize.y / 2, gridSize.y / 2) / gridCellSize) * gridCellSize;
            foodPos = new Vector2(x, y);

            if (foodPos == (Vector2)transform.position)
            {
                validPosition = false;
            }

            foreach (GameObject body in bodyParts)
            {
                if (foodPos == (Vector2)body.transform.position)
                {
                    validPosition = false;
                    break;
                }
            }

            maxAttempts--;
            if (maxAttempts <= 0)
            {
                Debug.LogWarning("Could not find valid food position!");
                Time.timeScale = 0;
                return;
            }
        } while (!validPosition);

        GameObject food = Instantiate(foodPrefab, foodPos, Quaternion.identity);
        food.tag = "Food";
        Food foodComponent = food.AddComponent<Food>(); // Explicitly create and initialize
        foodComponent.isBeingEaten = false; // Ensure it's not eaten initially
        Debug.Log($"Food spawned at: {foodPos} with component: {foodComponent != null}");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Trigger entered with: {other.gameObject.name}, Tag: {other.tag}, Position: {other.transform.position}");
        if (other.CompareTag("Food"))
        {
            Food foodComponent = other.gameObject.GetComponent<Food>();
            if (foodComponent != null && !foodComponent.isBeingEaten)
            {
                foodComponent.isBeingEaten = true;
                Debug.Log($"Eating Food at: {other.transform.position}, Current Score: {score}, Body Count: {bodyParts.Count}");
                Destroy(other.gameObject);
                AddBodyPart();
                score += 1; // Increase score by 1 for small food
                UpdateScore();
                SpawnFood();
                CheckAndSpawnBigFood(); // Check for big food spawn
                hasStartedGrowing = true; // Allow movement after first growth
                Debug.Log($"After Eating: Score: {score}, New Body Count: {bodyParts.Count}, Foods Spawned: 1");
            }
            else
            {
                Debug.LogWarning($"Food component missing or already being eaten! Component: {foodComponent}, isBeingEaten: {foodComponent?.isBeingEaten}");
            }
        }
        else if (other.CompareTag("Bigfood"))
        {
            BigFood bigFoodComponent = other.gameObject.GetComponent<BigFood>();
            if (bigFoodComponent != null && !bigFoodComponent.isBeingEaten)
            {
                bigFoodComponent.isBeingEaten = true;
                Debug.Log($"Eating Bigfood at: {other.transform.position}, Current Score: {score}, Body Count: {bodyParts.Count}");
                Destroy(other.gameObject);
                AddBodyPart();
                score += 5; // Increase score by 5 for big food
                UpdateScore();
                HideCountdown();
                Debug.Log($"After Eating Bigfood: Score: {score}, New Body Count: {bodyParts.Count}");
            }
            else
            {
                Debug.LogWarning("Bigfood component missing or already being eaten!");
            }
        }
    }

    void CheckAndSpawnBigFood()
    {
        int occupiedSpaces = bodyParts.Count + 1;

        if (occupiedSpaces >= nextBigFoodThreshold && nextBigFoodThreshold > 0)
        {
            Vector2 bigFoodPos;
            bool validPosition;
            int maxAttempts = 100;

            do
            {
                validPosition = true;
                float x = Mathf.Round(Random.Range(-gridSize.x / 2, gridSize.x / 2) / gridCellSize) * gridCellSize;
                float y = Mathf.Round(Random.Range(-gridSize.y / 2, gridSize.y / 2) / gridCellSize) * gridCellSize;
                bigFoodPos = new Vector2(x, y);

                if (bigFoodPos == (Vector2)transform.position)
                {
                    validPosition = false;
                }

                foreach (GameObject body in bodyParts)
                {
                    if (bigFoodPos == (Vector2)body.transform.position)
                    {
                        validPosition = false;
                        break;
                    }
                }

                maxAttempts--;
                if (maxAttempts <= 0)
                {
                    Debug.LogWarning("Could not find valid big food position!");
                    Time.timeScale = 0;
                    return;
                }
            } while (!validPosition);

            GameObject bigFood = Instantiate(bigFoodPrefab, bigFoodPos, Quaternion.identity);
            bigFood.tag = "Bigfood";
            BigFood bigFoodComponent = bigFood.AddComponent<BigFood>(); // Explicitly create and initialize
            bigFoodComponent.isBeingEaten = false; // Ensure it's not eaten initially
            StartCoroutine(DestroyBigFoodAfterTime(bigFood)); // Start timer for big food
            ShowCountdown(); // Show countdown UI
            Debug.Log($"Bigfood spawned at: {bigFoodPos} with component: {bigFoodComponent != null}");
            lastBigFoodSpawn = occupiedSpaces; // Update last spawn point
            SetNextBigFoodThreshold(); // Set new threshold based on last spawn
        }
    }

    void SetNextBigFoodThreshold()
    {
        int randomIncrement = Random.Range(7, 12); // Random increment between 7 and 11 (inclusive)
        nextBigFoodThreshold = lastBigFoodSpawn + randomIncrement; // Cumulative threshold
        Debug.Log($"Next Big Food Threshold set to: {nextBigFoodThreshold} (Increment: {randomIncrement}, Last Spawn: {lastBigFoodSpawn})");
    }

    private System.Collections.IEnumerator DestroyBigFoodAfterTime(GameObject bigFood)
    {
        float timeElapsed = 0f;
        while (timeElapsed < bigFoodLifetime)
        {
            if (countdownSlider != null)
            {
                float normalizedTime = timeElapsed / bigFoodLifetime; // 0 to 1
                countdownSlider.value = 1f - normalizedTime; // Move from 1 to 0 (right to left)
            }
            yield return new WaitForSeconds(0.1f); // Update every 0.1 seconds
            timeElapsed += 0.1f;
        }
        if (bigFood != null) // Check if big food still exists (not eaten)
        {
            Debug.Log($"Bigfood at {bigFood.transform.position} disappeared after {bigFoodLifetime} seconds");
            Destroy(bigFood);
            HideCountdown(); // Hide countdown UI when time runs out
        }
    }

    void UpdateScore()
    {
        if (scoreText != null)
        {
            scoreText.text = " " + score.ToString();
        }
    }

    void ShowCountdown()
    {
        if (countdownSlider != null)
        {
            countdownSlider.gameObject.SetActive(true); // Show countdown UI
            countdownSlider.value = 1f; // Reset slider to full
        }
    }

    void HideCountdown()
    {
        if (countdownSlider != null)
        {
            countdownSlider.gameObject.SetActive(false); // Hide countdown UI
        }
    }

    void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began) touchStartPos = touch.position;
            if (touch.phase == TouchPhase.Ended)
            {
                touchEndPos = touch.position;
                DetectSwipe();
            }
        }
        else if (Input.GetMouseButtonDown(0)) touchStartPos = Input.mousePosition;
        else if (Input.GetMouseButtonUp(0))
        {
            touchEndPos = Input.mousePosition;
            DetectSwipe();
        }
    }

    void DetectSwipe()
    {
        Vector2 swipeDirection = touchEndPos - touchStartPos;
        float swipeDistance = swipeDirection.magnitude;

        if (swipeDistance > minSwipeDistance)
        {
            swipeDirection.Normalize();
            float x = swipeDirection.x;
            float y = swipeDirection.y;

            Vector2 newDirection = Mathf.Abs(x) > Mathf.Abs(y)
                ? (x > 0 ? Vector2.right : Vector2.left)
                : (y > 0 ? Vector2.up : Vector2.down);

            if (newDirection != -direction)
            {
                direction = newDirection;
                hasStartedGrowing = true; // Allow movement after first swipe
                GetComponent<Transform>().rotation = Quaternion.LookRotation(Vector3.forward, direction); // Rotate head
                Debug.Log($"Direction changed to: {direction}");
            }
        }
    }

    void AddBodyPart()
    {
        Vector2 lastPosition = bodyParts.Count > 0
            ? (Vector2)bodyParts[bodyParts.Count - 1].transform.position
            : (Vector2)transform.position - direction * bodySpacing; // Use original spacing
        GameObject newPart = Instantiate(bodyPrefab, lastPosition, Quaternion.identity);
        newPart.GetComponent<SpriteRenderer>().sortingOrder = bodyParts.Count + 1;

        // Enable BoxCollider2D only after the second body segment
        if (bodyParts.Count >= 4)
        {
            BoxCollider2D collider = newPart.GetComponent<BoxCollider2D>();
            if (collider != null)
            {
                collider.enabled = true;
                Debug.Log($"Enabled collider for body at: {newPart.transform.position}");
            }
        }
        else
        {
            Debug.Log($"Kept collider disabled for first body at: {newPart.transform.position}");
        }

        bodyParts.Add(newPart);
    }

    void MoveSnake()
    {
        // Calculate new position based on current position and direction
        Vector2 newPosition = (Vector2)transform.position + direction * gridCellSize;

        // Apply wrap-around using modulo for seamless edge transition
        float halfGridX = gridSize.x / 2;
        float halfGridY = gridSize.y / 2;

        newPosition.x = ((newPosition.x + halfGridX) % gridSize.x + gridSize.x) % gridSize.x - halfGridX;
        newPosition.y = ((newPosition.y + halfGridY) % gridSize.y + gridSize.y) % gridSize.y - halfGridY;

        foreach (GameObject body in bodyParts)
        {
            if (newPosition == (Vector2)body.transform.position)
            {
                Debug.Log("Game Over: Collided with body! New Position: " + newPosition);
                // No need to handle game over here; SnakeBodyCollision will handle it
                return;
            }
        }

        // Update target and current position with wrap
        targetPosition = newPosition;
        Vector3 wrappedPosition = new Vector3(newPosition.x, newPosition.y, transform.position.z);
        transform.position = wrappedPosition;

        Debug.Log($"MoveSnake: New Position: {newPosition}, Wrapped Position: {wrappedPosition}, Current Position: {transform.position}");

        // Update body parts from head to tail with custom spacing and segmented rotation
        if (bodyParts.Count > 0)
        {
            Vector2 nextPosition = newPosition - direction * bodySpacing;
            nextPosition.x = ((nextPosition.x + halfGridX) % gridSize.x + gridSize.x) % gridSize.x - halfGridX;
            nextPosition.y = ((nextPosition.y + halfGridY) % gridSize.y + gridSize.y) % gridSize.y - halfGridY;

            // Rotate the first body segment to match the head's direction
            if (bodyParts.Count > 0)
            {
                bodyParts[0].transform.position = nextPosition;
                bodyParts[0].transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
            }

            // Propagate position and calculate rotation for subsequent segments based on the previous segment
            for (int i = 1; i < bodyParts.Count; i++)
            {
                Vector2 currentPosition = (Vector2)bodyParts[i].transform.position;
                bodyParts[i].transform.position = nextPosition;

                // Calculate direction from the current segment to the previous segment
                Vector2 prevPosition = (Vector2)bodyParts[i - 1].transform.position;
                Vector2 segmentDirection = (prevPosition - currentPosition).normalized;

                // Rotate to face the direction of the previous segment
                if (segmentDirection != Vector2.zero)
                {
                    bodyParts[i].transform.rotation = Quaternion.LookRotation(Vector3.forward, segmentDirection);
                }

                nextPosition = currentPosition - direction * bodySpacing;
                nextPosition.x = ((nextPosition.x + halfGridX) % gridSize.x + gridSize.x) % gridSize.x - halfGridX;
                nextPosition.y = ((nextPosition.y + halfGridY) % gridSize.y + gridSize.y) % gridSize.y - halfGridY;
            }
        }
    }
}

// Simple scripts for food and big food
public class Food : MonoBehaviour
{
    public bool isBeingEaten = false;
}

public class BigFood : MonoBehaviour
{
    public bool isBeingEaten = false;
}