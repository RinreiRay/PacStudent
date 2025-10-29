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
    private bool movementEnabled = false;
    private bool isPlayingDeathAnimation = false;
    private Vector2Int currentGridPosition;

    private Animator animator;
    private AudioSource audioSource;
    private ParticleSystem dustParticles;

    [SerializeField] private AudioClip movementSound;
    [SerializeField] private AudioClip pelletEatingSound;
    [SerializeField] private AudioClip wallCollisionSound;

    private int[,] levelMap;
    private GameObject levelGenerator;
    private LevelGenerator levelGen;

    private Tweener tweener;

    private HashSet<Vector2Int> collectedPellets = new HashSet<Vector2Int>();

    private Vector2Int lastWallCollisionPosition = new Vector2Int(int.MinValue, int.MinValue);
    private Vector2Int lastAttemptedDirection = Vector2Int.zero;
    private bool hasTriggeredWallCollision = false;

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
        if (isPlayingDeathAnimation)
        {
            UpdateAnimationState();
            return;
        }

        GatherInput();

        // Allow movement after first input
        if (!isLerping && hasReceivedFirstInput && movementEnabled)
        {
            TryMove();
        }

        // Update animations based on movement state
        UpdateAnimationState();
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
        Debug.Log($"PacStudent movement enabled: {enabled}");
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

            if (newInput != lastAttemptedDirection)
            {
                ResetWallCollisionTracking();
                lastAttemptedDirection = newInput;
            }

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
                ResetWallCollisionTracking();
                StartTweenMovement(targetGrid);
                UpdateAnimationDirection(GetWorldDirection(currentInput));
                return;
            }
            else
            {
                // Check for wall collision when movement is blocked (only once)
                CheckWallCollisionAtPosition(targetGrid, lastInput);
            }
        }

        // If lastInput direction is blocked, try currentInput direction
        if (currentInput != Vector2Int.zero && currentInput != lastInput)
        {
            Vector2Int targetGrid = currentGridPosition + currentInput;

            if (IsWalkable(targetGrid))
            {
                // Reset wall collision tracking when movement succeeds
                ResetWallCollisionTracking();
                StartTweenMovement(targetGrid);
                UpdateAnimationDirection(GetWorldDirection(currentInput));
                return;
            }
            else
            {
                CheckWallCollisionAtPosition(targetGrid, currentInput);
            }
        }

        // No valid moves - PacStudent stops
    }

    private void ResetWallCollisionTracking()
    {
        hasTriggeredWallCollision = false;
        lastWallCollisionPosition = new Vector2Int(int.MinValue, int.MinValue);
    }

    private void CheckWallCollisionAtPosition(Vector2Int gridPos, Vector2Int direction)
    {
        if (hasTriggeredWallCollision && lastWallCollisionPosition == gridPos)
        {
            return; // Don't trigger again
        }

        // Check if the blocked position is actually a wall
        if (levelMap != null &&
            gridPos.y >= 0 && gridPos.y < levelMap.GetLength(0) &&
            gridPos.x >= 0 && gridPos.x < levelMap.GetLength(1))
        {
            int tileValue = levelMap[gridPos.y, gridPos.x];

            // If it's a wall tile, trigger wall collision
            if (IsWall(tileValue))
            {
                // Mark as handled
                hasTriggeredWallCollision = true;
                lastWallCollisionPosition = gridPos;

                Vector3 wallWorldPos = GridToWorld(gridPos);
                OnWallCollision(wallWorldPos);

                Debug.Log($"Wall collision triggered at {gridPos} in direction {direction}");
            }
        }
    }

    private bool IsWall(int tileValue)
    {
        return tileValue == 1 || tileValue == 2 || tileValue == 3 ||
               tileValue == 4 || tileValue == 7 || tileValue == 8;
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
            if (isPlayingDeathAnimation)
            {
                animator.speed = 1f;
                return;
            }

            // Only animate when lerping (moving) and movement is enabled
            animator.speed = (isLerping && movementEnabled) ? 1f : 0f;
        }

        // Handle particle effects
        if ((!isLerping || !movementEnabled || isPlayingDeathAnimation) && dustParticles != null && dustParticles.isPlaying)
        {
            dustParticles.Stop();
        }
    }

    private void StartTweenMovement(Vector2Int targetGrid)
    {
        if (tweener == null)
        {
            Debug.LogError("Cannot move: Tweener is null!");
            return;
        }

        if (isLerping)
        {
            Debug.LogWarning("Already lerping, ignoring new movement request");
            return;
        }

        isLerping = true;
        Vector3 targetPos = GridToWorld(targetGrid);
        float duration = 1f / moveSpeed;

        bool tweenAdded = tweener.AddTween(transform, transform.position, targetPos, duration);

        if (!tweenAdded)
        {
            Debug.LogError("Failed to add tween!");
            isLerping = false;
            return;
        }

        currentGridPosition = targetGrid;

        // Start particle effect
        if (dustParticles != null && !dustParticles.isPlaying)
        {
            dustParticles.Play();
        }

        PlayMovementAudio(targetGrid);

        CheckForPelletAtPosition(targetGrid);

        StartCoroutine(WaitForTweenCompletion(duration));
    }

    private void PlayMovementAudio(Vector2Int targetGrid)
    {
        if (audioSource == null) return;

        // Check if there's a pellet at target position
        bool hasPellet = false;
        if (levelMap != null &&
            targetGrid.y >= 0 && targetGrid.y < levelMap.GetLength(0) &&
            targetGrid.x >= 0 && targetGrid.x < levelMap.GetLength(1))
        {
            int tileValue = levelMap[targetGrid.y, targetGrid.x];
            hasPellet = (tileValue == 5 || tileValue == 6) && !collectedPellets.Contains(targetGrid);
        }

        if (hasPellet && pelletEatingSound != null)
        {
            audioSource.PlayOneShot(pelletEatingSound);
        }
        else if (movementSound != null)
        {
            audioSource.PlayOneShot(movementSound);
        }
    }

    private IEnumerator WaitForTweenCompletion(float duration)
    {
        yield return new WaitForSeconds(duration);
        isLerping = false;
    }

    private void CheckForPelletAtPosition(Vector2Int gridPos)
    {
        if (collectedPellets.Contains(gridPos)) return;

        if (levelMap != null &&
            gridPos.y >= 0 && gridPos.y < levelMap.GetLength(0) &&
            gridPos.x >= 0 && gridPos.x < levelMap.GetLength(1))
        {
            int tileValue = levelMap[gridPos.y, gridPos.x];

            if (tileValue == 5 || tileValue == 6)
            {
                // Mark as collected
                collectedPellets.Add(gridPos);
                levelMap[gridPos.y, gridPos.x] = 0;

                // Add score based on pellet type
                if (ScoreManager.Instance != null)
                {
                    if (tileValue == 6) // Power pellet
                    {
                        ScoreManager.Instance.AddPowerPelletScore();

                        // Activate power pellet effect
                        if (PowerPelletManager.Instance != null)
                        {
                            PowerPelletManager.Instance.ActivatePowerPellet();
                        }

                        Debug.Log("Power pellet collected - 50 points added and power activated");
                    }
                    else // Normal pellet
                    {
                        ScoreManager.Instance.AddPelletScore();
                        Debug.Log("Normal pellet collected - 10 points added");
                    }
                }

                // Find and destroy the pellet GameObject
                DestroyPelletAtPosition(gridPos);

                Debug.Log($"{(tileValue == 6 ? "Power " : "")}Pellet collected at {gridPos}");
            }
        }
    }

    private void DestroyPelletAtPosition(Vector2Int gridPos)
    {
        Vector3 worldPos = GridToWorld(gridPos);
        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, 0.3f);

        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Pellet") || col.CompareTag("PowerPellet"))
            {
                Destroy(col.gameObject);
                break;
            }
        }
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

        // Check if it's walkable (including pellets)
        return tileValue == 0 || tileValue == 5 || tileValue == 6;
    }

    // Called by CollisionHandler
    public void OnWallCollision(Vector3 wallPosition)
    {
        Debug.Log($"Wall collision handled by PacStudent at position: {wallPosition}");

        // Play wall collision sound
        if (wallCollisionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(wallCollisionSound);
            Debug.Log("Playing wall collision sound");
        }
        else
        {
            Debug.LogWarning("Wall collision sound or audio source is null");
        }

        // Create particle effect at collision point
        CreateWallCollisionEffect(wallPosition);
    }

    public void OnPelletCollision(GameObject pellet, bool isPowerPellet)
    {
        // Check if pellet still exists
        if (pellet == null)
        {
            Debug.Log("Pellet is null, already destroyed");
            return;
        }

        // Get pellet position and check if already collected
        Vector2Int pelletGridPos = WorldToGrid(pellet.transform.position);

        if (collectedPellets.Contains(pelletGridPos))
        {
            Debug.Log("Pellet already collected, destroying GameObject");
            Destroy(pellet);
            return;
        }

        Debug.Log($"Pellet collision handled: {(isPowerPellet ? "Power Pellet" : "Normal Pellet")}");

        // Mark as collected
        collectedPellets.Add(pelletGridPos);

        // Update level map
        if (levelMap != null &&
            pelletGridPos.y >= 0 && pelletGridPos.y < levelMap.GetLength(0) &&
            pelletGridPos.x >= 0 && pelletGridPos.x < levelMap.GetLength(1))
        {
            levelMap[pelletGridPos.y, pelletGridPos.x] = 0;
        }

        // Add score through ScoreManager then destroy
        if (ScoreManager.Instance != null)
        {
            if (isPowerPellet)
            {
                ScoreManager.Instance.AddPowerPelletScore();
                Debug.Log("Power pellet collision - 50 points added");
            }
            else
            {
                ScoreManager.Instance.AddPelletScore();
                Debug.Log("Normal pellet collision - 10 points added");
            }
        }

        Destroy(pellet);
    }

    public void OnGhostCollision(GameObject ghost, PowerPelletManager.GhostState ghostState)
    {
        Debug.Log($"Ghost collision handled - Ghost state: {ghostState}");

        switch (ghostState)
        {
            case PowerPelletManager.GhostState.Normal:
                // Player dies
                if (LifeManager.Instance != null)
                {
                    LifeManager.Instance.LoseLife();
                }
                break;

            case PowerPelletManager.GhostState.Scared:
            case PowerPelletManager.GhostState.Recovering:
                // PacStudent eats the ghost
                GhostController ghostController = ghost.GetComponent<GhostController>();
                if (ghostController != null)
                {
                    // Kill the ghost through PowerPelletManager
                    if (PowerPelletManager.Instance != null)
                    {
                        PowerPelletManager.Instance.OnGhostKilled(ghostController);
                    }

                    // Add score
                    if (ScoreManager.Instance != null)
                    {
                        ScoreManager.Instance.AddGhostScore();
                    }

                    Debug.Log($"Ghost {ghost.name} eaten by PacStudent!");
                }
                break;

            case PowerPelletManager.GhostState.Dead:
                // No effect - dead ghosts don't affect player
                Debug.Log("Collision with dead ghost - no effect");
                break;
        }
    }

    public void ResetToStartPosition()
    {

        isPlayingDeathAnimation = false;

        // Reset to top-left corner (starting position)
        Vector2Int startGridPos = new Vector2Int(1, 1);
        currentGridPosition = startGridPos;
        transform.position = GridToWorld(startGridPos);

        // Reset movement state
        currentInput = Vector2Int.zero;
        lastInput = Vector2Int.zero;
        hasReceivedFirstInput = false;
        isLerping = false;

        // Reset collision tracking
        ResetWallCollisionTracking();

        // Reset animation to face right and ensure animator is ready
        if (animator != null)
        {
            animator.speed = 1f;
            UpdateAnimationDirection(Vector2Int.right);

            // Reset any death animation state
            animator.ResetTrigger("Death");

            animator.Play("MoveRight", 0, 0f);
        }

        Debug.Log("PacStudent reset to start position");
    }

    public void PlayDeathAnimation()
    {
        if (animator != null)
        {
            isPlayingDeathAnimation = true;

            // Stop particle effects
            if (dustParticles != null && dustParticles.isPlaying)
            {
                dustParticles.Stop();
            }

            // Stop any current movement animations
            animator.speed = 1f;

            // Trigger death animation
            animator.SetTrigger("Death");

            Debug.Log("Death animation triggered");
        }
    }

    public void WaitForPlayerInput()
    {
        hasReceivedFirstInput = false;
        currentInput = Vector2Int.zero;
        lastInput = Vector2Int.zero;

        // Set facing direction but don't move
        UpdateAnimationDirection(Vector2Int.right);
        UpdateAnimationState();

        Debug.Log("Waiting for player input...");
    }

    public void OnTeleporterCollision(GameObject teleporter)
    {
        Debug.Log("Teleporter collision handled");
    }

    public void OnCherryCollision(GameObject cherry)
    {
        Debug.Log("Cherry collision handled");

        CherryController cherryController = FindFirstObjectByType<CherryController>();
        if (cherryController != null)
        {
            cherryController.OnCherryCollected();
        }
    }

    private void CreateWallCollisionEffect(Vector3 collisionPoint)
    {
        GameObject effectObj = new GameObject("WallCollisionEffect");
        effectObj.transform.position = collisionPoint;

        ParticleSystem effect = effectObj.AddComponent<ParticleSystem>();
        var main = effect.main;
        main.startLifetime = 0.3f;
        main.startSpeed = 3f;
        main.startSize = 0.1f;
        main.startColor = Color.yellow;
        main.maxParticles = 10;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = effect.emission;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0.0f, 10)
        });
        emission.rateOverTime = 0;

        var shape = effect.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;

        Destroy(effectObj, 1f);
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
        dustMaterial.color = new Color(0f, 1f, 1f, 1f);
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