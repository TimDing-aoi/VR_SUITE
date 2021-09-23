using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static ObjectPooler;
using static Tracker;
using System;

public class Simulation2D : MonoBehaviour
{
    public static Simulation2D simulation;
    public GameObject firefly;
    // public GameObject inner;
    // public GameObject outer;
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
    public enum Phases
    {
        begin,
        trial,
        check,
        end,
        none
    }
    public Phases phase;

    private Vector3 pPos;
    private bool isTimeout = false;

    // Player position at current frame
    readonly List<float> pX = new List<float>();
    readonly List<float> pY = new List<float>();
    readonly List<float> pZ = new List<float>();

    // Player rotation at current frame
    readonly List<float> rX = new List<float>();
    readonly List<float> rY = new List<float>();
    readonly List<float> rZ = new List<float>();

    // Player linear and angular velocity
    readonly List<float> v = new List<float>();
    readonly List<float> w = new List<float>();

    // Firefly On/Off
    readonly List<bool> onoff = new List<bool>();

    private string contPath;

    [ShowOnly] public int points = 0;

    [ShowOnly] public int seed;
    private System.Random rand;

    private bool on = true;

    private List<GameObject> pooledFF = new List<GameObject>();

    private int frame = 0;

    public GameObject arrow;
    private MeshRenderer mesh;
    public TMPro.TMP_Text text;
    public Vector3 player_origin;

    private bool isON;

    private float lin;
    private float ang;

    // Start is called before the first frame update
    /// <summary>
    /// From "GoToSettings.cs" you can see that I just hard-coded each of the key
    /// strings in order to retrieve the values associated with each key and
    /// assign them to their respective variable here
    /// </summary>
    void Start()
    {
        simulation = this;
        mesh = arrow.GetComponent<MeshRenderer>();
        mesh.enabled = false;

        text.enabled = false;

        seed = PlayerPrefs.GetInt("Firefly Seed");
        rand = new System.Random(seed);
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
        maxPhi = PlayerPrefs.GetFloat("Max Angle");
        minPhi = PlayerPrefs.GetFloat("Min Angle");
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
            }
            // inner.SetActive(false);
            // outer.SetActive(false);
            firefly.SetActive(false);
        }
        contPath = PlayerPrefs.GetString("Continuous Path");

        using (StreamReader reader = new StreamReader(contPath))
        {
            reader.DiscardBufferedData();
            reader.ReadLine();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                print(line);

                onoff.Add(values[3] == "True");
                pX.Add(float.Parse(values[4]));
                pY.Add(float.Parse(values[5]));
                pZ.Add(float.Parse(values[6]));
                rX.Add(float.Parse(values[7]));
                rY.Add(float.Parse(values[8]));
                rZ.Add(float.Parse(values[9]));
                v.Add(float.Parse(values[11]));
                w.Add(float.Parse(values[12]));
            }
        }
        
        phase = Phases.begin;
    }

    // Update is called once per frame
    void Update()
    {
        if (SharedInstance.fill >= 1)
        {
            Vector3 position = new Vector3(pX[frame], pY[frame], pZ[frame]);
            Quaternion rotation = Quaternion.Euler(rX[frame], rY[frame], rZ[frame]);
            isON = onoff[frame];
            lin = v[frame];
            ang = w[frame];

            player.transform.position = position;
            player.transform.rotation = rotation;

            switch (phase)
            {
                case Phases.begin:
                    phase = Phases.none;
                    toggle = rand.NextDouble() <= ratio;
                    Begin();
                    tracker.UpdateView();
                    break;

                case Phases.trial:
                    phase = Phases.none;
                    Trial();
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
                    Check();
                    break;

                case Phases.none:
                    break;
            }
        }
        frame++;
    }

    async void Begin()
    {
        await new WaitUntil(() => isON);
        if (nFF > 1)
        {
            for (int i = 0; i < nFF; i++)
            {
                Vector3 position_i;
                bool tooClose;
                do
                {
                    tooClose = false;
                    float r_i = Mathf.Sqrt(Mathf.Pow(minDrawDistance, 2.0f) + Mathf.Pow(maxDrawDistance - minDrawDistance, 2.0f) * (float)rand.NextDouble());
                    float angle_i = Mathf.Sqrt(Mathf.Pow(minPhi, 2.0f) + Mathf.Pow(maxPhi - minPhi, 2.0f) * (float)rand.NextDouble());
                    float side_i = rand.NextDouble() < LR ? 1 : -1;
                    position_i = (player.transform.position - new Vector3(0.0f, 0.1f, 0.0f)) + Quaternion.AngleAxis(angle_i * side_i, Vector3.up) * player.transform.forward * r_i;
                    position_i.y = 0.0001f;
                    if (i > 0) for (int k = 0; k < i; k++) { if (Vector3.Distance(position_i, pooledFF[k].transform.position) <= 1.0f) tooClose = true; }
                }
                while (tooClose);

                // pooledI[i].transform.position = position_i;
                // pooledO[i].transform.position = position_i;
                pooledFF[i].transform.position = position_i;
            }
        }
        else
        {
            float r = Mathf.Sqrt(Mathf.Pow(minDrawDistance, 2.0f) + Mathf.Pow(maxDrawDistance - minDrawDistance, 2.0f) * (float)rand.NextDouble());
            float angle = Mathf.Sqrt(Mathf.Pow(minPhi, 2.0f) + Mathf.Pow(maxPhi - minPhi, 2.0f) * (float)rand.NextDouble());
            float side = rand.NextDouble() < LR ? 1 : -1;
            Vector3 position = (player.transform.position - new Vector3(0.0f, 0.1f, 0.0f)) + Quaternion.AngleAxis(angle * side, Vector3.up) * player.transform.forward * r;
            position.y = 0.0001f;
            // inner.transform.position = position;
            // outer.transform.position = position;
            firefly.transform.position = position;
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
                        Flash(FF);
                    }
                    break;
                case Modes.Fixed:
                    if (toggle)
                    {
                        foreach (GameObject FF in pooledFF)
                        {
                            FF.GetComponent<SpriteRenderer>().enabled = true;
                        }
                    }
                    else
                    {
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
                    Flash(firefly);
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
        phase = Phases.trial;
    }

    async void Trial()
    {
        await new WaitUntil(() => Vector3.Distance(player_origin, player.transform.position) > 0.5f);

        CancellationTokenSource source = new CancellationTokenSource();

        var t = Task.Run(async () => {
            await new WaitUntil(() => lin != 0.0f || ang != 0.0f); // Used to be rb.velocity.magnitude
        }, source.Token);

        if (await Task.WhenAny(t, Task.Delay((int)timeout * 1000)) == t)
        {
            await new WaitUntil(() => lin == 0.0f && ang == 0.0f); // Used to be rb.velocity.magnitude
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
                foreach (GameObject FF in pooledFF)
                {
                    FF.GetComponent<SpriteRenderer>().enabled = false;
                }
            }
            else
            {
                firefly.SetActive(false);
            }
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
        bool proximity = false;

        if (!isTimeout)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            // Debug.Log("Check Phase Start.");

            pPos = player.transform.position - new Vector3(0.0f, 0.1f, 0.0f);

            if (nFF > 2)
            {
                for (int i = 0; i < nFF; i++)
                {
                    if (Vector3.Distance(pPos, pooledFF[i].transform.position) <= fireflyZoneRadius)
                    {
                        proximity = true;
                    }
                }
            }
            else
            {
                if (Vector3.Distance(pPos, firefly.transform.position) <= fireflyZoneRadius) proximity = true;
            }



            float delay = c_lambda * Mathf.Exp(-c_lambda * ((float)rand.NextDouble() * (c_max - c_min) + c_min));
            // Debug.Log("firefly delay: " + delay);

            // print("check delay average: " + checkWait.Average());

            // Wait until this condition is met in a different thread(?...not actually sure if its
            // in a different thread tbh), or until the check delay time is up. If the latter occurs
            // and the player is close enough to a FF, then the player gets the reward.
            var t = Task.Run(async () =>
            {
                await new WaitUntil(() => lin != 0.0f || ang != 0.0f); // Used to be rb.velocity.magnitude
            }, source.Token);

            if (!(await Task.WhenAny(t, Task.Delay((int)delay * 1000)) == t) && proximity)
            {
                audioSource.clip = winSound;
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
            isTimeout = false;
        }

        audioSource.Play();

        if (nFF < 2 && !proximity)
        {
            firefly.SetActive(true);
            mesh.enabled = true;
            text.enabled = true;
            await new WaitForSeconds(2.0f);
            text.enabled = false;
            mesh.enabled = false;
            firefly.SetActive(false);
        }

        float wait = i_lambda * Mathf.Exp(-i_lambda * ((float)rand.NextDouble() * (i_max - i_min) + i_min));

        // print("inter delay average: " + interWait.Average());

        await new WaitForSeconds(wait);

        phase = Phases.begin;
        // Debug.Log("Check Phase End.");
    }

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
                obj.GetComponent<SpriteRenderer>().enabled = false;
                await new WaitForSeconds((1f / freq) - PW);
            }
        }
    }

    private float Tcalc(float t, float lambda)
    {
        return -1.0f / lambda * Mathf.Log(t / lambda);
    }
}
