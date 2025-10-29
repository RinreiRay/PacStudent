using System.Collections;
using UnityEngine;

public class GhostController : MonoBehaviour
{
    [SerializeField] private Vector3 initialPosition;
    [SerializeField] private GhostType ghostType = GhostType.Red;

    private PowerPelletManager.GhostState currentState = PowerPelletManager.GhostState.Normal;
    private Vector2Int currentDirection = Vector2Int.right;
    private Animator animator;
    private Rigidbody2D rb2D;
    private bool movementEnabled = false;

    public enum GhostType
    {
        Red,
        Orange, 
        Pink,
        Gold
    }

    void Start()
    {
        // Store initial position
        initialPosition = transform.position;

        animator = GetComponent<Animator>();

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

        gameObject.tag = "Ghost";

        SetState(PowerPelletManager.GhostState.Normal);
        SetDirection(Vector2Int.right);
    }

    void Update()
    {
        UpdateAnimatorParameters();
    }

    public void SetState(PowerPelletManager.GhostState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        UpdateAnimatorState();
        Debug.Log($"Ghost {gameObject.name} state changed to: {newState}");
    }

    public PowerPelletManager.GhostState GetCurrentState()
    {
        return currentState;
    }

    public void SetDirection(Vector2Int newDirection)
    {
        currentDirection = newDirection;
        UpdateAnimatorDirection();
    }

    public Vector2Int GetCurrentDirection()
    {
        return currentDirection;
    }

    private void UpdateAnimatorParameters()
    {
        if (animator == null) return;

        animator.SetBool("IsMoving", movementEnabled);

        if (currentState == PowerPelletManager.GhostState.Normal)
        {
            UpdateAnimatorDirection();
        }

        UpdateAnimatorState();
    }

    private void UpdateAnimatorDirection()
    {
        if (animator == null) return;

        int directionIndex = GetDirectionIndex(currentDirection);
        animator.SetInteger("Direction", directionIndex);
    }

    private void UpdateAnimatorState()
    {
        if (animator == null) return;

        int stateIndex = (int)currentState;
        animator.SetInteger("GhostState", stateIndex);
    }

    private int GetDirectionIndex(Vector2Int direction)
    {
        if (direction == Vector2Int.right) return 0;
        if (direction == Vector2Int.left) return 1;
        if (direction == Vector2Int.up) return 2;
        if (direction == Vector2Int.down) return 3;
        return 0; // Default to right
    }

    public void ResetToInitialPosition()
    {
        transform.position = initialPosition;
        SetDirection(Vector2Int.right); // Reset to default direction
        Debug.Log($"Ghost {gameObject.name} reset to initial position: {initialPosition}");
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
        Debug.Log($"Ghost {gameObject.name} movement enabled: {enabled}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            HandlePlayerCollision();
        }
    }

    private void HandlePlayerCollision()
    {
        PacStudentController pacStudent = FindFirstObjectByType<PacStudentController>();
        if (pacStudent == null) return;

        Debug.Log($"Ghost {gameObject.name} collision with player - Current state: {currentState}");
        pacStudent.OnGhostCollision(gameObject, currentState);
    }

}