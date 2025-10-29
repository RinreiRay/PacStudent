using System.Collections;
using UnityEngine;

public class TeleporterManager : MonoBehaviour
{
    [SerializeField] private GameObject teleporterPrefab;
    [SerializeField] private float teleportCooldown = 0.5f;

    private GameObject leftTeleporter;
    private GameObject rightTeleporter;
    private Vector2Int leftTeleporterGrid;
    private Vector2Int rightTeleporterGrid;
    private LevelGenerator levelGen;

    private bool teleportOnCooldown = false;

    void Start()
    {
        StartCoroutine(InitializeTeleporters());
    }

    private IEnumerator InitializeTeleporters()
    {
        // Wait for level generation to complete
        yield return new WaitForEndOfFrame();

        GameObject levelGenerator = GameObject.Find("LevelGenerator");
        if (levelGenerator != null)
        {
            levelGen = levelGenerator.GetComponent<LevelGenerator>();
            if (levelGen != null && levelGen.LevelMap != null)
            {
                FindTeleporterPositions();
                CreateTeleporters();
            }
        }
    }

    private void FindTeleporterPositions()
    {
        int[,] levelMap = levelGen.LevelMap;
        int rows = levelMap.GetLength(0);
        int cols = levelMap.GetLength(1);

        // Calculate exact middle row
        int middleRow = rows / 2;

        Debug.Log($"Map dimensions: {rows}x{cols}, Middle row: {middleRow}");

        // Check if middle row positions are walkable
        bool leftWalkable = levelMap[middleRow, 0] == 0;
        bool rightWalkable = levelMap[middleRow, cols - 1] == 0;

        if (leftWalkable && rightWalkable)
        {
            leftTeleporterGrid = new Vector2Int(0, middleRow);
            rightTeleporterGrid = new Vector2Int(cols - 1, middleRow);
            Debug.Log($"Found teleporters at exact middle row {middleRow}");
        }
        else
        {
            bool foundPositions = false;

            // Search within 2 rows of middle
            for (int offset = 1; offset <= 2 && !foundPositions; offset++)
            {
                // Try above middle
                int testRow = middleRow - offset;
                if (testRow >= 0 && levelMap[testRow, 0] == 0 && levelMap[testRow, cols - 1] == 0)
                {
                    leftTeleporterGrid = new Vector2Int(0, testRow);
                    rightTeleporterGrid = new Vector2Int(cols - 1, testRow);
                    foundPositions = true;
                    Debug.Log($"Found teleporters at row {testRow} (middle-{offset})");
                    break;
                }

                // Try below middle
                testRow = middleRow + offset;
                if (testRow < rows && levelMap[testRow, 0] == 0 && levelMap[testRow, cols - 1] == 0)
                {
                    leftTeleporterGrid = new Vector2Int(0, testRow);
                    rightTeleporterGrid = new Vector2Int(cols - 1, testRow);
                    foundPositions = true;
                    Debug.Log($"Found teleporters at row {testRow} (middle+{offset})");
                    break;
                }
            }

            if (!foundPositions)
            {
                Debug.LogError("Could not find suitable teleporter positions!");
                return;
            }
        }

        Debug.Log($"Teleporter positions - Left: {leftTeleporterGrid} (col 0), Right: {rightTeleporterGrid} (col {cols - 1})");
        Debug.Log($"Left tile value: {levelMap[leftTeleporterGrid.y, leftTeleporterGrid.x]}");
        Debug.Log($"Right tile value: {levelMap[rightTeleporterGrid.y, rightTeleporterGrid.x]}");
    }

    private void CreateTeleporters()
    {
        if (teleporterPrefab == null)
        {
            // Default teleporter prefab
            teleporterPrefab = new GameObject("TeleporterPrefab");
            teleporterPrefab.SetActive(false);
        }

        // Create left teleporter
        leftTeleporter = CreateTeleporter(leftTeleporterGrid, "LeftTeleporter");

        // Create right teleporter
        rightTeleporter = CreateTeleporter(rightTeleporterGrid, "RightTeleporter");
    }

    private GameObject CreateTeleporter(Vector2Int gridPos, string name)
    {
        Vector3 worldPos = GridToWorld(gridPos);
        GameObject teleporter = Instantiate(teleporterPrefab, worldPos, Quaternion.identity);
        teleporter.name = name;
        teleporter.SetActive(true);

        // Setup collision components
        Rigidbody2D rb = teleporter.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = teleporter.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        Collider2D col = teleporter.GetComponent<Collider2D>();
        if (col == null)
        {
            BoxCollider2D boxCol = teleporter.AddComponent<BoxCollider2D>();
            boxCol.size = Vector2.one * 1.2f;
            boxCol.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }

        teleporter.tag = "Teleporter";

        TeleportTrigger trigger = teleporter.AddComponent<TeleportTrigger>();
        trigger.Initialize(this, gridPos);

        Debug.Log($"Created teleporter '{name}' at grid {gridPos}, world {worldPos}");
        return teleporter;
    }

    public void HandleTeleportation(Vector2Int fromGrid, PacStudentController pacStudent)
    {
        if (teleportOnCooldown) return;

        Vector2Int targetGrid;
        string teleportDirection;

        // Determine destination
        if (fromGrid.Equals(leftTeleporterGrid))
        {
            targetGrid = rightTeleporterGrid;
            teleportDirection = "left to right";
        }
        else if (fromGrid.Equals(rightTeleporterGrid))
        {
            targetGrid = leftTeleporterGrid;
            teleportDirection = "right to left";
        }
        else
        {
            Debug.LogWarning($"Teleportation attempted from unknown position: {fromGrid}");
            Debug.LogWarning($"Expected positions - Left: {leftTeleporterGrid}, Right: {rightTeleporterGrid}");
            return;
        }

        // Perform teleportation
        Vector3 targetWorldPos = GridToWorld(targetGrid);
        pacStudent.TeleportTo(targetGrid, targetWorldPos);

        Debug.Log($"Teleported PacStudent {teleportDirection} from {fromGrid} to {targetGrid}");

        // Start cooldown
        StartCoroutine(TeleportCooldown());
    }

    private IEnumerator TeleportCooldown()
    {
        teleportOnCooldown = true;
        yield return new WaitForSeconds(teleportCooldown);
        teleportOnCooldown = false;
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

    public void LogTeleporterInfo()
    {
        Debug.Log($"Left Teleporter: Grid {leftTeleporterGrid}, World {GridToWorld(leftTeleporterGrid)}");
        Debug.Log($"Right Teleporter: Grid {rightTeleporterGrid}, World {GridToWorld(rightTeleporterGrid)}");
    }
}