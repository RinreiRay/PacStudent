using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource audio1; // Intro music
    public AudioSource audio2; // Main loop music

    [Header("Music Clips")]
    [SerializeField] private AudioClip normalMusic;
    [SerializeField] private AudioClip scaredMusic;
    [SerializeField] private AudioClip deadGhostMusic;

    [SerializeField] private float delayAudio = 0.5f;

    private bool startedLoop;
    private AudioClip currentBackgroundMusic;

    public static AudioController Instance { get; private set; }

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

    // Start is called before the first frame update
    void Start()
    {
        currentBackgroundMusic = normalMusic;
        audio1.Play();
    }

    // Update is called once per frame
    void Update()
    {
        if (!audio1.isPlaying && !startedLoop)
        {
            StartCoroutine(PlayLoop());
            startedLoop = true;
        }
    }

    private IEnumerator PlayLoop()
    {
        yield return new WaitForSeconds(delayAudio);

        if (currentBackgroundMusic != null)
        {
            audio2.clip = currentBackgroundMusic;
            audio2.loop = true;
            audio2.Play();
        }
    }

    public void PlayNormalMusic()
    {
        ChangeBackgroundMusic(normalMusic);
    }

    public void PlayScaredMusic()
    {
        ChangeBackgroundMusic(scaredMusic);
    }

    public void PlayDeadGhostMusic()
    {
        ChangeBackgroundMusic(deadGhostMusic);
    }

    private void ChangeBackgroundMusic(AudioClip newClip)
    {
        if (newClip == null || currentBackgroundMusic == newClip) return;

        currentBackgroundMusic = newClip;

        if (startedLoop && audio2 != null)
        {
            audio2.clip = currentBackgroundMusic;
            audio2.loop = true;
            audio2.Play();
        }

        Debug.Log($"Background music changed to: {newClip.name}");
    }

    public void StopAllMusic()
    {
        if (audio1 != null) audio1.Stop();
        if (audio2 != null) audio2.Stop();
    }
}