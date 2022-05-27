///////////////////////////////////////////////////////////////////////////////////////////
///                                                                                     ///
/// Monkey2D.cs                                                                         ///
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
using System;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEngine.XR.Provider;
using UnityEngine.XR.Management;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;
using System.Collections;

public class Monkey2D : MonoBehaviour
{
    public static Monkey2D SharedMonkey;

    public GameObject firefly;
    //public GameObject marker;
    //public GameObject panel;
    public Camera Lcam;
    public Camera Rcam;
    public GameObject FP;
    //public GameObject Marker;
    // public GameObject inner;
    // public GameObject outer;
    wrmhl juiceBox = new wrmhl();

    [Tooltip("SerialPort of your device.")]
    public string portName = "COM3";

    [Tooltip("Baudrate")]
    public int baudRate = 1000000;

    [Tooltip("Timeout")]
    public int ReadTimeout = 5000;

    [Tooltip("QueueLength")]
    public int QueueLength = 1;
    [Tooltip("Radius of firefly")]
    [ShowOnly] public float fireflySize;
    [Tooltip("Maximum distance allowed from center of firefly")]
    [ShowOnly] public float fireflyZoneRadius;
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
    [ShowOnly] public float ratio;
    [Tooltip("Frequency of flashing firefly (Flashing Firefly Only)")]
    [ShowOnly] public float freq;
    [Tooltip("Duty cycle for flashing firefly (percentage of one period determing how long it stays on during one period) (Flashing Firefly Only)")]
    [ShowOnly] public float duty;
    // Pulse Width; how long in seconds it stays on during one period
    private float PW;
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
    [Tooltip("How many fireflies can appear at once")]
    [ShowOnly] public float nFF;
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
    private bool isMoving;
    private bool LRFB;
    private float moveRatio;
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
    private float c_lambda;
    // 1 / Mean for intertrial time exponential distribution
    private float i_lambda;
    // x values for exponential distribution
    private float c_min;
    private float c_max;
    private float i_min;
    private float i_max;
    private float velMin;
    private float velMax;
    private float rotMin;
    private float rotMax;
    public enum Phases
    {
        begin = 0,
        trial = 1,
        check = 2,
        //question = 3,
        juice = 3,
        ITI = 4,
        none = 9
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

    // Think the FF moves or not?
    //readonly List<int> answer = new List<int>();
    [ShowOnly] public bool isQuestion = false;

    // Timed Out?
    readonly List<int> timedout = new List<int>();

    public EyeData data = new EyeData();
    //public GameObject OurCamera;

    // Left Eye Gaze
    readonly List<string> GazeLeftVerbose = new List<string>();

    // Left Eye Origin
    readonly List<string> GazeLeftOriginVerbose = new List<string>();

    // Right Eye Gaze
    readonly List<string> GazeRightVerbose = new List<string>();

    // Right Eye Origin
    readonly List<string> GazeRightOriginVerbose = new List<string>();

    // Combined Gaze
    readonly List<string> GazeCombVerbose = new List<string>();

    // Combined Origin
    readonly List<string> GazeOriginCombVerbose = new List<string>();

    // Pupil Diameters
    readonly List<string> PupilDiamVerbose = new List<string>();

    // Eye Openness
    readonly List<string> OpennessVerbose = new List<string>();

    // Eye Convergence Distance
    readonly List<float> ConvergenceDistanceVerbose = new List<float>();
    //readonly List<float> ConvergenceDistanceVerbose_frame = new List<float>();
    readonly List<float> ConvergenceDistanceL = new List<float>();
    readonly List<float> ConvergenceDistanceR = new List<float>();

    // Gaze hit locations
    readonly List<string> HitLocations = new List<string>();
    //readonly List<string> HitLocations_frame = new List<string>();
    readonly List<string> HitLocations2D = new List<string>();
    readonly List<string> HitLocationsL = new List<string>();
    readonly List<string> HitLocationsR = new List<string>();
    readonly List<string> HitLocationsL2D = new List<string>();
    readonly List<string> HitLocationsR2D = new List<string>();

    // Current Phase
    readonly List<int> epoch = new List<int>();

    // Was Always ON?
    readonly List<bool> alwaysON = new List<bool>();

    //public float GazeX = 0.0f;
    //public float GazeY = 0.0f;
    //public float GazeZ = 0.0f;
    //public float GazeW = 0.0f;
    //public float GazeXOrigin = 0.0f;
    //public float GazeYOrigin = 0.0f;
    //public float GazeZOrigin = 0.0f;

    //public float LeftGazeX = 0.0f;
    //public float LeftGazeY = 0.0f;
    //public float LeftGazeZ = 0.0f;

    //public float RightGazeX = 0.0f;
    //public float RightGazeY = 0.0f;
    //public float RightGazeZ = 0.0f;

    //public float RightGazeOriginX = 0.0f;
    //public float RightGazeOriginY = 0.0f;
    //public float RightGazeOriginZ = 0.0f;

    //public float LeftGazeOriginX = 0.0f;
    //public float LeftGazeOriginY = 0.0f;
    //public float LeftGazeOriginZ = 0.0f;

    //public float LeftPupilDiam = 0.0f;
    //public float RightPupilDiam = 0.0f;

    //public float LeftEyeopenness = 0.0f;
    //public float RightEyeopenness = 0.0f;

    public float FrameTimeElement = 0;

    public float delayTime = .2f;

    public bool Detector = false;
    //List<float> Frametime = new List<float>();

    public int LengthOfRay = 75;
    //[SerializeField] private LineRenderer GazeRayRenderer;

    public string sceneTypeVerbose;
    public string systemStartTimeVerbose;

    public static Vector3 hitpoint;

    private Vector2 RHit2D;
    private Vector2 LHit2D;
    //readonly private float angleR;
    //readonly private float angleL;

    // File paths
    private string path;

    [ShowOnly] public int trialNum;
    private float trialT0;
    //private float trialT;
    private float programT0 = 0.0f;

    [ShowOnly] public float points = 0;
    [Tooltip("How long the juice valve is open")]
    [ShowOnly] public float juiceTime;

    [Tooltip("Maximum number of trials before quitting (0 for infinity)")]
    [ShowOnly] public int ntrials;

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

    //public GameObject arrow;
    //private MeshRenderer mesh;
    //public TMPro.TMP_Text text;
    public Vector3 player_origin;

    private string contPath;

    private float ipd;

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

    //public float left = -0.2F;
    //public float right = 0.2F;
    //public float top = 0.2F;
    //public float bottom = -0.2F;


    //public Camera MainR;
    //public Camera MainL;
    //public GameObject texL;
    //public GameObject texR;
    public float offset = 0.01f;
    //public float offsetL = 0.01f;
    private float lm02;
    private float rm02;

    private Matrix4x4 lm;
    private Matrix4x4 rm;

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
        //UnityEngine.XR.XRDevice.DisableAutoXRCameraTracking(MainL, true);
        //UnityEngine.XR.XRDevice.DisableAutoXRCameraTracking(MainR, true);

        //CommandBuffer commandBuffer = new CommandBuffer();

        XRSettings.occlusionMaskScale = 10f;
        XRSettings.useOcclusionMesh = false;
        Lcam.ResetProjectionMatrix();
        Rcam.ResetProjectionMatrix();
        Lcam.ResetStereoProjectionMatrices();
        Rcam.ResetStereoProjectionMatrices();
        Lcam.ResetStereoViewMatrices();
        Rcam.ResetStereoViewMatrices();
        //Lcam.projectionMatrix = Matrix4x4.Perspective(68, 16f / 9f, 0.01f, 1000f);
        //Rcam.projectionMatrix = Matrix4x4.Perspective(68, 16f / 9f, 0.01f, 1000f);
        //Lcam.cameraType = CameraType.VR;
        //Rcam.cameraType = CameraType.VR;
        //Lcam.orthographic = false;
        //Rcam.orthographic = false;

        //lm = Lcam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left); ; //lm = Lcam.projectionMatrix;
        //lm02 = lm.m02;
        //rm = Rcam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right); //rm = Rcam.projectionMatrix;
        //rm02 = rm.m02;
        print(lm.ToString());
        //print("lm02: " + lm02.ToString());
        //print("rm02: " + rm02.ToString());
        List<XRDisplaySubsystem> displaySubsystems = new List<XRDisplaySubsystem>();
        SubsystemManager.GetInstances<XRDisplaySubsystem>(displaySubsystems);
        if (displaySubsystems.Count > 0)
        {
            //while (!SharedEye.ready);
        }
        print(XRSettings.loadedDeviceName);
        if (!XRSettings.enabled)
        {
            XRSettings.enabled = true;
        }
        XRSettings.occlusionMaskScale = 2f;
        XRSettings.useOcclusionMesh = false;


        //displaySubsystems[0].scaleOfAllViewports = 1.0f;
        //print(displaySubsystems[0].scaleOfAllViewports);

        portName = PlayerPrefs.GetString("Port");
        juiceBox.set(portName, baudRate, ReadTimeout, QueueLength);
        juiceBox.connect();

        SharedMonkey = this;

        //Lcam.targetTexture.width = Mathf.RoundToInt(Lcam.targetTexture.width * 2f);

        //int height = Screen.height;
        //int width = Screen.width;
        //Screen.SetResolution((int)Mathf.Round(width * 1.5f), height, FullScreenMode.FullScreenWindow);
        //SetVanishingPoint(Camera.main, new Vector2(-0.03f, 0.0f));
        //Matrix4x4 viewL = Lcam.projectionMatrix;
        //Matrix4x4 viewR = Rcam.projectionMatrix;

        //viewL.m11 *= 1.5f;
        //viewR.m11 *= 1.5f;

        //Lcam.pixelRect = new Rect(Screen.width, Screen.height, 2000f, 1000f);

        //Lcam.projectionMatrix = viewL;
        //Rcam.projectionMatrix = viewR;

        //Camera.main.SetStereoViewMatrix(Camera.StereoscopicEye.Left, viewL);
        //Camera.main.SetStereoViewMatrix(Camera.StereoscopicEye.Right, viewR);

        //mesh = arrow.GetComponent<MeshRenderer>();
        //mesh.enabled = false;

        //text.enabled = false;
        ntrials = (int)PlayerPrefs.GetFloat("Num Trials");
        if (ntrials == 0) ntrials = 9999;
        seed = UnityEngine.Random.Range(1, 10000);
        rand = new System.Random(seed);
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
        velMin = PlayerPrefs.GetFloat("Min Linear Speed");
        velMax = PlayerPrefs.GetFloat("Max Linear Speed");
        rotMin = PlayerPrefs.GetFloat("Min Angular Speed");
        rotMax = PlayerPrefs.GetFloat("Max Angular Speed");
        minDrawDistance = PlayerPrefs.GetFloat("Minimum Firefly Distance");
        maxDrawDistance = PlayerPrefs.GetFloat("Maximum Firefly Distance");
        juiceTime = PlayerPrefs.GetFloat("Juice Time");
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
        moveRatio = PlayerPrefs.GetFloat("Ratio Moving");
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
            UnityEngine.Debug.Log(e, this);
            mode = Modes.Fixed;
            // lifeSpan = PlayerPrefs.GetFloat("Firefly Life Span");
        }
        nFF = PlayerPrefs.GetFloat("Number of Fireflies");
        if (nFF > 1)
        {
            for (int i = 0; i < nFF; i++)
            {
                GameObject obj = Instantiate(firefly);
                // GameObject in_ = Instantiate(inner);
                // GameObject out_ = Instantiate(outer);
                obj.name = ("Firefly " + i).ToString();
                // in_.name = ("Inner " + i).ToString();
                // out_.name = ("Outer " + i).ToString();
                pooledFF.Add(obj);
                // pooledI.Add(in_);
                // pooledO.Add(out_);
                obj.SetActive(true);
                // in_.SetActive(true);
                // out_.SetActive(true);
                obj.GetComponent<SpriteRenderer>().enabled = true;
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
            // inner.SetActive(false);
            // outer.SetActive(false);
            firefly.SetActive(false);
        }

        //ipd = ;

        timeout = PlayerPrefs.GetFloat("Timeout");
        path = PlayerPrefs.GetString("Path");
        //UnityEngine.Debug.Log(path);
        //rewardAmt = PlayerPrefs.GetFloat("Reward");
        //rigidbodyFirstPersonControllerv2.movementSettings.ForwardSpeed = PlayerPrefs.GetFloat("Max Linear Speed");
        //rigidbodyFirstPersonControllerv2.movementSettings.BackwardSpeed = PlayerPrefs.GetFloat("Max Linear Speed");
        trialNum = 0;

        player.transform.position = new Vector3(0.0f, p_height, 0.0f);
        player.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
        //print("Begin test.");
        systemStartTimeVerbose = DateTime.Now.ToString("MM-dd_HH-mm-ss");
        contPath = path + "/continuous_data_" + PlayerPrefs.GetInt("Optic Flow Seed").ToString() + ".txt";
        //string firstLine = "TrialNum,TrialTime,Phase,OnOff,PosX,PosY,PosZ,RotX,RotY,RotZ,RotW,LinearVelocty,AngularVelocity,FFX,FFY,FFZ,FFV,LeftGazeX,LeftGazeY,LeftGazeZ,LeftGazeX0,LeftGazeY0,LeftGazeZ0,LHitX,LHitY,LHitZ,LConvergenceDist,2DLHitX,2DLHitY,RightGazeX,RightGazeY,RightGazeZ,RightGazeX0,RightGazeY0,RightGazeZ0,RHitX,RHitY,RHitZ,RConvergenceDist,2DRHitX,2DRHitY,GazeX,GazeY,GazeZ,GazeX0,GazeY0,GazeZ0,HitX,HitY,HitZ,ConvergeDist,2DHitX,2DHitY,LeftPupilDiam,RightPupilDiam,LeftOpen,RightOpen";
        string firstLine = "TrialNum,TrialTime,Phase,OnOff,PosX,PosY,PosZ,RotX,RotY,RotZ,RotW,LinearVelocty,AngularVelocity,FFX,FFY,FFZ,FFV,GazeX,GazeY,GazeZ,GazeX0,GazeY0,GazeZ0,HitX,HitY,HitZ,ConvergeDist,LeftPupilDiam,RightPupilDiam,LeftOpen,RightOpen";
        File.WriteAllText(contPath, firstLine + "\n");

        //Debug.Log(ntrials);

        programT0 = Time.realtimeSinceStartup;
        currPhase = Phases.begin;
        phase = Phases.begin;

        player.transform.position = Vector3.up * p_height;
        player.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
        //Assert.IsNotNull(GazeRayRenderer);
        //OurCamera = GameObject.FindWithTag("MainCamera");
    }

    void LateUpdate()
    {
        //Matrix4x4 lm = PerspectiveOffCenter(left, right, bottom, top, Lcam.nearClipPlane, Lcam.farClipPlane);
        Matrix4x4 lm = Lcam.projectionMatrix; //Lcam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
        lm.m02 = lm02 + offset;
        //Lcam.SetStereoProjectionMatrix(Camera.StereoscopicEye.Left, lm);
        //Lcam.projectionMatrix = lm;
        Matrix4x4 rm = Rcam.projectionMatrix; //Rcam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
        rm.m02 = rm02 - offset;
        //Rcam.SetStereoProjectionMatrix(Camera.StereoscopicEye.Right, rm);
        Rcam.projectionMatrix = rm;
        //print("new lm02: " + lm.m02.ToString());
        //print("new rm02: " + rm.m02.ToString());
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
        //MainL.transform.position = player.transform.position;
        //MainL.transform.rotation = player.transform.rotation;
        //MainR.transform.position = player.transform.position;
        //MainR.transform.rotation = player.transform.rotation;
        //Lcam.transform.position = player.transform.position;
        //Lcam.transform.rotation = player.transform.rotation;
        //Rcam.transform.position = player.transform.position;
        //Rcam.transform.rotation = player.transform.rotation;
        //texL.transform.position = MainL.transform.position + MainL.transform.forward * offset;
        //texL.transform.rotation = new Quaternion(0.0f, MainL.transform.rotation.y, 0.0f, MainL.transform.rotation.w); 
        //texR.transform.position = MainR.transform.position + MainR.transform.forward * offset;
        //texR.transform.rotation = new Quaternion(0.0f, MainR.transform.rotation.y, 0.0f, MainR.transform.rotation.w);
        frameTime.Add(Time.realtimeSinceStartup - programT0);
        if (nFF < 2)
        {
            ffPos_frame.Add(firefly.transform.position.ToString("F8").Trim(toTrim).Replace(" ", ""));
        }
        position_frame.Add(player.transform.position.ToString("F8").Trim(toTrim).Replace(" ", ""));
        rotation_frame.Add(player.transform.rotation.ToString("F8").Trim(toTrim).Replace(" ", ""));

        //if (isMoving && nFF < 2)
        //{
        //    firefly.transform.position += move * Time.deltaTime;
        //    //fv = move.magnitude;
        //}

        if (playing)
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
                            first = false;
                        }
                        else
                        {
                            toggle = rand.NextDouble() <= ratio;
                        }
                    }
                    currentTask = Begin();
                    //tracker.UpdateView();
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
                                pooledFF[i].GetComponent<SpriteRenderer>().enabled = false;
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
    }

    /// <summary>
    /// Capture data at 90 Hz
    /// 
    /// Set Unity's fixed timestep to 1/90 (0.011111111...) in order to get 90 Hz recording
    /// Edit -> Project Settings -> Time -> Fixed Timestep
    /// </summary>
    public void FixedUpdate()
    {
        //if (!Rcam.gameObject.activeInHierarchy)
        //{
        //    Rcam.gameObject.SetActive(true);
        //    Lcam.gameObject.SetActive(false);
        //}
        //else
        //{
        //    Rcam.gameObject.SetActive(false);
        //    Lcam.gameObject.SetActive(true);
        //}
        var keyboard = Keyboard.current;
        if (keyboard.enterKey.isPressed || trialNum > ntrials)
        {
            //print("finished");
            playing = false;

            File.AppendAllText(contPath, sb.ToString());
            Save();
            juiceBox.close();
            SceneManager.LoadScene("MainMenu");
            //Application.Quit();
            // Environment.Exit(Environment.ExitCode);
            //currPhase = Phases.none;
            //print(stringList.Count);
            //source.Cancel();
            //if (mode != Modes.Flash) Task.WhenAll(currentTask); else Task.WhenAll(currentTask, flashTask);


            //SceneManager.UnloadSceneAsync("Human2D");
        }

        if (keyboard.spaceKey.isPressed) juiceBox.send("j100");

        if (playing)
        {
            if (isBegin)
            {
                trialT0 = Time.realtimeSinceStartup;
                trialNum++;
                if (trialNum <= ntrials)
                {
                    beginTime.Add(trialT0 - programT0);
                    n.Add(trialNum);
                    isBegin = false;
                }
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
            if (isMoving && nFF < 2)
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
                v.Add(0.0f);
                w.Add(0.0f);
            }
            catch (Exception e)
            {
                //lol this is because the joystick thing keeps hacing this exception that like doesn't matter, i don't really need to handle this exception
            }
            f_position.Add(firefly.transform.position.ToString("F8").Trim(toTrim).Replace(" ", ""));

            //ViveSR.Error error = SRanipal_Eye_API.GetEyeData(ref data);

            if (true)//(error != ViveSR.Error.WORK)
            {
                //var left = data.verbose_data.left;
                //var right = data.verbose_data.right;
                //var combined = data.verbose_data.combined;

                float x = 0.0f; // combined.eye_data.gaze_direction_normalized.x;
                float y = 0.0f; // combined.eye_data.gaze_direction_normalized.y;
                float z = 0.0f; // combined.eye_data.gaze_direction_normalized.z;

                GazeCombVerbose.Add(string.Join(",", x, y, z));//combined.eye_data.gaze_direction_normalized.x, combined.eye_data.gaze_direction_normalized.y, combined.eye_data.gaze_direction_normalized.z));
                //print(GazeCombVerbose[0]);
                GazeOriginCombVerbose.Add(string.Join(",", player.transform.position.x, player.transform.position.y, player.transform.position.z));//combined.eye_data.gaze_origin_mm.x, combined.eye_data.gaze_origin_mm.y, combined.eye_data.gaze_origin_mm.z));

                // Use when this actually works (still in development at HTC)
                //ConvergenceDistanceVerbose.Add(combined.convergence_distance_mm);

                PupilDiamVerbose.Add(string.Join(",", 0.0f, 0.0f));//left.pupil_diameter_mm, right.pupil_diameter_mm));

                OpennessVerbose.Add(string.Join(",", 0.0f, 0.0f));//left.eye_openness, right.eye_openness));

                //SystemsTimeVerbose.Add(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);

                //UnityEngine.Debug.Log(error);

                //var tuple = CalculateConvergenceDistanceAndCoords(player.transform.position, new Vector3(-x, y, z), ~((1 << 12) | (1 << 13)));
                HitLocations.Add(Vector3.zero.ToString("F8").Trim(toTrim).Replace(" ", ""));//tuple.Item1.ToString("F8").Trim(toTrim).Replace(" ", ""));

                if (Camera.main.gameObject.activeInHierarchy)
                {
                    //HitLocations2D.Add(((Vector2)Camera.main.WorldToScreenPoint(tuple.Item1)).ToString("F8").Trim(toTrim).Replace(" ", ""));
                    HitLocations2D.Add(string.Join(",", 0.0f, 0.0f));
                }
                else
                {
                    HitLocations2D.Add(string.Join(",", 0.0f, 0.0f));
                }

                //marker.transform.position = tuple.Item1;
                //var alpha = Vector3.SignedAngle(player.transform.position, player.transform.position + new Vector3(-x, y, z), player.transform.forward) * Mathf.Deg2Rad;
                //var hypo = 10.0f / Mathf.Cos(alpha);
                //print(hypo);
                //Marker.transform.localPosition = new Vector3(-x, y, z) * hypo;
                ConvergenceDistanceVerbose.Add(0.0f);//tuple.Item2);

                if (isFull)
                {
                    //float xL = left.gaze_direction_normalized.x;
                    //float yL = left.gaze_direction_normalized.y;
                    //float zL = left.gaze_direction_normalized.z;
                    //float xL0 = -ipd / 2.0f;//left.gaze_origin_mm.x;
                    //float yL0 = 0.0f; //left.gaze_origin_mm.y;
                    //float zL0 = 0.0f; //left.gaze_origin_mm.z;

                    //float xR = right.gaze_direction_normalized.x;
                    //float yR = right.gaze_direction_normalized.y;
                    //float zR = right.gaze_direction_normalized.z;
                    //float xR0 = ipd / 2.0f;//right.gaze_origin_mm.x;
                    //float yR0 = 0.0f; //right.gaze_origin_mm.y;
                    //float zR0 = 0.0f; //right.gaze_origin_mm.z;

                    //GazeLeftVerbose.Add(string.Join(",", xL, yL, zL));

                    //GazeLeftOriginVerbose.Add(string.Join(",", xL0, yL0, zL0));

                    //GazeRightVerbose.Add(string.Join(",", xR, yR, zR));

                    //GazeRightOriginVerbose.Add(string.Join(",", xR0, yR0, zR0));

                    //var tupleL = CalculateConvergenceDistanceAndCoords(new Vector3(xL0, yL0, zL0), new Vector3(-xL, yL, zL), 1 << 14);
                    //var tupleR = CalculateConvergenceDistanceAndCoords(new Vector3(xR0, yR0, zR0), new Vector3(-xR, yR, zR), 1 << 14);

                    //HitLocationsL.Add(tupleL.Item1.ToString("F8").Trim(toTrim).Replace(" ", ""));
                    //HitLocationsR.Add(tupleR.Item1.ToString("F8").Trim(toTrim).Replace(" ", ""));

                    //var xLHit = ((Camera.main.WorldToScreenPoint(tupleL.Item1).x / Lcam.pixelWidth) - 0.5f) * 2.4f;
                    //var yLHit = ((Camera.main.WorldToScreenPoint(tupleL.Item1).y / Lcam.pixelHeight) - 0.5f) * 1.34f;
                    //var xRHit = ((Camera.main.WorldToScreenPoint(tupleR.Item1).x / Rcam.pixelWidth) - 0.5f) * 2.4f;
                    //var yRHit = ((Camera.main.WorldToScreenPoint(tupleR.Item1).y / Rcam.pixelHeight) - 0.5f) * 1.34f;

                    //LHit2D = new Vector2(xLHit, yLHit);
                    //RHit2D = new Vector2(xRHit, yRHit);

                    //print(LHit2D);

                    //Marker.transform.localPosition = LHit2D;

                    //angleL = Vector3.Angle(new Vector3(xLHit, yLHit, 0.01f) - new Vector3(xL0, yL0, zL0), Lcam.transform.forward);
                    //angleR = Vector3.Angle(new Vector3(xRHit, yRHit, 0.01f) - new Vector3(xR0, yR0, zR0), Rcam.transform.forward);

                    //print("angleL = " + angleL);

                    //HitLocationsL2D.Add(LHit2D.ToString("F8").Trim(toTrim).Replace(" ", ""));
                    //HitLocationsR2D.Add(RHit2D.ToString("F8").Trim(toTrim).Replace(" ", ""));

                    //ConvergenceDistanceL.Add(tuple.Item2);
                    //ConvergenceDistanceR.Add(tuple.Item2);

                    //File.AppendAllText(contPath, string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26}", trial[0], trialTime[0], epoch[0], onoff[0] ? 1 : 0, position[0], rotation[0], v[0], w[0], f_position[0], currFV[0], GazeLeftVerbose[0], GazeLeftOriginVerbose[0], HitLocationsL[0], ConvergenceDistanceL[0], HitLocationsL2D[0], GazeRightVerbose[0], GazeRightOriginVerbose[0], HitLocationsR[0], ConvergenceDistanceR[0], HitLocationsR2D[0], GazeCombVerbose[0], GazeOriginCombVerbose[0], HitLocations[0], ConvergenceDistanceVerbose[0], HitLocations2D[0], PupilDiamVerbose[0], OpennessVerbose[0]) + "\n");
                    sb.Append(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15}", trial[0], trialTime[0], epoch[0], onoff[0] ? 1 : 0, position[0], rotation[0], v[0], w[0], f_position[0], currFV[0], GazeCombVerbose[0], GazeOriginCombVerbose[0], HitLocations[0], ConvergenceDistanceVerbose[0], PupilDiamVerbose[0], OpennessVerbose[0]) + "\n");
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
                    GazeCombVerbose.Clear();
                    GazeOriginCombVerbose.Clear();
                    HitLocations.Clear();
                    ConvergenceDistanceVerbose.Clear();
                    PupilDiamVerbose.Clear();
                    OpennessVerbose.Clear();
                    //HitLocations2D.Clear();
                    //GazeLeftVerbose.Clear();
                    //GazeLeftOriginVerbose.Clear();
                    //HitLocationsL.Clear();
                    //HitLocationsL2D.Clear();
                    //ConvergenceDistanceL.Clear();
                    //GazeRightVerbose.Clear();
                    //GazeRightOriginVerbose.Clear();
                    //HitLocationsR.Clear();
                    //HitLocationsR2D.Clear();
                    //ConvergenceDistanceR.Clear();
                }
            }
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
    async Task Begin()
    {
        // Debug.Log("Begin Phase Start.");
        //SharedJoystick.MaxSpeed = RandomizeSpeeds(velMin, velMax);
        //SharedJoystick.RotSpeed = RandomizeSpeeds(rotMin, rotMax);
        //max_v.Add(SharedJoystick.MaxSpeed);
        //max_w.Add(SharedJoystick.RotSpeed);
        //await new WaitForSeconds(0.5f);
        loopCount = 0;
        if (!ab)
        {
            float threshold = (float)rand.NextDouble() * (1.0f - 0.11f) + 0.11f;
            await new WaitUntil(() => SharedJoystick.moveX >= 0.11f || SharedJoystick.moveX <= -0.11f || SharedJoystick.moveY >= threshold || SharedJoystick.moveY <= -threshold && !SharedJoystick.ptb); // Used to be rb.velocity.magnitude
        }
        else
        {
            await new WaitUntil(() => SharedJoystick.moveX <= 0.11f && SharedJoystick.moveX >= -0.11f && SharedJoystick.moveY <= 0.11f && SharedJoystick.moveY >= -0.11f && !SharedJoystick.ptb); // Used to be rb.velocity.magnitude
        }

        currPhase = Phases.begin;
        isBegin = true;

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
                    //float angle_i = Mathf.Sqrt(Mathf.Pow(minPhi, 2.0f) + Mathf.Pow(maxPhi - minPhi, 2.0f) * (float)rand.NextDouble());
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
                // pooledI[i].transform.position = position_i;
                // pooledO[i].transform.position = position_i;
                // pooledFF[i].transform.position = position_i;
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
            //float angle = Mathf.Sqrt(Mathf.Pow(minPhi, 2.0f) + Mathf.Pow(maxPhi - minPhi, 2.0f) * (float)rand.NextDouble());
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
            // inner.transform.position = position;
            // outer.transform.position = position;
            firefly.transform.position = position;
            ffPositions.Add(position);
            initialD = Vector3.Distance(player.transform.position, firefly.transform.position);
        }

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

        if (isMoving && nFF < 2)
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
        if (nFF > 1)
        {
            switch (mode)
            {
                case Modes.ON:
                    foreach (GameObject FF in pooledFF)
                    {
                        FF.GetComponent<SpriteRenderer>().enabled = true;
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
                            FF.GetComponent<SpriteRenderer>().enabled = true;
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
                            FF.GetComponent<SpriteRenderer>().enabled = true;
                        }
                        await new WaitForSeconds(lifeSpan);
                        foreach (GameObject FF in pooledFF)
                        {
                            FF.GetComponent<SpriteRenderer>().enabled = false;
                        }
                    }
                    break;
            }
        }
        else
        {
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
                        firefly.SetActive(true);
                        await new WaitForSeconds(lifeSpan);
                        firefly.SetActive(false);
                    }
                    break;
            }
        }
        phase = Phases.trial;
        currPhase = Phases.trial;
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
            await new WaitUntil(() => Vector3.Distance(player_origin, player.transform.position) > 0.5f); // Used to be rb.velocity.magnitude
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

        if (mode == Modes.Flash)
        {
            on = false;
        }

        if (toggle)
        {
            if (nFF > 1)
            {
                //foreach (GameObject FF in pooledFF)
                //{
                //    FF.GetComponent<SpriteRenderer>().enabled = false;
                //}
                pooledFF[loopCount].GetComponent<SpriteRenderer>().enabled = false;
            }
            else
            {
                firefly.SetActive(false);
            }
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
    async Task Check()
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
            source = new CancellationTokenSource();
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

        if (nFF > 2)
        {
            //for (int i = 0; i < nFF; i++)
            //{
            //    ffPosStr = string.Concat(ffPosStr, ffPositions[i].ToString("F8").Trim(toTrim).Replace(" ", "")).Substring(1);
            //    distances.Add(Vector3.Distance(pPos, ffPositions[i]));
            //    if (distances[i] <= fireflyZoneRadius)
            //    {
            //        proximity = true;
            //    }
            //}
            ffPosStr = string.Concat(ffPosStr, ffPositions[loopCount].ToString("F8").Trim(toTrim).Replace(" ", "")).Substring(1);
            distances.Add(Vector3.Distance(pPos, ffPositions[loopCount]));
            if (distances[loopCount] <= fireflyZoneRadius)
            {
                proximity = true;
            }
        }
        else
        {
            if (Vector3.Distance(pPos, firefly.transform.position) <= fireflyZoneRadius) proximity = true;
            ffPosStr = firefly.transform.position.ToString("F8").Trim(toTrim).Replace(" ", "");
            distances.Add(Vector3.Distance(pPos, firefly.transform.position));
        }

        if (isReward && proximity)
        {
            audioSource.clip = winSound;
            points++;
            string toSend = "j" + juiceTime.ToString() + "\n";
            juiceBox.send(juiceTime.ToString());
            currPhase = Phases.juice;
            await new WaitForSeconds(juiceTime);
        }
        else
        {
            audioSource.clip = loseSound;
        }

        //if (PlayerPrefs.GetInt("Feedback ON") == 0)
        //{
        //    audioSource.clip = neutralSound;
        //}
        audioSource.Play();

        if (nFF > 1)
        {
            score.Add(isReward && proximity ? 1 : 0);
            timedout.Add(isTimeout ? 1 : 0);
            cPos.Add(pos.ToString("F8").Trim(toTrim).Replace(" ", ""));
            cRot.Add(rot.ToString("F8").Trim(toTrim).Replace(" ", ""));
            dist.Add(distances[loopCount].ToString("F8"));
            if (loopCount < nFF)
            {
                if (!isTimeout && isReward && proximity)
                {
                    loopCount++;

                    float wait = i_lambda * Mathf.Exp(-i_lambda * ((float)rand.NextDouble() * (i_max - i_min) + i_min));

                    interWait.Add(wait);

                    //isEnd = true;
                    // print("inter delay average: " + interWait.Average());

                    //particleSystem.GetComponent<ParticleSystemRenderer>().enabled = false;

                    await new WaitForSeconds(wait);

                    phase = Phases.trial;
                }
                else
                {
                    //if ((PlayerPrefs.GetInt("Feedback ON") == 1))
                    //{
                    //    firefly.SetActive(true);
                    //    mesh.enabled = true;
                    //    text.enabled = true;
                    //    await new WaitForSeconds(2.0f);
                    //    text.enabled = false;
                    //    mesh.enabled = false;
                    //    firefly.SetActive(false);
                    //}

                    player.transform.position = Vector3.up * p_height;
                    player.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

                    if (loopCount + 1 < nFF)
                    {
                        for (int i = loopCount + 1; i < nFF; i++)
                        {
                            distances.Add(Vector3.Distance(pPos, ffPositions[i]));
                        }
                    }

                    float wait = i_lambda * Mathf.Exp(-i_lambda * ((float)rand.NextDouble() * (i_max - i_min) + i_min));

                    interWait.Add(wait);

                    isEnd = true;
                    // print("inter delay average: " + interWait.Average());

                    //particleSystem.GetComponent<ParticleSystemRenderer>().enabled = false;

                    await new WaitForSeconds(wait);

                    //particleSystem.GetComponent<ParticleSystemRenderer>().enabled = true;

                    phase = Phases.begin;
                    // Debug.Log("Check Phase End.");
                }
            }
            else
            {
                ffPositions.Clear();
                distances.Clear();
                isTimeout = false;
            }

        }
        else
        {
            timedout.Add(isTimeout ? 1 : 0);
            score.Add(isReward && proximity ? 1 : 0);
            ffPos.Add(ffPosStr);
            dist.Add(distances[0].ToString("F8"));
            cPos.Add(pos.ToString("F8").Trim(toTrim).Replace(" ", ""));
            cRot.Add(rot.ToString("F8").Trim(toTrim).Replace(" ", ""));

            ffPositions.Clear();
            distances.Clear();

            isTimeout = false;

            //if (!proximity && (PlayerPrefs.GetInt("Feedback ON") == 1))
            //{
            //    firefly.SetActive(true);
            //    mesh.enabled = true;
            //    text.enabled = true;
            //    await new WaitForSeconds(2.0f);
            //    text.enabled = false;
            //    mesh.enabled = false;
            //    firefly.SetActive(false);
            //}

            player.transform.position = Vector3.up * p_height;
            player.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

            //if (isMoving)
            //{
            //    currPhase = Phases.question;

            //    await new WaitUntil(() => SharedJoystick.currentSpeed == 0.0f && SharedJoystick.currentRot == 0.0f);

            //    panel.SetActive(true);

            //    await new WaitUntil(() => SharedJoystick.currentRot < -10.0f || SharedJoystick.currentRot > 10.0f);

            //    if (SharedJoystick.currentRot < 0.0f)
            //    {
            //        // no
            //        answer.Add(0);
            //    }
            //    else
            //    {
            //        // yes
            //        answer.Add(1);
            //    }

            //    panel.SetActive(false);
            //}

            float wait = i_lambda * Mathf.Exp(-i_lambda * ((float)rand.NextDouble() * (i_max - i_min) + i_min));

            currPhase = Phases.ITI;

            interWait.Add(wait);

            isEnd = true;
            // print("inter delay average: " + interWait.Average());

            //particleSystem.GetComponent<ParticleSystemRenderer>().enabled = false;

            await new WaitForSeconds(wait);

            //particleSystem.GetComponent<ParticleSystemRenderer>().enabled = true;

            phase = Phases.begin;
            // Debug.Log("Check Phase End.");
        }
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

    //private void SetVanishingPoint(Camera cam, Vector2 perspectiveOffset)
    //{
    //    var m = cam.projectionMatrix;
    //    var w = 2 * cam.nearClipPlane / m.m00;
    //    var h = 2 * cam.nearClipPlane / m.m11;

    //    var left = -w / 2 - perspectiveOffset.x;
    //    var right = left + w;
    //    var bottom = -h / 2 - perspectiveOffset.y;
    //    var top = bottom + h;

    //    cam.projectionMatrix = PerspectiveOffCenter(left, right, bottom, top, cam.nearClipPlane, cam.farClipPlane);
    //}

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
            string firstLine;

            List<int> temp;

            if (!isFull)
            {
                //firstLine = "TrialNum,TrialTime,Phase,OnOff,PosX,PosY,PosZ,RotX,RotY,RotZ,RotW,LinearVelocty,AngularVelocity,FFX,FFY,FFZ,FFV,LeftGazeX,LeftGazeY,LeftGazeZ,LeftGazeX0,LeftGazeY0,LeftGazeZ0,LHitX,LHitY,LHitZ,LConvergenceDist,2DLHitX,2DLHitY,RightGazeX,RightGazeY,RightGazeZ,RightGazeX0,RightGazeY0,RightGazeZ0,RHitX,RHitY,RHitZ,RConvergenceDist,2DRHitX,2DRHitY,GazeX,GazeY,GazeZ,GazeX0,GazeY0,GazeZ0,HitX,HitY,HitZ,ConvergeDist,2DHitX,2DHitY,LeftPupilDiam,RightPupilDiam,LeftOpen,RightOpen";
                //}
                //else
                //{
                //StringBuilder csvCont = new StringBuilder();


                //firstLine = "TrialNum,TrialTime,Phase,OnOff,PosX,PosY,PosZ,RotX,RotY,RotZ,RotW,LinearVelocty,AngularVelocity,FFX,FFY,FFZ,FFV,GazeX,GazeY,GazeZ,GazeX0,GazeY0,GazeZ0,HitX,HitY,HitZ,ConvergeDist,LeftPupilDiam,RightPupilDiam,LeftOpen,RightOpen";


                //csvCont.AppendLine(firstLine);

                //temp = new List<int>() {
                //    epoch.Count,
                //    trial.Count,
                //    trialTime.Count,
                //    onoff.Count,
                //    position.Count,
                //    rotation.Count,
                //    v.Count,
                //    w.Count,
                //    currFV.Count,
                //    f_position.Count,
                //    GazeCombVerbose.Count,
                //    GazeOriginCombVerbose.Count,
                //    HitLocations.Count,
                //    ConvergenceDistanceVerbose.Count,
                //    PupilDiamVerbose.Count,
                //    OpennessVerbose.Count,
                //};

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

                //temp.Sort();

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
                //for (int i = 0; i < temp[0]; i++)
                //{
                //    var line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15}", trial[i], trialTime[i], epoch[i], onoff[i] ? 1 : 0, position[i], rotation[i], v[i], w[i], f_position[i], currFV[i], GazeCombVerbose[i], GazeOriginCombVerbose[i], HitLocations[i], ConvergenceDistanceVerbose[i], PupilDiamVerbose[i], OpennessVerbose[i]);
                //    //UnityEngine.Debug.Log(i + " " + line);
                //    csvCont.AppendLine(line);
                //}
                //string contPath = path + "/continuous_data_" + PlayerPrefs.GetInt("Optic Flow Seed").ToString() + ".txt";

                ////File.Create(contPath);
                //File.WriteAllText(contPath, csvCont.ToString());
            }

            StringBuilder csvDisc = new StringBuilder();

            if (nFF > 1)
            {
                string ffPosStr = "";
                string distStr = "";

                for (int i = 0; i < nFF; i++)
                {
                    ffPosStr = string.Concat(ffPosStr, string.Format("ffX{0},ffY{0},ffZ{0},", i));
                    distStr = string.Concat(distStr, string.Format("distToFF{0},", i));
                }

                firstLine = string.Format("n,max_v,max_w,f,ffv,PosX0,PosY0,PosZ0,RotX0,RotY0,RotZ0,{0}pCheckX,pCheckY,pCheckZ,rCheckX,rCheckY,rCheckZ,{1}rewarded,timeout,beginTime,checkTime,duration,delays,ITI", ffPosStr, distStr);
            }
            else
            {
                firstLine = "n,max_v,max_w,ffv,onDuration,PosX0,PosY0,PosZ0,RotX0,RotY0,RotZ0,RotW0,ffX,ffY,ffZ,pCheckX,pCheckY,pCheckZ,rCheckX,rCheckY,rCheckZ,rCheckW,distToFF,rewarded,timeout,beginTime,checkTime,duration,delays,ITI";
            }

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
            //if (isMoving)
            //{
            //    temp.Add(answer.Count);
            //}
            temp.Sort();
            for (int i = 0; i < temp[0]; i++)
            {
                var line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17}", n[i], max_v[i], max_w[i], fv[i], onDur[i], origin[i], heading[i], ffPos[i], cPos[i], cRot[i], dist[i], score[i], timedout[i], beginTime[i], checkTime[i], endTime[i], checkWait[i], interWait[i]);
                csvDisc.AppendLine(line);
            }

            string discPath = path + "/discontinuous_data_" + PlayerPrefs.GetInt("Optic Flow Seed").ToString() + ".txt";

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

            xmlWriter.WriteStartElement("JuiceTime");
            xmlWriter.WriteString(PlayerPrefs.GetFloat("Juice Time").ToString());
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

        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log(e);
        }
    }
}