using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CherryController : MonoBehaviour
{
    [Header("Cherry Settings")]
    [SerializeField] private GameObject cherryPrefab;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float spawnDelay = 5f;
    [SerializeField] private AudioClip cherryCollectSound;

    [Header("Mask Settings")]
    [SerializeField] private Sprite levelMaskSprite;
    [SerializeField] private string maskSortingLayer = "Default";
    [SerializeField] private int maskOrderInLayer = -1;
    [SerializeField] private bool debugMask = false;

    private GameObject currentCherry;
    private Camera mainCamera;
    private LevelGenerator levelGenerator;
    private AudioSource audioSource;
    private bool isMoving = false;
    private SpriteMask levelMask;

    // Camera bounds for spawning and movement
    private Vector2 levelCenter;
    private float spawnOffset = 2f;
    private float despawnOffset = 2f;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
        levelGenerator = FindFirstObjectByType<LevelGenerator>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Wait for levelgen
        StartCoroutine(InitializeAfterLevelGeneration());
    }

    private IEnumerator InitializeAfterLevelGeneration()
    {
        yield return new WaitForSeconds(0.1f);

        // Calculate level center
        CalculateLevelCenter();

        // Create the level mask
        CreateLevelMask();

        // Start the spawning cycle
        StartCoroutine(SpawnCycle());
    }

    private void CreateLevelMask()
    {
        GameObject maskObject = new GameObject("Level Mask");
        maskObject.transform.parent = transform;

        // Add SpriteMask
        levelMask = maskObject.AddComponent<SpriteMask>();

        // Set up the mask sprite
        if (levelMaskSprite != null)
        {
            levelMask.sprite = levelMaskSprite;
        }

        // Configure mask properties
        levelMask.alphaCutoff = 0.1f;
        levelMask.isCustomRangeActive = false; // Affect all sprites behind it

        // Position and scale the mask to cover the level area
        if (levelGenerator != null && levelGenerator.LevelMap != null)
        {
            int rows = levelGenerator.LevelMap.GetLength(0);
            int cols = levelGenerator.LevelMap.GetLength(1);

            // Position at level center
            Vector3 maskPosition = new Vector3(
                (cols - 1) * 0.5f + levelGenerator.levelOffset.x,
                -(rows - 1) * 0.5f + levelGenerator.levelOffset.y,
                0
            );
            maskObject.transform.position = maskPosition;

            Vector3 maskScale = new Vector3(cols + 2, rows + 2, 1);
            maskObject.transform.localScale = maskScale;
        }

        // Debugging
        if (debugMask)
        {
            SpriteRenderer debugRenderer = maskObject.AddComponent<SpriteRenderer>();
            debugRenderer.sprite = levelMask.sprite;
            debugRenderer.color = new Color(1, 0, 0, 0.3f); // Semi-transparent red
            debugRenderer.sortingLayerName = maskSortingLayer;
            debugRenderer.sortingOrder = maskOrderInLayer + 1;
        }
    }

    private void CalculateLevelCenter()
    {
        if (levelGenerator != null && levelGenerator.LevelMap != null)
        {
            int rows = levelGenerator.LevelMap.GetLength(0);
            int cols = levelGenerator.LevelMap.GetLength(1);
            levelCenter = new Vector2((cols - 1) * 0.5f, -(rows - 1) * 0.5f);
            levelCenter += levelGenerator.levelOffset;
        }
        else
        {
            // Fallback to camera center
            levelCenter = mainCamera.transform.position;
            Debug.LogWarning("Using camera center as fallback for level center");
        }
    }

    private IEnumerator SpawnCycle()
    {
        yield return new WaitForSeconds(spawnDelay);

        while (true)
        {
            if (currentCherry == null && !isMoving)
            {
                SpawnCherry();
            }

            yield return new WaitForSeconds(5.0f);
        }
    }

    private void SpawnCherry()
    {
        if (cherryPrefab == null)
        {
            Debug.LogError("Cherry prefab is not assigned");
            return;
        }

        // Get camera bounds
        Vector2 cameraSize = GetCameraWorldSize();
        Vector2 cameraCenter = mainCamera.transform.position;

        // Choose random angle for spawn direction (0-360 degrees)
        float spawnAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

        // Calculate spawn position outside camera view
        Vector2 spawnDirection = new Vector2(Mathf.Cos(spawnAngle), Mathf.Sin(spawnAngle));
        float spawnDistance = Mathf.Max(cameraSize.x, cameraSize.y) * 0.5f + spawnOffset;
        Vector3 spawnPosition = cameraCenter + spawnDirection * spawnDistance;

        // Calculate target position (opposite side, through center)
        Vector2 targetDirection = -spawnDirection; // Opposite direction
        float targetDistance = spawnDistance + despawnOffset;
        Vector3 targetPosition = cameraCenter + targetDirection * targetDistance;

        // Instantiate cherry
        currentCherry = Instantiate(cherryPrefab, spawnPosition, Quaternion.identity);
        SetupCherry(currentCherry);

        Debug.Log("Cherry Spawned");

        // Start movement through center
        StartCoroutine(MoveCherry(currentCherry, spawnPosition, targetPosition));
    }

    private Vector2 GetCameraWorldSize()
    {
        if (mainCamera.orthographic)
        {
            float height = mainCamera.orthographicSize * 2f;
            float width = height * mainCamera.aspect;
            return new Vector2(width, height);
        }
        else
        {
            // For perspective camera (fallback)
            float distance = Mathf.Abs(mainCamera.transform.position.z);
            float height = 2f * distance * Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float width = height * mainCamera.aspect;
            return new Vector2(width, height);
        }
    }

    private void SetupCherry(GameObject cherry)
    {
        SpriteRenderer spriteRenderer = cherry.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = 100;
            spriteRenderer.sortingLayerName = "Default";
            spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }

        cherry.tag = "Cherry";

        if (cherry.GetComponent<Collider2D>() == null)
        {
            CircleCollider2D collider = cherry.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.3f;
        }

        // Cherry Collision
        Rigidbody2D rb = cherry.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = cherry.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
    }

    private IEnumerator MoveCherry(GameObject cherry, Vector3 startPos, Vector3 endPos)
    {
        if (cherry == null) yield break;

        isMoving = true;
        float journeyLength = Vector3.Distance(startPos, endPos);
        float journeyTime = journeyLength / moveSpeed;
        float elapsedTime = 0;

        while (elapsedTime < journeyTime && cherry != null)
        {
            elapsedTime += Time.deltaTime;
            float fractionOfJourney = elapsedTime / journeyTime;

            // Straight line lerp
            cherry.transform.position = Vector3.Lerp(startPos, endPos, fractionOfJourney);

            // Check if cherry is too far
            if (IsOutOfCameraBounds(cherry.transform.position))
            {
                Debug.Log("Cherry went too far, destroying...");
                break;
            }

            yield return null;
        }

        if (cherry != null)
        {
            DestroyCherry();
        }

        isMoving = false;

        // Wait before spawning next cherry
        yield return new WaitForSeconds(spawnDelay);
    }

    private bool IsOutOfCameraBounds(Vector3 position)
    {
        Vector2 cameraSize = GetCameraWorldSize();
        Vector2 cameraCenter = mainCamera.transform.position;

        // Check if outside camera bounds
        float maxDistance = Mathf.Max(cameraSize.x, cameraSize.y) * 0.5f + despawnOffset + 1f;
        float distanceFromCamera = Vector2.Distance(position, cameraCenter);

        return distanceFromCamera > maxDistance;
    }

    public void OnCherryCollected()
    {
        // Play sound
        if (cherryCollectSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(cherryCollectSound);
        }

        // Destroy cherry and reset timer
        DestroyCherry();
        isMoving = false;
    }

    private void DestroyCherry()
    {
        if (currentCherry != null)
        {
            Debug.Log("Cherry destroyed");
            Destroy(currentCherry);
            currentCherry = null;
        }
    }
}