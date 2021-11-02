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
    [HideInInspector]
    public int lineOnOff = 0;
    public GameObject marker;
    public GameObject panel;
    public Camera Lcam;
    public Camera Rcam;
    public GameObject FP;
    public GameObject Marker;
    // public GameObject inner;
    // public GameObject outer;
    [Tooltip("Radius of firefly")]
    [HideInInspector]
    public float fireflySize;
    [Tooltip("Maximum distance allowed from center of firefly")]
    [HideInInspector]
    public float fireflyZoneRadius;
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
    [HideInInspector]
    public float ratio;
    [Tooltip("Frequency of flashing firefly (Flashing Firefly Only)")]
    [HideInInspector]
    public float freq;
    [Tooltip("Duty cycle for flashing firefly (percentage of one period determing how long it stays on during one period) (Flashing Firefly Only)")]
    [HideInInspector]
    public float duty;
    // Pulse Width; how long in seconds it stays on during one period
    private float PW;
    public GameObject player;
    public AudioSource audioSource;
    public AudioClip winSound;
    public AudioClip neutralSound;
    public AudioClip loseSound;
    [Tooltip("Minimum distance firefly can spawn")]
    [HideInInspector]
    public float minDrawDistance;
    [Tooltip("Maximum distance firefly can spawn")]
    [HideInInspector]
    public float maxDrawDistance;
    [Tooltip("Ranges for which firefly n spawns inside")]
    [HideInInspector]
    public List<float> ranges = new List<float>();
    [Tooltip("Minimum angle from forward axis that firefly can spawn")]
    [HideInInspector]
    public float minPhi;
    [Tooltip("Maximum angle from forward axis that firefly can spawn")]
    [HideInInspector]
    public float maxPhi;
    [Tooltip("Indicates whether firefly spawns more on the left or right; < 0.5 means more to the left, > 0.5 means more to the right, = 0.5 means equally distributed between left and right")]
    [HideInInspector]
    public float LR;
    [Tooltip("How long the firefly stays from the beginning of the trial (Fixed Firefly Only)")]
    [HideInInspector]
    public float lifeSpan;
    [Tooltip("How many fireflies can appear at once")]
    [HideInInspector]
    public float nFF;
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
    [HideInInspector]
    public float timeout;
    [HideInInspector]
    public float interMin;
    [Tooltip("Player height")]
    [HideInInspector]
    public float p_height;
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
    [HideInInspector]
    public Phases phase;

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
    [HideInInspector]
    public bool isQuestion = false;

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

    [HideInInspector]
    public float FrameTimeElement = 0;

    [HideInInspector]
    public float delayTime = .2f;

    public bool Detector = false;
    //List<float> Frametime = new List<float>();

    public int LengthOfRay = 75;
    //[SerializeField] private LineRenderer GazeRayRenderer;

    [HideInInspector]
    public string sceneTypeVerbose;
    [HideInInspector]
    public string systemStartTimeVerbose;

    public static Vector3 hitpoint;

    // File paths
    private string path;

    [HideInInspector]
    public int trialNum;
    //private float trialT;
    private float programT0 = 0.0f;

    //private float points = 0;
    [Tooltip("How much the player receives for successfully completing the task")]
    [HideInInspector]
    public float rewardAmt;
    [HideInInspector]
    public float points = 0;
    [Tooltip("Maximum number of trials before quitting (0 for infinity)")]
    [HideInInspector]
    public int ntrials = 999;

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
    [HideInInspector]
    public string phaseString;

    readonly private List<GameObject> pooledFF = new List<GameObject>();

    private bool first = true;
    private readonly char[] toTrim = { '(', ')' };

    [HideInInspector]
    public float initialD = 0.0f;

    private float velocity;

    public GameObject arrow;
    private MeshRenderer mesh;
    public TMPro.TMP_Text text;
    [HideInInspector]
    public Vector3 player_origin;
    [HideInInspector]
    public Quaternion player_rotation_initial;
    [HideInInspector]
    public Vector3 ff_origin;

    float trialT0;

    private string contPath;

    public ParticleSystem particleSystem;

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
    void Start()
    {
        UnityEngine.XR.InputTracking.disablePositionalTracking = true;
        UnityEngine.XR.XRDevice.DisableAutoXRCameraTracking(Lcam, true);
        UnityEngine.XR.XRDevice.DisableAutoXRCameraTracking(Rcam, true);

        Lcam.ResetProjectionMatrix();
        Rcam.ResetProjectionMatrix();

        List<XRDisplaySubsystem> displaySubsystems = new List<XRDisplaySubsystem>();
        SubsystemManager.GetInstances<XRDisplaySubsystem>(displaySubsystems);

        if (!XRSettings.enabled)
        {
            XRSettings.enabled = true;
        }
        XRSettings.occlusionMaskScale = 2f;
        XRSettings.useOcclusionMesh = false;

        nFF = 20;

        for (int i = 0; i < nFF; i++)
        {
            distances.Add(0.0f);
            ffPositions.Add(Vector3.zero);
        }

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
    /// Phase check
    /// </summary>
    void Update()
    {
        phaseString = currPhase.ToString();
        print(phase.ToString());
        if (playing && Time.realtimeSinceStartup - programT0 > 0.3f)
        {
            switch (phase)
            {
                case Phases.begin:
                    phase = Phases.none;
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

        prevPos = player.transform.position;

        particleSystem.transform.position = player.transform.position - (Vector3.up * (p_height - 0.0001f));
        if (Input.GetKey(KeyCode.Return))
        {
            print("ended.");
            playing = false;
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
        }
    }

    /// <summary>
    /// Wait until the player is not moving, then:
    /// 1. Add trial begin time to respective list
    /// 2. Update position
    /// 3. Start firefly behavior depending on mode, and switch phase to trial
    /// </summary>
    async Task Begin()
    {
        // Debug.Log("Begin Phase Start.");

        //await new WaitForSeconds(0.5f);

        currPhase = Phases.begin;
        isBegin = true;
        particleSystem.gameObject.SetActive(true);
        SetFireflyLocation();
        foreach (GameObject FF in pooledFF)
        {
            FF.SetActive(true);
        }
        
        phase = Phases.trial;
        currPhase = Phases.trial;
        Debug.Log("Begin Phase End.");
    }

    /// <summary>
    /// Wait for the player to start moving;
    /// wait until the player stops moving;
    /// start the check phase. 
    /// Go back to begin phase if player doesn't move before timeout.
    /// </summary>
    async Task Trial()
    {
        source = new CancellationTokenSource();

        var t = Task.Run(async () =>
        {
            await new WaitUntil(() => Mathf.Abs(SharedJoystick.currentSpeed) >= velocityThreshold); // Used to be rb.velocity.magnitude
        }, source.Token);

        var t1 = Task.Run(async () =>
        {
            await new WaitForSeconds(timeout); // Used to be rb.velocity.magnitude
        }, source.Token);

        if (await Task.WhenAny(t, t1) == t)
        {
            await new WaitUntil(() => (Mathf.Abs(SharedJoystick.currentSpeed) < velocityThreshold && Mathf.Abs(SharedJoystick.currentRot) < rotationThreshold && (SharedJoystick.moveX == 0.0f && SharedJoystick.moveY == 0.0f)) || t1.IsCompleted); // Used to be rb.velocity.magnitude // || (angleL > 3.0f or angleR > 3.0f)
        }
        else
        {
            isTimeout = true;
        }

        source.Cancel();

        move = new Vector3(0.0f, 0.0f, 0.0f);
        velocity = 0.0f;
        phase = Phases.check;
        currPhase = Phases.check;
    }

    /// <summary>
    /// Do nothing for now
    /// </summary>
    async Task Check()
    {
    }

    void SetFireflyLocation()
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
}