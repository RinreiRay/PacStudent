using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacStudentController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;

    private Vector2Int lastInput = Vector2Int.zero;
    private Vector2Int currentInput = Vector2Int.zero;

    private bool hasReceivedFirstInput = false;
    private bool isLerping = false;
    private Vector2Int currentGridPosition;

    private Animator animator;
    private AudioSource audioSource;
    private ParticleSystem dustParticles;

    [SerializeField] private AudioClip movementSound;
    [SerializeField] private AudioClip pelletEatingSound;

    private int[,] levelMap;
    private GameObject levelGenerator;
    private LevelGenerator levelGen;

    private Tweener tweener;

    void Start()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        // Particle system
        SetupParticles();

        // Get level data
        levelGenerator = GameObject.Find("LevelGenerator");
        if (levelGenerator != null)
        {
            StartCoroutine(InitializeLevelMapDelayed());
        }

        tweener = FindFirstObjectByType<Tweener>();
        if (tweener == null)
        {
            GameObject tweenerObject = new GameObject("Tweener");
            tweener = tweenerObject.AddComponent<Tweener>();
            Debug.Log("Created new Tweener GameObject");
        }

        // Initialize grid position based on starting transform position
        currentGridPosition = WorldToGrid(transform.position);

        // Set initial facing direction (right) but not moving
        UpdateAnimationDirection(Vector2Int.right);

        Debug.Log($"PacStudent starting at grid position: {currentGridPosition}");
    }

    void Update()
    {
        GatherInput();

        // Allow movement after first input
        if (!isLerping && hasReceivedFirstInput)
        {
            TryMove();
        }

        // Update animations based on movement state
        UpdateAnimationState();
    }

    private void GatherInput()
    {
        // Check for WASD input and store in lastInput
        Vector2Int newInput = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.W))
        {
            newInput = Vector2Int.down; // Inverted grid coords
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            newInput = Vector2Int.left;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            newInput = Vector2Int.up;
        }
        else if (Input.GetKeyDown(KeyCode.D)) // Inverted grid coords
        {
            newInput = Vector2Int.right;
        }

        if (newInput != Vector2Int.zero)
        {
            lastInput = newInput;
            if (!hasReceivedFirstInput)
            {
                hasReceivedFirstInput = true;
                currentInput = lastInput; // Set initial movement direction
                Debug.Log("First input received");
            }
        }
    }

    private void TryMove()
    {
        // Try to move in the lastInput direction
        if (lastInput != Vector2Int.zero)
        {
            Vector2Int targetGrid = currentGridPosition + lastInput;

            if (IsWalkable(targetGrid))
            {
                currentInput = lastInput;
                StartTweenMovement(targetGrid);
                UpdateAnimationDirection(GetWorldDirection(currentInput));
                return;
            }
        }

        // If lastInput direction is blocked, try currentInput direction
        if (currentInput != Vector2Int.zero)
        {
            Vector2Int targetGrid = currentGridPosition + currentInput;

            if (IsWalkable(targetGrid))
            {
                StartTweenMovement(targetGrid);
                UpdateAnimationDirection(GetWorldDirection(currentInput));
                return;
            }
        }

        // No valid moves - PacStudent stops
    }

    private Vector2Int GetWorldDirection(Vector2Int gridDirection)
    {
        if (gridDirection == Vector2Int.up) return Vector2Int.down;    // Grid up = World down
        if (gridDirection == Vector2Int.down) return Vector2Int.up;    // Grid down = World up
        return gridDirection; // Left and right remain the same
    }

    private void UpdateAnimationDirection(Vector2Int direction)
    {
        if (animator == null) return;

        // Animator direction parameters
        // 0 = Right, 1 = Left, 2 = Up, 3 = Down
        int directionIndex = GetDirectionIndex(direction);
        animator.SetInteger("Direction", directionIndex);
    }

    private int GetDirectionIndex(Vector2Int direction)
    {
        if (direction == Vector2Int.right) return 0;
        if (direction == Vector2Int.left) return 1;
        if (direction == Vector2Int.up) return 2;
        if (direction == Vector2Int.down) return 3;
        return 0; // Default to right
    }

    private void UpdateAnimationState()
    {
        if (animator != null)
        {
            // Only animate when lerping (moving)
            animator.speed = isLerping ? 1f : 0f;
        }

        // Handle particle effects
        if (!isLerping && dustParticles != null && dustParticles.isPlaying)
        {
            dustParticles.Stop();
        }

        // Handle audio
        if (!isLerping && audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    private void StartTweenMovement(Vector2Int targetGrid)
    {
        if (tweener == null)
        {
            Debug.LogError("Cannot move: Tweener is null!");
            return;
        }

        isLerping = true;
        Vector3 targetPos = GridToWorld(targetGrid);
        float duration = 1f / moveSpeed;

        // Use the Tweener class to handle movement
        tweener.AddTween(transform, transform.position, targetPos, duration);
        currentGridPosition = targetGrid;

        // Start particle effect
        if (dustParticles != null && !dustParticles.isPlaying)
        {
            dustParticles.Play();
        }

        // Play appropriate audio
        PlayMovementAudio(targetGrid);

        // Start coroutine to handle tween completion
        StartCoroutine(WaitForTweenCompletion(duration));
    }

    private IEnumerator WaitForTweenCompletion(float duration)
    {
        yield return new WaitForSeconds(duration);

        isLerping = false;
        CheckForPellet();
    }

    private bool IsWalkable(Vector2Int gridPos)
    {
        // Check bounds
        if (levelMap == null) return false;
        if (gridPos.y < 0 || gridPos.y >= levelMap.GetLength(0)) return false;
        if (gridPos.x < 0 || gridPos.x >= levelMap.GetLength(1)) return false;

        int tileValue = levelMap[gridPos.y, gridPos.x];

        // Walkable: 0,5,6
        // Non-walkable: 1,2,3,4,7,8

        // Ghost wall
        if (tileValue == 8)
        {
            return false;
        }

        // Check if it's walkable
        return tileValue == 0 || tileValue == 5 || tileValue == 6;
    }

    private void PlayMovementAudio(Vector2Int targetGrid)
    {
        if (audioSource == null) return;

        // Check if there's a pellet
        bool hasPellet = false;
        if (levelMap != null &&
            targetGrid.y >= 0 && targetGrid.y < levelMap.GetLength(0) &&
            targetGrid.x >= 0 && targetGrid.x < levelMap.GetLength(1))
        {
            int tileValue = levelMap[targetGrid.y, targetGrid.x];
            hasPellet = (tileValue == 5 || tileValue == 6);
        }

        // Play audio clip
        if (hasPellet && pelletEatingSound != null)
        {
            audioSource.clip = pelletEatingSound;
            audioSource.Play();
        }
        else if (movementSound != null)
        {
            audioSource.clip = movementSound;
            audioSource.Play();
        }
    }

    private void CheckForPellet()
    {
        // Check if current position has a pellet
        if (levelMap != null &&
            currentGridPosition.y >= 0 && currentGridPosition.y < levelMap.GetLength(0) &&
            currentGridPosition.x >= 0 && currentGridPosition.x < levelMap.GetLength(1))
        {
            int tileValue = levelMap[currentGridPosition.y, currentGridPosition.x];

            if (tileValue == 5 || tileValue == 6)
            {
                // Collect pellet
                CollectPellet(currentGridPosition, tileValue == 6);

                // Update level map to mark pellet as collected
                levelMap[currentGridPosition.y, currentGridPosition.x] = 0;
            }
        }
    }

    private void CollectPellet(Vector2Int position, bool isPowerPellet)
    {
        // Find and destroy the pellet GameObject at this position
        Vector3 worldPos = GridToWorld(position);
        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, 0.1f);

        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Pellet") || col.CompareTag("PowerPellet"))
            {
                Destroy(col.gameObject);

                // Add score, trigger events, etc.
                if (isPowerPellet)
                {
                    // Handle power pellet collection
                    Debug.Log("Power Pellet collected!");
                }
                else
                {
                    // Handle normal pellet collection
                    Debug.Log("Pellet collected!");
                }
                break;
            }
        }
    }

    

    private Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector2 levelOffset = Vector2.zero;
        if (levelGen != null)
        {
            levelOffset = levelGen.levelOffset;
        }

        Vector3 adjustedPos = worldPos - (Vector3)levelOffset;

        return new Vector2Int(
            Mathf.RoundToInt(adjustedPos.x),
            Mathf.RoundToInt(-adjustedPos.y) // Inverted Y level array
        );
    }

    private Vector3 GridToWorld(Vector2Int gridPos)
    {
        Vector2 levelOffset = Vector2.zero;
        if (levelGen != null)
        {
            levelOffset = levelGen.levelOffset;
        }

        Vector3 worldPos = new Vector3(gridPos.x, -gridPos.y, 0);
        return worldPos + (Vector3)levelOffset;
    }

    private void InitializeLevelMap()
    {
        levelGen = levelGenerator.GetComponent<LevelGenerator>();
        if (levelGen != null && levelGen.LevelMap != null)
        {
            levelMap = levelGen.LevelMap;
            Debug.Log($"Level map initialized: {levelMap.GetLength(0)}x{levelMap.GetLength(1)}");
        }
    }

    private IEnumerator InitializeLevelMapDelayed()
    {
        yield return null; // Wait one frame
        InitializeLevelMap();

        // Verify starting position is walkable
        if (levelMap != null)
        {
            bool startWalkable = IsWalkable(currentGridPosition);
            Debug.Log($"Starting position walkable: {startWalkable}");

            if (!startWalkable)
            {
                // Find nearest walkable position
                FindNearestWalkablePosition();
            }
        }
    }

    private void FindNearestWalkablePosition()
    {
        // Search in expanding squares around current position
        for (int radius = 1; radius <= 5; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    Vector2Int testPos = currentGridPosition + new Vector2Int(dx, dy);
                    if (IsWalkable(testPos))
                    {
                        currentGridPosition = testPos;
                        transform.position = GridToWorld(testPos);
                        Debug.Log($"Moved PacStudent to walkable position: {testPos}");
                        return;
                    }
                }
            }
        }
        Debug.LogError("Could not find walkable starting position!");
    }

    private void SetupParticles()
    {
        GameObject particleObj = new GameObject("DustParticles");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = new Vector3(0, 0, -0.5f);

        dustParticles = particleObj.AddComponent<ParticleSystem>();
        var renderer = dustParticles.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 10;

        // Material
        Material dustMaterial = new Material(Shader.Find("Sprites/Default"));
        dustMaterial.color = new Color(0f, 1f, 1f,1f);
        renderer.material = dustMaterial;


        // Configure main module
        var main = dustParticles.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 2f;
        main.startSize = 0.2f;
        main.startColor = new Color(0f, 1f, 1f, 1f);
        main.maxParticles = 4;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Configure emission
        var emission = dustParticles.emission;
        emission.rateOverTime = 20f;

        var shape = dustParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.2f;
        shape.position = Vector3.zero;

        // Dust effect Velocity
        var velocityOverLifetime = dustParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(1f);

        // Size over lifetime
        var sizeOverLifetime = dustParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(1f, 1f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Start without particles
        dustParticles.Stop();
    }
}