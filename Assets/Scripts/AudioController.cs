using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{

    public AudioSource audio1;
    public AudioSource audio2;
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
            audio2.Play();
            startedLoop = true;
        }
    }
}
