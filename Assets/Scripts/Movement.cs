using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private AudioClip moveSound;
    private AudioSource audioSource;
    [SerializeField] private Animator animator;
    private enum Direction { Right, Down, Left, Up }
    private Direction currentDirection;
    private Tweener tweener;
    private int currentCornerIndex = 0;

    private readonly Vector3[] corners = new Vector3[]
    {
        new Vector3(0.5f, 5.5f, 0f),    // Top-left
        new Vector3(5.5f, 5.5f, 0f),    // Top-right
        new Vector3(5.5f, 1.5f, 0f),   // Bottom-right
        new Vector3(0.5f, 1.5f, 0f)  // Bottom-left
    };

    private void Awake()
    {
        animator = GetComponent<Animator>();
        tweener = GetComponent<Tweener>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = moveSound;
        audioSource.loop = true;
    }

    // Start is called before the first frame update
    private void Start()
    {
        corners[0] = transform.position;

        Vector3 offset = corners[0] - new Vector3(0.5f, 5.5f, 0f);
        for (int i = 1; i < corners.Length; i++)
        {
            corners[i] += offset;
        }

        StartMovement();
    }

    private void StartMovement()
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition = corners[(currentCornerIndex + 1) % corners.Length];
        float distance = Vector3.Distance(startPosition, endPosition);
        float duration = distance / moveSpeed;

        StartCoroutine(StartMovementSequence(startPosition, endPosition, duration));
    }

    private IEnumerator StartMovementSequence(Vector3 startPosition, Vector3 endPosition, float duration)
    {
        // Change animation state first
        SetAnimationState();

        // Wait for next frame to ensure animation state has changed
        yield return null;

        // Start audio and movement
        audioSource.Play();
        tweener.AddTween(transform, startPosition, endPosition, duration);

        // Schedule next movement
        Invoke(nameof(OnMovementComplete), duration + 0.01f);
    }

    private void OnMovementComplete()
    {
        audioSource.Stop();

        Invoke(nameof(PrepareNextMovement), 0.1f);
    }

    private void PrepareNextMovement()
    {
        currentCornerIndex = (currentCornerIndex + 1) % corners.Length;
        UpdateDirection();
        transform.position = corners[currentCornerIndex];
        StartMovement();
    }

    private void UpdateDirection()
    {
        switch (currentCornerIndex)
        {
            case 0: currentDirection = Direction.Right; break;
            case 1: currentDirection = Direction.Down; break;
            case 2: currentDirection = Direction.Left; break;
            case 3: currentDirection = Direction.Up; break;
        }
    }

    private void SetAnimationState()
    {
        string directionString = currentDirection.ToString().ToLower();
        animator.SetTrigger(directionString);
    }
}
