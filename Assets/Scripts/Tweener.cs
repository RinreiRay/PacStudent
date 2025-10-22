using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tweener : MonoBehaviour
{
    //private Tween activeTween;
    private List<Tween> activeTweens;

    // Start is called before the first frame update
    void Start()
    {
        activeTweens = new List<Tween>();
    }


    // Update is called once per frame
    void Update()
    {
        for (int i = activeTweens.Count - 1; i >= 0; i--)
        {
            Tween currentTween = activeTweens[i];
            float currentTime = Time.time;
            float progress = (currentTime - currentTween.StartTime) / currentTween.Duration;

            progress = Mathf.Clamp01(progress);

            // Apply smoothstep easing
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            currentTween.Target.position = Vector3.Lerp(
                currentTween.StartPos,
                currentTween.EndPos,
                easedProgress
            );

            // Only remove if progress is complete
            if (progress >= 1f)
            {
                currentTween.Target.position = currentTween.EndPos;
                activeTweens.RemoveAt(i);
            }
        }
    }

    public bool AddTween(Transform targetObject, Vector3 startPos, Vector3 endPos, float duration)
    {
        // Remove any existing tween for this target
        activeTweens.RemoveAll(t => t.Target == targetObject);

        activeTweens.Add(new Tween(targetObject, startPos, endPos, Time.time, duration));
        return true;
    }
}
