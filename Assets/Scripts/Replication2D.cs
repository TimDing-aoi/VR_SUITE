using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static ObjectPooler;
using System.Linq;

public class Replcation2D : MonoBehaviour
{
    public GameObject firefly;
    // public RigidbodyFirstPersonControllerv2 rigidbodyFirstPersonControllerv2; <-- may use 
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
    [ShowOnly] public float nFF; // Not currently used at the moment, but will be implemented later
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
    // 1 / Mean for check time exponential distribution
    private float c_lambda;
    // 1 / Mean for intertrial time exponential distribution
    private float i_lambda;
    // x values for exponential distribution
    private float c_min;
    private float c_max;
    private float i_min;
    private float i_max;
    private enum Phases
    {
        begin,
        trial,
        check,
        end,
        none
    }
    private Phases phase;
    private Vector3 screenPosition;
    private Rigidbody rb;
    public Camera cam;
    private Vector3 pPos;
    private Vector3 fPos;
    private bool isTimeout = false;

    // Firefly coords
    readonly List<float> ffX = new List<float>();
    readonly List<float> ffY = new List<float>();
    readonly List<float> ffZ = new List<float>();

    // Player position at current frame
    readonly List<float> pX = new List<float>();
    readonly List<float> pY = new List<float>();
    readonly List<float> pZ = new List<float>();

    // Player rotation at current frame
    readonly List<float> rX = new List<float>();
    readonly List<float> rY = new List<float>();
    readonly List<float> rZ = new List<float>();

    // File paths
    private string contPath;
    private string discPath;

    private float points = 0;
    [Tooltip("How much the player receives for successfully completing the task")]
    [ShowOnly] public float rewardAmt;

    private int seed;
    private System.Random rand;

    private bool on = true;

    private int frame = 0;

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
        minDrawDistance = PlayerPrefs.GetFloat("Minimum Firefly Distance");
        maxDrawDistance = PlayerPrefs.GetFloat("Maximum Firefly Distance");
        cam.fieldOfView = PlayerPrefs.GetFloat("Max Angle");
        maxPhi = cam.fieldOfView * cam.pixelWidth / cam.pixelHeight * Mathf.Deg2Rad / 2f;
        minPhi = PlayerPrefs.GetFloat("Min Angle") * cam.pixelWidth / cam.pixelHeight * Mathf.Deg2Rad / 2f;
        LR = PlayerPrefs.GetFloat("Left Right");
        fireflyZoneRadius = PlayerPrefs.GetFloat("Reward Zone Radius");
        fireflySize = PlayerPrefs.GetFloat("Size");
        firefly.transform.localScale = new Vector3(fireflySize, fireflySize, 1);
        ratio = PlayerPrefs.GetFloat("Ratio");
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
                    lifeSpan = PlayerPrefs.GetFloat("Firefly Life Span");
                    break;
                default:
                    throw new System.Exception("No mode selected, defaulting to FIXED");
            }
        }
        catch (System.Exception e)
        {
            Debug.Log(e, this);
            mode = Modes.Fixed;
            lifeSpan = PlayerPrefs.GetFloat("Firefly Life Span");
        }
        nFF = PlayerPrefs.GetFloat("Number of Fireflies");
        timeout = PlayerPrefs.GetFloat("Timeout");
        contPath = PlayerPrefs.GetString("Continuous Path");
        discPath = PlayerPrefs.GetString("Discrete Path");
        rewardAmt = PlayerPrefs.GetFloat("Reward");
        rb = player.GetComponent<Rigidbody>();
        //print("Begin test.");
        UnityEngine.XR.XRDevice.DisableAutoXRCameraTracking(cam, true);
        UnityEngine.XR.InputTracking.disablePositionalTracking = true;
        cam.transform.localPosition = new Vector3(0f, 0.0f, 0f);

        var d_reader = new StreamReader(discPath);
        d_reader.ReadLine();

        List<float> x_tmp = new List<float>();
        List<float> y_tmp = new List<float>();
        List<float> z_tmp = new List<float>();

        while (!d_reader.EndOfStream)
        {
            var line = d_reader.ReadLine();
            var values = line.Split(',');

            x_tmp.Add(float.Parse(values[7]));
            y_tmp.Add(float.Parse(values[8]));
            z_tmp.Add(float.Parse(values[9]));
        }

        var reader = new StreamReader(contPath);
        reader.ReadLine();

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            var values = line.Split(',');

            pX.Add(float.Parse(values[4]));
            pY.Add(float.Parse(values[5]));
            pZ.Add(float.Parse(values[6]));
            rX.Add(float.Parse(values[7]));
            rY.Add(float.Parse(values[8]));
            rZ.Add(float.Parse(values[9]));

            ffX.Add(x_tmp[int.Parse(values[0])]);
            ffY.Add(y_tmp[int.Parse(values[0])]);
            ffZ.Add(z_tmp[int.Parse(values[0])]);
        }

        phase = Phases.begin;
    }

    private void FixedUpdate()
    {

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
    /// </summary>
    void Update()
    {
        cam.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        if (SharedInstance.fill >= 1)
        {

            player.transform.position = new Vector3(pX[frame], pY[frame], pZ[frame]);
            player.transform.rotation = Quaternion.Euler(rX[frame], rY[frame], rZ[frame]);

            firefly.transform.position = new Vector3(ffX[frame], ffY[frame], ffZ[frame]);

            switch (phase)
            {
                case Phases.begin:
                    phase = Phases.none;
                    toggle = rand.NextDouble() <= ratio;
                    Begin();
                    break;

                case Phases.trial:
                    phase = Phases.none;
                    Trial();
                    break;

                case Phases.check:
                    phase = Phases.none;
                    if (mode == Modes.ON)
                    {
                        firefly.SetActive(false);
                    }
                    Check();
                    break;

                case Phases.none:
                    break;
            }
        }
        frame++;
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
    async void Begin()
    {
        // Debug.Log("Begin Phase Start.");
        await new WaitUntil(() => rb.velocity.magnitude < 0.01f);
        phase = Phases.trial;
        // Debug.Log("Begin Phase End.");
        switch (mode)
        {
            case Modes.ON:
                firefly.SetActive(true);
                break;
            case Modes.Flash:
                on = true;
                Flash();
                break;
            case Modes.Fixed:
                if (toggle)
                {
                    firefly.SetActive(true);
                }
                else
                {
                    firefly.SetActive(true);
                    await new WaitForSeconds(lifeSpan);
                    firefly.SetActive(false);
                }
            break;
        }
    }

    /// <summary>
    /// Doesn't really do much besides wait for the player to start moving, and, afterwards,
    /// wait until the player stops moving and then start the check phase. Also will go back to
    /// begin phase if player doesn't move before timeout
    /// </summary>
    async void Trial()
    {
        // Debug.Log("Trial Phase Start.");

        CancellationTokenSource source = new CancellationTokenSource();

        var t = Task.Run(async () =>
        {
            await new WaitUntil(() => rb.velocity.magnitude > 0.5f);
        }, source.Token);

        if (await Task.WhenAny(t, Task.Delay((int)timeout * 1000)) == t)
        {
            await new WaitUntil(() => rb.velocity.magnitude < 0.01f);
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
            firefly.SetActive(false);
        }
        phase = Phases.check;
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
        if (!isTimeout)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            // Debug.Log("Check Phase Start.");
            pPos = player.transform.position;
            fPos = firefly.transform.position;

            float delay = c_lambda * Mathf.Exp(-c_lambda * ((float)rand.NextDouble() * (c_max - c_min) + c_min));

            var t = Task.Run(async () =>
            {
                await new WaitUntil(() => rb.velocity.magnitude > 0.5f);
            }, source.Token);

            if (!(await Task.WhenAny(t, Task.Delay((int)delay * 1000)) == t) && Vector3.Distance(pPos, fPos) <= fireflyZoneRadius)
            {
                audioSource.clip = winSound;
                points += rewardAmt;
            }
            else
            {
                audioSource.clip = loseSound;
            }
            source.Cancel();
        }
        else
        {
            audioSource.clip = loseSound;
        }

        audioSource.Play();

        float wait = i_lambda * Mathf.Exp(-i_lambda * ((float)rand.NextDouble() * (i_max - i_min) + i_min));

        await new WaitForSeconds(wait);

        phase = Phases.begin;
        // Debug.Log("Check Phase End.");
    }

    public async void Flash()
    {
        while (on)
        {
            if (toggle && !firefly.activeInHierarchy)
            {
                firefly.SetActive(true);
            }
            else
            {
                firefly.SetActive(true);
                await new WaitForSeconds(PW);
                firefly.SetActive(false);
                await new WaitForSeconds((1f / freq) - PW);
            }
        }
    }

    private float Tcalc(float t, float lambda)
    {
        return -1.0f / lambda * Mathf.Log(t / lambda);
    }
}