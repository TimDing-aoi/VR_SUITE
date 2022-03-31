using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class timelinestamps : MonoBehaviour
{
    public static timelinestamps sharedTimeStamps;
    public float preparation_1 = 0.1f;
    public float preparation_2 = 0.2f;
    public float habituation_1 = 0.1f;
    public float habituation_2 = 0.2f;
    public float habituation_3 = 0.05f;
    public float observation = 0.3f;
    public float feedback = 0.2f;
    public float preparation_total = 0.3f;
    public float habituation_total = 0.35f;
    private void Start()
    {
        sharedTimeStamps = this;
    }
}