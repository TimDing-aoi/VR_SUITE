using UnityEngine;
using UnityEngine.Events;
using static Reward2D;

public class ProgressBar : FillBar
{
    void Start()
    {

    }

    void Update()
    {
        float progress = Vector3.Distance(new Vector3(0f, 0f, 0f), SharedReward.player.transform.position) / 30;
        if (progress > 1)
        {
            progress = 1;
        }
        if (progress < 0.04)
        {
            progress = 0;
        }
        var objectScale = transform.localScale;
        // Sets the local scale of game object
        transform.localScale = new Vector3(progress, objectScale.y, objectScale.z);
    }

}