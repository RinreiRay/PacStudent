using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LifeManager : MonoBehaviour
{
    [SerializeField] private int maxLives = 3;
    [SerializeField] private int currentLives;
    [SerializeField] private GameObject livesPanel;
    [SerializeField] private GameObject[] heartSprites;
    [SerializeField] private GameObject deathParticlesPrefab;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private float deathAnimationBuffer = 0f;

    private PacStudentController pacStudent;
    private AudioSource audioSource;

    public static LifeManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            currentLives = maxLives;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        pacStudent = FindFirstObjectByType<PacStudentController>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        UpdateLifeUI();
    }

    public void LoseLife()
    {
        if (currentLives > 0)
        {
            currentLives--;
            UpdateLifeUI();

            Debug.Log($"Remaining lives: {currentLives}");

            if (currentLives <= 0)
            {
                // Game Over
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerGameOver();
                }
            }
            else
            {
                StartCoroutine(HandlePlayerDeath());
            }
        }
    }

    private IEnumerator HandlePlayerDeath()
    {
        // Disable movement for all entities
        if (pacStudent != null)
        {
            pacStudent.SetMovementEnabled(false);
        }

        // Disable ghost movement
        GhostController[] ghosts = FindObjectsByType<GhostController>(FindObjectsSortMode.None);
        foreach (GhostController ghost in ghosts)
        {
            ghost.SetMovementEnabled(false);
        }

        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        if (deathParticlesPrefab != null && pacStudent != null)
        {
            GameObject particles = Instantiate(deathParticlesPrefab, pacStudent.transform.position, Quaternion.identity);
            Destroy(particles, 3f);
        }

        if (pacStudent != null)
        {
            pacStudent.PlayDeathAnimation();

            yield return StartCoroutine(WaitForDeathAnimationComplete());
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        RespawnPlayer();
    }

    private IEnumerator WaitForDeathAnimationComplete()
    {
        if (pacStudent == null) yield break;

        Animator animator = pacStudent.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("No animator found on PacStudent, using fallback timer");
            yield return new WaitForSeconds(1.5f);
            yield break;
        }

        // Wait one frame for animation to start
        yield return null;
        float actualDeathAnimationTime = 1.2f; // 1.2 seconds

        yield return new WaitForSeconds(actualDeathAnimationTime + deathAnimationBuffer);

        Debug.Log($"Death animation completed after {actualDeathAnimationTime} seconds");
    }

    private void RespawnPlayer()
    {
        // Reset PacStudent to starting position
        if (pacStudent != null)
        {
            pacStudent.ResetToStartPosition();
        }

        // Reset all ghosts to their initial positions and normal state
        GhostController[] ghosts = FindObjectsByType<GhostController>(FindObjectsSortMode.None);
        foreach (GhostController ghost in ghosts)
        {
            ghost.ResetToInitialPosition();
            ghost.SetState(PowerPelletManager.GhostState.Normal);
            ghost.SetMovementEnabled(true);
        }

        // Reset power pellet effects
        if (PowerPelletManager.Instance != null)
        {
            PowerPelletManager.Instance.ResetPowerPelletEffects();
        }

        // Re-enable player movement (wait for input)
        if (pacStudent != null)
        {
            pacStudent.SetMovementEnabled(true);
            pacStudent.WaitForPlayerInput();
        }

        Debug.Log("Player respawned");
    }

    private void UpdateLifeUI()
    {
        if (heartSprites == null) return;

        for (int i = 0; i < heartSprites.Length; i++)
        {
            if (heartSprites[i] != null)
            {
                heartSprites[i].SetActive(i < currentLives);
            }
        }
    }

    public int GetCurrentLives()
    {
        return currentLives;
    }

    public void AddLife()
    {
        if (currentLives < maxLives)
        {
            currentLives++;
            UpdateLifeUI();
        }
    }
}