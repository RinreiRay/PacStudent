using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PowerPelletManager : MonoBehaviour
{
    [SerializeField] private float powerPelletDuration = 10f;
    [SerializeField] private float recoveringTime = 3f;
    [SerializeField] private AudioClip scaredMusic;
    [SerializeField] private AudioClip deadGhostMusic;

    private bool isPowerPelletActive = false;
    private float powerPelletTimer = 0f;
    private bool hasTriggeredRecovering = false;
    private List<GhostController> ghosts = new List<GhostController>();
    private AudioController audioController;

    public static PowerPelletManager Instance { get; private set; }

    public enum GhostState
    {
        Normal,
        Scared,
        Recovering,
        Dead
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Find all ghosts in the scene
        GhostController[] ghostArray = FindObjectsByType<GhostController>(FindObjectsSortMode.None);
        ghosts.AddRange(ghostArray);

        audioController = FindFirstObjectByType<AudioController>();
    }

    void Update()
    {
        if (isPowerPelletActive)
        {
            powerPelletTimer -= Time.deltaTime;

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.UpdateGhostTimerUI();
            }

            if (!hasTriggeredRecovering && powerPelletTimer <= recoveringTime && powerPelletTimer > 0)
            {
                SetGhostsToRecovering();
                hasTriggeredRecovering = true;
            }

            // Power pellet effect ends
            if (powerPelletTimer <= 0)
            {
                EndPowerPelletEffect();
            }
        }
    }

    public void ActivatePowerPellet()
    {
        isPowerPelletActive = true;
        powerPelletTimer = powerPelletDuration;
        hasTriggeredRecovering = false;

        // Set all living ghosts to scared state
        foreach (GhostController ghost in ghosts)
        {
            if (ghost != null && ghost.GetCurrentState() != GhostState.Dead)
            {
                ghost.SetState(GhostState.Scared);
                Debug.Log($"Ghost {ghost.name} set to Scared state");
            }
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.UpdateGhostTimerUI();
        }

        // Change background music
        if (audioController != null)
        {
            audioController.PlayScaredMusic();
        }

        Debug.Log($"Power pellet activated! Duration: {powerPelletDuration}s, Recovery starts at: {recoveringTime}s");
    }

    private void SetGhostsToRecovering()
    {
        int ghostsChanged = 0;

        foreach (GhostController ghost in ghosts)
        {
            if (ghost != null && ghost.GetCurrentState() == GhostState.Scared)
            {
                ghost.SetState(GhostState.Recovering);
                ghostsChanged++;
                Debug.Log($"Ghost {ghost.name} changed to Recovering state");
            }
            else if (ghost != null)
            {
                Debug.Log($"Ghost {ghost.name} not changed - current state: {ghost.GetCurrentState()}");
            }
        }

        Debug.Log($"Recovery phase started: {ghostsChanged} ghosts changed to Recovering state");
    }

    private void EndPowerPelletEffect()
    {
        isPowerPelletActive = false;
        hasTriggeredRecovering = false;

        // Set all scared/recovering ghosts back to normal (except dead ones)
        foreach (GhostController ghost in ghosts)
        {
            if (ghost != null && ghost.GetCurrentState() != GhostState.Dead)
            {
                ghost.SetState(GhostState.Normal);
                Debug.Log($"Ghost {ghost.name} returned to Normal state");
            }
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.UpdateGhostTimerUI();
        }

        // Restore background music
        if (audioController != null)
        {
            audioController.PlayNormalMusic();
        }

        Debug.Log("Power pellet effect ended - all ghosts returned to normal");
    }

    public void ResetPowerPelletEffects()
    {
        isPowerPelletActive = false;
        powerPelletTimer = 0f;
        hasTriggeredRecovering = false;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.UpdateGhostTimerUI();
        }

        foreach (GhostController ghost in ghosts)
        {
            if (ghost != null)
            {
                ghost.SetState(GhostState.Normal);
                Debug.Log($"Ghost {ghost.name} reset to Normal state");
            }
        }

        if (audioController != null)
        {
            audioController.PlayNormalMusic();
        }

        Debug.Log("Power pellet effects reset - all flags cleared");
    }

    public void OnGhostKilled(GhostController ghost)
    {
        if (ghost != null && ghost.GetCurrentState() != GhostState.Dead)
        {
            Debug.Log($"Ghost {ghost.name} killed by player");

            ghost.SetState(GhostState.Dead);

            // Disable collision temporarily
            Collider2D ghostCollider = ghost.GetComponent<Collider2D>();
            if (ghostCollider != null)
            {
                ghostCollider.enabled = false;
            }

            // Change music to dead ghost music
            if (audioController != null)
            {
                audioController.PlayDeadGhostMusic();
            }

            StartCoroutine(RespawnGhost(ghost));
        }
    }

    private IEnumerator RespawnGhost(GhostController ghost)
    {
        yield return new WaitForSeconds(3f);

        if (ghost != null)
        {
            // Re-enable collision
            Collider2D ghostCollider = ghost.GetComponent<Collider2D>();
            if (ghostCollider != null)
            {
                ghostCollider.enabled = true;
            }

            if (isPowerPelletActive)
            {
                if (hasTriggeredRecovering || powerPelletTimer <= recoveringTime)
                {
                    ghost.SetState(GhostState.Recovering);
                    Debug.Log($"Ghost {ghost.name} respawned in Recovering state");
                }
                else
                {
                    ghost.SetState(GhostState.Scared);
                    Debug.Log($"Ghost {ghost.name} respawned in Scared state");
                }
            }
            else
            {
                ghost.SetState(GhostState.Normal);
                Debug.Log($"Ghost {ghost.name} respawned in Normal state");
            }

            // Reset ghost position
            ghost.ResetToInitialPosition();

            if (audioController != null)
            {
                if (isPowerPelletActive)
                {
                    audioController.PlayScaredMusic();
                }
                else
                {
                    audioController.PlayNormalMusic();
                }
            }

            Debug.Log($"Ghost {ghost.name} respawned");
        }
    }

    public bool IsPowerPelletActive()
    {
        return isPowerPelletActive;
    }

    public float GetRemainingTime()
    {
        return powerPelletTimer;
    }
}