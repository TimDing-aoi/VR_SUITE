using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RewardArena;

public class UpdateScene : MonoBehaviour
{
    public GameObject distalObj;
    public ParticleSystem particleSystem;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    async void UpdateExp()
    {
        await new WaitForEndOfFrame();
        // These settings still need to be implemented:
        // Distal Object Type
        // Distal Object Height
        // Distal Object Width

        // Settings below that are commented out still need to be added to the menu

        SharedReward.c_lambda = 1.0f / PlayerPrefs.GetFloat("Mean 1");
        SharedReward.i_lambda = 1.0f / PlayerPrefs.GetFloat("Mean 2");
        SharedReward.checkMin = PlayerPrefs.GetFloat("Minimum Wait to Check");
        SharedReward.checkMax = PlayerPrefs.GetFloat("Maximum Wait to Check");
        SharedReward.interMin = PlayerPrefs.GetFloat("Minimum Intertrial Wait");
        SharedReward.interMax = PlayerPrefs.GetFloat("Maximum Intertrial Wait");
        SharedReward.c_min = SharedReward.Tcalc(SharedReward.checkMin, SharedReward.c_lambda);
        SharedReward.c_max = SharedReward.Tcalc(SharedReward.checkMax, SharedReward.c_lambda);
        SharedReward.i_min = SharedReward.Tcalc(SharedReward.interMin, SharedReward.i_lambda);
        SharedReward.i_max = SharedReward.Tcalc(SharedReward.interMax, SharedReward.i_lambda);
        SharedReward.velMin = PlayerPrefs.GetFloat("Min Linear Speed");
        SharedReward.velMax = PlayerPrefs.GetFloat("Max Linear Speed");
        SharedReward.rotMin = PlayerPrefs.GetFloat("Min Angular Speed");
        SharedReward.rotMax = PlayerPrefs.GetFloat("Max Angular Speed");
        //SharedReward.dim = PlayerPrefs.GetInt("Dimensions");
        SharedReward.minDrawDistance = PlayerPrefs.GetFloat("Minimum Firefly Distance");
        SharedReward.maxDrawDistance = PlayerPrefs.GetFloat("Maximum Firefly Distance");
        SharedReward.LR = PlayerPrefs.GetFloat("Left Right");
        if (SharedReward.LR == 0.5f)
        {
            SharedReward.maxPhi = PlayerPrefs.GetFloat("Max Angle");
            SharedReward.minPhi = -SharedReward.maxPhi;
        }
        else
        {
            SharedReward.maxPhi = PlayerPrefs.GetFloat("Max Angle");
            SharedReward.minPhi = PlayerPrefs.GetFloat("Min Angle");
        }
        SharedReward.fireflyZoneRadius = PlayerPrefs.GetFloat("Reward Zone Radius");
        SharedReward.fireflySize = PlayerPrefs.GetFloat("Size");
        SharedReward.firefly.transform.localScale = new Vector3(SharedReward.fireflySize, SharedReward.fireflySize, 1);
        SharedReward.ratio = PlayerPrefs.GetFloat("Ratio");
        //SharedReward.ramp = PlayerPrefs.GetInt("Ramp") == 1;
        //SharedReward.rampTime = PlayerPrefs.GetFloat("Ramp Time");
        //SharedReward.rampDelay = PlayerPrefs.GetFloat("Ramp Delay");
        SharedReward.velocities.Add(PlayerPrefs.GetFloat("V1"));
        SharedReward.velocities.Add(PlayerPrefs.GetFloat("V2"));
        SharedReward.velocities.Add(PlayerPrefs.GetFloat("V3"));
        SharedReward.velocities.Add(PlayerPrefs.GetFloat("V4"));
        SharedReward.velocities.Add(PlayerPrefs.GetFloat("V5"));
        SharedReward.velocities.Add(PlayerPrefs.GetFloat("V6"));
        SharedReward.velocities.Add(PlayerPrefs.GetFloat("V7"));
        SharedReward.velocities.Add(PlayerPrefs.GetFloat("V8"));
        SharedReward.velocities.Add(PlayerPrefs.GetFloat("V9"));
        SharedReward.velocities.Add(PlayerPrefs.GetFloat("V10"));
        SharedReward.velocities.Add(PlayerPrefs.GetFloat("V11"));
        SharedReward.velocities.Add(PlayerPrefs.GetFloat("V12"));

        SharedReward.v_ratios.Add(PlayerPrefs.GetFloat("VR1"));
        SharedReward.v_ratios.Add(PlayerPrefs.GetFloat("VR2"));
        SharedReward.v_ratios.Add(PlayerPrefs.GetFloat("VR3"));
        SharedReward.v_ratios.Add(PlayerPrefs.GetFloat("VR4"));
        SharedReward.v_ratios.Add(PlayerPrefs.GetFloat("VR5"));
        SharedReward.v_ratios.Add(PlayerPrefs.GetFloat("VR6"));
        SharedReward.v_ratios.Add(PlayerPrefs.GetFloat("VR7"));
        SharedReward.v_ratios.Add(PlayerPrefs.GetFloat("VR8"));
        SharedReward.v_ratios.Add(PlayerPrefs.GetFloat("VR9"));
        SharedReward.v_ratios.Add(PlayerPrefs.GetFloat("VR10"));
        SharedReward.v_ratios.Add(PlayerPrefs.GetFloat("VR11"));
        SharedReward.v_ratios.Add(PlayerPrefs.GetFloat("VR12"));

        for (int i = 1; i < 12; i++)
        {
            SharedReward.v_ratios[i] = SharedReward.v_ratios[i] + SharedReward.v_ratios[i - 1];
        }

        SharedReward.durations.Add(PlayerPrefs.GetFloat("D1"));
        SharedReward.durations.Add(PlayerPrefs.GetFloat("D2"));
        SharedReward.durations.Add(PlayerPrefs.GetFloat("D3"));
        SharedReward.durations.Add(PlayerPrefs.GetFloat("D4"));
        SharedReward.durations.Add(PlayerPrefs.GetFloat("D5"));

        SharedReward.ratios.Add(PlayerPrefs.GetFloat("R1"));
        SharedReward.ratios.Add(PlayerPrefs.GetFloat("R2"));
        SharedReward.ratios.Add(PlayerPrefs.GetFloat("R3"));
        SharedReward.ratios.Add(PlayerPrefs.GetFloat("R4"));
        SharedReward.ratios.Add(PlayerPrefs.GetFloat("R5"));

        for (int i = 1; i < 5; i++)
        {
            SharedReward.ratios[i] = SharedReward.ratios[i] + SharedReward.ratios[i - 1];
        }

        SharedReward.isMoving = PlayerPrefs.GetInt("Moving ON") == 1;
        SharedReward.LRFB = PlayerPrefs.GetInt("VertHor") == 0;
        try
        {
            switch (PlayerPrefs.GetString("Switch Behavior"))
            {
                case "always on":
                    SharedReward.mode = Modes.ON;
                    break;
                case "flashing":
                    SharedReward.mode = Modes.Flash;
                    SharedReward.freq = PlayerPrefs.GetFloat("Frequency");
                    SharedReward.duty = PlayerPrefs.GetFloat("Duty Cycle") / 100f;
                    SharedReward.PW = SharedReward.duty / SharedReward.freq;
                    break;
                case "fixed":
                    SharedReward.mode = Modes.Fixed;
                    break;
                default:
                    throw new System.Exception("No mode selected, defaulting to FIXED");
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.Log(e, this);
            SharedReward.mode = Modes.Fixed;
        }
        SharedReward.timeout = PlayerPrefs.GetFloat("Timeout");
        //SharedReward.rewardAmt = PlayerPrefs.GetFloat("Reward");
        var lifeSpan = PlayerPrefs.GetFloat("Life Span");
        var dist = PlayerPrefs.GetFloat("Draw Distance");
        var density = PlayerPrefs.GetFloat("Density");
        var p_height = PlayerPrefs.GetFloat("Triangle Height");
        float baseH = 0.0185f;

        var main = particleSystem.main;
        var emission = particleSystem.emission;
        var shape = particleSystem.shape;

        main.startLifetime = lifeSpan;
        main.startSize = p_height * baseH;
        main.maxParticles = Mathf.RoundToInt(Mathf.Pow(dist, 2.0f) * Mathf.PI * density / p_height);//Mathf.Pow(t_height, 2.0f));

        emission.rateOverTime = Mathf.CeilToInt(main.maxParticles / 10000.0f) / lifeSpan * 10000.0f / (p_height);// Mathf.Pow(t_height, 2.0f);

        shape.randomPositionAmount = dist;
    }
}
