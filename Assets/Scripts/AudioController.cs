using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{

    public AudioSource audio1;
    public AudioSource audio2;
    [SerializeField] private float delayAudio = 0.5f;

    private bool startedLoop;

    // Start is called before the first frame update
    void Start()
    {
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

        audio2.Play();
    }
}