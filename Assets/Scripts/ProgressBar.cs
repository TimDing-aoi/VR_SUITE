using UnityEngine;
using UnityEngine.Events;
using static Reward2D;

public class ProgressBar : FillBar
{
    public Canvas bar_canvas;
    public float maxFFR;
    public float minFFR;

    void Start()
    {
        minFFR = PlayerPrefs.GetFloat("Minimum Firefly Distance");
        maxFFR = PlayerPrefs.GetFloat("Maximum Firefly Distance");
    }

    void Update()
    {
        float progress = Vector3.Distance(new Vector3(0f, 0f, 0f), SharedReward.player.transform.position) / ((maxFFR+minFFR)/2);
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