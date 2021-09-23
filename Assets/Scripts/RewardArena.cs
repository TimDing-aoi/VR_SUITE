///////////////////////////////////////////////////////////////////////////////////////////
///                                                                                     ///
/// Reward2D.cs                                                                         ///
/// by Joshua Calugay                                                                   ///
/// jc10487@nyu.edu                                                                     ///
/// jcal1696@gmail.com (use this to contact me since, whoever is reading this is        ///
///                     probably my successor, which means I no longer work at          ///
///                     NYU, which means I no longer have my NYU email.)                ///
/// Last Updated: 6/17/2020                                                             ///
/// For the Angelaki Lab                                                                ///
///                                                                                     ///
/// <summary>                                                                           ///
/// This script takes care of the FF behavior.                                          ///
///                                                                                     ///
/// There are 3 modes: "ON, Flash, Fixed". ON means it's always on, Flash means it      ///
/// flashes based on user-specified frequency and duty cycle. Fixed means it stays on   ///
/// for a fixed amount of time that is specified by the user.                           ///
///                                                                                     ///
/// This code handles up to 5 FF. The code waits for the player to be completely still. ///
/// Once that condition is met, the FF(s) spawn. After a user-specified amount of time, ///
/// the trial will timeout, and the next one will begin once the player is completely   ///
/// still. If the trial hasn't timed out, the code waits for the player to start        ///
/// moving. Once the player moves, the code waits for the player to stop moving before  ///
/// checking the player's position against a FF. If the player ends up near a FF, they  ///
/// win; otherwise, they lose. This repeats until the user exits the application.       ///
/// </summary>                                                                          ///
///////////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViveSR.anipal.Eye;
using UnityEngine;
using static AlloEgoJoystick;
using static ViveSR.anipal.Eye.SRanipal_Eye_Framework;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System;
using System.IO.Ports;
using System.Net.Sockets;
using System.Runtime.InteropServices;

public class RewardArena : MonoBehaviour
{
    public static RewardArena SharedReward;

    [DllImport("MotionCueing")] public static extern IntPtr Create();
    public ParticleSystem particleSystem;
    public GameObject firefly;
    [Tooltip("Radius of firefly")]
    [ShowOnly] public float fireflySize;
    [Tooltip("Maximum distance allowed from center of firefly")]
    [ShowOnly] public float fireflyZoneRadius;
    // Enumerable experiment mode selector
    public enum Modes
    {
        ON,
        Flash,
        Fixed
    }
    public Modes mode;
    // Toggle for whether trial is an always on trial or not
    private bool toggle;
    [Tooltip("Ratio of trials that will have fireflies always on")]
    [ShowOnly] public float ratio;
    [Tooltip("Frequency of flashing firefly (Flashing Firefly Only)")]
    [ShowOnly] public float freq;
    [Tooltip("Duty cycle for flashing firefly (percentage of one period determing how long it stays on during one period) (Flashing Firefly Only)")]
    [ShowOnly] public float duty;
    // Pulse Width; how long in seconds it stays on during one period
    public float PW;
    public GameObject player;
    public AudioSource audioSource;
    public AudioClip winSound;
    public AudioClip neutralSound;
    public AudioClip loseSound;
    [Tooltip("Minimum distance firefly can spawn")]
    [ShowOnly] public float minDrawDistance;
    [Tooltip("Maximum distance firefly can spawn")]
    [ShowOnly] public float maxDrawDistance;
    [Tooltip("Minimum angle from forward axis that firefly can spawn")]
    [ShowOnly] public float minPhi;
    [Tooltip("Maximum angle from forward axis that firefly can spawn")]
    [ShowOnly] public float maxPhi;
    [Tooltip("Indicates whether firefly spawns more on the left or right; < 0.5 means more to the left, > 0.5 means more to the right, = 0.5 means equally distributed between left and right")]
    [ShowOnly] public float LR;
    [Tooltip("How long the firefly stays from the beginning of the trial (Fixed Firefly Only)")]
    [ShowOnly] public float lifeSpan;
    //[Tooltip("How many fireflies can appear at once")]
    //[ShowOnly] public float nFF;
    readonly public List<float> velocities = new List<float>();
    readonly public List<float> v_ratios = new List<float>();
    readonly public List<Vector3> directions = new List<Vector3>()
    {
        Vector3.left,
        Vector3.right,
        Vector3.forward,
        Vector3.back
    };
    readonly public List<float> durations = new List<float>();
    readonly public List<float> ratios = new List<float>();
    public bool isMoving;
    public bool LRFB;
    private Vector3 move;
    [Tooltip("Trial timeout (how much time player can stand still before trial ends")]
    [ShowOnly] public float timeout;
    [Tooltip("Minimum x value to plug into exponential distribution from which time to wait before check is pulled")]
    [ShowOnly] public float checkMin;
    [Tooltip("Maximum x value to plug into exponential distribution from which time to wait before check is pulled")]
    [ShowOnly] public float checkMax;
    [Tooltip("Minimum x value to plug into exponential distribution from which time to wait before new trial is pulled")]
    [ShowOnly] public float interMax;
    [Tooltip("Maximum x value to plug into exponential distribution from which time to wait before new trial is pulled")]
    [ShowOnly] public float interMin;
    [Tooltip("Player height")]
    [ShowOnly] public float p_height;
    // 1 / Mean for check time exponential distribution
    public float c_lambda;
    // 1 / Mean for intertrial time exponential distribution
    public float i_lambda;
    // x values for exponential distribution
    public float c_min;
    public float c_max;
    public float i_min;
    public float i_max;
    public float velMin;
    public float velMax;
    public float rotMin;
    public float rotMax;
    public enum Phases
    {
        begin = 0,
        trial = 1,
        check = 2,
        question = 3,
        none = 4
    }
    public Phases phase;
    
    private Vector3 pPos;
    private bool isTimeout = false;

    // Trial number
    readonly List<int> trial = new List<int>();
    readonly List<int> n = new List<int>();

    // Firefly on/off
    readonly List<bool> onoff = new List<bool>();

    // Firefly ON Duration
    readonly List<float> onDur = new List<float>();

    // Firefly Check Coords
    readonly List<string> ffPos = new List<string>();
    readonly List<string> ffPos_frame = new List<string>();
    readonly List<Vector3> ffPositions = new List<Vector3>();

    // Player position at Check()
    readonly List<string> cPos = new List<string>();

    // Player rotation at Check()
    readonly List<string> cRot = new List<string>();

    // Player origin at beginning of trial
    readonly List<string> origin = new List<string>();

    // Player rotation at origin
    readonly List<string> heading = new List<string>();

    // Player position, continuous
    readonly List<string> position = new List<string>();
    readonly List<string> position_frame = new List<string>();

    // Player rotation, continuous
    readonly List<string> rotation = new List<string>();
    readonly List<string> rotation_frame = new List<string>();

    // Firefly position, continuous
    readonly List<string> f_position = new List<string>();

    // Player linear and angular velocity
    readonly List<float> v = new List<float>();
    readonly List<float> w = new List<float>();
    readonly List<float> max_v = new List<float>();
    readonly List<float> max_w = new List<float>();

    // Firefly velocity
    readonly List<float> fv = new List<float>();
    readonly List<float> currFV = new List<float>();

    // Distances from player to firefly
    readonly List<string> dist = new List<string>();
    readonly List<float> distances = new List<float>();

    // Times
    readonly List<float> beginTime = new List<float>();
    readonly List<float> frameTime = new List<float>();
    readonly List<float> trialTime = new List<float>();
    readonly List<float> checkTime = new List<float>();
    readonly List<float> endTime = new List<float>();
    readonly List<float> checkWait = new List<float>();
    readonly List<float> interWait = new List<float>();
    // add when firefly disappears

    // Rewarded?
    readonly List<int> score = new List<int>();

    // Timed Out?
    readonly List<int> timedout = new List<int>();

    // Current Phase
    readonly List<int> epoch = new List<int>();

    // Was Always ON?
    readonly List<bool> alwaysON = new List<bool>();

    // File paths
    private string path;

    [ShowOnly] public int trialNum;
    private float trialT0;
    private float trialT;
    private float programT0 = 0.0f;

    private float points = 0;
    [Tooltip("How much the player receives for successfully completing the task")]
    [ShowOnly] public float rewardAmt;

    [Tooltip("Whether or not the trial ends when the mouse stops")]
    [ShowOnly] public bool onStop = false;

    private int seed;
    private System.Random rand;

    private bool on = true;
    
    // above/below threshold
    private bool ab = true;

    // Full data record
    private bool isFull = false;

    private bool isBegin = false;
    private bool isCheck = false;
    private bool isEnd = false;

    private Phases currPhase;

    readonly private List<GameObject> pooledFF = new List<GameObject>();

    private bool first = true;
    // private List<GameObject> pooledI = new List<GameObject>();
    // private List<GameObject> pooledO = new List<GameObject>();

    private readonly char[] toTrim = { '(', ')' };

    [ShowOnly] public float initialD = 0.0f;

    private float velocity;
    public Vector3 player_origin;

    private string contPath;

    private int loopCount = 0;

    private StringBuilder sb;
    private bool playing = true;

    public bool ramp = false;
    public float rampTime;
    public float rampDelay;

    private SerialPort sp;
    private string port;
    private int baudrate;

    private float pitch;
    private float roll;
    private float yaw;

    public int dim;

    private float xVel;
    private float zVel;

    IntPtr MotionCueingClass;

    // Start is called before the first frame update
    /// <summary>
    /// From "GoToSettings.cs" you can see that I just hard-coded each of the key
    /// strings in order to retrieve the values associated with each key and
    /// assign them to their respective variable here. Also initialize some 
    /// variables depending on what mode is selected.
    /// 
    /// Catch exception if no mode detected from PlayerPrefs and default to Fixed
    /// 
    /// Set head tracking for VR headset OFF
    /// </summary>
    void Start()
    {
        SharedReward = this;

        sp = new SerialPort(port, baudrate);
        sp.ReadTimeout = 1;
        sp.Open();

        //text.enabled = false;

        seed = UnityEngine.Random.Range(1, 10000);
        rand = new System.Random(seed);
        //p_height = PlayerPrefs.GetFloat("Player Height");
        c_lambda = 1.0f / PlayerPrefs.GetFloat("Mean 1");
        i_lambda = 1.0f / PlayerPrefs.GetFloat("Mean 2");
        checkMin = PlayerPrefs.GetFloat("Minimum Wait to Check");
        checkMax = PlayerPrefs.GetFloat("Maximum Wait to Check");
        interMin = PlayerPrefs.GetFloat("Minimum Intertrial Wait");
        interMax = PlayerPrefs.GetFloat("Maximum Intertrial Wait");
        c_min = Tcalc(checkMin, c_lambda);
        c_max = Tcalc(checkMax, c_lambda);
        i_min = Tcalc(interMin, c_lambda);
        i_max = Tcalc(interMax, c_lambda);
        velMin = PlayerPrefs.GetFloat("Min Linear Speed");
        velMax = PlayerPrefs.GetFloat("Max Linear Speed");
        rotMin = PlayerPrefs.GetFloat("Min Angular Speed");
        rotMax = PlayerPrefs.GetFloat("Max Angular Speed");
        onStop = PlayerPrefs.GetInt("End Trial on Stop") == 1;
        dim = PlayerPrefs.GetInt("Dimensions");
        minDrawDistance = PlayerPrefs.GetFloat("Minimum Firefly Distance");
        maxDrawDistance = PlayerPrefs.GetFloat("Maximum Firefly Distance");
        LR = PlayerPrefs.GetFloat("Left Right");
        if (LR == 0.5f)
        {
            maxPhi = PlayerPrefs.GetFloat("Max Angle");
            minPhi = -maxPhi;
        }
        else
        { 
            maxPhi = PlayerPrefs.GetFloat("Max Angle");
            minPhi = PlayerPrefs.GetFloat("Min Angle");
        }
        fireflyZoneRadius = PlayerPrefs.GetFloat("Reward Zone Radius");
        fireflySize = PlayerPrefs.GetFloat("Size");
        firefly.transform.localScale = new Vector3(fireflySize, fireflySize, 1);
        ratio = PlayerPrefs.GetFloat("Ratio");
        ramp = PlayerPrefs.GetInt("Ramp") == 1;
        rampTime = PlayerPrefs.GetFloat("Ramp Time");
        rampDelay = PlayerPrefs.GetFloat("Ramp Delay");

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

        v_ratios.Add(PlayerPrefs.GetFloat("VR1"));
        v_ratios.Add(PlayerPrefs.GetFloat("VR2"));
        v_ratios.Add(PlayerPrefs.GetFloat("VR3"));
        v_ratios.Add(PlayerPrefs.GetFloat("VR4"));
        v_ratios.Add(PlayerPrefs.GetFloat("VR5"));
        v_ratios.Add(PlayerPrefs.GetFloat("VR6"));
        v_ratios.Add(PlayerPrefs.GetFloat("VR7"));
        v_ratios.Add(PlayerPrefs.GetFloat("VR8"));
        v_ratios.Add(PlayerPrefs.GetFloat("VR9"));
        v_ratios.Add(PlayerPrefs.GetFloat("VR10"));
        v_ratios.Add(PlayerPrefs.GetFloat("VR11"));
        v_ratios.Add(PlayerPrefs.GetFloat("VR12"));

        for (int i = 1; i < 12; i++)
        {
            v_ratios[i] = v_ratios[i] + v_ratios[i - 1];
        }

        durations.Add(PlayerPrefs.GetFloat("D1"));
        durations.Add(PlayerPrefs.GetFloat("D2"));
        durations.Add(PlayerPrefs.GetFloat("D3"));
        durations.Add(PlayerPrefs.GetFloat("D4"));
        durations.Add(PlayerPrefs.GetFloat("D5"));

        ratios.Add(PlayerPrefs.GetFloat("R1"));
        ratios.Add(PlayerPrefs.GetFloat("R2"));
        ratios.Add(PlayerPrefs.GetFloat("R3"));
        ratios.Add(PlayerPrefs.GetFloat("R4"));
        ratios.Add(PlayerPrefs.GetFloat("R5"));

        for (int i = 1; i < 5; i++)
        {
            ratios[i] = ratios[i] + ratios[i - 1];
        }

        isMoving = PlayerPrefs.GetInt("Moving ON") == 1;
        LRFB = PlayerPrefs.GetInt("VertHor") == 0;
        //ab = PlayerPrefs.GetInt("AboveBelow") == 1;
        isFull = PlayerPrefs.GetInt("Full ON") == 1;
        try
        {
            switch (PlayerPrefs.GetString("Switch Behavior"))
            {
                case "always on":
                    mode = Modes.ON;
                    break;
                case "flashing":
                    mode = Modes.Flash; 
                    freq = PlayerPrefs.GetFloat("Frequency");
                    duty = PlayerPrefs.GetFloat("Duty Cycle") / 100f;
                    PW = duty / freq;
                    break;
                case "fixed":
                    mode = Modes.Fixed;
                    // lifeSpan = PlayerPrefs.GetFloat("Firefly Life Span");
                    break;
                default:
                    throw new System.Exception("No mode selected, defaulting to FIXED");
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.Log(e, this);
            mode = Modes.Fixed;
            // lifeSpan = PlayerPrefs.GetFloat("Firefly Life Span");
        }
        //nFF = PlayerPrefs.GetFloat("Number of Fireflies");
        //if (nFF > 1)
        //{
        //    for (int i = 0; i < nFF; i++)
        //    {
        //        GameObject obj = Instantiate(firefly);
        //        // GameObject in_ = Instantiate(inner);
        //        // GameObject out_ = Instantiate(outer);
        //        obj.name = ("Firefly " + i).ToString();
        //        // in_.name = ("Inner " + i).ToString();
        //        // out_.name = ("Outer " + i).ToString();
        //        pooledFF.Add(obj);
        //        // pooledI.Add(in_);
        //        // pooledO.Add(out_);
        //        obj.SetActive(true);
        //        // in_.SetActive(true);
        //        // out_.SetActive(true);
        //        obj.GetComponent<SpriteRenderer>().enabled = true;
        //        switch (i)
        //        {
        //            case 0:
        //                obj.GetComponent<SpriteRenderer>().color = Color.black;
        //                break;
        //            case 1:
        //                obj.GetComponent<SpriteRenderer>().color = Color.red;
        //                break;
        //            case 2:
        //                obj.GetComponent<SpriteRenderer>().color = Color.blue;
        //                break;
        //            case 3:
        //                obj.GetComponent<SpriteRenderer>().color = Color.yellow;
        //                break;
        //            case 4:
        //                obj.GetComponent<SpriteRenderer>().color = Color.green;
        //                break;
        //        }
        //    }
        //    // inner.SetActive(false);
        //    // outer.SetActive(false);
        //    firefly.SetActive(false);
        //}

        //ipd = ;

        timeout = PlayerPrefs.GetFloat("Timeout");
        path = PlayerPrefs.GetString("Path");
        //UnityEngine.Debug.Log(path);
        rewardAmt = PlayerPrefs.GetFloat("Reward");
        //rigidbodyFirstPersonControllerv2.movementSettings.ForwardSpeed = PlayerPrefs.GetFloat("Max Linear Speed");
        //rigidbodyFirstPersonControllerv2.movementSettings.BackwardSpeed = PlayerPrefs.GetFloat("Max Linear Speed");
        trialNum = 0;

        player.transform.position = new Vector3(0.0f, p_height, 0.0f);
        player.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
        //print("Begin test.");
        //contPath = path + "/continuous_data_" + PlayerPrefs.GetInt("Optic Flow Seed").ToString() + ".csv";
        //string firstLine = "TrialNum,TrialTime,Phase,OnOff,PosX,PosY,PosZ,RotX,RotY,RotZ,RotW,LinearVelocty,AngularVelocity,FFX,FFY,FFZ,FFV,LeftGazeX,LeftGazeY,LeftGazeZ,LeftGazeX0,LeftGazeY0,LeftGazeZ0,LHitX,LHitY,LHitZ,LConvergenceDist,2DLHitX,2DLHitY,RightGazeX,RightGazeY,RightGazeZ,RightGazeX0,RightGazeY0,RightGazeZ0,RHitX,RHitY,RHitZ,RConvergenceDist,2DRHitX,2DRHitY,GazeX,GazeY,GazeZ,GazeX0,GazeY0,GazeZ0,HitX,HitY,HitZ,ConvergeDist,2DHitX,2DHitY,LeftPupilDiam,RightPupilDiam,LeftOpen,RightOpen";
        //File.AppendAllText(contPath, firstLine + "\n");

        programT0 = Time.realtimeSinceStartup;
        currPhase = Phases.begin;
        phase = Phases.begin;

        player.transform.position = Vector3.up * p_height;
        player.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
        //Assert.IsNotNull(GazeRayRenderer);
        //OurCamera = GameObject.FindWithTag("MainCamera");

        //MotionCue();
        Read();
    }

    /// <summary>
    /// Update is called once per frame
    /// 
    /// for some reason, I can't set the camera's local rotation to 0,0,0 in Start()
    /// so I'm doing it here, and it gets called every frame, so added bonus of 
    /// ensuring it stays at 0,0,0.
    /// 
    /// SharedInstance.fill was an indicator of how many objects loaded in properly,
    /// but I found a way to make it so that everything loads pretty much instantly,
    /// so I don't really need it, but it's nice to have to ensure that the experiment
    /// doesn't start until the visual stimulus (i.e. floor triangles) are ready. 
    /// 
    /// Every frame, add the time it occurs, the trial time (resets every new trial),
    /// trial number, and position and rotation of player.
    /// 
    /// Switch phases here to ensure that each phase occurs on a frame
    /// 
    /// For Flashing and Fixed, toggle will be true or false depending on whether or not
    /// nextDouble returns a number smaller than or equal to the ratio
    /// 
    /// In the case of multiple FF, I turned the sprite renderer on and off, rather than
    /// using SetActive(). I was trying to do something will colliders to detect whether
    /// or not there is already another FF within a certain range, and in order to do that
    /// I would have to keep the sprite active, so I couldn't use SetActive(false). 
    /// The thing I was trying to do didn't work, but I already started turning the 
    /// sprite renderer on and off, and it works fine, so it's staying like that. This
    /// applies to all other instances of GetComponent<SpriteRenderer>() in the code.
    /// </summary>
    void Update()
    {
        frameTime.Add(Time.realtimeSinceStartup - programT0);
        //if (nFF < 2)
        //{
        //    ffPos_frame.Add(firefly.transform.position.ToString("F8").Trim(toTrim).Replace(" ", ""));
        //}
        position_frame.Add(player.transform.position.ToString("F8").Trim(toTrim).Replace(" ", ""));
        rotation_frame.Add(player.transform.rotation.ToString("F8").Trim(toTrim).Replace(" ", ""));

        //if (isMoving && nFF < 2)
        //{
        //    firefly.transform.position += move * Time.deltaTime;
        //    //fv = move.magnitude;
        //}

        switch (phase)
        {
            case Phases.begin:
                phase = Phases.none;
                if (first)
                {
                    toggle = true;
                    first = false;
                }
                else
                {
                    toggle = rand.NextDouble() <= ratio;
                }
                Begin();
                //tracker.UpdateView();
                break;

            case Phases.trial:
                phase = Phases.none;
                Trial();
                break;

            case Phases.check:
                phase = Phases.none;
                if (mode == Modes.ON)
                {
                    //if (nFF > 1)
                    //{
                    //    for (int i = 0; i < nFF; i++)
                    //    {
                    //        pooledFF[i].GetComponent<SpriteRenderer>().enabled = false;
                    //    }
                    //}
                    //else
                    //{
                    //    firefly.SetActive(false);
                    //}
                    firefly.SetActive(false);
                }
                Check();
                break;

            case Phases.none:
                break;
        }
    }

    /// <summary>
    /// Capture data at 120 Hz
    /// 
    /// Set Unity's fixed timestep to 1/120 (0.00833333...) in order to get 120 Hz recording
    /// Edit -> Project Settings -> Time -> Fixed Timestep
    /// </summary>
    public void FixedUpdate()
    {
        if (playing)
        {
            zVel = 0.1f * pitch;
            xVel = 0.1f * roll;
            
            switch (dim)
            {
                case 1:
                    player.transform.position += new Vector3(0.0f, 0.0f, zVel * Time.fixedDeltaTime);
                    break;
                case 2:
                    player.transform.position += new Vector3(xVel * Time.fixedDeltaTime, 0.0f, zVel * Time.fixedDeltaTime);
                    break;
                default:
                    player.transform.position += new Vector3(0.0f, 0.0f, zVel * Time.fixedDeltaTime);
                    break;
            }
            particleSystem.gameObject.transform.position = player.transform.position;

            if (isBegin)
            {
                trialT0 = Time.realtimeSinceStartup;
                beginTime.Add(trialT0 - programT0);
                trialNum++;
                n.Add(trialNum);
                isBegin = false;
            }
            if (isCheck)
            {
                checkTime.Add(Time.realtimeSinceStartup - programT0);
                isCheck = false;
            }
            if (isEnd)
            {
                endTime.Add(Time.realtimeSinceStartup - trialT0);
                if (toggle)
                {
                    onDur.Add(endTime[endTime.Count - 1]);
                }
                isEnd = false;
            }
            epoch.Add((int)currPhase);
            if (isMoving)
            {
                firefly.transform.position += move * Time.deltaTime;
                currFV.Add(velocity);
            }
            else
            {
                currFV.Add(0.0f);
            }
            onoff.Add(firefly.activeInHierarchy);
            //if (nFF > 1)
            //{
            //    onoff.Add(pooledFF[0].GetComponent<SpriteRenderer>().enabled);
            //}
            //else
            //{
            //    onoff.Add(firefly.activeInHierarchy);
            //}
            if (Time.realtimeSinceStartup - trialT0 < 0.0001)
            {
                trialTime.Add(0.0f);
            }
            else
            {
                trialTime.Add(Time.realtimeSinceStartup - trialT0);
            }
            trial.Add(trialNum);
            position.Add(player.transform.position.ToString("F8").Trim(toTrim).Replace(" ", ""));
            rotation.Add(player.transform.rotation.ToString("F8").Trim(toTrim).Replace(" ", ""));
            try
            {
                v.Add(SharedJoystick.currentSpeed); // Used to be rb.velocity.magnitude
                w.Add(SharedJoystick.currentRot);
            }
            catch (Exception e)
            {
                //lol this is because the joystick thing keeps hacing this exception that like doesn't matter, i don't really need to handle this exception
            }
            f_position.Add(firefly.transform.position.ToString("F8").Trim(toTrim).Replace(" ", ""));

            sb.Append(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}", trial[0], trialTime[0], epoch[0], onoff[0] ? 1 : 0, position[0], rotation[0], v[0], w[0], f_position[0], currFV[0]) + "\n");
            epoch.Clear();
            trial.Clear();
            trialTime.Clear();
            onoff.Clear();
            position.Clear();
            rotation.Clear();
            v.Clear();
            w.Clear();
            currFV.Clear();
            f_position.Clear();
         }
        Keyboard keyboard = Keyboard.current;

        if (keyboard.spaceKey.wasReleasedThisFrame) sp.Write("j100");

        if (keyboard.enterKey.wasReleasedThisFrame && playing)
        {
            playing = false;
            sp.Close();
            // Environment.Exit(Environment.ExitCode);
            File.AppendAllText(contPath, sb.ToString());
            sb.Clear();
            Save();
            SceneManager.LoadScene("MainMenu");
        }
    }
    /// <summary>
    /// FixedUpdate() but as a separate function with some other things in it, might be useful later
    /// </summary>
    //public void notusedfornow()
    //{
    //    //UnityEngine.Debug.Log(Time.deltaTime);
    //    string ppos;
    //    string prot;
    //    float pv = 0.0f;
    //    float pw = 0.0f;
    //    string fpos;
    //    //float fv = 0.0f;
    //    int show = firefly.activeInHierarchy ? 1 : 0;

    //    if (isBegin)
    //    {
    //        trialT0 = Time.realtimeSinceStartup;
    //        beginTime.Add(trialT0 - programT0);
    //        trialNum++;
    //        n.Add(trialNum);
    //        isBegin = false;
    //    }
    //    if (isCheck)
    //    {
    //        checkTime.Add(Time.realtimeSinceStartup - programT0);
    //        isCheck = false;
    //    }
    //    if (isEnd)
    //    {
    //        endTime.Add(Time.realtimeSinceStartup - trialT0 - programT0);
    //        isEnd = false;
    //    }
    //    if (isMoving && nFF < 2)
    //    {
    //        firefly.transform.position += move * Time.deltaTime;
    //        //fv = move.magnitude;
    //    }
    //    if (nFF > 1)
    //    {
    //        //onoff.Add(pooledFF[0].GetComponent<SpriteRenderer>().enabled);
    //    }
    //    else
    //    {
    //        //onoff.Add(firefly.activeInHierarchy);
    //    }
    //    if (Time.realtimeSinceStartup - trialT0 < 0.0001)
    //    {
    //        trialT = 0.0f;
    //    }
    //    else
    //    {
    //        trialT = Time.realtimeSinceStartup - trialT0;
    //    }
    //    //trialTime.Add(trialT);
    //    //trial.Add(trialNum);
    //    //position.Add(player.transform.position.ToString("F8").Trim(toTrim).Replace(" ", ""));
    //    //rotation.Add(player.transform.rotation.ToString("F8").Trim(toTrim).Replace(" ", ""));
    //    ppos = player.transform.position.ToString("F8").Trim(toTrim).Replace(" ", "");
    //    prot = player.transform.rotation.ToString("F8").Trim(toTrim).Replace(" ", "");
    //    try
    //    {
    //        //v.Add(SharedJoystick.currentSpeed); // Used to be rb.velocity.magnitude
    //        //w.Add(SharedJoystick.currentRot);
    //        pv = SharedJoystick.currentSpeed;
    //        pw = SharedJoystick.currentRot;
    //    }
    //    catch (Exception e)
    //    {
    //        //lol this is because the joystick thing keeps hacing this exception that like doesn't matter, i don't really need to handle this exception
    //    }
    //    //f_position.Add(firefly.transform.position.ToString("F8").Trim(toTrim).Replace(" ", ""));
    //    fpos = firefly.transform.position.ToString("F8").Trim(toTrim).Replace(" ", "");

    //    string gazeL = "0.0,0.0,0.0";
    //    string gazeL0 = "0.0,0.0,0.0";
    //    string gazeR = "0.0,0.0,0.0";
    //    string gazeR0 = "0.0,0.0,0.0";
    //    string gaze = "0.0,0.0,0.0";
    //    string gaze0 = "0.0,0.0,0.0";
    //    string pupils = "0.0,0.0";
    //    string open = "0.0,0.0";
    //    string hit = "0.0,0.0,0.0";
    //    float condist = 0.0f;
    //    ViveSR.Error error = SRanipal_Eye_API.GetEyeData(ref data);

    //    if (error == ViveSR.Error.WORK)
    //    {
    //        var left = data.verbose_data.left;
    //        var right = data.verbose_data.right;
    //        var combined = data.verbose_data.combined;

    //        float x = combined.eye_data.gaze_direction_normalized.x;
    //        float y = combined.eye_data.gaze_direction_normalized.y;
    //        float z = combined.eye_data.gaze_direction_normalized.z;

    //        //GazeLeftVerbose.Add(string.Join(",", left.gaze_direction_normalized.x, left.gaze_direction_normalized.y, left.gaze_direction_normalized.z));
    //        gazeL = string.Join(",", left.gaze_direction_normalized.x, left.gaze_direction_normalized.y, left.gaze_direction_normalized.z);

    //        //GazeLeftOriginVerbose.Add(string.Join(",", left.gaze_origin_mm.x, left.gaze_origin_mm.y, left.gaze_origin_mm.z));
    //        gazeL0 = string.Join(",", left.gaze_origin_mm.x, left.gaze_origin_mm.y, left.gaze_origin_mm.z);

    //        //GazeRightVerbose.Add(string.Join(",", right.gaze_direction_normalized.x, right.gaze_direction_normalized.y, right.gaze_direction_normalized.z));
    //        gazeR = string.Join(",", right.gaze_direction_normalized.x, right.gaze_direction_normalized.y, right.gaze_direction_normalized.z);

    //        //GazeRightOriginVerbose.Add(string.Join(",", right.gaze_origin_mm.x, right.gaze_origin_mm.y, right.gaze_origin_mm.z));
    //        gazeR0 = string.Join(",", right.gaze_origin_mm.x, right.gaze_origin_mm.y, right.gaze_origin_mm.z);

    //        //GazeCombVerbose.Add(string.Join(",", x, y, z));//combined.eye_data.gaze_direction_normalized.x, combined.eye_data.gaze_direction_normalized.y, combined.eye_data.gaze_direction_normalized.z));
    //        gaze = string.Join(",", x, y, z);

    //        //GazeOriginCombVerbose.Add(string.Join(",", player.transform.position.x, player.transform.position.y, player.transform.position.z));//combined.eye_data.gaze_origin_mm.x, combined.eye_data.gaze_origin_mm.y, combined.eye_data.gaze_origin_mm.z));
    //        gaze0 = string.Join(",", player.transform.position.x, player.transform.position.y, player.transform.position.z);

    //        //ConvergenceDistanceVerbose.Add(combined.convergence_distance_mm);
    //        //condist = combined.convergence_distance_mm;

    //        //PupilDiamVerbose.Add(string.Join(",", left.pupil_diameter_mm, right.pupil_diameter_mm));
    //        pupils = string.Join(",", left.pupil_diameter_mm, right.pupil_diameter_mm);

    //        //OpennessVerbose.Add(string.Join(",", left.eye_openness, right.eye_openness));
    //        open = string.Join(",", left.eye_openness, right.eye_openness);

    //        //SystemsTimeVerbose.Add(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);

    //        //UnityEngine.Debug.Log(error);

    //        var tuple = CalculateConvergenceDistanceAndCoords(player.transform.position, new Vector3(-x, y, z), ~((1 << 12) | (1 << 13)));
    //        hit = tuple.Item1.ToString("F8").Trim(toTrim).Replace(" ", "");
    //        //marker.transform.position = tuple.Item1;
    //        condist = tuple.Item2;

    //    }
    //    else
    //    {
    //        //UnityEngine.Debug.Log(error);
    //    }

    //    // "TrialNum,TrialTime,OnOff,PosX,PosY,PosZ,RotX,RotY,RotZ,RotW,LinearVelocty,AngularVelocity,FFX,FFY,FFZ,LeftGazeX,LeftGazeY,LeftGazeZ,LeftGazeX0,LeftGazeY0,LeftGazeZ0,RightGazeX,RightGazeY,RightGazeZ,RightGazeX0,RightGazeY0,RightGazeZ0,GazeX,GazeY,GazeZ,GazeX0,GazeY0,GazeZ0,HitX,HitY,HitZ,ConvergeDist,LeftPupilDiam,RightPupilDiam,LeftOpen,RightOpen"
    //    File.AppendAllText(contPath, string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18}", trialNum, trialT, show, ppos, prot, pv, pw, fpos, velocity, gazeL, gazeL0, gazeR, gazeR0, gaze, gaze0, hit, condist, pupils, open) + "\n");
    //}

    /// <summary>
    /// Wait until the player is not moving, then:
    /// 1. Add trial begin time to respective list
    /// 2. Update position; 
    ///     r is calculated so that all distances between the min and max are equally likely to occur,
    ///     angle is calculated in much the same way,
    ///     side just determines whether it'll appear on the left or right side of the screen,
    ///     position is calculated by adding an offset to the player's current position;
    ///         Quaternion.AngleAxis calculates a rotation based on an angle (first arg)
    ///         and an axis (second arg, Vector3.up is shorthand for the y-axis). Multiply that by the 
    ///         forward vector and radius r (or how far away the firefly should be from the player) to 
    ///         get the final position of the firefly
    /// 3. Record player origin and rotation, as well as firefly location
    /// 4. Start firefly behavior depending on mode, and switch phase to trial
    /// </summary>
    async void Begin()
    {
        // Debug.Log("Begin Phase Start.");
        float maxV = RandomizeSpeeds(velMin, velMax);
        float maxW = RandomizeSpeeds(rotMin, rotMax);
        max_v.Add(maxV);
        max_w.Add(maxW);
        //await new WaitForSeconds(0.5f);
        loopCount = 0;

        currPhase = Phases.begin;
        isBegin = true;

        //if (nFF > 1)
        //{
        //    List<Vector3> posTemp = new List<Vector3>();
        //    Vector3 closestPos = new Vector3(0.0f, 0.0f, 0.0f);
        //    float[] distTemp = new float[(int)nFF];
        //    int[] idx = new int[distTemp.Length];
        //    for (int i = 0; i < nFF; i++)
        //    {
        //        Vector3 position_i;
        //        bool tooClose;
        //        do
        //        {
        //            tooClose = false;
        //            float r_i = minDrawDistance + (maxDrawDistance - minDrawDistance) * Mathf.Sqrt((float)rand.NextDouble());
        //            //float angle_i = Mathf.Sqrt(Mathf.Pow(minPhi, 2.0f) + Mathf.Pow(maxPhi - minPhi, 2.0f) * (float)rand.NextDouble());
        //            float angle_i = (float)rand.NextDouble() * (maxPhi - minPhi) + minPhi;
        //            //float angle_i = (float)rand.NextDouble() * 360.0f;
        //            if (LR != 0.5f)
        //            {
        //                float side_i = rand.NextDouble() < LR ? 1 : -1;
        //                position_i = (player.transform.position - new Vector3(0.0f, p_height, 0.0f)) + Quaternion.AngleAxis(angle_i * side_i, Vector3.up) * player.transform.forward * r_i;
        //            }
        //            else
        //            {
        //                position_i = (player.transform.position - new Vector3(0.0f, p_height, 0.0f)) + Quaternion.AngleAxis(angle_i, Vector3.up) * player.transform.forward * r_i;
        //            }
        //            position_i.y = 0.0001f;
        //            if (i > 0) for (int k = 0; k < i; k++) { if (Vector3.Distance(position_i, pooledFF[k].transform.position) <= 1.0f || Mathf.Abs(position_i.x - pooledFF[k - 1].transform.position.x) >= 0.5f || Mathf.Abs(position_i.z - pooledFF[k - 1].transform.position.z) <= 0.5f) tooClose = true; }
        //        }
        //        while (tooClose);
        //        // pooledI[i].transform.position = position_i;
        //        // pooledO[i].transform.position = position_i;
        //        // pooledFF[i].transform.position = position_i;
        //        posTemp.Add(position_i);
        //        distTemp[i] = Vector3.Distance(player.transform.position, position_i);
        //        idx[i] = i;
        //        ffPositions.Add(position_i);
        //    }
        //    Array.Sort(distTemp, idx);
        //    for (int i = 0; i < idx.Length; i++) { pooledFF[i].transform.position = posTemp[idx[i]]; }
        //}
        //else
        //{
        Vector3 position;
        float R = minDrawDistance + (maxDrawDistance - minDrawDistance) * Mathf.Sqrt((float)rand.NextDouble());
        //float angle = Mathf.Sqrt(Mathf.Pow(minPhi, 2.0f) + Mathf.Pow(maxPhi - minPhi, 2.0f) * (float)rand.NextDouble());
        float angle = (float)rand.NextDouble() * (maxPhi - minPhi) + minPhi;
        //float angle = (float)rand.NextDouble() * 360.0f;
        if (LR != 0.5f)
        {
            float side = rand.NextDouble() < LR ? 1 : -1;
            position = (player.transform.position - new Vector3(0.0f, p_height, 0.0f)) + Quaternion.AngleAxis(angle * side, Vector3.up) * player.transform.forward * R;
        }
        else
        {
            position = (player.transform.position - new Vector3(0.0f, p_height, 0.0f)) + Quaternion.AngleAxis(angle, Vector3.up) * player.transform.forward * R;
        }
        position.y = 0.0001f;
        // inner.transform.position = position;
        // outer.transform.position = position;
        firefly.transform.position = position;
        ffPositions.Add(position);
        initialD = Vector3.Distance(player.transform.position, firefly.transform.position);
        //}

        // Here, I do something weird to the Vector3. "F8" is how many digits I want when I
        // convert to string, Trim takes off the parenthesis at the beginning and end of 
        // the converted Vector3 (Vector3.zero.ToString("F2"), for example, outputs:
        //
        //      "(0.00, 0.00, 0.00)"
        //
        // Replace(" ", "") removes all whitespace characters, so the above string would
        // look like this:
        //
        //      "0.00,0.00,0.00"
        //
        // Which is csv format.

        player_origin = player.transform.position;
        origin.Add(player_origin.ToString("F8").Trim(toTrim).Replace(" ", ""));
        heading.Add(player.transform.rotation.ToString("F8").Trim(toTrim).Replace(" ", ""));

        if (isMoving)
        {
            //if ((float)rand.NextDouble() < moveRatio)
            //{
                float r = (float)rand.NextDouble();

                if (r <= v_ratios[0])
                {
                    //v1
                    velocity = velocities[0];
                }
                else if (r > v_ratios[0] && r <= v_ratios[1])
                {
                    //v2
                    velocity = velocities[1];
                }
                else if (r > v_ratios[1] && r <= v_ratios[2])
                {
                    //v3
                    velocity = velocities[2];
                }
                else if (r > v_ratios[2] && r <= v_ratios[3])
                {
                    //v4
                    velocity = velocities[3];
                }
                else if (r > v_ratios[3] && r <= v_ratios[4])
                {
                    //v5
                    velocity = velocities[4];
                }
                else if (r > v_ratios[4] && r <= v_ratios[5])
                {
                    //v6
                    velocity = velocities[5];
                }
                else if (r > v_ratios[5] && r <= v_ratios[6])
                {
                    //v7
                    velocity = velocities[6];
                }
                else if (r > v_ratios[6] && r <= v_ratios[7])
                {
                    //v8
                    velocity = velocities[7];
                }
                else if (r > v_ratios[7] && r <= v_ratios[8])
                {
                    //v9
                    velocity = velocities[8];
                }
                else if (r > v_ratios[8] && r <= v_ratios[9])
                {
                    //v10
                    velocity = velocities[9];
                }
                else if (r > v_ratios[10] && r <= v_ratios[11])
                {
                    //v11
                    velocity = velocities[10];
                }
                else
                {
                    //v12
                    velocity = velocities[11];
                }

                var direction = new Vector3();
                if (LRFB)
                {
                    direction = Vector3.right;
                }
                else
                {
                    direction = Vector3.forward;
                }
                fv.Add(velocity);
                move = direction * velocity;
            //}
            //else
            //{
            //    move = new Vector3(0.0f, 0.0f, 0.0f);
            //    fv.Add(0.0f);
            //}
        }
        else
        {
            fv.Add(0.0f);
        }

        // Debug.Log("Begin Phase End.");
        //if (nFF > 1)
        //{
        //    switch (mode)
        //    {
        //        case Modes.ON:
        //            foreach (GameObject FF in pooledFF)
        //            {
        //                FF.GetComponent<SpriteRenderer>().enabled = true;
        //            }
        //            break;
        //        case Modes.Flash:
        //            on = true;
        //            foreach (GameObject FF in pooledFF)
        //            {
        //                Flash(FF);
        //            }
        //            break;
        //        case Modes.Fixed:
        //            if (toggle)
        //            {
        //                foreach (GameObject FF in pooledFF)
        //                {
        //                    FF.GetComponent<SpriteRenderer>().enabled = true;
        //                    // Add alwaysON for all fireflies
        //                }
        //            }
        //            else
        //            {
        //                float r = (float)rand.NextDouble();

        //                if (r <= ratios[0])
        //                {
        //                    // duration 1
        //                    lifeSpan = durations[0];
        //                }
        //                else if (r > ratios[0] && r <= ratios[1])
        //                {
        //                    // duration 2
        //                    lifeSpan = durations[1];
        //                }
        //                else if (r > ratios[1] && r <= ratios[2])
        //                {
        //                    // duration 3
        //                    lifeSpan = durations[2];
        //                }
        //                else if (r > ratios[2] && r <= ratios[3])
        //                {
        //                    // duration 4
        //                    lifeSpan = durations[3];
        //                }
        //                else
        //                {
        //                    // duration 5
        //                    lifeSpan = durations[4];
        //                }
        //                onDur.Add(lifeSpan);
        //                foreach (GameObject FF in pooledFF)
        //                {
        //                    FF.GetComponent<SpriteRenderer>().enabled = true;
        //                }
        //                await new WaitForSeconds(lifeSpan);
        //                foreach (GameObject FF in pooledFF)
        //                {
        //                    FF.GetComponent<SpriteRenderer>().enabled = false;
        //                }
        //            }
        //            break;
        //    }
        //}
        //else
        //{
        switch (mode)
        {
            case Modes.ON:
                firefly.SetActive(true);
                break;
            case Modes.Flash:
                on = true;
                Flash(firefly);
                break;
            case Modes.Fixed:
                if (toggle)
                {
                    firefly.SetActive(true);
                    alwaysON.Add(true);
                }
                else
                {
                    alwaysON.Add(false);
                    float r = (float)rand.NextDouble();

                    if (r <= ratios[0])
                    {
                        // duration 1
                        lifeSpan = durations[0];
                    }
                    else if (r > ratios[0] && r <= ratios[1])
                    {
                        // duration 2
                        lifeSpan = durations[1];
                    }
                    else if (r > ratios[1] && r <= ratios[2])
                    {
                        // duration 3
                        lifeSpan = durations[2];
                    }
                    else if (r > ratios[2] && r <= ratios[3])
                    {
                        // duration 4
                        lifeSpan = durations[3];
                    }
                    else
                    {
                        // duration 5
                        lifeSpan = durations[4];
                    }
                    onDur.Add(lifeSpan);
                    OnOff(lifeSpan);
                }
                break;
        }
        //}
        phase = Phases.trial;
        currPhase = Phases.trial;
    }

    /// <summary>
    /// Doesn't really do much besides wait for the player to start moving, and, afterwards,
    /// wait until the player stops moving and then start the check phase. Also will go back to
    /// begin phase if player doesn't move before timeout
    /// </summary>
    async void Trial()
    {
        // Debug.Log("Trial Phase Start.");

        if (onStop)
        {
            CancellationTokenSource source = new CancellationTokenSource();

            var t = Task.Run(async () => {
                await new WaitUntil(() => Vector3.Distance(player_origin, player.transform.position) > 0.1f); // Used to be rb.velocity.magnitude
            }, source.Token);

            if (await Task.WhenAny(t, Task.Delay((int)timeout * 1000)) == t)
            {
                await new WaitUntil(() => (SharedJoystick.currentSpeed == 0.0f && SharedJoystick.currentRot == 0.0f && !SharedJoystick.ptb)); // Used to be rb.velocity.magnitude // || (angleL > 3.0f or angleR > 3.0f)
            }
            else
            {
                print("Timed out");
                isTimeout = true;
            }

            source.Cancel();
        }
        else
        {
            await new WaitUntil(() => Vector3.Distance(player_origin, firefly.transform.position) < 0.05f);
        }

        if (mode == Modes.Flash)
        {
            on = false;
        }

        if (toggle)
        {
            //if (nFF > 1)
            //{
            //    //foreach (GameObject FF in pooledFF)
            //    //{
            //    //    FF.GetComponent<SpriteRenderer>().enabled = false;
            //    //}
            //    pooledFF[loopCount].GetComponent<SpriteRenderer>().enabled = false;
            //}
            //else
            //{
            firefly.SetActive(false);
            //}
        }


        move = new Vector3(0.0f, 0.0f, 0.0f);
        velocity = 0.0f;
        phase = Phases.check;
        currPhase = Phases.check;
        // print(phase);
        // Debug.Log("Trial Phase End.");
    }

    /// <summary>
    /// Save the player's position (pPos) and the firefly (reward zone)'s position (fPos)
    /// and start a coroutine to wait for some random amount of time between the user's
    /// specified minimum and maximum wait times
    /// </summary>
    async void Check()
    {
        //await new WaitForSeconds(0.2f);

        string ffPosStr = "";

        bool proximity = false;

        bool isReward = true;

        Vector3 pos = new Vector3();
        Quaternion rot = new Quaternion();

        pPos = player.transform.position - new Vector3(0.0f, p_height, 0.0f);

        pos = player.transform.position;
        rot = player.transform.rotation;

        if (!isTimeout)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            // Debug.Log("Check Phase Start.");

            float delay = c_lambda * Mathf.Exp(-c_lambda * ((float)rand.NextDouble() * (c_max - c_min) + c_min));
            // Debug.Log("firefly delay: " + delay);
            checkWait.Add(delay);

            // print("check delay average: " + checkWait.Average());

            // Wait until this condition is met in a different thread(?...not actually sure if its
            // in a different thread tbh), or until the check delay time is up. If the latter occurs
            // and the player is close enough to a FF, then the player gets the reward.
            var t = Task.Run(async () =>
            {
                await new WaitUntil(() => Vector3.Distance(pos, player.transform.position) > 0.05f); // Used to be rb.velocity.magnitude
                //UnityEngine.Debug.Log("exceeded threshold");
            }, source.Token);

            if (await Task.WhenAny(t, Task.Delay((int)(delay * 1000))) == t)
            {
                //audioSource.clip = winSound;
                //points += rewardAmt;
                //isReward = true;
                //UnityEngine.Debug.Log("rewarded");
                audioSource.clip = loseSound;
                isReward = false;
            }
            source.Cancel();
        }
        else
        {
            checkWait.Add(0.0f);

            audioSource.clip = loseSound;
        }

        isCheck = true;

        //if (nFF > 2)
        //{
        //    //for (int i = 0; i < nFF; i++)
        //    //{
        //    //    ffPosStr = string.Concat(ffPosStr, ffPositions[i].ToString("F8").Trim(toTrim).Replace(" ", "")).Substring(1);
        //    //    distances.Add(Vector3.Distance(pPos, ffPositions[i]));
        //    //    if (distances[i] <= fireflyZoneRadius)
        //    //    {
        //    //        proximity = true;
        //    //    }
        //    //}
        //    ffPosStr = string.Concat(ffPosStr, ffPositions[loopCount].ToString("F8").Trim(toTrim).Replace(" ", "")).Substring(1);
        //    distances.Add(Vector3.Distance(pPos, ffPositions[loopCount]));
        //    if (distances[loopCount] <= fireflyZoneRadius)
        //    {
        //        proximity = true;
        //    }
        //}
        //else
        //{
        if (Vector3.Distance(pPos, firefly.transform.position) <= fireflyZoneRadius) proximity = true;
        ffPosStr = firefly.transform.position.ToString("F8").Trim(toTrim).Replace(" ", "");
        distances.Add(Vector3.Distance(pPos, firefly.transform.position));
        //}

        if (isReward && proximity)
        {
            audioSource.clip = winSound;
            points++;
        }
        else
        {
            audioSource.clip = loseSound;
        }

        if (PlayerPrefs.GetInt("Feedback ON") == 0)
        {
            audioSource.clip = neutralSound;
        }
        audioSource.Play();

        //if (nFF > 1)
        //{
        //    score.Add(isReward && proximity ? 1 : 0);
        //    timedout.Add(isTimeout ? 1 : 0);
        //    cPos.Add(pos.ToString("F8").Trim(toTrim).Replace(" ", ""));
        //    cRot.Add(rot.ToString("F8").Trim(toTrim).Replace(" ", ""));
        //    dist.Add(distances[loopCount].ToString("F8"));
        //    if (loopCount < nFF)
        //    {
        //        if (!isTimeout && isReward && proximity)
        //        {
        //            loopCount++;

        //            float wait = i_lambda * Mathf.Exp(-i_lambda * ((float)rand.NextDouble() * (i_max - i_min) + i_min));

        //            interWait.Add(wait);

        //            //isEnd = true;
        //            // print("inter delay average: " + interWait.Average());

        //            //particleSystem.GetComponent<ParticleSystemRenderer>().enabled = false;

        //            await new WaitForSeconds(wait);

        //            phase = Phases.trial;
        //        }
        //        else
        //        {
        //            //player.transform.position = Vector3.up * p_height;
        //            //player.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

        //            if (loopCount + 1 < nFF)
        //            {
        //                for (int i = loopCount + 1; i < nFF; i++)
        //                {
        //                    distances.Add(Vector3.Distance(pPos, ffPositions[i]));
        //                }
        //            }

        //            float wait = i_lambda * Mathf.Exp(-i_lambda * ((float)rand.NextDouble() * (i_max - i_min) + i_min));

        //            interWait.Add(wait);

        //            isEnd = true;
        //            // print("inter delay average: " + interWait.Average());

        //            //particleSystem.GetComponent<ParticleSystemRenderer>().enabled = false;

        //            await new WaitForSeconds(wait);

        //            //particleSystem.GetComponent<ParticleSystemRenderer>().enabled = true;

        //            phase = Phases.begin;
        //            // Debug.Log("Check Phase End.");
        //        }
        //    }
        //    else
        //    {
        //        ffPositions.Clear();
        //        distances.Clear();
        //        isTimeout = false;
        //    }
            
        //}
        //else
        //{
        timedout.Add(isTimeout ? 1 : 0);
        score.Add(isReward && proximity ? 1 : 0);
        ffPos.Add(ffPosStr);
        dist.Add(distances[0].ToString("F8"));
        cPos.Add(pos.ToString("F8").Trim(toTrim).Replace(" ", ""));
        cRot.Add(rot.ToString("F8").Trim(toTrim).Replace(" ", ""));

        ffPositions.Clear();
        distances.Clear();

        isTimeout = false;

        //player.transform.position = Vector3.up * p_height;
        //player.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

        float wait = i_lambda * Mathf.Exp(-i_lambda * ((float)rand.NextDouble() * (i_max - i_min) + i_min));

        interWait.Add(wait);

        isEnd = true;
        // print("inter delay average: " + interWait.Average());

        //particleSystem.GetComponent<ParticleSystemRenderer>().enabled = false;

        await new WaitForSeconds(wait);

        //particleSystem.GetComponent<ParticleSystemRenderer>().enabled = true;

        phase = Phases.begin;
        // Debug.Log("Check Phase End.");
        //}
    }

    /// <summary>
    /// Used when user specifies that the FF flashes.
    /// 
    /// Pulse Width (s) is the length of the pulse, i.e. how long the firefly stays on. This
    /// is calculated with Duty Cycle (%), which is a percentage of the frequency of the
    /// desired signal. Frequency (Hz) is how often you want the object to flash per second.
    /// Here, we have 1 / frequency because the inverse of frequency is Period (s), denoted
    /// as T, which is the same definition as Frequency except it is given in seconds.
    /// </summary>
    /// <param name="obj">Object to flash</param>
    public async void Flash(GameObject obj)
    {
        while (on)
        {
            if (toggle && !obj.activeInHierarchy)
            {
                obj.GetComponent<SpriteRenderer>().enabled = false;
            }
            else
            {
                obj.GetComponent<SpriteRenderer>().enabled = true;
                await new WaitForSeconds(PW);
                if (ramp) Ramp(obj, rampTime, rampDelay);
                obj.GetComponent<SpriteRenderer>().enabled = false;
                await new WaitForSeconds((1f / freq) - PW);
            }
        }
    }

    public async void Ramp(GameObject obj, float time, float delay)
    {
        bool ramp = true;

        Color col = obj.GetComponent<MeshRenderer>().material.color;

        await new WaitForSeconds(delay);

        while (ramp)
        {
            float delta = 255f / (time / Time.deltaTime);
            float alpha = col.a - delta;
            if (alpha < 0f)
            {
                alpha = 0f;
                ramp = false;
            }
            col = new Vector4(0f, 0f, 255f, alpha);
        }
    }

    public async void OnOff(float time)
    {
        CancellationTokenSource source = new CancellationTokenSource();

        firefly.SetActive(true);

        var t = Task.Run(async () =>
        {
            await new WaitForSeconds(time);
        }, source.Token);

        if (ramp) Ramp(firefly, rampTime, rampDelay);

        if (await Task.WhenAny(t, Task.Run(async () => { await new WaitUntil(() => currPhase == Phases.check); })) == t)
        {
            firefly.SetActive(false);
        }
        else
        {
            firefly.SetActive(false);
        }

        source.Cancel();
    }

    private async void Read()
    {
        while (playing)
        {
            if (sp.BytesToRead > 0)
            {
                string[] line = sp.ReadLine().Split(',');
                pitch = float.Parse(line[0]);
                roll = float.Parse(line[1]);
                yaw = float.Parse(line[2]);
            }
            await new WaitForFixedUpdate();
        }
    }

    private async void Write(string msg)
    {
        sp.Write(msg);
        await new WaitForFixedUpdate();
    }

    public async void MotionCue()
    {
        CMotionCueing motionCueing = new CMotionCueing("192.168.100.10", 61557, "192.168.0.22", 0);

        //StreamReader sr = new StreamReader("C:\\Users\\jc10487\\Documents\\MATLAB\\test.txt");
        //string line = sr.ReadLine();

        int mode = 4; // 1 = straight 2 = circle

        List<double> t = new List<double>();
        List<double> vel = new List<double>();
        List<double> x = new List<double>();
        List<double> y = new List<double>();
        List<double> yaw = new List<double>();
        if (mode == 1)
        {
            for (double i = -5; i < 15; i += 0.01)
            {
                t.Add(i);
            }

            for (int i = 0; i < t.Count(); i++)
            {
                vel.Add(0);
            }
            for (int i = 0; i < t.Count(); i++)
            {
                if (t[i] > 0)
                {
                    vel[i] = 1;
                }
            }
            for (int i = 0; i < t.Count(); i++)
            {
                if (t[i] > 0 && t[i] < 1)
                {
                    vel[i] = t[i];
                }
            }
            for (int i = 0; i < t.Count(); i++)
            {
                x.Add(vel[i] * 0.3);
                y.Add(0);
                yaw.Add(0);
            }
        }
        else if (mode == 2)
        {
            for (double i = -5; i < 35.03; i += 0.01)
            {
                t.Add(i);
            }
            for (int i = 0; i < t.Count(); i++)
            {
                vel.Add(0);
            }
            for (int i = 0; i < t.Count(); i++)
            {
                if (t[i] > 0)
                {
                    vel[i] = 0.1;
                }
            }
            for (int i = 0; i < t.Count(); i++)
            {
                if (t[i] > 0 && t[i] < 1)
                {
                    vel[i] = t[i] * 0.1;
                }
            }
            for (int i = 0; i < t.Count(); i++)
            {
                x.Add(vel[i]);
                y.Add(0);
                if (t[i] > 2.0)
                {
                    yaw.Add(28.6479);
                }
                else
                {
                    yaw.Add(0);
                }
            }
        }
        else if (mode == 3)
        {
            for (double i = -5; i < 40.01; i += 0.01)
            {
                t.Add(i);
            }
            for (int i = 0; i < t.Count(); i++)
            {
                vel.Add(0);
                yaw.Add(0);
            }
            for (int i = 0; i < 6; i++)
            {
                for (int k = 0; k < t.Count(); k++)
                {
                    if (t[k] > i * 10 && t[k] <= i * 10 + 3)
                    {
                        vel[k] = 0.05;
                    }
                    if (t[k] > i * 10 + 5 && t[k] <= i * 10 + 9)
                    {
                        yaw[k] = 90/4;
                    }
                }
            }
            for (int i = 0; i < t.Count(); i++)
            {
                x.Add(vel[i]);
                y.Add(0);
            }
        }
        else if (mode == 4)
        {
            for (double i = -5; i < 40.01; i += 0.01)
            {
                t.Add(i);
            }
            for (int i = 0; i < t.Count(); i++)
            {
                vel.Add(0);
                yaw.Add(0);
            }
            for (int i = 0; i < 6; i++)
            {
                for (int k = 0; k < t.Count(); k++)
                {
                    if (t[k] > i * 10 && t[k] <= i * 10 + 3)
                    {
                        vel[k] = 0.05;
                    }
                    if (t[k] > i * 10 + 5 && t[k] <= i * 10 + 9)
                    {
                        yaw[k] = 90 / 4;
                    }
                }
            }
            for (int i = 0; i < t.Count(); i++)
            {
                y.Add(vel[i]);
                x.Add(0);
            }
        }

        print("motion cue start");

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < t.Count; i++)
        {
            motionCueing.SetInputs(x[i], y[i], yaw[i], 0);
            motionCueing.Calculate();
            motionCueing.SetFrame();
            sb.Append(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}\n", motionCueing.frame.lateral, motionCueing.frame.heave, motionCueing.frame.surge, motionCueing.frame.roll, motionCueing.frame.pitch, motionCueing.frame.yaw, x[i], y[i], yaw[i]));
            //print(string.Format("{0},{1},{2},{3},{4},{5}\n", motionCueing.frame.lateral, motionCueing.frame.heave, motionCueing.frame.surge, motionCueing.frame.roll, motionCueing.frame.pitch, motionCueing.frame.yaw));
            UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (i < 10003)
            {
                await new WaitForFixedUpdate();
            }
            else
            {
                await new WaitUntil(() => keyboard.spaceKey.wasPressedThisFrame);
            }
        }

        if (mode == 1)
        {
            File.AppendAllText("C:\\Users\\jc10487\\Documents\\MATLAB\\test_output_straight.txt", sb.ToString());
        }
        else if (mode == 2)
        {
            File.AppendAllText("C:\\Users\\jc10487\\Documents\\MATLAB\\test_output_circle.txt", sb.ToString());
        }
        else if (mode == 3)
        {
            File.AppendAllText("C:\\Users\\jc10487\\Documents\\MATLAB\\test_output_motion1.txt", sb.ToString());
        }
        else if (mode == 4)
        {
            File.AppendAllText("C:\\Users\\jc10487\\Documents\\MATLAB\\test_output_motion2.txt", sb.ToString());
        }

        print("done");

        //string heave = motionCueing.frame.heave.ToString();
        //UnityEngine.Debug.Log("test");
        //UnityEngine.Debug.Log(69);
        //UnityEngine.Debug.Log(heave);
        //UnityEngine.Debug.Log(motionCueing.frame.surge);
        //UnityEngine.Debug.Log(motionCueing.frame.lateral);
        //UnityEngine.Debug.Log(motionCueing.frame.pitch);
        //UnityEngine.Debug.Log(motionCueing.frame.roll);
        //UnityEngine.Debug.Log(motionCueing.frame.yaw);
        motionCueing.Dispose();
    }

    public float Tcalc(float t, float lambda)
    {
        return -1.0f / lambda * Mathf.Log(t / lambda);
    }

    public float RandomizeSpeeds(float min, float max)
    {
        return (float)(rand.NextDouble() * (max - min) + min);
    }

    /// <summary>
    /// Cast a ray towards the floor in the direction of the user's gaze and return the 
    /// intersection of that ray and the floor. Record the location and distance to the
    /// intersection in lists.
    /// </summary>
    /// <param name="origin"></param> Vector3 describing origin of ray
    /// <param name="direction"></param> Vector3 describing direction of ray
    /// <returns></returns>
    public (Vector3, float) CalculateConvergenceDistanceAndCoords(Vector3 origin, Vector3 direction, int layerMask)
    {
        Vector3 coords = Vector3.zero;
        float hit = Mathf.Infinity;

        if (Physics.Raycast(origin, Quaternion.AngleAxis(Vector3.SignedAngle(Vector3.forward, player.transform.forward, Vector3.up), Vector3.up) * direction, out RaycastHit hitInfo, Mathf.Infinity, layerMask))
        {
            coords = hitInfo.point;
            hit = hitInfo.distance;
            //HitLocations.Add(coords.ToString("F8").Trim(toTrim).Replace(" ", ""));
            //ConvergenceDistanceVerbose.Add(hitInfo.distance);
        }

        return (coords, hit);
    }

    //public void OnApplicationQuit()
    //{
    //    if (!isFull)
    //    {
    //        Save();
    //    }
    //    else
    //    {

    //    }
    //}

    /// <summary>
    /// If you provide filepaths beforehand, the program will save all of your data as .csv files.
    /// 
    /// I did something weird where I saved the rotation/position data as strings; I did this
    /// because the number of columns that the .csv file will have will vary depending on the
    /// number of FF. Each FF has it's own position and distance from the player, and that data
    /// has to be saved along with everything else, and I didn't want to allocate memory for all
    /// the maximum number of FF if not every experiment will have 5 FF, so concatenating all of
    /// the available FF positions and distances into one string and then adding each string as
    /// one entry in a list was my best idea.
    /// </summary>
    public void Save()
    { 
        try
        {
            File.AppendAllText(path + "/frame_data_" + PlayerPrefs.GetInt("Optic Flow Seed").ToString() + ".txt", "time,position_x,position_y,position_z,rotation_x,rotation_y,rotation_z,rotation_w,ff_position_x,ff_position_y,ff_position_z\n");
            //File.Create(path + "/frame_time_" + SharedInstance.seed.ToString() + ".txt");
            for (int i = 0; i < frameTime.Count; i++)
            {
                File.AppendAllText(path + "/frame_data_" + PlayerPrefs.GetInt("Optic Flow Seed").ToString() + ".txt", frameTime[i].ToString() + "," + position_frame[i] + "," + rotation_frame[i] + "," + ffPos_frame[i] + "\n");
            }

            StringBuilder csvCont = new StringBuilder();

            string firstLine;

            List<int> temp;

            if (!isFull)
            {
                //firstLine = "TrialNum,TrialTime,Phase,OnOff,PosX,PosY,PosZ,RotX,RotY,RotZ,RotW,LinearVelocty,AngularVelocity,FFX,FFY,FFZ,FFV,LeftGazeX,LeftGazeY,LeftGazeZ,LeftGazeX0,LeftGazeY0,LeftGazeZ0,LHitX,LHitY,LHitZ,LConvergenceDist,2DLHitX,2DLHitY,RightGazeX,RightGazeY,RightGazeZ,RightGazeX0,RightGazeY0,RightGazeZ0,RHitX,RHitY,RHitZ,RConvergenceDist,2DRHitX,2DRHitY,GazeX,GazeY,GazeZ,GazeX0,GazeY0,GazeZ0,HitX,HitY,HitZ,ConvergeDist,2DHitX,2DHitY,LeftPupilDiam,RightPupilDiam,LeftOpen,RightOpen";
            //}
            //else
            //{
                firstLine = "TrialNum,TrialTime,Phase,OnOff,PosX,PosY,PosZ,RotX,RotY,RotZ,RotW,LinearVelocty,AngularVelocity,FFX,FFY,FFZ,FFV,GazeX,GazeY,GazeZ,GazeX0,GazeY0,GazeZ0,HitX,HitY,HitZ,ConvergeDist,LeftPupilDiam,RightPupilDiam,LeftOpen,RightOpen";
            

                csvCont.AppendLine(firstLine);

                temp = new List<int>() {
                    epoch.Count,
                    trial.Count,
                    trialTime.Count,
                    onoff.Count,
                    position.Count,
                    rotation.Count,
                    v.Count,
                    w.Count,
                    currFV.Count
                };

            //if (isFull)
            //{
            //    temp.Add(HitLocations2D.Count);
            //    temp.Add(GazeLeftVerbose.Count);
            //    temp.Add(GazeLeftOriginVerbose.Count);
            //    temp.Add(HitLocationsL.Count);
            //    temp.Add(HitLocationsL2D.Count);
            //    temp.Add(ConvergenceDistanceL.Count);
            //    temp.Add(GazeRightVerbose.Count);
            //    temp.Add(GazeRightOriginVerbose.Count);
            //    temp.Add(HitLocationsR.Count);
            //    temp.Add(HitLocationsR2D.Count);
            //    temp.Add(ConvergenceDistanceR.Count);
            //}

            //for (int i = 0; i < temp.Count; i++)
            //{
            //    UnityEngine.Debug.Log(temp[i]);
            //}

                temp.Sort();

            //if (isFull)
            //{
            //    for (int i = 0; i < temp[0]; i++)
            //    {
            //        var line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26}", trial[i], trialTime[i], epoch[i], onoff[i] ? 1 : 0, position[i], rotation[i], v[i], w[i], f_position[i], currFV[i], GazeLeftVerbose[i], GazeLeftOriginVerbose[i], HitLocationsL[i], ConvergenceDistanceL[i], HitLocationsL2D[i], GazeRightVerbose[i], GazeRightOriginVerbose[i], HitLocationsR[i], ConvergenceDistanceR[i], HitLocationsR2D[i], GazeCombVerbose[i], GazeOriginCombVerbose[i], HitLocations[i], ConvergenceDistanceVerbose[i], HitLocations2D[i], PupilDiamVerbose[i], OpennessVerbose[i]);
            //        //UnityEngine.Debug.Log(i + " " + line);
            //        csvCont.AppendLine(line);
            //    }
            //}
            //else
            //{
                for (int i = 0; i < temp[0]; i++)
                {
                    var line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15}", trial[i], trialTime[i], epoch[i], onoff[i] ? 1 : 0, position[i], rotation[i], v[i], w[i], f_position[i], currFV[i]);
                    //UnityEngine.Debug.Log(i + " " + line);
                    csvCont.AppendLine(line);
                }
            }

            string contPath = path + "/continuous_data_" + PlayerPrefs.GetInt("Optic Flow Seed").ToString() + ".csv";

            //File.Create(contPath);
            File.WriteAllText(contPath, csvCont.ToString());

            StringBuilder csvDisc = new StringBuilder();

            //if (nFF > 1)
            //{
            //    string ffPosStr = "";
            //    string distStr = "";

            //    for (int i = 0; i < nFF; i++)
            //    {
            //        ffPosStr = string.Concat(ffPosStr, string.Format("ffX{0},ffY{0},ffZ{0},", i));
            //        distStr = string.Concat(distStr, string.Format("distToFF{0},", i));
            //    }
            //    if (isMoving)
            //    {
            //        firstLine = string.Format("n,max_v,max_w,f,ffv,answer,PosX0,PosY0,PosZ0,RotX0,RotY0,RotZ0,{0}pCheckX,pCheckY,pCheckZ,rCheckX,rCheckY,rCheckZ,{1}rewarded,timeout,beginTime,checkTime,duration,delays,ITI", ffPosStr, distStr);
            //    }
            //    else
            //    {
            //        firstLine = string.Format("n,max_v,max_w,f,ffv,PosX0,PosY0,PosZ0,RotX0,RotY0,RotZ0,{0}pCheckX,pCheckY,pCheckZ,rCheckX,rCheckY,rCheckZ,{1}rewarded,timeout,beginTime,checkTime,duration,delays,ITI", ffPosStr, distStr);
            //    }
            //}
            //else
            //{
            if (isMoving)
            {
                firstLine = "n,max_v,max_w,ffv,onDuration,answer,PosX0,PosY0,PosZ0,RotX0,RotY0,RotZ0,RotW0,ffX,ffY,ffZ,pCheckX,pCheckY,pCheckZ,rCheckX,rCheckY,rCheckZ,rCheckW,distToFF,rewarded,timeout,beginTime,checkTime,duration,delays,ITI";
            }
            else
            {
                firstLine = "n,max_v,max_w,ffv,onDuration,PosX0,PosY0,PosZ0,RotX0,RotY0,RotZ0,RotW0,ffX,ffY,ffZ,pCheckX,pCheckY,pCheckZ,rCheckX,rCheckY,rCheckZ,rCheckW,distToFF,rewarded,timeout,beginTime,checkTime,duration,delays,ITI";
                }
            //}

            csvDisc.AppendLine(firstLine);

            temp = new List<int>()
            {
                origin.Count,
                heading.Count,
                ffPos.Count,
                dist.Count,
                n.Count,
                cPos.Count,
                cRot.Count,
                beginTime.Count,
                checkTime.Count,
                endTime.Count,
                checkWait.Count,
                interWait.Count,
                score.Count,
                timedout.Count,
                max_v.Count,
                max_w.Count,
                fv.Count,
                onDur.Count
            };
            temp.Sort();
            if (isMoving)
            {
                for (int i = 0; i < temp[0]; i++)
                {
                    var line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18}", n[i], max_v[i], max_w[i], fv[i], onDur[i], origin[i], heading[i], ffPos[i], cPos[i], cRot[i], dist[i], score[i], timedout[i], beginTime[i], checkTime[i], endTime[i], checkWait[i], interWait[i]);
                    csvDisc.AppendLine(line);
                }
            }
            else
            {
                for (int i = 0; i < temp[0]; i++)
                {
                    var line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17}", n[i], max_v[i], max_w[i], fv[i], onDur[i], origin[i], heading[i], ffPos[i], cPos[i], cRot[i], dist[i], score[i], timedout[i], beginTime[i], checkTime[i], endTime[i], checkWait[i], interWait[i]);
                    csvDisc.AppendLine(line);
                }
            }

            string discPath = path + "/discontinuous_data_" + PlayerPrefs.GetInt("Optic Flow Seed").ToString() + ".csv";

            //File.Create(discPath);
            File.WriteAllText(discPath, csvDisc.ToString());

            //PlayerPrefs.GetInt("Save") == 1)

            string configPath = path + "/config_" + PlayerPrefs.GetInt("Optic Flow Seed").ToString() + ".xml";

            XmlWriter xmlWriter = XmlWriter.Create(configPath);

            xmlWriter.WriteStartDocument();

            xmlWriter.WriteStartElement("Settings");

            xmlWriter.WriteStartElement("Setting");
            xmlWriter.WriteAttributeString("Type", "Optic Flow Settings");

            xmlWriter.WriteStartElement("LifeSpan");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Life Span").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("DrawDistance");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Draw Distance").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("Density");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Density").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("TriangleHeight");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Triangle Height").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("PlayerHeight");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Player Height").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AboveBelow");
            xmlWriter.WriteString(PlayerPrefs.GetInt("AboveBelow").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("Setting");
            xmlWriter.WriteAttributeString("Type", "Joystick Settings");

            xmlWriter.WriteStartElement("MinLinearSpeed");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Min Linear Speed").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("MaxLinearSpeed");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Max Linear Speed").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("MinAngularSpeed");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Min Angular Speed").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("MaxAngularSpeed");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Max Angular Speed").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("PerturbationOn");
            xmlWriter.WriteString(PlayerPrefs.GetInt("Perturbation On").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("PerturbVelocityMin");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Perturb Velocity Min").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("PerturbVelocityMax");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Perturb Velocity Max").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("PerturbRotationMin");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Perturb Rotation Min").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("PerturbRotationMax");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Perturb Rotation Max").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("Setting");
            xmlWriter.WriteAttributeString("Type", "Firefly Settings");

            xmlWriter.WriteStartElement("Size");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Size").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("RewardZoneRadius");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Reward Zone Radius").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("MinimumFireflyDistance");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Minimum Firefly Distance").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("MaximumFireflyDistance");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Maximum Firefly Distance").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("MinAngle");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Min Angle").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("MaxAngle");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Max Angle").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("LeftRight");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Left Right").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("Ratio");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Ratio").ToString());
            xmlWriter.WriteEndElement();

            //xmlWriter.WriteStartElement("Reward");
            //xmlWriter.WriteString(PlayerPrefs.GetFloat("Reward").ToString());
            //xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("NumberofFireflies");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Number of Fireflies").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("D1");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("D1").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("D2");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("D2").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("D3");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("D3").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("D4");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("D4").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("D5");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("D5").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("R1");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("R1").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("R2");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("R2").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("R3");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("R3").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("R4");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("R4").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("R5");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("R5").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("Timeout");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Timeout").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("Frequency");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Frequency").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("DutyCycle");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Duty Cycle").ToString());
            xmlWriter.WriteEndElement();

            //xmlWriter.WriteStartElement("FireflyLifeSpan");
            //xmlWriter.WriteString(PlayerPrefs.GetFloat("Firefly Life Span").ToString());
            //xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("MinimumWaittoCheck");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Minimum Wait to Check").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("MaximumWaittoCheck");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Maximum Wait to Check").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("Mean1");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Mean 1").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("MinimumIntertrialWait");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Minimum Intertrial Wait").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("MaximumIntertrialWait");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Maximum Intertrial Wait").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("Mean2");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Mean 2").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("OpticFlowSeed");
            xmlWriter.WriteString(PlayerPrefs.GetInt("Optic Flow Seed").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("FireflySeed");
            xmlWriter.WriteString(seed.ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("SwitchBehavior");
            xmlWriter.WriteString(PlayerPrefs.GetString("Switch Behavior"));
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("Setting");
            xmlWriter.WriteAttributeString("Type", "Moving Firefly Settings");

            xmlWriter.WriteStartElement("MovingON");
            xmlWriter.WriteString(PlayerPrefs.GetInt("Moving ON").ToString());
            xmlWriter.WriteEndElement();

            //xmlWriter.WriteStartElement("RatioMoving");
            //xmlWriter.WriteString(PlayerPrefs.GetFloat("Ratio Moving").ToString());
            //xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("VertHor");
            xmlWriter.WriteString(PlayerPrefs.GetInt("VertHor").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("V1");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("V1").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("V2");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("V2").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("V3");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("V3").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("V4");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("V4").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("V5");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("V5").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("V6");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("V6").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("V7");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("V7").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("V8");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("V8").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("V9");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("V9").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("V10");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("V10").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("V11");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("V11").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("V12");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("V12").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("VR1");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("VR1").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("VR2");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("VR2").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("VR3");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("VR3").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("VR4");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("VR4").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("VR5");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("VR5").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("VR6");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("VR6").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("VR7");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("VR7").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("VR8");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("VR8").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("VR9");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("VR9").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("VR10");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("VR10").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("VR11");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("VR11").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("VR12");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("VR12").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("Setting");
            xmlWriter.WriteAttributeString("Type", "Data Collection Settings");

            xmlWriter.WriteStartElement("Path");
            xmlWriter.WriteString(PlayerPrefs.GetString("Path"));
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("FullON");
            xmlWriter.WriteString(PlayerPrefs.GetInt("Full ON").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndDocument();
            xmlWriter.Close();

            SceneManager.LoadScene("MainMenu");
            SceneManager.UnloadSceneAsync("Mouse Arena");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log(e);
        }
    }
}

public class CMotionCueing : IDisposable
{
    public struct MotionCueingInputs
    {
        public double ballX;
        public double ballY;
        public double ballYaw;
        public double moogYaw;
        public double arenaR;
        public double avoidanceR;
        public double vrX;
        public double vrY;
        public double vrYaw;
    };

    public struct Frame 
    {
        public double lateral;
        public double surge;
        public double heave;
        public double pitch;
        public double roll;
        public double yaw;
    }

    [DllImport("MotionCueing.dll")] static private extern IntPtr Create();

    [DllImport("MotionCUeing.dll")] static private extern void Destroy(IntPtr pObj);

    [DllImport("MotionCueing.dll")] static public extern void CallUpdateParameters(IntPtr pObj, IntPtr gpList);

    [DllImport("MotionCueing.dll")] static public extern void CallResetLastMoogXYVel(IntPtr pObj);

    [DllImport("MotionCueing.dll")] static public extern void CallCalculation(IntPtr pObj);

    [DllImport("MotionCueing.dll")] static public extern void CallImportInputs(IntPtr pObj, double ball_x, double ball_y, double ball_yaw, double moog_yaw, double arena_radius, double avoidance_radius);

    [DllImport("MotionCueing.dll")] static public extern float GetHeave(IntPtr pObj);

    [DllImport("MotionCueing.dll")] static public extern float GetSurge(IntPtr pObj);

    [DllImport("MotionCueing.dll")] static public extern float GetLateral(IntPtr pObj);

    [DllImport("MotionCueing.dll")] static public extern float GetYaw(IntPtr pObj);

    [DllImport("MotionCueing.dll")] static public extern float GetPitch(IntPtr pObj);

    [DllImport("MotionCueing.dll")] static public extern float GetRoll(IntPtr pObj);

    //private string address;
    //private int port;
    private TcpClient hexaClient;
    private TcpClient motorClient;
    private NetworkStream hexaStream;
    private NetworkStream motorStream;
    private IntPtr m_pNativeObject;
    private float ainOffset = -0.053f;
    private float ainVScale = 40.0f;
    private double[][] unfiltered = new double[3][];
    private double[][] filtered = new double[3][];
    private double[] limit = new double[3];
    private double[] FA = new double[3];
    private double[] FB = new double[3];
    public Frame frame;

    public CMotionCueing(string hexaAddress, int hexaPort, string motorAddress, int motorPort)
    {
        // init pointer to motion cueing object
        this.m_pNativeObject = Create();

        // init 6 points for hexapod (lateral -> tx, surge -> ty, heave -> tz, pitch -> rx, roll -> ry, yaw -> rz;
        // if you look this up i'm pretty sure axes won't be lined up but this is how it was defined originally
        // so just roll with it
        this.frame.lateral = 0.0f;
        this.frame.surge = 0.0f;
        this.frame.heave = 0.0f;
        this.frame.pitch = 0.0f;
        this.frame.roll = 0.0f;
        this.frame.yaw = 0.0f;

        // init tcp/ip connection
        //this.address = addy;
        //this.port = portnum;
        //this.hexaClient = new TcpClient(hexaAddress, hexaPort);
        //this.hexaStream = hexaClient.GetStream();
        //this.motorClient = new TcpClient(motorAddress, motorPort);
        //this.motorStream = hexaClient.GetStream();

        FA[0] = 1;
        FA[1] = -1.955578240315036;
        FA[2] = 0.956543676511203;

        FB[0] = 2.413590490419615e-4;
        FB[1] = 4.827180980839230e-4;
        FB[2] = 2.413590490419615e-4;

        limit[0] = 1;
        limit[1] = 1;
        limit[2] = 90;

        unfiltered[0] = new double[3] { 0, 0, 0 };
        unfiltered[1] = new double[3] { 0, 0, 0 };
        unfiltered[2] = new double[3] { 0, 0, 0 };

        filtered[0] = new double[3] { 0, 0, 0 };
        filtered[1] = new double[3] { 0, 0, 0 };
        filtered[2] = new double[3] { 0, 0, 0 };
    }
    public void Dispose()
    {
        hexaStream.Close();
        motorStream.Close();
        hexaClient.Close();
        motorClient.Close();
        Dispose(true);
    }

    protected virtual void Dispose(bool bDisposing)
    {
        if (this.m_pNativeObject != IntPtr.Zero)
        {
            Destroy(this.m_pNativeObject);
            this.m_pNativeObject = IntPtr.Zero;
        }
        if (bDisposing)
        {
            GC.SuppressFinalize(this);
        }
    }
    ~CMotionCueing()
    {
        Dispose(false);
    }

    public void SetInputs(double ball_x, double ball_y, double ball_yaw, double moog_yaw)
    {
        // double ball_x = *get from computer mouse input*
        // double ball_y = *get from computer mouse input*
        // double ball_yaw = *get from computer mouse input*
        // double moog_yaw = GetMotorYaw();
        double arena_radius = 1.0;
        double avoidance_radius = 0.6;

        //Debug.Log(string.Format("Unfiltered:{0},{1},{2}", ball_x, ball_y, ball_yaw));
        Tuple<double, double, double> tuple = ApplyLowPassFilter(ball_x, ball_y, ball_yaw);

        ball_x = tuple.Item1;
        ball_y = tuple.Item2;
        ball_yaw = tuple.Item3;
        //Debug.Log(string.Format("Filtered:{0},{1},{2}", ball_x, ball_y, ball_yaw));

        CallImportInputs(this.m_pNativeObject, ball_x, ball_y, ball_yaw, moog_yaw, arena_radius, avoidance_radius);
    }

    public void SetInputsTest()
    {
        CallImportInputs(this.m_pNativeObject, 0.1, 0.1, 10, 10, 10, 4);
    }

    public void Calculate()
    {
        CallCalculation(this.m_pNativeObject);
    }

    public void SetFrame()
    {
        this.frame.lateral = GetLateral(this.m_pNativeObject);
        this.frame.surge = GetSurge(this.m_pNativeObject);
        this.frame.heave = GetHeave(this.m_pNativeObject);
        this.frame.pitch = GetPitch(this.m_pNativeObject);
        this.frame.roll = GetRoll(this.m_pNativeObject);
        this.frame.yaw = GetYaw(this.m_pNativeObject);
    }

    public Tuple<double, double, double> ApplyLowPassFilter(double vx, double vy, double vyaw)
    {
        unfiltered[0][2] = vx;
        unfiltered[1][2] = vy;
        unfiltered[2][2] = vyaw;

        for (int i = 0; i < 3; i++)
        {
            unfiltered[i][2] = CheckLimit(unfiltered[i][2], limit[i]);
            LowPassFilter(unfiltered[i], filtered[i], 3, FA, FB);
            for (int k = 1; k < 3; k++)
            {
                unfiltered[i][k - 1] = unfiltered[i][k];
                filtered[i][k - 1] = filtered[i][k];
            }
        }

        vx = filtered[0][2];
        vy = filtered[1][2];
        vyaw = filtered[2][2];

        return Tuple.Create(vx, vy, vyaw);
    }

    public void EnableMotor(int mode)
    {
        byte[] send = Encoding.ASCII.GetBytes("drv.dis");

        motorStream.Write(send, 0, send.Length);

        switch (mode)
        {
            case 1: // position mode
                send = Encoding.ASCII.GetBytes("drv.cmdsource 0");
                motorStream.Write(send, 0, send.Length);

                send = Encoding.ASCII.GetBytes("drv.opmode 2");
                motorStream.Write(send, 0, send.Length);

                send = Encoding.ASCII.GetBytes("drv.en");
                motorStream.Write(send, 0, send.Length);
                break;

            case 2: // velocity mode, analog control
                send = Encoding.ASCII.GetBytes("drv.cmdsource 3");
                motorStream.Write(send, 0, send.Length);

                send = Encoding.ASCII.GetBytes("drv.opmode 1");
                motorStream.Write(send, 0, send.Length);

                send = Encoding.ASCII.GetBytes("ain.offset " + ainOffset.ToString("F3"));
                motorStream.Write(send, 0, send.Length);

                send = Encoding.ASCII.GetBytes("ain.vscale " + ainVScale.ToString("F3"));
                motorStream.Write(send, 0, send.Length);

                send = Encoding.ASCII.GetBytes("drv.en");
                motorStream.Write(send, 0, send.Length);
                break;
        }
    }

    public void DisableMotor()
    {
        byte[] send = Encoding.ASCII.GetBytes("drv.cmdsource 0"); 

        motorStream.Write(send, 0, send.Length);

        send = Encoding.ASCII.GetBytes("drv.opmode 2");
        motorStream.Write(send, 0, send.Length);

        send = Encoding.ASCII.GetBytes("drv.dis");
        motorStream.Write(send, 0, send.Length);
    }

    public void GoHome()
    {
        byte[] trash = new byte[4096];
        byte[] send = Encoding.ASCII.GetBytes("drv.dis");

        motorStream.Write(send, 0, send.Length);
        
        send = Encoding.ASCII.GetBytes("drv.cmdsource 0");
        motorStream.Write(send, 0, send.Length);

        send = Encoding.ASCII.GetBytes("drv.opmode 2");
        motorStream.Write(send, 0, send.Length);

        send = Encoding.ASCII.GetBytes("drv.en");
        motorStream.Write(send, 0, send.Length);

        send = Encoding.ASCII.GetBytes("pl.fb");
        motorStream.Write(send, 0, send.Length);

        // make sure stream is empty before reading
        while (motorStream.DataAvailable) motorStream.Read(trash, 0, trash.Length);

        byte[] data = new byte[256];
        int bytes = motorStream.Read(data, 0, data.Length);
        float yaw = float.Parse(Encoding.ASCII.GetString(data, 0, bytes));

        MonoBehaviour.print(yaw);

        float mod = yaw % 360.0f;
        float diff = yaw - mod;

        if (mod > 180.0f)
        {
            diff += 360.0f;
        }

        send = Encoding.ASCII.GetBytes("MT.p " + diff.ToString("F3"));
        motorStream.Write(send, 0, send.Length);

        send = Encoding.ASCII.GetBytes("MT.v 30");
        motorStream.Write(send, 0, send.Length);

        send = Encoding.ASCII.GetBytes("MT.Acc 90.0");
        motorStream.Write(send, 0, send.Length);

        send = Encoding.ASCII.GetBytes("MT.Dec 90.0");
        motorStream.Write(send, 0, send.Length);

        send = Encoding.ASCII.GetBytes("MT.set 0");
        motorStream.Write(send, 0, send.Length);

        send = Encoding.ASCII.GetBytes("MT.move 0");
        motorStream.Write(send, 0, send.Length);
    }

    public double GetMotorYaw()
    {
        byte[] send = Encoding.ASCII.GetBytes("pl.fb");
        byte[] trash = new byte[4096];
        byte[] data = new byte[256];

        // make sure stream is empty before reading
        while (motorStream.DataAvailable) motorStream.Read(trash, 0, trash.Length);

        motorStream.Write(send, 0, send.Length);

        int bytes = motorStream.Read(data, 0, data.Length);

        return double.Parse(Encoding.ASCII.GetString(data, 0, bytes));
    }
    
    private double CheckLimit(double x, double limit)
    {
        double ans = x;
        if (x > limit)
        {
            ans = limit;
        }
        else if (x < -limit)
        {
            ans = -limit;
        }
        return ans;
    }

    private void LowPassFilter(double[] input, double[] output, int n, double[] FA, double[] FB)
    {
        output[n - 1] = 0.0;
        for (int i = 0; i < n; i++)
        {
            output[n - 1] += FB[i] * input[n - 1 - i];
        }
        for (int i = 1; i < n; i++)
        {
            output[n - 1] -= FA[i] * output[n - 1 - i];
        }
        output[n - 1] /= FA[0];
    }
}