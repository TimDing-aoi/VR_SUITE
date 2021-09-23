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
using System.Threading;
using System.Threading.Tasks;
using ViveSR.anipal.Eye;
using UnityEngine;
using static AlloEgoJoystick;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.XR;

public class Reward2D : MonoBehaviour
{
    public static Reward2D SharedReward;

    public GameObject firefly;
    public GameObject line;
    [HideInInspector] public int lineOnOff = 0;
    public GameObject marker;
    public GameObject panel;
    public Camera Lcam;
    public Camera Rcam;
    public GameObject FP;
    public GameObject Marker;
    // public GameObject inner;
    // public GameObject outer;
    [Tooltip("Radius of firefly")]
    [HideInInspector] public float fireflySize;
    [Tooltip("Maximum distance allowed from center of firefly")]
    [HideInInspector] public float fireflyZoneRadius;
    // Enumerable experiment mode selector
    private enum Modes
    {
        ON,
        Flash,
        Fixed
    }
    private Modes mode;
    // Toggle for whether trial is an always on trial or not
    private bool toggle;
    [Tooltip("Ratio of trials that will have fireflies always on")]
    [HideInInspector] public float ratio;
    [Tooltip("Frequency of flashing firefly (Flashing Firefly Only)")]
    [HideInInspector] public float freq;
    [Tooltip("Duty cycle for flashing firefly (percentage of one period determing how long it stays on during one period) (Flashing Firefly Only)")]
    [HideInInspector] public float duty;
    // Pulse Width; how long in seconds it stays on during one period
    private float PW;
    public GameObject player;
    public AudioSource audioSource;
    public AudioClip winSound;
    public AudioClip neutralSound;
    public AudioClip loseSound;
    [Tooltip("Minimum distance firefly can spawn")]
    [HideInInspector] public float minDrawDistance;
    [Tooltip("Maximum distance firefly can spawn")]
    [HideInInspector] public float maxDrawDistance;
    [Tooltip("Ranges for which firefly n spawns inside")]
    [HideInInspector] public List<float> ranges = new List<float>();
    [Tooltip("Minimum angle from forward axis that firefly can spawn")]
    [HideInInspector] public float minPhi;
    [Tooltip("Maximum angle from forward axis that firefly can spawn")]
    [HideInInspector] public float maxPhi;
    [Tooltip("Indicates whether firefly spawns more on the left or right; < 0.5 means more to the left, > 0.5 means more to the right, = 0.5 means equally distributed between left and right")]
    [HideInInspector] public float LR;
    [Tooltip("How long the firefly stays from the beginning of the trial (Fixed Firefly Only)")]
    [HideInInspector] public float lifeSpan;
    [Tooltip("How many fireflies can appear at once")]
    [HideInInspector] public float nFF;
    int multiMode=1;
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
    readonly public List<float> amplitudes = new List<float>();
    readonly public List<float> ampRatios = new List<float>();
    readonly public List<float> ampDurations = new List<float>();
    readonly public List<float> toggleDurations = new List<float>();
    readonly public List<float> toggleRatios = new List<float>();
    private bool isFlowToggle;
    private bool isGaussian;
    private float currentAmp = 0;
    private float currentAmpDur = 0;
    private Vector3 currentDirection;
    private float flowDuration = 0;
    private bool isMoving;
    private bool LRFB;
    private Vector3 move;
    [Tooltip("Trial timeout (how much time player can stand still before trial ends")]
    [HideInInspector] public float timeout;
    [Tooltip("Minimum x value to plug into exponential distribution from which time to wait before check is pulled")]
    [HideInInspector] public float checkMin;
    [Tooltip("Maximum x value to plug into exponential distribution from which time to wait before check is pulled")]
    [HideInInspector] public float checkMax;
    [Tooltip("Minimum x value to plug into exponential distribution from which time to wait before new trial is pulled")]
    [HideInInspector] public float interMax;
    [Tooltip("Maximum x value to plug into exponential distribution from which time to wait before new trial is pulled")]
    [HideInInspector] public float interMin;
    [Tooltip("Player height")]
    [HideInInspector] public float p_height;
    // 1 / Mean for check time exponential distribution
    private float c_lambda;
    // 1 / Mean for intertrial time exponential distribution
    private float i_lambda;
    // x values for exponential distribution
    private float c_min;
    private float c_max;
    private float i_min;
    private float i_max;
    public enum Phases
    {
        begin = 0,
        trial = 1,
        check = 2,
        question = 3,
        feedback = 4,
        ITI = 4,
        none = 9
    }
    [HideInInspector] public Phases phase;

    private Vector3 pPos;
    private bool isTimeout = false;

    // Trial number
    readonly List<int> n = new List<int>();

    // Firefly ON Duration
    readonly List<float> onDur = new List<float>();

    // Firefly Check Coords
    readonly List<string> ffPos = new List<string>();
    //readonly List<string> ffPos_frame = new List<string>();
    readonly List<Vector3> ffPositions = new List<Vector3>();


    // Player position at Check()
    readonly List<string> cPos = new List<string>();
    readonly List<string> cPosTemp = new List<string>();

    // Player rotation at Check()
    readonly List<string> cRot = new List<string>();
    readonly List<string> cRotTemp = new List<string>();

    // Player origin at beginning of trial
    readonly List<string> origin = new List<string>();

    // Player rotation at origin
    readonly List<string> heading = new List<string>();

    // Player linear and angular velocity
    readonly List<float> max_v = new List<float>();
    readonly List<float> max_w = new List<float>();

    // Firefly velocity
    readonly List<float> fv = new List<float>();

    // Distances from player to firefly
    readonly List<string> dist = new List<string>();
    readonly List<float> distances = new List<float>();


    // Times
    readonly List<float> beginTime = new List<float>();
    readonly List<float> checkTime = new List<float>();
    readonly List<string> checkTimeStrList = new List<string>();
    string checkTimeString;
    readonly List<float> rewardTime = new List<float>();
    readonly List<float> juiceDuration = new List<float>();
    readonly List<float> endTime = new List<float>();
    readonly List<float> checkWait = new List<float>();
    readonly List<float> interWait = new List<float>();

    // Rewarded?
    readonly List<int> score = new List<int>();

    // Think the FF moves or not?
    readonly List<int> answer = new List<int>();
    [HideInInspector] public bool isQuestion = false;

    // Gaussian Perturbation
    readonly List<float> gaussAmp = new List<float>();
    readonly List<float> gaussDur = new List<float>();
    readonly List<float> gaussSD = new List<float>();

    // Flow Toggle Time
    readonly List<float> flowDur = new List<float>();

    // Timed Out?
    readonly List<int> timedout = new List<int>();

    public EyeData data = new EyeData();

    // Gaze hit locations
    readonly List<string> HitLocations2D = new List<string>();

    // Was Always ON?
    readonly List<bool> alwaysON = new List<bool>();

    // Joystick Perturbation (discontinuous)
    readonly List<float> tautau = new List<float>();
    readonly List<float> filterTau = new List<float>();

    [HideInInspector] public float FrameTimeElement = 0;

    [HideInInspector] public float delayTime = .2f;

    public bool Detector = false;
    //List<float> Frametime = new List<float>();

    public int LengthOfRay = 75;
    //[SerializeField] private LineRenderer GazeRayRenderer;

    [HideInInspector] public string sceneTypeVerbose;
    [HideInInspector] public string systemStartTimeVerbose;

    public static Vector3 hitpoint;

    // File paths
    private string path;

    [HideInInspector] public int trialNum;
    //private float trialT;
    private float programT0 = 0.0f;

    //private float points = 0;
    [Tooltip("How much the player receives for successfully completing the task")]
    [HideInInspector] public float rewardAmt;
    [HideInInspector] public float points = 0;
    [Tooltip("Maximum number of trials before quitting (0 for infinity)")]
    [HideInInspector] public int ntrials;

    private int seed;
    private System.Random rand;

    private bool on = true;
    
    // above/below threshold
    private bool ab = true;

    // Full data record
    private bool isFull = false;

    public bool isBegin = false;
    private bool isCheck = false;
    private bool isEnd = false;

    private Phases currPhase;
    [HideInInspector] public string phaseString;

    readonly private List<GameObject> pooledFF = new List<GameObject>();

    private bool first = true;
    // private List<GameObject> pooledI = new List<GameObject>();
    // private List<GameObject> pooledO = new List<GameObject>();

    private readonly char[] toTrim = { '(', ')' };

    [HideInInspector] public float initialD = 0.0f;

    private float velocity;

    public GameObject arrow;
    private MeshRenderer mesh;
    public TMPro.TMP_Text text;
    [HideInInspector] public Vector3 player_origin;
    [HideInInspector] public Quaternion player_rotation_initial;
    [HideInInspector] public Vector3 ff_origin;

    float trialT0;

    private string contPath;

    public ParticleSystem particleSystem;

    private int loopCount = 0;

    CancellationTokenSource source;
    private Task currentTask;
    private Task flashTask;
    private Task writeTask;
    private bool playing = true;
    //private bool writing = true;

    private List<string> stringList = new List<string>();
    StringBuilder sb = new StringBuilder();
    float separation;

    bool proximity;
    bool isReward;


    int ptb;

    List<float> densities = new List<float>();

    float velocityThreshold;
    float rotationThreshold;

    float timeSinceLastFixedUpdate;

    List<Vector2> fixedLocations = new List<Vector2>();

    Vector3 prevPos = Vector3.zero;

    float avgSpeed = 0.0f;
    int idx = 0;
    
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
        UnityEngine.XR.InputTracking.disablePositionalTracking = true;
        UnityEngine.XR.XRDevice.DisableAutoXRCameraTracking(Lcam, true);
        UnityEngine.XR.XRDevice.DisableAutoXRCameraTracking(Rcam, true);

        Lcam.ResetProjectionMatrix();
        Rcam.ResetProjectionMatrix();

        List<XRDisplaySubsystem> displaySubsystems = new List<XRDisplaySubsystem>();
        SubsystemManager.GetInstances<XRDisplaySubsystem>(displaySubsystems);
        if (displaySubsystems.Count > 0)
        {
            //while (!SharedEye.ready);
        }

        //print(XRSettings.loadedDeviceName);

        if (!XRSettings.enabled)
        {
            XRSettings.enabled = true;
        }
        XRSettings.occlusionMaskScale = 2f;
        XRSettings.useOcclusionMesh = false;

        SharedReward = this;

        mesh = arrow.GetComponent<MeshRenderer>();
        mesh.enabled = false;

        //string fireflyLocationCSVPath = "C:\\Users\\lab\\Desktop\\fireflyPoints.csv";

        //using (var reader = new StreamReader(fireflyLocationCSVPath))
        //{
        //    while (!reader.EndOfStream)
        //    {
        //        var line = reader.ReadLine();
        //        var values = line.Split(',');

        //        fixedLocations.Add(new Vector2(float.Parse(values[0]), float.Parse(values[1])));
        //    }
        //}

        ntrials = (int)PlayerPrefs.GetFloat("Num Trials");
        if (ntrials == 0) ntrials = 9999;
        seed = UnityEngine.Random.Range(1, 10000);
        rand = new System.Random(seed);
        ptb = PlayerPrefs.GetInt("Type");
        p_height = PlayerPrefs.GetFloat("Player Height");
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

        multiMode = (int)PlayerPrefs.GetFloat("Multi Mode");
        minDrawDistance = PlayerPrefs.GetFloat("Minimum Firefly Distance");
        maxDrawDistance = PlayerPrefs.GetFloat("Maximum Firefly Distance");

        nFF = PlayerPrefs.GetFloat("Number of Fireflies");

        for (int i = 0; i < nFF; i++)
        {
            distances.Add(0.0f);
        }
        //Nasta Added for sequential
        // Get ranges based on number of ff
        if (nFF > 1 && multiMode == 1)
        {
            //Debug.Log("Making of the ranges happened ");
            ranges.Add(minDrawDistance);
            ffPositions.Add(Vector3.zero);

            if (nFF < 3)
            {
                //fix this later
                ranges.Add(PlayerPrefs.GetFloat("Range One"));
                ffPositions.Add(Vector3.zero);
            }

            if (nFF >= 3 && nFF < 4)
            {
                ranges.Add(PlayerPrefs.GetFloat("Range One"));
                ranges.Add(PlayerPrefs.GetFloat("Range Two"));
                ffPositions.Add(Vector3.zero);
                ffPositions.Add(Vector3.zero);
            }

            if (nFF >= 4 && nFF < 5)
            {
                ranges.Add(PlayerPrefs.GetFloat("Range One"));
                ranges.Add(PlayerPrefs.GetFloat("Range Two"));
                ranges.Add(PlayerPrefs.GetFloat("Range Three"));
                ffPositions.Add(Vector3.zero);
                ffPositions.Add(Vector3.zero);
                ffPositions.Add(Vector3.zero);
            }

            if (nFF >= 5 && nFF < 6)
            {
                ranges.Add(PlayerPrefs.GetFloat("Range One"));
                ranges.Add(PlayerPrefs.GetFloat("Range Two"));
                ranges.Add(PlayerPrefs.GetFloat("Range Three"));
                ranges.Add(PlayerPrefs.GetFloat("Range Four"));
                ffPositions.Add(Vector3.zero);
                ffPositions.Add(Vector3.zero);
                ffPositions.Add(Vector3.zero);
                ffPositions.Add(Vector3.zero);
            }

            ranges.Add(maxDrawDistance);
        }
        else if (nFF > 1 && multiMode == 2)
        {
            for (int i = 0; i < nFF; i++)
            {
                ffPositions.Add(Vector3.zero);
            }
        }
        //Nasta Add ends
        Debug.Log("This is ranges");
        Debug.Log(string.Join(",", ranges));
        Debug.Log(ffPositions.Count);


        LR = 0.5f;//PlayerPrefs.GetFloat("Left Right");
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
        lineOnOff = (int)PlayerPrefs.GetFloat("Line OnOff");
        line.transform.localScale = new Vector3(10000f, 0.125f * p_height * 10, 1);
        if (lineOnOff == 1)
        {
            line.SetActive(true);
        }
        else
        {
            line.SetActive(false);
        }

        if (ptb != 2)
        {
            velocityThreshold = PlayerPrefs.GetFloat("Velocity Threshold");
            rotationThreshold = PlayerPrefs.GetFloat("Rotation Threshold");
        }
        else
        {
            velocityThreshold = 1.0f;
            rotationThreshold = 1.0f;
        }

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

        isGaussian = PlayerPrefs.GetInt("Gaussian Perturbation ON") == 1;
        isFlowToggle = PlayerPrefs.GetInt("Optic Flow OnOff") == 1;

        //print(isFlowToggle);
        //print(isGaussian);

        for (int i = 1; i < 13; i++)
        {
            string temp = string.Format("A{0}", i);
            amplitudes.Add(PlayerPrefs.GetFloat(temp));

            temp = string.Format("AR{0}", i);
            ampRatios.Add(PlayerPrefs.GetFloat(temp));

            temp = string.Format("AD{0}", i);
            ampDurations.Add(PlayerPrefs.GetFloat(temp));

            temp = string.Format("T{0}", i);
            toggleDurations.Add(PlayerPrefs.GetFloat(temp));

            temp = string.Format("TR{0}", i);
            toggleRatios.Add(PlayerPrefs.GetFloat(temp));
        }

        for (int i = 1; i < 12; i++)
        {
            ampRatios[i] = ampRatios[i] + ampRatios[i - 1];

            toggleRatios[i] = toggleRatios[i] + toggleRatios[i - 1];

            //print(ampRatios[i]);
        }

        isMoving = PlayerPrefs.GetInt("Moving ON") == 1;
        LRFB = PlayerPrefs.GetInt("VertHor") == 0;
        ab = PlayerPrefs.GetInt("AboveBelow") == 1;
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
            UnityEngine.Debug.LogError(e, this);
            mode = Modes.Fixed;
            // lifeSpan = PlayerPrefs.GetFloat("Firefly Life Span");
        }

        // Nasta added this for sequential
        if (nFF > 1)
        {
            for (int i = 0; i < nFF; i++)
            {

                GameObject obj = Instantiate(firefly);
                obj.name = ("Firefly " + i).ToString();
                pooledFF.Add(obj);
                obj.SetActive(true);
                //Nasta Need to add a delay here to create sequential flash
                obj.GetComponent<SpriteRenderer>().enabled = true;
                Debug.Log("reads multiple ff");
                if (multiMode == 1)
                {
                    switch (i)
                    {
                        case 0:
                            obj.GetComponent<SpriteRenderer>().color = Color.black;
                            break;
                        case 1:
                            obj.GetComponent<SpriteRenderer>().color = Color.red;
                            break;
                        case 2:
                            obj.GetComponent<SpriteRenderer>().color = Color.blue;
                            break;
                        case 3:
                            obj.GetComponent<SpriteRenderer>().color = Color.yellow;
                            break;
                        case 4:
                            obj.GetComponent<SpriteRenderer>().color = Color.green;
                            break;
                    }
                }
            }
            firefly.SetActive(false);
        }
        //Nasta add ends
        //ipd = ;

        timeout = PlayerPrefs.GetFloat("Timeout");
        path = PlayerPrefs.GetString("Path");
        rewardAmt = PlayerPrefs.GetFloat("Reward");
        trialNum = 0;

        player.transform.position = new Vector3(0.0f, p_height, 0.0f);
        player.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
        systemStartTimeVerbose = DateTime.Now.ToString("MM-dd_HH-mm-ss");
        contPath = path + "/continuous_data_" + PlayerPrefs.GetInt("Optic Flow Seed").ToString() + ".txt";

        string firstLine = "";
        if (ptb == 2)
        {
            firstLine = "TrialNum,TrialTime,Phase,OnOff,PosX,PosY,PosZ,RotX,RotY,RotZ,RotW,CleanLinearVelocity,CleanAngularVelocity,FFX,FFY,FFZ,FFV,GazeX,GazeY,GazeZ,GazeX0,GazeY0,GazeZ0,HitX,HitY,HitZ,ConvergeDist,LeftPupilDiam,RightPupilDiam,LeftOpen,RightOpen\n";
        }
        else
        {
            firstLine = "TrialNum,TrialTime,Phase,OnOff,PosX,PosY,PosZ,RotX,RotY,RotZ,RotW,CleanLinearVelocity,CleanAngularVelocity,FFX,FFY,FFZ,FFV,GazeX,GazeY,GazeZ,GazeX0,GazeY0,GazeZ0,HitX,HitY,HitZ,ConvergeDist,LeftPupilDiam,RightPupilDiam,LeftOpen,RightOpen,Velocity Ksi,Velocity Eta,Rotation Ksi,Rotation Eta,PerturbLinearVelocity,PerturbAngularVelocity\n";
        }
        File.WriteAllText(contPath, firstLine);
  
        programT0 = Time.realtimeSinceStartup;
        timeSinceLastFixedUpdate = Time.realtimeSinceStartup;
        currPhase = Phases.begin;
        phase = Phases.begin;

        player.transform.position = Vector3.up * p_height;
        player.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

        player_rotation_initial = player.transform.rotation;

        firefly.SetActive(false);
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
        phaseString = currPhase.ToString();
        //print(phaseString);

        //if (playing)
        //{
        //    switch (phase)
        //    {
        //        case Phases.begin:
        //            phase = Phases.none;
        //            if (mode == Modes.ON)
        //            {
        //                if (nFF > 1)
        //                {
        //                    toggle = true;
        //                    first = false;
        //                }
        //            }
        //            else
        //            {
        //                toggle = rand.NextDouble() <= ratio;
        //            }
        //            currentTask = Begin();
        //            break;
        if (playing && Time.realtimeSinceStartup - programT0 > 0.3f)
        {
            switch (phase)
            {
                case Phases.begin:
                    phase = Phases.none;
                    if (mode == Modes.ON)
                    {
                        if (nFF > 1)
                        {
                            toggle = true;
                        }
                    }
                    else
                    {
                        toggle = rand.NextDouble() <= ratio;
                    }
                    currentTask = Begin();
                    break;


                case Phases.trial:
                    phase = Phases.none;
                    currentTask = Trial();
                    break;

                case Phases.check:
                    phase = Phases.none;
                    if (mode == Modes.ON)
                    {
                        if (nFF > 1)
                        {
                            for (int i = 0; i < nFF; i++)
                            {
                                pooledFF[i].SetActive(false);
                            }
                        }
                        else
                        {
                            firefly.SetActive(false);
                        }
                    }
                    currentTask = Check();
                    break;

                case Phases.none:
                    break;
            }
        }
        if (currentTask != null && currentTask.IsFaulted)
        {
            print(currentTask.Exception);
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
        var tNow = Time.realtimeSinceStartup;

        //if (idx < 10)
        //{
        //    avgSpeed += Vector3.Magnitude(player.transform.position - prevPos) / (tNow - timeSinceLastFixedUpdate);
        //    idx++;
        //}
        //else
        //{
        //    print(avgSpeed / 10);
        //    avgSpeed = 0;
        //    idx = 0;
        //}

        prevPos = player.transform.position;

        particleSystem.transform.position = player.transform.position - (Vector3.up * (p_height - 0.0001f));

        // we can add here on quit too so it saves even if we dont hit enter
        if ((Input.GetKey(KeyCode.Return) || trialNum > ntrials) && playing)
        {
            //print("finished");
            playing = false;

            File.AppendAllText(contPath, sb.ToString());
            sb.Clear();
            Save();
            SceneManager.LoadScene("MainMenu");
        }

        if (playing)
        {
            if (isBegin)
            {
                player_origin = player.transform.position;
                player_rotation_initial = player.transform.rotation;
                origin.Add(player_origin.ToString("F8").Trim(toTrim).Replace(" ", ""));
                heading.Add(player_rotation_initial.ToString("F8").Trim(toTrim).Replace(" ", ""));
                trialNum++;
                if (trialNum <= ntrials)
                {
                    beginTime.Add(Time.realtimeSinceStartup - programT0 < 0.0001 ? 0.0f : Time.realtimeSinceStartup - programT0);
                    trialT0 = beginTime[beginTime.Count - 1];
                    n.Add(trialNum);
                    isBegin = false;
                }
            }
            //else if (isCheck)
            //{
            //    checkTime.Add(Time.realtimeSinceStartup - programT0);
            //    isCheck = false;
            //}



            //Nasta Added for sequential
            if (isCheck)
            {
                isCheck = false;
                if (nFF > 1 && multiMode == 1)
                {
                    //print(proximity && isReward);
                    if (proximity && isReward)
                    {
                        checkTimeString = string.Concat(checkTimeString, ",", (Time.realtimeSinceStartup - programT0).ToString("F5"));
                    }
                    else
                    {
                        for (int i = loopCount; i < nFF; i++)
                        {
                            checkTimeString = string.Concat(checkTimeString, ",", "0.00000");
                        }
                    }
                }
                else
                {
                    checkTime.Add(Time.realtimeSinceStartup - programT0);
                }
            }
            //Nasta Add Ends
            if (isMoving && nFF < 2)
            {
                firefly.transform.position += move * Time.deltaTime;
            }

            if (isEnd)
            {
                endTime.Add(Time.realtimeSinceStartup - programT0);
                if (toggle)
                {
                    onDur.Add(endTime[endTime.Count - 1] - beginTime[beginTime.Count - 1]);
                }
                checkTimeStrList.Add(checkTimeString.Substring(1));
                checkTimeString = "";
                isEnd = false;
            }

            //if (nFF > 1)
            //{
            //    onoff.Add(pooledFF[0].GetComponent<SpriteRenderer>().enabled);
            //}
            //else
            //{
            //    onoff.Add(firefly.activeInHierarchy);
            //}

            ///This part I'll have to work on later, but it's getting there; basically, I can make it so that there's no sudden jump to 0 but the data still records as such,
            ///but only for the player position. I still have to figure out the firefly position and rotation, as well as spawning the firefly in the correct position in
            ///front of the player
            ///For now, I still have the player reset to 0,0,0 so I'd have to remove that reset if I implement this method

            ViveSR.Error error = SRanipal_Eye_API.GetEyeData(ref data);

            if (tNow - timeSinceLastFixedUpdate >= Time.fixedDeltaTime / 2.0f)
            {
                float x;
                float y;
                float z;
                Vector3 location = Vector3.zero;
                float direction = 0.0f;
                var left = new SingleEyeData();
                var right = new SingleEyeData();
                var combined = new CombinedEyeData();

                if (error == ViveSR.Error.WORK)
                {
                    left = data.verbose_data.left;
                    right = data.verbose_data.right;
                    combined = data.verbose_data.combined;

                    x = combined.eye_data.gaze_direction_normalized.x;
                    y = combined.eye_data.gaze_direction_normalized.y;
                    z = combined.eye_data.gaze_direction_normalized.z;

                    var tuple = CalculateConvergenceDistanceAndCoords(player.transform.position, new Vector3(-x, y, z), ~((1 << 12) | (1 << 13)));

                    location = tuple.Item1;
                    direction = tuple.Item2;

                    if (Camera.main.gameObject.activeInHierarchy)
                    {
                        HitLocations2D.Add(string.Join(",", 0.0f, 0.0f));
                    }
                    else
                    {
                        HitLocations2D.Add(string.Join(",", 0.0f, 0.0f));
                    }

                    var alpha = Vector3.SignedAngle(player.transform.position, player.transform.position + new Vector3(-x, y, z), player.transform.forward) * Mathf.Deg2Rad;
                    var hypo = 10.0f / Mathf.Cos(alpha);
                    Marker.transform.localPosition = new Vector3(-x, y, z) * hypo;
                }
                else
                {
                    x = 0.0f;
                    y = 0.0f;
                    z = 0.0f;

                    left.pupil_diameter_mm = 0.0f;
                    left.eye_openness = 0.0f;
                    right.pupil_diameter_mm = 0.0f;
                    right.eye_openness = 0.0f;
                }


                var tempPos = player.transform.position - player_origin;

                //Nasta Added for sequential
                if (multiMode == 1)
                {
                    tempPos = player.transform.position ;
                }
                var tempQuat = Quaternion.Inverse(player_rotation_initial) * new Quaternion(tempPos.x, tempPos.y, tempPos.z, 0.0f) * player_rotation_initial;
                string transformedPos = new Vector3(tempQuat.x, tempQuat.y, tempQuat.z).ToString("F8").Trim(toTrim).Replace(" ", "");
                string transformedRot = (player.transform.rotation * Quaternion.Inverse(player_rotation_initial)).ToString("F8").Trim(toTrim).Replace(" ", "");

                var tempFFPos = firefly.transform.position - player_origin;
                var tempFFQuat = Quaternion.Inverse(player_rotation_initial) * new Quaternion(tempFFPos.x, tempFFPos.y, tempFFPos.z, 0.0f) * player_rotation_initial;
                string transformedFFPos = new Vector3(tempFFQuat.x, tempFFQuat.y, tempFFQuat.z).ToString("F8").Trim(toTrim).Replace(" ", "");
                
                
                // continous saving
                sb.Append(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15}",
                       trialNum,
                       Time.realtimeSinceStartup,
                       (int)currPhase,
                       firefly.activeInHierarchy ? 1 : 0,
                       transformedPos,
                       transformedRot,
                       SharedJoystick.cleanVel,
                       SharedJoystick.cleanRot,
                       transformedFFPos,
                       velocity,
                       string.Join(",", x, y, z),
                       string.Join(",", player.transform.position.x, player.transform.position.y, player.transform.position.z),
                       location.ToString("F8").Trim(toTrim).Replace(" ", ""),
                       direction,
                       string.Join(",", left.pupil_diameter_mm, right.pupil_diameter_mm),
                       string.Join(",", left.eye_openness, right.eye_openness)));

                Console.WriteLine("{0} chars: {1}", sb.Length, sb.ToString());

                if (ptb == 2)
                {
                    sb.Append("\n");
                }
                else
                {
                    sb.Append(string.Format(",{0},{1},{2},{3},{4},{5}\n",
                        SharedJoystick.velKsi,
                        SharedJoystick.velEta,
                        SharedJoystick.rotKsi,
                        SharedJoystick.rotEta,
                        SharedJoystick.currentSpeed,
                        SharedJoystick.currentRot));
                }

                timeSinceLastFixedUpdate = Time.realtimeSinceStartup;
            }
        }
    }

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
    async Task Begin()
    {
        // Debug.Log("Begin Phase Start.");
        
        //await new WaitForSeconds(0.5f);
        
        currPhase = Phases.begin;
        isBegin = true;
        loopCount = 0;


        //Nasta Added for sequential

        if (nFF > 1 && multiMode == 1)
        {
            for (int i = 0; i < nFF; i++)
            {
                bool tooClose;
                do
                {
                    tooClose = false;
                    Vector3 position;
                    float r = ranges[i] + (ranges[i + 1] - ranges[i]) * Mathf.Sqrt((float)rand.NextDouble());
                    float angle = (float)rand.NextDouble() * (maxPhi - minPhi) + minPhi;
                    if (LR != 0.5f)
                    {
                        float side = rand.NextDouble() < LR ? 1 : -1;
                        position = (player.transform.position - new Vector3(0.0f, p_height, 0.0f)) + Quaternion.AngleAxis(angle * side, Vector3.up) * player.transform.forward * r;
                    }
                    else
                    {
                        position = (player.transform.position - new Vector3(0.0f, p_height, 0.0f)) + Quaternion.AngleAxis(angle, Vector3.up) * player.transform.forward * r;
                    }
                    position.y = 0.0001f;
                    if (i > 0)
                    {
                        for (int k = 0; k < i; k++) 
                        { 
                            if (Vector3.Distance(position, pooledFF[k].transform.position) <= separation) tooClose = true;
                        } // || Mathf.Abs(position.x - pooledFF[k - 1].transform.position.x) >= 0.5f || Mathf.Abs(position.z - pooledFF[k - 1].transform.position.z) <= 0.5f) tooClose = true; 
                    }

                    pooledFF[i].transform.position = position;
                    ffPositions[i] = position;
                    //tooClose = false;

                    Debug.Log(tooClose);

                } while (tooClose);
            }
        }
        else if (nFF > 1 && multiMode == 2)
        {
            for (int i = 0; i < nFF; i++)
            {
                bool tooClose;
                do
                {
                    tooClose = false;
                    Vector3 position;
                    float r = minDrawDistance + (maxDrawDistance - minDrawDistance) * Mathf.Sqrt((float)rand.NextDouble());
                    float angle = (float)rand.NextDouble() * (maxPhi - minPhi) + minPhi;
                    if (LR != 0.5f)
                    {
                        float side = rand.NextDouble() < LR ? 1 : -1;
                        position = (player.transform.position - new Vector3(0.0f, p_height, 0.0f)) + Quaternion.AngleAxis(angle * side, Vector3.up) * player.transform.forward * r;
                    }
                    else
                    {
                        position = (player.transform.position - new Vector3(0.0f, p_height, 0.0f)) + Quaternion.AngleAxis(angle, Vector3.up) * player.transform.forward * r;
                    }
                    position.y = 0.0001f;
                    if (i > 0) for (int k = 0; k < i; k++) { if (Vector3.Distance(position, pooledFF[k].transform.position) <= separation) tooClose = true; } // || Mathf.Abs(position.x - pooledFF[k - 1].transform.position.x) >= 0.5f || Mathf.Abs(position.z - pooledFF[k - 1].transform.position.z) <= 0.5f) tooClose = true; }
                    pooledFF[i].transform.position = position;
                    ffPositions[i] = position;
                } while (tooClose);
            }
        }
        else
        {
            Vector3 position;
            float r = minDrawDistance + (maxDrawDistance - minDrawDistance) * Mathf.Sqrt((float)rand.NextDouble());
            float angle = (float)rand.NextDouble() * (maxPhi - minPhi) + minPhi;
            if (LR != 0.5f)
            {
                float side = rand.NextDouble() < LR ? 1 : -1;
                position = (player.transform.position - new Vector3(0.0f, p_height, 0.0f)) + Quaternion.AngleAxis(angle * side, Vector3.up) * player.transform.forward * r;
            }
            else
            {
                position = (player.transform.position - new Vector3(0.0f, p_height, 0.0f)) + Quaternion.AngleAxis(angle, Vector3.up) * player.transform.forward * r;
            }
            position.y = 0.0001f;
            firefly.transform.position = position;
            ffPositions.Add(position);
        }
        //Nasta Add ends



        if (!isGaussian && !isFlowToggle)
        {
            switch (ptb)
            {
                case 0:
                    SharedJoystick.DiscreteTau();
                    break;

                case 1:
                    SharedJoystick.ContinuousTau();
                    break;

                default:
                    break;
            }
            tautau.Add(SharedJoystick.currentTau);
            filterTau.Add(SharedJoystick.filterTau);
            max_v.Add(SharedJoystick.MaxSpeed);
            max_w.Add(SharedJoystick.RotSpeed);
        }
        else
        {
            max_v.Add(SharedJoystick.MaxSpeed);
            max_w.Add(SharedJoystick.RotSpeed);
        }

        //print(Mathf.Abs(SharedJoystick.currentSpeed) >= velocityThreshold && Mathf.Abs(SharedJoystick.currentRot) >= rotationThreshold);

        if (!ab)
        {
            await new WaitUntil(() => Mathf.Abs(SharedJoystick.currentSpeed) >= velocityThreshold); // Used to be rb.velocity.magnitude
        }
        else
        {
            await new WaitUntil(() => Mathf.Abs(SharedJoystick.currentSpeed) <= velocityThreshold && Mathf.Abs(SharedJoystick.currentRot) <= rotationThreshold); // Used to be rb.velocity.magnitude
        }

        
        if (lineOnOff == 1) line.SetActive(true);

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


        //if (lineOnOff == 1) line.transform.position = firefly.transform.position;

        if (isGaussian)
        {
            SetGaussParams();
        }
        else
        {
            gaussAmp.Add(0.0f);
            gaussDur.Add(0.0f);
            gaussSD.Add(0.0f);
        }

        if (isFlowToggle)
        {
            particleSystem.gameObject.SetActive(true);

            SetStimParams();
            SetFireflyLocation();
            OnOff(particleSystem.gameObject, flowDuration);
        }
        else
        {
            flowDur.Add(0.0f);
        }

        if (isMoving && nFF < 2)
        {
            //print("setting moving FF params");
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
            else if (r > v_ratios[9] && r <= v_ratios[10])
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
                direction = player.transform.right;
            }
            else
            {
                direction = player.transform.forward;
            }
            fv.Add(velocity);
            move = direction * velocity;
        }
        else
        {
            fv.Add(0.0f);
        }


        


        if (nFF > 1)
        {
            switch (mode)
            {
                case Modes.ON:
                    foreach (GameObject FF in pooledFF)
                    {
                        FF.SetActive(true);
                    }
                    break;
                case Modes.Flash:
                    on = true;
                    foreach (GameObject FF in pooledFF)
                    {
                        flashTask = Flash(FF);
                    }
                    break;
                case Modes.Fixed:
                    if (toggle)
                    {
                        foreach (GameObject FF in pooledFF)
                        {
                            FF.SetActive(true);
                            // Add alwaysON for all fireflies
                        }
                    }
                    else
                    {
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
                        foreach (GameObject FF in pooledFF)
                        {
                            FF.SetActive(true);
                        }
                        await new WaitForSeconds(lifeSpan);
                        foreach (GameObject FF in pooledFF)
                        {
                            FF.SetActive(false);
                        }
                    }
                    break;
            }
        }
        else
        {
            //print(mode.ToString());

            switch (mode)
            {

                case Modes.ON:
                    firefly.SetActive(true);
                    break;
                case Modes.Flash:
                    on = true;
                    flashTask = Flash(firefly);
                    break;
                case Modes.Fixed:
                    if (toggle)
                    {
                        alwaysON.Add(true);

                        if (isGaussian)
                        {
                            print("start");

                            SharedJoystick.MaxSpeed = 0.0f;
                            SharedJoystick.RotSpeed = 0.0f;

                            var tuple = MakeProfile();
                            float halfArea = tuple.Item1;
                            float[] perturb = tuple.Item2;

                            for (int i = 0; i < perturb.Length; i++)
                            {
                                if (i == Mathf.RoundToInt(perturb.Length / 2.0f))
                                {
                                    SetFireflyLocation();

                                    print(Vector3.Magnitude(firefly.transform.position - player.transform.position));
                                    firefly.transform.position += Vector3.Normalize(player.transform.forward) * halfArea;

                                    firefly.SetActive(true);
                                }
                                player.transform.position += currentDirection * perturb[i];
                                await new WaitForFixedUpdate();
                            }

                            print(Vector3.Magnitude(firefly.transform.position - player.transform.position));

                            SharedJoystick.MaxSpeed = 20.0f;
                            SharedJoystick.RotSpeed = 90.0f;
                        }
                        else
                        {
                            SetFireflyLocation();
                            firefly.SetActive(true);
                        }
                        //Debug.Log("always on");
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

                        if (isGaussian)
                        {
                            onDur.Add(lifeSpan);

                            if (isGaussian)
                            {
                                print("start");

                                SharedJoystick.MaxSpeed = 0.0f;
                                SharedJoystick.RotSpeed = 0.0f;

                                var tuple = MakeProfile();
                                float halfArea = tuple.Item1;
                                float[] perturb = tuple.Item2;

                                for (int i = 0; i < perturb.Length; i++)
                                {
                                    if (i == Mathf.RoundToInt(perturb.Length / 2))
                                    {
                                        SetFireflyLocation();

                                        print(Vector3.Magnitude(firefly.transform.position - player.transform.position));
                                        firefly.transform.position += Vector3.Normalize(player.transform.forward) * halfArea;

                                        OnOff(lifeSpan);
                                    }
                                    player.transform.position += currentDirection * perturb[i];
                                    await new WaitForFixedUpdate();
                                }

                                print(Vector3.Magnitude(firefly.transform.position - player.transform.position));

                                SharedJoystick.MaxSpeed = 20.0f;
                                SharedJoystick.RotSpeed = 90.0f;
                            }
                        }
                        else
                        {
                            SetFireflyLocation();
                            onDur.Add(lifeSpan);
                            OnOff(lifeSpan);
                        }
                    }
                    break;
            }
        }
        phase = Phases.trial;
        currPhase = Phases.trial;
        Debug.Log("Begin Phase End.");
    }

    /// <summary>
    /// Doesn't really do much besides wait for the player to start moving, and, afterwards,
    /// wait until the player stops moving and then start the check phase. Also will go back to
    /// begin phase if player doesn't move before timeout
    /// </summary>
    async Task Trial()
    {
        // Debug.Log("Trial Phase Start.");

        source = new CancellationTokenSource();

        var t = Task.Run(async () => {
            await new WaitUntil(() => Mathf.Abs(SharedJoystick.currentSpeed) >= velocityThreshold); // Used to be rb.velocity.magnitude
        }, source.Token);

        var t1 = Task.Run(async () => {
            await new WaitForSeconds(timeout); // Used to be rb.velocity.magnitude
        }, source.Token);

        if (await Task.WhenAny(t, t1) == t)
        {
            await new WaitUntil(() => (Mathf.Abs(SharedJoystick.currentSpeed) < velocityThreshold && Mathf.Abs(SharedJoystick.currentRot) < rotationThreshold && (SharedJoystick.moveX == 0.0f && SharedJoystick.moveY == 0.0f)) || t1.IsCompleted); // Used to be rb.velocity.magnitude // || (angleL > 3.0f or angleR > 3.0f)
        }
        else
        {
            //print("Timed out");
            isTimeout = true;
        }

        source.Cancel();

        if (mode == Modes.Flash)
        {
            on = false;
        }

        //if (toggle)
        //{
        //    if (nFF > 1)
        //    {
        //        pooledFF[loopCount].GetComponent<SpriteRenderer>().enabled = false;
        //    }
        //    else
        //    {
        //        firefly.SetActive(false);
        //    }
        //}

        //Nasta Added for sequential
        if (toggle)
        {
            if (nFF > 1 && multiMode == 1)
            {
                if (isTimeout || Vector3.Distance(player.transform.position, pooledFF[loopCount].transform.position) > fireflyZoneRadius)
                {
                    string ffPosStr = "";
                    foreach (GameObject FF in pooledFF)
                    {
                        ffPosStr = string.Concat(ffPosStr, ",", FF.transform.position.ToString("F5").Trim(toTrim).Replace(" ", ""));
                        FF.SetActive(false);
                    }
                    ffPos.Add(ffPosStr.Substring(1));

                }
                else
                {
                    pooledFF[loopCount].SetActive(false);
                }

                if (toggle && (isTimeout || loopCount + 1 >= nFF))
                {
                    onDur.Add(Time.realtimeSinceStartup - beginTime[beginTime.Count - 1] - programT0);
                }
            }
            else if (nFF > 1 && multiMode == 2)
            {
                foreach (GameObject FF in pooledFF)
                {
                    FF.SetActive(false);
                }
            }
            else
            {
                firefly.SetActive(false);
            }

            if (toggle && multiMode != 1)
            {
                onDur.Add(Time.realtimeSinceStartup - beginTime[beginTime.Count - 1] - programT0);
            }
        }
        //Nasta Add ends

        move = new Vector3(0.0f, 0.0f, 0.0f);
        velocity = 0.0f;
        line.SetActive(false);
        phase = Phases.check;
        currPhase = Phases.check;
        // Debug.Log("Trial Phase End.");
    }

    /// <summary>
    /// Save the player's position (pPos) and the firefly (reward zone)'s position (fPos)
    /// and start a coroutine to wait for some random amount of time between the user's
    /// specified minimum and maximum wait times
    /// </summary>
    async Task Check()
    {
        Debug.Log(loopCount);

        string ffPosStr = "";

        proximity = false;

        isReward = true;

        float distance = 0.0f;

        Vector3 pos;
        Quaternion rot;

        pPos = player.transform.position - new Vector3(0.0f, p_height, 0.0f);

        pos = player.transform.position;
        rot = player.transform.rotation;

        if (!isTimeout)
        {
            //source = new CancellationTokenSource();
            // Debug.Log("Check Phase Start.");

            float delay = c_lambda * Mathf.Exp(-c_lambda * ((float)rand.NextDouble() * (c_max - c_min) + c_min));
            // Debug.Log("firefly delay: " + delay);
            checkWait.Add(delay);

            // print("check delay average: " + checkWait.Average());

            // Wait until this condition is met in a different thread(?...not actually sure if its
            // in a different thread tbh), or until the check delay time is up. If the latter occurs
            // and the player is close enough to a FF, then the player gets the reward.
            if (ptb != 2)
            {
                await new WaitForSeconds(delay);
            } 
            else
            {
                //var t = Task.Run(async () =>
                //{
                //    await new WaitUntil(() => Vector3.Distance(pos, player.transform.position) > 0.05f); // Used to be rb.velocity.magnitude
                //    //UnityEngine.Debug.Log("exceeded threshold");
                //}, source.Token);

                //if (await Task.WhenAny(t, Task.Delay((int)(delay * 1000))) == t)
                //{
                //    audioSource.clip = loseSound;
                //    isReward = false;
                //}
            }
            source.Cancel();
        }
        else
        {
            checkWait.Add(0.0f);

            audioSource.clip = loseSound;
        }


        //Nasta commented out for sequential
        //if (nFF > 2)
        //{
        //    ffPosStr = string.Concat(ffPosStr, ffPositions[loopCount].ToString("F8").Trim(toTrim).Replace(" ", "")).Substring(1);
        //    distances.Add(Vector3.Distance(pPos, ffPositions[loopCount]));
        //    if (distances[loopCount] <= fireflyZoneRadius)
        //    {
        //        proximity = true;
        //    }
        //}
        //else
        //{
        //    if (Vector3.Distance(pPos, firefly.transform.position) <= fireflyZoneRadius) proximity = true;
        //    ffPosStr = firefly.transform.position.ToString("F8").Trim(toTrim).Replace(" ", "");
        //    distances.Add(Vector3.Distance(pPos, firefly.transform.position));
        //}

        //Nasta Added for sequential

        if (nFF > 1 && multiMode == 1)
        {
            //ffPosStr = string.Concat(ffPosStr, ",", pooledFF[loopCount].transform.position.ToString("F5").Trim(toTrim).Replace(" ", "")).Substring(1);
            foreach (GameObject FF in pooledFF)
            {
                ffPosStr = string.Concat(ffPosStr, ",", FF.transform.position.ToString("F5").Trim(toTrim).Replace(" ", ""));
                
            }

            Debug.Log(ffPosStr.Substring(1));

            distance = Vector3.Distance(pPos, pooledFF[loopCount].transform.position);
            //print(distance);
            distances[loopCount] = distance;
            if (distances[loopCount] <= fireflyZoneRadius)
            {
                //print(distances[loopCount]);
                proximity = true;
            }
        }
        else if (nFF > 1 && multiMode == 2)
        {
            foreach (GameObject FF in pooledFF)
            {
                //ffPosStr = string.Concat(ffPosStr, ",", FF.transform.position.ToString("F5").Trim(toTrim).Replace(" ", "")).Substring(1);
                distance = Vector3.Distance(pPos, FF.transform.position);
                //print(distance);
                if (distance <= fireflyZoneRadius)
                {
                    proximity = true;
                }
                distances.Add(distance);
            }
        }
        else
        {
            if (Vector3.Distance(pPos, firefly.transform.position) <= fireflyZoneRadius) proximity = true;
            distance = Vector3.Distance(pPos, firefly.transform.position);
            //ffPosStr = firefly.transform.position.ToString("F5").Trim(toTrim).Replace(" ", "");
            distances.Add(distance);
        }

        isCheck = true;

        //if (isReward && proximity)
        //{
        //    audioSource.clip = winSound;
        //    //points++;
        //}
        //else
        //{
        //    audioSource.clip = loseSound;
        //}

        //print(string.Format("Proximity: {0}", proximity));
        //print(string.Format("isReward: {0}", isReward));

        //Nasta Added sequential
        if (isReward && proximity)
        {
            if (nFF > 1 && multiMode == 1)
            {
                if (loopCount + 1 < nFF)
                {
                    //juiceTime += Mathf.Lerp(maxJuiceTime, minJuiceTime, Mathf.InverseLerp(0.0f, fireflyZoneRadius, distance));
                    //Debug.Log(string.Format("Firefly {0} Hit.", loopCount + 1));
                    audioSource.clip = neutralSound;
                    audioSource.Play();

                    player_origin = player.transform.position;
                }
                else
                {
                    //juiceTime += Mathf.Lerp(maxJuiceTime, minJuiceTime, Mathf.InverseLerp(0.0f, fireflyZoneRadius, distance));
                    //Debug.Log(string.Format("Firefly {0} Hit. Reward: {1}", loopCount + 1, juiceTime));
                    audioSource.clip = winSound;
                    //print(juiceTime);
                    //juiceDuration.Add(juiceTime);
                    audioSource.Play();
                    points++;
                    //SendMarker("j", juiceTime);
                    //await new WaitForSeconds((juiceTime / 1000.0f) + 0.25f);
                    //juiceTime = 0;
                    //Debug.Log("Juice: " + DateTime.Now.ToLongTimeString());
                }
            }
            else
            {
                audioSource.clip = winSound;
                //juiceTime = Mathf.Lerp(maxJuiceTime, minJuiceTime, Mathf.InverseLerp(0.0f, fireflyZoneRadius, distance));
                //print(juiceTime);
                //juiceDuration.Add(juiceTime);
                if (PlayerPrefs.GetInt("Feedback ON") == 0)
		        {
		            audioSource.clip = neutralSound;
		        }
                audioSource.Play();

                points++;
                //SendMarker("j", juiceTime);

                //await new WaitForSeconds((juiceTime / 1000.0f) + 0.25f);
                //Debug.Log("Juice: " + DateTime.Now.ToLongTimeString());
            }
        }
        else
        {
            audioSource.clip = loseSound;
            //juiceDuration.Add(0.0f);
            rewardTime.Add(0.0f);
            audioSource.Play();
            
            //await new WaitForSeconds((juiceTime / 1000.0f) + 0.25f);
        }

        // Nasta Added sequential
        if (nFF > 1 && multiMode == 1)
        {
            if (loopCount + 1 < nFF)
            {
                if (!isTimeout && isReward && proximity )
                {
                    //distances.Add(Vector3.Distance(pPos, pooledFF[loopCount].transform.position));
                    cPosTemp.Add(pos.ToString("F5").Trim(toTrim).Replace(" ", ""));
                    cRotTemp.Add(rot.ToString("F5").Trim(toTrim).Replace(" ", ""));

                    loopCount++;
                    phase = Phases.trial;
                    currPhase = Phases.trial;

                }
                else
                {

                    //Nasta Note: Here, even if an ff in the middle 
                    if (loopCount + 1 < nFF)
                    {
                        for (int i = loopCount + 1; i < nFF; i++)
                        {
                            //ffPosStr = string.Concat(ffPosStr, ",", pooledFF[loopCount].transform.position.ToString("F5").Trim(toTrim).Replace(" ", "")).Substring(1);
                            distances[i] = Vector3.Distance(pPos, pooledFF[i].transform.position);
                            cPosTemp.Add(Vector3.zero.ToString("F5").Trim(toTrim).Replace(" ", ""));
                            cRotTemp.Add(Quaternion.identity.ToString("F5").Trim(toTrim).Replace(" ", ""));
                        }
                        cPosTemp.Add(Vector3.zero.ToString("F5").Trim(toTrim).Replace(" ", ""));
                        cRotTemp.Add(Quaternion.identity.ToString("F5").Trim(toTrim).Replace(" ", ""));
                    }

                    player.transform.position = Vector3.up * p_height;
                    player.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

                    score.Add(isReward && proximity ? 1 : 0);
                    timedout.Add(isTimeout ? 1 : 0);
                    cPos.Add(string.Join(",", cPosTemp));
                    cRot.Add(string.Join(",", cRotTemp));
                    dist.Add(string.Join(",", distances));
                    answer.Add(0);
                    densities.Add(0.0f);
                    ffPos.Add(ffPosStr.Substring(1));

                    float wait = i_lambda * Mathf.Exp(-i_lambda * ((float)rand.NextDouble() * (i_max - i_min) + i_min));

                    currPhase = Phases.ITI;

                    interWait.Add(wait);

                    isEnd = true;

                    ffPosStr = "";
                    cPosTemp.Clear();
                    cRotTemp.Clear();
                    isTimeout = false;

                    await new WaitForSeconds(wait);

                    phase = Phases.begin;
                    // Debug.Log("Check Phase End.");
                }
            }
            else
            {
                score.Add(isReward && proximity ? 1 : 0);
                timedout.Add(isTimeout ? 1 : 0);
                cPosTemp.Add(pos.ToString("F5").Trim(toTrim).Replace(" ", ""));
                cRotTemp.Add(rot.ToString("F5").Trim(toTrim).Replace(" ", ""));
                cPos.Add(string.Join(",", cPosTemp));
                cRot.Add(string.Join(",", cRotTemp));
                dist.Add(string.Join(",", distances));
                answer.Add(0);
                densities.Add(0.0f);
                ffPos.Add(ffPosStr.Substring(1));

                float wait = i_lambda * Mathf.Exp(-i_lambda * ((float)rand.NextDouble() * (i_max - i_min) + i_min));

                currPhase = Phases.ITI;

                interWait.Add(wait);

                isEnd = true;

                cPosTemp.Clear();
                cRotTemp.Clear();
                ffPosStr = "";
                isTimeout = false;
                //nasta added 
                player.transform.position = Vector3.up * p_height;
                player.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                //Nasta Add ends
                await new WaitForSeconds(wait);

                phase = Phases.begin;
            }
        }
        else if (nFF > 1 && multiMode == 2)
        {
            score.Add(isReward && proximity ? 1 : 0);
            timedout.Add(isTimeout ? 1 : 0);
            cPos.Add(pos.ToString("F5").Trim(toTrim).Replace(" ", ""));
            cRot.Add(rot.ToString("F5").Trim(toTrim).Replace(" ", ""));
            dist.Add(string.Join(",", distances));
            answer.Add(0);
            ffPos.Add(ffPosStr);

            float wait = i_lambda * Mathf.Exp(-i_lambda * ((float)rand.NextDouble() * (i_max - i_min) + i_min));

            currPhase = Phases.ITI;

            interWait.Add(wait);

            isEnd = true;

            distances.Clear();
            ffPosStr = "";
            isTimeout = false;

            await new WaitForSeconds(wait);
            
            phase = Phases.begin;
        }
        else
        {
            timedout.Add(isTimeout ? 1 : 0);
            score.Add(isReward && proximity ? 1 : 0);
            ffPos.Add(ffPosStr);
            dist.Add(distances[0].ToString("F5"));
            cPos.Add(pos.ToString("F5").Trim(toTrim).Replace(" ", ""));
            cRot.Add(rot.ToString("F5").Trim(toTrim).Replace(" ", ""));

            ffPositions.Clear();
            distances.Clear();
            ffPosStr = "";

            isTimeout = false;
            player.transform.position = Vector3.up * p_height;
            player.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

            if (isMoving)
            {
                currPhase = Phases.question;

                await new WaitUntil(() => Mathf.Abs(SharedJoystick.currentSpeed) <= velocityThreshold && Mathf.Abs(SharedJoystick.currentRot) <= rotationThreshold && (SharedJoystick.moveX == 0.0f && SharedJoystick.moveY == 0.0f));

                panel.SetActive(true);

                await new WaitUntil(() => SharedJoystick.currentRot < -10.0f || SharedJoystick.currentRot > 10.0f);

                if (SharedJoystick.currentRot < 0.0f)
                {
                    // no
                    answer.Add(0);
                }
                else
                {
                    // yes
                    answer.Add(1);
                }

                panel.SetActive(false);

                await new WaitUntil(() => (Mathf.Abs(SharedJoystick.currentSpeed) <= velocityThreshold && Mathf.Abs(SharedJoystick.currentRot) <= rotationThreshold) && (SharedJoystick.moveX == 0.0f && SharedJoystick.moveY == 0.0f));
            }
            else
            {
                answer.Add(0);
            }

            //player.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            //player.transform.position = Vector3.up * p_height;

            if (!proximity && (PlayerPrefs.GetInt("Feedback ON") == 1))
            {
                currPhase = Phases.feedback;
                firefly.SetActive(true);
                mesh.enabled = true;
                text.enabled = true;
                await new WaitForSeconds(2.0f);
                text.enabled = false;
                mesh.enabled = false;
                firefly.SetActive(false);
            }

            float wait = i_lambda * Mathf.Exp(-i_lambda * ((float)rand.NextDouble() * (i_max - i_min) + i_min));

            currPhase = Phases.ITI;

            interWait.Add(wait);

            //Debug.Log("Trial End: " + DateTime.Now.ToLongTimeString());

            isEnd = true;

            await new WaitForSeconds(wait);

            phase = Phases.begin;
            // Debug.Log("Check Phase End.");
        }


    }

    void SetFireflyLocation()
    {
        if (nFF > 1)
        {
            List<Vector3> posTemp = new List<Vector3>();
            Vector3 closestPos = new Vector3(0.0f, 0.0f, 0.0f);
            float[] distTemp = new float[(int)nFF];
            int[] idx = new int[distTemp.Length];
            for (int i = 0; i < nFF; i++)
            {
                Vector3 position_i;
                bool tooClose;
                do
                {
                    tooClose = false;
                    float r_i = minDrawDistance + (maxDrawDistance - minDrawDistance) * Mathf.Sqrt((float)rand.NextDouble());
                    float angle_i = (float)rand.NextDouble() * (maxPhi - minPhi) + minPhi;
                    if (LR != 0.5f)
                    {
                        float side_i = rand.NextDouble() < LR ? 1 : -1;
                        position_i = (player.transform.position - new Vector3(0.0f, p_height, 0.0f)) + Quaternion.AngleAxis(angle_i * side_i, Vector3.up) * player.transform.forward * r_i;
                    }
                    else
                    {
                        position_i = (player.transform.position - new Vector3(0.0f, p_height, 0.0f)) + Quaternion.AngleAxis(angle_i, Vector3.up) * player.transform.forward * r_i;
                    }
                    position_i.y = 0.0001f;
                    if (i > 0) for (int k = 0; k < i; k++) { if (Vector3.Distance(position_i, pooledFF[k].transform.position) <= 1.0f || Mathf.Abs(position_i.x - pooledFF[k - 1].transform.position.x) >= 0.5f || Mathf.Abs(position_i.z - pooledFF[k - 1].transform.position.z) <= 0.5f) tooClose = true; }
                }
                while (tooClose);
                posTemp.Add(position_i);
                distTemp[i] = Vector3.Distance(player.transform.position, position_i);
                idx[i] = i;
                ffPositions.Add(position_i);
            }
            Array.Sort(distTemp, idx);
            for (int i = 0; i < idx.Length; i++) { pooledFF[i].transform.position = posTemp[idx[i]]; }
        }
        else
        {
            Vector3 position;
            float r = minDrawDistance + (maxDrawDistance - minDrawDistance) * Mathf.Sqrt((float)rand.NextDouble());
            float angle = (float)rand.NextDouble() * (maxPhi - minPhi) + minPhi;
            if (LR != 0.5f)
            {
                float side = rand.NextDouble() < LR ? 1 : -1;
                position = (player.transform.position - new Vector3(0.0f, p_height, 0.0f)) + Quaternion.AngleAxis(angle * side, Vector3.up) * player.transform.forward * r;
            }
            else
            {
                position = (player.transform.position - new Vector3(0.0f, p_height, 0.0f)) + Quaternion.AngleAxis(angle, Vector3.up) * player.transform.forward * r;
            }
            position.y = 0.0001f;
            firefly.transform.position = position;
            ffPositions.Add(position);
            initialD = Vector3.Distance(player.transform.position, firefly.transform.position);
        }
    }

    void SetStimParams()
    {
        float r = (float)rand.NextDouble();

        if (r <= toggleRatios[0])
        {
            //v1
            flowDuration = toggleDurations[0];
        }
        else if (r > toggleRatios[0] && r <= toggleRatios[1])
        {
            //v2
            //flowDuration = toggleDurations[1];
        }
        else if (r > toggleRatios[1] && r <= toggleRatios[2])
        {
            //v3
            //flowDuration = toggleDurations[2];
        }
        else if (r > toggleRatios[2] && r <= toggleRatios[3])
        {
            //v4
            flowDuration = toggleDurations[3];
        }
        else if (r > toggleRatios[3] && r <= toggleRatios[4])
        {
            //v5
            //flowDuration = toggleDurations[4];
        }
        else if (r > toggleRatios[4] && r <= toggleRatios[5])
        {
            //v6
            flowDuration = toggleDurations[5];
        }
        else if (r > toggleRatios[5] && r <= toggleRatios[6])
        {
            //v7
            flowDuration = toggleDurations[6];
        }
        else if (r > toggleRatios[6] && r <= toggleRatios[7])
        {
            //v8
            flowDuration = toggleDurations[7];
        }
        else if (r > toggleRatios[7] && r <= toggleRatios[8])
        {
            //v9
            flowDuration = toggleDurations[8];
        }
        else if (r > toggleRatios[8] && r <= toggleRatios[9])
        {
            //v10
            flowDuration = toggleDurations[9];
        }
        else if (r > toggleRatios[9] && r <= toggleRatios[10])
        {
            //v11
            flowDuration = toggleDurations[10];
        }
        else
        {
            //v12
            flowDuration = toggleDurations[11];
        }

        flowDur.Add(flowDuration);
    }

    void SetGaussParams()
    {
        //if ((float)rand.NextDouble() < moveRatio)
        //{
        float r = (float)rand.NextDouble();

        if (r <= ampRatios[0])
        {
            //v1
            currentAmp = amplitudes[0];
            currentAmpDur = ampDurations[0];
            //flowDuration = toggleDurations[0];
        }
        else if (r > ampRatios[0] && r <= ampRatios[1])
        {
            //v2
            print("2");
            currentAmp = amplitudes[1];
            currentAmpDur = ampDurations[1];
            //flowDuration = toggleDurations[1];
        }
        else if (r > ampRatios[1] && r <= ampRatios[2])
        {
            //v3
            print("3");
            currentAmp = amplitudes[2];
            currentAmpDur = ampDurations[2];
            //flowDuration = toggleDurations[2];
        }
        else if (r > ampRatios[2] && r <= ampRatios[3])
        {
            //v4
            print("4");
            currentAmp = amplitudes[3];
            currentAmpDur = ampDurations[3];
            //flowDuration = toggleDurations[3];
        }
        else if (r > ampRatios[3] && r <= ampRatios[4])
        {
            //v5
            print("5");
            currentAmp = amplitudes[4];
            currentAmpDur = ampDurations[4];
            //flowDuration = toggleDurations[4];
        }
        else if (r > ampRatios[4] && r <= ampRatios[5])
        {
            //v6
            print("6");
            currentAmp = amplitudes[5];
            currentAmpDur = ampDurations[5];
            //flowDuration = toggleDurations[5];
        }
        else if (r > ampRatios[5] && r <= ampRatios[6])
        {
            //v7
            print("7");
            currentAmp = amplitudes[6];
            currentAmpDur = ampDurations[6];
            //flowDuration = toggleDurations[6];
        }
        else if (r > ampRatios[6] && r <= ampRatios[7])
        {
            //v8
            print("8");
            currentAmp = amplitudes[7];
            currentAmpDur = ampDurations[7];
            //flowDuration = toggleDurations[7];
        }
        else if (r > ampRatios[7] && r <= ampRatios[8])
        {
            //v9
            print("9");
            currentAmp = amplitudes[8];
            currentAmpDur = ampDurations[8];
            //flowDuration = toggleDurations[8];
        }
        else if (r > ampRatios[8] && r <= ampRatios[9])
        {
            //v10
            print("10");
            currentAmp = amplitudes[9];
            currentAmpDur = ampDurations[9];
            //flowDuration = toggleDurations[9];
        }
        else if (r > ampRatios[9] && r <= ampRatios[10])
        {
            //v11
            print("11");
            currentAmp = amplitudes[10];
            currentAmpDur = ampDurations[10];
            //flowDuration = toggleDurations[10];
        }
        else
        {
            //v12
            print("12");
            currentAmp = amplitudes[11];
            currentAmpDur = ampDurations[11];
            //flowDuration = toggleDurations[11];
        }

        currentDirection = player.transform.forward;

        gaussAmp.Add(currentAmp);
        gaussDur.Add(currentAmpDur);
    }

    public async void OnOff(float time)
    {
        CancellationTokenSource source = new CancellationTokenSource();

        firefly.SetActive(true);

        var t = Task.Run(async () =>
        {
            await new WaitForSeconds(time);
        }, source.Token);

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

    public async void OnOff(GameObject obj, float time)
    {
        CancellationTokenSource source = new CancellationTokenSource();

        firefly.SetActive(true);

        var t = Task.Run(async () =>
        {
            await new WaitForSeconds(time);
        }, source.Token);

        if (await Task.WhenAny(t, Task.Run(async () => { await new WaitUntil(() => currPhase == Phases.check); })) == t)
        {
            obj.SetActive(false);
        }
        else
        {
            obj.SetActive(false);
        }

        source.Cancel();
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
    /// 
    public async Task Flash(GameObject obj)
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
                obj.GetComponent<SpriteRenderer>().enabled = false;
                await new WaitForSeconds((1f / freq) - PW);
            }
        }
    }

    public async void WriteToFile()
    {
        StringBuilder sb = new StringBuilder();
        //Debug.Log("Writing Start");
        while (playing || stringList.Count > 0)
        {
            if (stringList.Count > 0)
            {
                sb.Append(stringList[0]);
                stringList.RemoveAt(0);

                //Debug.Log("writing...");
            }
            await new WaitForFixedUpdate();
        }
        File.AppendAllText(contPath, sb.ToString());
        //writing = false;
    }

    private float Tcalc(float t, float lambda)
    {
        return -1.0f / lambda * Mathf.Log(t / lambda);
    }

    public float RandomizeSpeeds(float min, float max)
    {
        return (float)(rand.NextDouble() * (max - min) + min);
    }

    public (float, float[]) MakeProfile()
    {
        float sigma = 1.0f / (2.0f * Mathf.Sqrt(currentAmpDur));
        gaussSD.Add(sigma);
        float mean = 0.0f;
        int size = Mathf.RoundToInt(currentAmpDur / Time.fixedDeltaTime); // 3 seconds

        float[] temp = new float[size];
        float[] t = new float[size];

        for (int i = 0; i < size; i++)
        {
            t[i] = Time.fixedDeltaTime * i - (Time.fixedDeltaTime * size / 2);
        }

        float alpha = 1.0f / (sigma * Mathf.Sqrt(2.0f * Mathf.PI));

        for (int i = 0; i < size; i++)
        {
            temp[i] = currentAmp * alpha * Mathf.Exp(-0.5f * ((Mathf.Pow(t[i] - mean, 2.0f)) / Mathf.Pow(sigma, 2.0f)));
        }

        float halfArea = 0.0f;

        for (int i = Mathf.RoundToInt(size / 2.0f); i < size; i++)
        {
            halfArea += temp[i];
        }

        print(halfArea);

        return (halfArea, temp);
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
        }

        return (coords, hit);
    }


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
            string firstLine;

            List<int> temp;

            StringBuilder csvDisc = new StringBuilder();

            if (nFF > 1)
            {
                string ffPosStr = "";
                string cPosStr = "";
                string cRotStr = "";
                string distStr = "";
                string checkStr = "";

                for (int i = 0; i < nFF; i++)
                {
                    ffPosStr = string.Concat(ffPosStr, string.Format("ffX{0},ffY{0},ffZ{0},", i));
                    cPosStr = string.Concat(cPosStr, string.Format("pCheckX{0},pCheckY{0},pCheckZ{0},", i));
                    cRotStr = string.Concat(cRotStr, string.Format("rCheckX{0},rCheckY{0},rCheckZ{0},rCheckW{0},", i));
                    distStr = string.Concat(distStr, string.Format("distToFF{0},", i));
                    checkStr = string.Concat(checkStr, string.Format("checkTime{0},", i));
                }

                if (nFF > 1 && multiMode == 1)
                {
                    firstLine = string.Format("n,max_v,max_w,ffv,onDuration,PosX0,PosY0,PosZ0,RotX0,RotY0,RotZ0,RotW0,{0}{1}{2}{3}rewarded,timeout,beginTime,{4}endTime,checkWait,interWait", ffPosStr, cPosStr, cRotStr, distStr, checkStr);
                }
                else
                {
                    firstLine = string.Format("n,max_v,max_w,ffv,onDuration,density,PosX0,PosY0,PosZ0,RotX0,RotY0,RotZ0,RotW0,{0}pCheckX,pCheckY,pCheckZ,rCheckX,rCheckY,rCheckZ,rCheckW,{1}rewarded,timeout,juiceDuration,beginTime,checkTime,rewardTime,endTime,checkWait,interWait", ffPosStr, distStr);
                }
            }
            else
            {
                firstLine = "n,max_v,max_w,ffv,onDuration,density,PosX0,PosY0,PosZ0,RotX0,RotY0,RotZ0,RotW0,ffX,ffY,ffZ,pCheckX,pCheckY,pCheckZ,rCheckX,rCheckY,rCheckZ,rCheckW,distToFF,rewarded,timeout,juiceDuration,beginTime,checkTime,rewardTime,endTime,checkWait,interWait," + PlayerPrefs.GetString("Name") + "," + PlayerPrefs.GetString("Date") + "," + PlayerPrefs.GetInt("Run Number").ToString("D3");
            }

            csvDisc.AppendLine(firstLine);

            temp = new List<int>()
            {
                origin.Count,
                heading.Count,
                ffPos.Count,
                dist.Count,
                n.Count,
                answer.Count,
                cPos.Count,
                cRot.Count,
                beginTime.Count,
                endTime.Count,
                checkWait.Count,
                interWait.Count,
                score.Count,
                timedout.Count,
                max_v.Count,
                max_w.Count,
                fv.Count,
                onDur.Count,
            };
            if (ptb != 2)
            {
                //print("akis ptb");
                temp.Add(tautau.Count);
                temp.Add(filterTau.Count);
            }
            if (isGaussian || isFlowToggle)
            {
                //print("jp protocol");
                temp.Add(gaussAmp.Count);
                temp.Add(gaussDur.Count);
                temp.Add(gaussSD.Count);
                temp.Add(flowDur.Count);
            }

            
            //nasta added
            if (nFF > 1 && multiMode == 1)
            {
                temp.Add(checkTimeStrList.Count);
            }
            else
            {
                temp.Add(checkTime.Count);
            }

            //for (int i = 0; i < temp.Count; i++)
            //{
            //    print(temp[i]);
            //}

            temp.Sort();

            var totalScore = 0;

            if (multiMode == 1)
            {
                for (int i = 0; i < temp[0]; i++)
                {
                    //     firstLine = string.Format("n,max_v,max_w,ffv,onDuration,PosX0,PosY0,PosZ0,RotX0,RotY0,RotZ0,RotW0,{0}pCheckX,pCheckY,pCheckZ,rCheckX,rCheckY,rCheckZ,rCheckW,{1}timeout,beginTime,{2}rewardTime,endTime,checkWait,interWait", ffPosStr, distStr, checkStr);

                    var line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17}",
                        n[i],
                        max_v[i],
                        max_w[i],
                        fv[i],
                        onDur[i],
                        origin[i],
                        heading[i],
                        ffPos[i],
                        cPos[i],
                        cRot[i],
                        dist[i],
                        score[i],
                        timedout[i],
                        beginTime[i],
                        checkTimeStrList[i],
                        endTime[i],
                        checkWait[i],
                        interWait[i]);
                    csvDisc.AppendLine(line);

                    totalScore += score[i];
                }
            }
            else if (ptb == 0 || ptb == 1)
            {
                for (int i = 0; i < temp[0]; i++)
                {
                    var line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19}",
                        n[i],
                        max_v[i],
                        max_w[i],
                        fv[i],
                        onDur[i],
                        origin[i],
                        heading[i],
                        ffPos[i],
                        cPos[i],
                        cRot[i], 
                        dist[i],
                        score[i], 
                        timedout[i], 
                        beginTime[i],
                        checkTime[i], 
                        endTime[i], 
                        checkWait[i], 
                        interWait[i], 
                        tautau[i],
                        filterTau[i]);
                    csvDisc.AppendLine(line);
                }
            }
            else
            {
                for (int i = 0; i < temp[0]; i++)
                {
                    var line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22}",
                        n[i],
                        max_v[i],
                        max_w[i],
                        fv[i],
                        onDur[i],
                        answer[i],
                        origin[i],
                        heading[i],
                        ffPos[i],
                        cPos[i],
                        cRot[i],
                        dist[i],
                        score[i],
                        timedout[i],
                        beginTime[i],
                        checkTime[i],
                        endTime[i],
                        checkWait[i],
                        interWait[i],
                        gaussAmp[i],
                        gaussDur[i],
                        gaussSD[i],
                        flowDur[i]);
                    csvDisc.AppendLine(line);
                }
            }

            string discPath = path + "/discontinuous_data_" + PlayerPrefs.GetInt("Optic Flow Seed").ToString() + ".txt";

            //File.Create(discPath);
            File.WriteAllText(discPath, csvDisc.ToString());

            //PlayerPrefs.GetInt("Save") == 1)

            string configPath = path + "/config_" + PlayerPrefs.GetInt("Optic Flow Seed").ToString() + ".xml";

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.OmitXmlDeclaration = true;
            settings.NewLineOnAttributes = true;

            XmlWriter xmlWriter = XmlWriter.Create(configPath, settings);

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

            xmlWriter.WriteStartElement("EyeMode");
            xmlWriter.WriteString(PlayerPrefs.GetInt("Eye Mode").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("FPMode");
            xmlWriter.WriteString(PlayerPrefs.GetInt("FP Mode").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("FeedbackON");
            xmlWriter.WriteString(PlayerPrefs.GetInt("Feedback ON").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AboveBelow");
            xmlWriter.WriteString(PlayerPrefs.GetInt("AboveBelow").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("Setting");
            xmlWriter.WriteAttributeString("Type", "Joystick Settings");

            xmlWriter.WriteStartElement("Type");
            xmlWriter.WriteString(PlayerPrefs.GetInt("Type").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("TauTau");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Tau Tau").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("FilterTau");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Filter Tau").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("RotationNoiseGain");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Rotation Noise Gain").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("VelocityNoiseGain");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Velocity Noise Gain").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("NumberOfTaus");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Number Of Taus").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("MinTau");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Min Tau").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("MaxTau");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Max Tau").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("MeanDistance");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Mean Distance").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("MeanTravelTime");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Mean Travel Time").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("VelocityThreshold");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Velocity Threshold").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("RotationThreshold");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Rotation Threshold").ToString());
            xmlWriter.WriteEndElement();

            //xmlWriter.WriteStartElement("MinVelocityGain");
            //xmlWriter.WriteString(PlayerPrefs.GetFloat("Min Velocity Gain").ToString());
            //xmlWriter.WriteEndElement();

            //xmlWriter.WriteStartElement("MaxVelocityGain");
            //xmlWriter.WriteString(PlayerPrefs.GetFloat("Max Velocity Gain").ToString());
            //xmlWriter.WriteEndElement();

            //xmlWriter.WriteStartElement("MinRotationGain");
            //xmlWriter.WriteString(PlayerPrefs.GetFloat("Min Rotation Gain").ToString());
            //xmlWriter.WriteEndElement();

            //xmlWriter.WriteStartElement("MaxRotationGain");
            //xmlWriter.WriteString(PlayerPrefs.GetFloat("MinRotationGain").ToString());
            //xmlWriter.WriteEndElement();

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

            xmlWriter.WriteStartElement("LineOnOff");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Line OnOff").ToString());
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
            xmlWriter.WriteAttributeString("Type", "Gaussian Perturbation Settings");

            xmlWriter.WriteStartElement("GaussianPerturbationON");
            xmlWriter.WriteString(PlayerPrefs.GetInt("Gaussian Perturbation ON").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("A1");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("A1").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("A2");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("A2").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("A3");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("A3").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("A4");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("A4").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("A5");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("A5").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("A6");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("A6").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("A7");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("A7").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("A8");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("A8").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("A9");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("A9").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("A10");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("A10").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("A11");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("A11").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("A12");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("A12").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AR1");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("AR1").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AR2");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("AR2").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AR3");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("AR3").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AR4");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("AR4").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AR5");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("AR5").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AR6");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("AR6").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AR7");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("AR7").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AR8");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("AR8").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AR9");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("AR9").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AR10");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("AR10").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AR11");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("AR11").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AR12");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("AR12").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AD1");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("AD1").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AD2");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("AD2").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AD3");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("AD3").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AD4");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("AD4").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AD5");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("AD5").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AD6");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("AD6").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AD7");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("AD7").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AD8");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("AD8").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AD9");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("AD9").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AD10");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("AD10").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AD11");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("AD11").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("AD12");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("AD12").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("Setting");
            xmlWriter.WriteAttributeString("Type", "Optic Flow OnOff Settings");

            xmlWriter.WriteStartElement("OpticFlowOnOff");
            xmlWriter.WriteString(PlayerPrefs.GetInt("Optic Flow OnOff").ToString());
            xmlWriter.WriteEndElement();


            xmlWriter.WriteStartElement("T1");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("T1").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("T2");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("T2").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("T3");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("T3").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("T4");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("T4").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("T5");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("T5").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("T6");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("T6").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("T7");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("T7").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("T8");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("T8").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("T9");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("T9").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("T10");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("T10").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("T11");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("T11").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("T12");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("T12").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("TR1");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("TR1").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("TR2");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("TR2").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("TR3");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("TR3").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("TR4");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("TR4").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("TR5");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("TR5").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("TR6");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("TR6").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("TR7");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("TR7").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("TR8");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("TR8").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("TR9");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("TR9").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("TR10");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("TR10").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("TR11");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("TR11").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("TR12");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("TR12").ToString());
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
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError(e);
        }
    }
}