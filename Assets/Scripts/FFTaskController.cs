using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class FFTaskController : MonoBehaviour
{
    // FF Task Components
    [Header("Basic Task Components")]
    public GameObject firefly;
    public Camera mainCamera;
    public GameObject player;
    public ParticleSystem particleSystem;
    public AudioSource source;
    public AudioClip winSound;
    public AudioClip neutralSound;
    public AudioClip loseSound;

    // Task properties
    protected float timeout;
    protected float minCheckTime;
    protected float maxCheckTime;
    protected float minITI;
    protected float maxITI;
    protected float meanCheck;
    protected float meanITI;
    protected float minVelocity;
    protected float maxVelocity;
    protected float minRotation;
    protected float maxRotation;
    protected float playerHeight;
    protected int currTrialNum;
    protected int maxTrials;
    protected float initialStartTime;
    enum Phases
    {
        begin = 0,
        trial = 1,
        check = 2,
        question = 3,
        feedback = 4,
        juice = 3,
        ITI = 4,
        none = 9
    }
    Phases phase { get; set; }

    // Firefly Properties
    protected float fireflySize;
    protected float fireflyZoneRadius;
    protected float minSpawnDistance;
    protected float maxSpawnDistance;
    protected float minSpawnAngle;
    protected float maxSpawnAngle;
    protected float leftRightRatio;
    protected float lifeSpan;
    protected float n;
    enum Modes
    {
        ON,
        Flash,
        Fixed
    }
    Modes mode;
    protected float easyTrialRatio;
    protected float flashFrequency;
    protected float flashDutyCycle;
    protected float pulseWidth;
    protected List<float> velocities
    {
        get { return velocities; }
        set
        {
            velocities.Add(PlayerPrefs.GetFloat("V1"));
            velocities.Add(PlayerPrefs.GetFloat("V2"));
            velocities.Add(PlayerPrefs.GetFloat("V3"));
            velocities.Add(PlayerPrefs.GetFloat("V4"));
            velocities.Add(PlayerPrefs.GetFloat("V5"));
            velocities.Add(PlayerPrefs.GetFloat("V6"));
            velocities.Add(PlayerPrefs.GetFloat("V7"));
            velocities.Add(PlayerPrefs.GetFloat("V8"));
            velocities.Add(PlayerPrefs.GetFloat("V9"));
            velocities.Add(PlayerPrefs.GetFloat("V10"));
            velocities.Add(PlayerPrefs.GetFloat("V11"));
            velocities.Add(PlayerPrefs.GetFloat("V12"));
        }
    }
    protected List<float> velocityRatios
    {
        get { return velocityRatios; }
        set
        {
            velocityRatios.Add(PlayerPrefs.GetFloat("VR1"));
            velocityRatios.Add(PlayerPrefs.GetFloat("VR2"));
            velocityRatios.Add(PlayerPrefs.GetFloat("VR3"));
            velocityRatios.Add(PlayerPrefs.GetFloat("VR4"));
            velocityRatios.Add(PlayerPrefs.GetFloat("VR5"));
            velocityRatios.Add(PlayerPrefs.GetFloat("VR6"));
            velocityRatios.Add(PlayerPrefs.GetFloat("VR7"));
            velocityRatios.Add(PlayerPrefs.GetFloat("VR8"));
            velocityRatios.Add(PlayerPrefs.GetFloat("VR9"));
            velocityRatios.Add(PlayerPrefs.GetFloat("VR10"));
            velocityRatios.Add(PlayerPrefs.GetFloat("VR11"));
            velocityRatios.Add(PlayerPrefs.GetFloat("VR12"));
        }
    }
    protected List<Vector3> directions { 
        get { return directions; }
        set
        {
            directions.Add(Vector3.left);
            directions.Add(Vector3.right);
            directions.Add(Vector3.forward);
            directions.Add(Vector3.back);
        }
    }
    protected float fireflyVelocity;
    protected List<float> durations;
    protected List<float> ratios;
    protected List<Vector3> ffSpawnLocations;
    protected float movingRatio;
    protected Vector3 move;

    // Player properties
    protected float playerLinearVelocity { get; set; }
    protected float playerAngularVelocity { get; set; }

    // Flags
    protected bool flagEasyTrial;
    protected bool flagMovingFF;
    protected bool flagLRFB;
    protected bool flagTimeout;
    protected bool flagPlaying;
    protected bool flagBegin;
    protected bool flagCheck;
    protected bool flagEnd;

    // Data collection
    protected string filePath { get; set; }
    protected string subjectName { get; set; }
    protected string subjectSession { get; set; }
    protected StringBuilder stringBuilder { get; set; }

    List<float> listStartTimes { get; set; }
    List<float> listCheckTimes { get; set; }
    List<float> listEndTimes { get; set; }
    List<float> listOnDuration { get; set; }
    List<int> listTrialCount { get; set; }



    // Start is called before the first frame update
    void Start()
    {
        initialStartTime = Time.realtimeSinceStartup;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //virtual public async void Begin()
    //{
    //    // Debug.Log("Begin Phase Start.");
    //    SharedJoystick.MaxSpeed = RandomizeSpeeds(velMin, velMax);
    //    SharedJoystick.RotSpeed = RandomizeSpeeds(rotMin, rotMax);
    //    max_v.Add(SharedJoystick.MaxSpeed);
    //    max_w.Add(SharedJoystick.RotSpeed);
    //    //await new WaitForSeconds(0.5f);
    //    loopCount = 0;
    //    if (!ab)
    //    {
    //        float threshold = (float)rand.NextDouble() * (1.0f - 0.11f) + 0.11f;
    //        await new WaitUntil(() => SharedJoystick.moveX >= 0.11f || SharedJoystick.moveX <= -0.11f || SharedJoystick.moveY >= threshold || SharedJoystick.moveY <= -threshold && !SharedJoystick.ptb); // Used to be rb.velocity.magnitude
    //    }
    //    else
    //    {
    //        await new WaitUntil(() => SharedJoystick.moveX <= 0.11f && SharedJoystick.moveX >= -0.11f && SharedJoystick.moveY <= 0.11f && SharedJoystick.moveY >= -0.11f && !SharedJoystick.ptb); // Used to be rb.velocity.magnitude
    //    }

    //    currPhase = Phases.begin;
    //    isBegin = true;

    //    if (nFF > 1)
    //    {
    //        List<Vector3> posTemp = new List<Vector3>();
    //        Vector3 closestPos = new Vector3(0.0f, 0.0f, 0.0f);
    //        float[] distTemp = new float[(int)nFF];
    //        int[] idx = new int[distTemp.Length];
    //        for (int i = 0; i < nFF; i++)
    //        {
    //            Vector3 position_i;
    //            bool tooClose;
    //            do
    //            {
    //                tooClose = false;
    //                float r_i = minDrawDistance + (maxDrawDistance - minDrawDistance) * Mathf.Sqrt((float)rand.NextDouble());
    //                //float angle_i = Mathf.Sqrt(Mathf.Pow(minPhi, 2.0f) + Mathf.Pow(maxPhi - minPhi, 2.0f) * (float)rand.NextDouble());
    //                float angle_i = (float)rand.NextDouble() * (maxPhi - minPhi) + minPhi;
    //                if (LR != 0.5f)
    //                {
    //                    float side_i = rand.NextDouble() < LR ? 1 : -1;
    //                    position_i = (player.transform.position - new Vector3(0.0f, p_height, 0.0f)) + Quaternion.AngleAxis(angle_i * side_i, Vector3.up) * player.transform.forward * r_i;
    //                }
    //                else
    //                {
    //                    position_i = (player.transform.position - new Vector3(0.0f, p_height, 0.0f)) + Quaternion.AngleAxis(angle_i, Vector3.up) * player.transform.forward * r_i;
    //                }
    //                position_i.y = 0.0001f;
    //                if (i > 0) for (int k = 0; k < i; k++) { if (Vector3.Distance(position_i, pooledFF[k].transform.position) <= 1.0f || Mathf.Abs(position_i.x - pooledFF[k - 1].transform.position.x) >= 0.5f || Mathf.Abs(position_i.z - pooledFF[k - 1].transform.position.z) <= 0.5f) tooClose = true; }
    //            }
    //            while (tooClose);
    //            // pooledI[i].transform.position = position_i;
    //            // pooledO[i].transform.position = position_i;
    //            // pooledFF[i].transform.position = position_i;
    //            posTemp.Add(position_i);
    //            distTemp[i] = Vector3.Distance(player.transform.position, position_i);
    //            idx[i] = i;
    //            ffPositions.Add(position_i);
    //        }
    //        Array.Sort(distTemp, idx);
    //        for (int i = 0; i < idx.Length; i++) { pooledFF[i].transform.position = posTemp[idx[i]]; }
    //    }
    //    else
    //    {
    //        Vector3 position;
    //        float r = minDrawDistance + (maxDrawDistance - minDrawDistance) * Mathf.Sqrt((float)rand.NextDouble());
    //        //float angle = Mathf.Sqrt(Mathf.Pow(minPhi, 2.0f) + Mathf.Pow(maxPhi - minPhi, 2.0f) * (float)rand.NextDouble());
    //        float angle = (float)rand.NextDouble() * (maxPhi - minPhi) + minPhi;
    //        if (LR != 0.5f)
    //        {
    //            float side = rand.NextDouble() < LR ? 1 : -1;
    //            position = (player.transform.position - new Vector3(0.0f, p_height, 0.0f)) + Quaternion.AngleAxis(angle * side, Vector3.up) * player.transform.forward * r;
    //        }
    //        else
    //        {
    //            position = (player.transform.position - new Vector3(0.0f, p_height, 0.0f)) + Quaternion.AngleAxis(angle, Vector3.up) * player.transform.forward * r;
    //        }
    //        position.y = 0.0001f;
    //        // inner.transform.position = position;
    //        // outer.transform.position = position;
    //        firefly.transform.position = position;
    //        ffPositions.Add(position);
    //        initialD = Vector3.Distance(player.transform.position, firefly.transform.position);
    //    }
    //    if (lineOnOff == 1) line.SetActive(true);

    //    // Here, I do something weird to the Vector3. "F8" is how many digits I want when I
    //    // convert to string, Trim takes off the parenthesis at the beginning and end of 
    //    // the converted Vector3 (Vector3.zero.ToString("F2"), for example, outputs:
    //    //
    //    //      "(0.00, 0.00, 0.00)"
    //    //
    //    // Replace(" ", "") removes all whitespace characters, so the above string would
    //    // look like this:
    //    //
    //    //      "0.00,0.00,0.00"
    //    //
    //    // Which is csv format.

    //    player_origin = player.transform.position;
    //    origin.Add(player_origin.ToString("F8").Trim(toTrim).Replace(" ", ""));
    //    heading.Add(player.transform.rotation.ToString("F8").Trim(toTrim).Replace(" ", ""));

    //    if (lineOnOff == 1) line.transform.position = firefly.transform.position;

    //    if (isMoving && nFF < 2)
    //    {
    //        //if ((float)rand.NextDouble() < moveRatio)
    //        //{
    //        float r = (float)rand.NextDouble();

    //        if (r <= v_ratios[0])
    //        {
    //            //v1
    //            velocity = velocities[0];
    //        }
    //        else if (r > v_ratios[0] && r <= v_ratios[1])
    //        {
    //            //v2
    //            velocity = velocities[1];
    //        }
    //        else if (r > v_ratios[1] && r <= v_ratios[2])
    //        {
    //            //v3
    //            velocity = velocities[2];
    //        }
    //        else if (r > v_ratios[2] && r <= v_ratios[3])
    //        {
    //            //v4
    //            velocity = velocities[3];
    //        }
    //        else if (r > v_ratios[3] && r <= v_ratios[4])
    //        {
    //            //v5
    //            velocity = velocities[4];
    //        }
    //        else if (r > v_ratios[4] && r <= v_ratios[5])
    //        {
    //            //v6
    //            velocity = velocities[5];
    //        }
    //        else if (r > v_ratios[5] && r <= v_ratios[6])
    //        {
    //            //v7
    //            velocity = velocities[6];
    //        }
    //        else if (r > v_ratios[6] && r <= v_ratios[7])
    //        {
    //            //v8
    //            velocity = velocities[7];
    //        }
    //        else if (r > v_ratios[7] && r <= v_ratios[8])
    //        {
    //            //v9
    //            velocity = velocities[8];
    //        }
    //        else if (r > v_ratios[8] && r <= v_ratios[9])
    //        {
    //            //v10
    //            velocity = velocities[9];
    //        }
    //        else if (r > v_ratios[9] && r <= v_ratios[10])
    //        {
    //            //v11
    //            velocity = velocities[10];
    //        }
    //        else
    //        {
    //            //v12
    //            velocity = velocities[11];
    //        }

    //        var direction = new Vector3();
    //        if (LRFB)
    //        {
    //            direction = Vector3.right;
    //        }
    //        else
    //        {
    //            direction = Vector3.forward;
    //        }
    //        fv.Add(velocity);
    //        move = direction * velocity;
    //        //}
    //        //else
    //        //{
    //        //    move = new Vector3(0.0f, 0.0f, 0.0f);
    //        //    fv.Add(0.0f);
    //        //}
    //    }
    //    else
    //    {
    //        fv.Add(0.0f);
    //    }

    //    // Debug.Log("Begin Phase End.");
    //    if (nFF > 1)
    //    {
    //        switch (mode)
    //        {
    //            case Modes.ON:
    //                foreach (GameObject FF in pooledFF)
    //                {
    //                    FF.GetComponent<SpriteRenderer>().enabled = true;
    //                }
    //                break;
    //            case Modes.Flash:
    //                on = true;
    //                foreach (GameObject FF in pooledFF)
    //                {
    //                    flashTask = Flash(FF);
    //                }
    //                break;
    //            case Modes.Fixed:
    //                if (toggle)
    //                {
    //                    foreach (GameObject FF in pooledFF)
    //                    {
    //                        FF.GetComponent<SpriteRenderer>().enabled = true;
    //                        // Add alwaysON for all fireflies
    //                    }
    //                }
    //                else
    //                {
    //                    float r = (float)rand.NextDouble();

    //                    if (r <= ratios[0])
    //                    {
    //                        // duration 1
    //                        lifeSpan = durations[0];
    //                    }
    //                    else if (r > ratios[0] && r <= ratios[1])
    //                    {
    //                        // duration 2
    //                        lifeSpan = durations[1];
    //                    }
    //                    else if (r > ratios[1] && r <= ratios[2])
    //                    {
    //                        // duration 3
    //                        lifeSpan = durations[2];
    //                    }
    //                    else if (r > ratios[2] && r <= ratios[3])
    //                    {
    //                        // duration 4
    //                        lifeSpan = durations[3];
    //                    }
    //                    else
    //                    {
    //                        // duration 5
    //                        lifeSpan = durations[4];
    //                    }
    //                    onDur.Add(lifeSpan);
    //                    foreach (GameObject FF in pooledFF)
    //                    {
    //                        FF.GetComponent<SpriteRenderer>().enabled = true;
    //                    }
    //                    await new WaitForSeconds(lifeSpan);
    //                    foreach (GameObject FF in pooledFF)
    //                    {
    //                        FF.GetComponent<SpriteRenderer>().enabled = false;
    //                    }
    //                }
    //                break;
    //        }
    //    }
    //    else
    //    {
    //        switch (mode)
    //        {
    //            case Modes.ON:
    //                firefly.SetActive(true);
    //                break;
    //            case Modes.Flash:
    //                on = true;
    //                flashTask = Flash(firefly);
    //                break;
    //            case Modes.Fixed:
    //                if (toggle)
    //                {
    //                    firefly.SetActive(true);
    //                    alwaysON.Add(true);
    //                    //Debug.Log("always on");
    //                }
    //                else
    //                {
    //                    alwaysON.Add(false);
    //                    float r = (float)rand.NextDouble();

    //                    if (r <= ratios[0])
    //                    {
    //                        // duration 1
    //                        lifeSpan = durations[0];
    //                    }
    //                    else if (r > ratios[0] && r <= ratios[1])
    //                    {
    //                        // duration 2
    //                        lifeSpan = durations[1];
    //                    }
    //                    else if (r > ratios[1] && r <= ratios[2])
    //                    {
    //                        // duration 3
    //                        lifeSpan = durations[2];
    //                    }
    //                    else if (r > ratios[2] && r <= ratios[3])
    //                    {
    //                        // duration 4
    //                        lifeSpan = durations[3];
    //                    }
    //                    else
    //                    {
    //                        // duration 5
    //                        lifeSpan = durations[4];
    //                    }
    //                    onDur.Add(lifeSpan);
    //                    OnOff(lifeSpan);
    //                }
    //                break;
    //        }
    //    }
    //    phase = Phases.trial;
    //    currPhase = Phases.trial;
    //}

    virtual public async void Trial()
    {

    }

    virtual public async void Check()
    {

    }

    /// <summary>
    /// Capture data at moment it's called
    /// </summary>
    virtual public async void CaptureData()
    {
        await new WaitForFixedUpdate();

        if (flagPlaying)
        {
            float timeStamp = 0.0f;

            if (flagBegin)
            {
                if (currTrialNum <= maxTrials)
                {
                    timeStamp = Time.realtimeSinceStartup - initialStartTime < 0.0001f ? 0.0f : Time.realtimeSinceStartup - initialStartTime;
                    listStartTimes.Add(timeStamp);
                    listTrialCount.Add(currTrialNum);
                    flagBegin = !flagBegin;
                }
            }
            else if (flagCheck)
            {
                timeStamp = Time.realtimeSinceStartup - initialStartTime;
                listCheckTimes.Add(timeStamp);
                flagCheck = !flagCheck;
            }
            else if (flagEnd)
            {
                timeStamp = Time.realtimeSinceStartup - initialStartTime;
                listEndTimes.Add(timeStamp);
                if (flagEasyTrial)
                {
                    listOnDuration.Add(listEndTimes[listEndTimes.Count - 1]);
                }
                currTrialNum++;
                flagEnd = !flagEnd;
            }
            else
            {
                timeStamp = Time.realtimeSinceStartup - initialStartTime;
            }

            string dataPoint = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
                currTrialNum,
                timeStamp,
                phase,
                firefly.activeInHierarchy ? 1 : 0,
                player.transform.position.ToString("F8").Trim('(', ')').Replace(" ", ""),
                player.transform.rotation.ToString("F8").Trim('(', ')').Replace(" ", ""),
                playerLinearVelocity,
                playerAngularVelocity,
                firefly.transform.position.ToString("F8").Trim('(', ')').Replace(" ", ""),
                fireflyVelocity) + "\n";

            stringBuilder.Append(dataPoint);
        }
    }
}
