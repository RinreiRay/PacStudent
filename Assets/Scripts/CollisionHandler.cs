using UnityEngine;
using System.Collections;

public class CollisionHandler : MonoBehaviour
{
    private PacStudentController pacStudent;
    private Rigidbody2D rb2D;

    void Start()
    {
        pacStudent = GetComponent<PacStudentController>();

        rb2D = GetComponent<Rigidbody2D>();
        if (rb2D == null)
        {
            rb2D = gameObject.AddComponent<Rigidbody2D>();
        }

        rb2D.bodyType = RigidbodyType2D.Kinematic;
        rb2D.gravityScale = 0f;

        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            CircleCollider2D circleCol = gameObject.AddComponent<CircleCollider2D>();
            circleCol.radius = 0.4f;
            circleCol.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }

        Debug.Log("Collision Handler initialized");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (pacStudent == null) return;

        Debug.Log($"Collision detected with: {other.name}, Tag: {other.tag}, Position: {other.transform.position}");

        // Handle different collision types
        switch (other.tag)
        {
            case "Wall":
                HandleWallCollision(other);
                break;
            case "Pellet":
                HandlePelletCollision(other, false);
                break;
            case "PowerPellet":
                HandlePelletCollision(other, true);
                break;
            case "Ghost":
                HandleGhostCollision(other);
                break;
            case "Teleporter":
                HandleTeleporterCollision(other);
                break;
            case "Cherry":
                HandleCherryCollision(other);
                break;
            default:
                Debug.Log($"Unhandled collision with tag: {other.tag}");
                break;
        }
    }

    private void HandleWallCollision(Collider2D wall)
    {
        Debug.Log($"Wall collision detected by CollisionHandler with {wall.name}");

        // Notify PacStudent controller about wall collision
        if (pacStudent != null)
        {
            pacStudent.OnWallCollision(wall.transform.position);
        }
    }

    private void HandlePelletCollision(Collider2D pellet, bool isPowerPellet)
    {
        Debug.Log($"{(isPowerPellet ? "Power " : "")}Pellet collision detected by CollisionHandler");

        // Check if pellet still exists
        if (pellet == null || pellet.gameObject == null)
        {
            Debug.Log("Pellet already destroyed by movement system");
            return;
        }

        if (pacStudent != null)
        {
            pacStudent.OnPelletCollision(pellet.gameObject, isPowerPellet);
        }
    }

    private void HandleGhostCollision(Collider2D ghost)
    {
        if (pacStudent == null) return;

        GhostController ghostController = ghost.GetComponent<GhostController>();
        PowerPelletManager.GhostState ghostState = PowerPelletManager.GhostState.Normal;

        if (ghostController != null)
        {
            ghostState = ghostController.GetCurrentState();
            Debug.Log($"Ghost collision - Ghost: {ghost.name}, State: {ghostState}");
        }
        else
        {
            Debug.LogWarning($"Ghost {ghost.name} has no GhostController component!");
        }

        pacStudent.OnGhostCollision(ghost.gameObject, ghostState);
    }

    private void HandleTeleporterCollision(Collider2D teleporter)
    {
        if (pacStudent != null)
        {
            pacStudent.OnTeleporterCollision(teleporter.gameObject);
        }
    }

    private void HandleCherryCollision(Collider2D cherry)
    {
        if (pacStudent != null)
        {
            pacStudent.OnCherryCollision(cherry.gameObject);
        }

        // Add cherry score
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddCherryScore();
        }
    }
}