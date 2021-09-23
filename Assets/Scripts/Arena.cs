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

public class Arena : MonoBehaviour
{
    public GameObject player;
    public SpriteRenderer screen1;
    public SpriteRenderer screen2;
    public SpriteRenderer screen3;

    private int seed;
    private System.Random rand;

    private Vector2 RHit2D;
    private Vector2 LHit2D;

    // Full data record
    private bool isFull = false;

    // File paths
    private string path;
    private string contPath;

    private float ipd;

    // Trial number
    readonly List<int> trial = new List<int>();

    // Player position, continuous
    readonly List<string> position = new List<string>();

    // Player rotation, continuous
    readonly List<string> rotation = new List<string>();

    // Player linear and angular velocity
    readonly List<float> v = new List<float>();
    readonly List<float> w = new List<float>();

    // Times
    readonly List<float> trialTime = new List<float>();

    // Screen Touch
    readonly List<int> click = new List<int>();

    // Rewarded?
    readonly List<int> reward = new List<int>();

    public EyeData data = new EyeData();

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
    readonly List<float> ConvergenceDistanceL = new List<float>();
    readonly List<float> ConvergenceDistanceR = new List<float>();

    // Gaze hit locations
    readonly List<string> HitLocations = new List<string>();
    readonly List<string> HitLocations2D = new List<string>();
    readonly List<string> HitLocationsL = new List<string>();
    readonly List<string> HitLocationsR = new List<string>();
    readonly List<string> HitLocationsL2D = new List<string>();
    readonly List<string> HitLocationsR2D = new List<string>();

    // means
    public float mean1;
    public float mean2;
    public float mean3;

    private readonly char[] toTrim = { '(', ')' };

    private float trialT0;

    private bool press = false;

    // Start is called before the first frame update
    void Start()
    {
        seed = UnityEngine.Random.Range(1, 10000);
        rand = new System.Random(seed);
        List<XRDisplaySubsystem> displaySubsystems = new List<XRDisplaySubsystem>();
        SubsystemManager.GetInstances<XRDisplaySubsystem>(displaySubsystems);
        if (displaySubsystems.Count > 0)
        {
            while (!SharedEye.ready) ;
        }
        path = PlayerPrefs.GetString("Path");
        isFull = PlayerPrefs.GetInt("Full ON") == 1;
        mean1 = PlayerPrefs.GetFloat("Mean 1");
        mean2 = PlayerPrefs.GetFloat("Mean 2");
        mean3 = PlayerPrefs.GetFloat("Mean 3");
        screen1.color = Color.blue;
        screen2.color = Color.blue;
        screen3.color = Color.blue;
        trialT0 = Time.realtimeSinceStartup;
        SharedJoystick.MaxSpeed = 1.4f;
        SharedJoystick.RotSpeed = 90.0f;
    }

    public void FixedUpdate()
    {
        press = SharedJoystick.press == 1;
        if (Time.realtimeSinceStartup - trialT0 < 0.0001)
        {
            trialTime.Add(0.0f);
        }
        else
        {
            trialTime.Add(Time.realtimeSinceStartup - trialT0);
        }
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
        if ((float)rand.NextDouble() < Time.deltaTime / mean1)
        {
            // change screen 1 color to red
            screen1.color = Color.red;
        }
        if ((float)rand.NextDouble() < Time.deltaTime / mean2)
        {
            // change screen 2 color to red
            screen2.color = Color.red;
        }
        if ((float)rand.NextDouble() < Time.deltaTime / mean3)
        {
            // change screen 3 color to red
            screen3.color = Color.red;
        }
        if (Vector3.Distance(player.transform.position, screen1.transform.position) < 1.0f && press)
        {
            if (screen1.color == Color.red)
            {
                screen1.color = Color.blue;
                reward.Add(1);
            }
            click.Add(1);
        }
        else if (Vector3.Distance(player.transform.position, screen2.transform.position) < 1.0f && press)
        {
            if (screen2.color == Color.red)
            {
                screen2.color = Color.blue;
                reward.Add(1);
            }
            click.Add(1);
        }
        else if (Vector3.Distance(player.transform.position, screen3.transform.position) < 1.0f && press)
        {
            if (screen3.color == Color.red)
            {
                screen3.color = Color.blue;
                reward.Add(1);
            }
            click.Add(1);
        }
        else
        {
            click.Add(0);
            reward.Add(0);
        }

        ViveSR.Error error = SRanipal_Eye_API.GetEyeData(ref data);

        if (error == ViveSR.Error.WORK)
        {
            var left = data.verbose_data.left;
            var right = data.verbose_data.right;
            var combined = data.verbose_data.combined;

            float x = combined.eye_data.gaze_direction_normalized.x;
            float y = combined.eye_data.gaze_direction_normalized.y;
            float z = combined.eye_data.gaze_direction_normalized.z;

            GazeCombVerbose.Add(string.Join(",", x, y, z));//combined.eye_data.gaze_direction_normalized.x, combined.eye_data.gaze_direction_normalized.y, combined.eye_data.gaze_direction_normalized.z));

            GazeOriginCombVerbose.Add(string.Join(",", player.transform.position.x, player.transform.position.y, player.transform.position.z));//combined.eye_data.gaze_origin_mm.x, combined.eye_data.gaze_origin_mm.y, combined.eye_data.gaze_origin_mm.z));

            // Use when this actually works (still in development at HTC)
            //ConvergenceDistanceVerbose.Add(combined.convergence_distance_mm);

            PupilDiamVerbose.Add(string.Join(",", left.pupil_diameter_mm, right.pupil_diameter_mm));

            OpennessVerbose.Add(string.Join(",", left.eye_openness, right.eye_openness));

            //SystemsTimeVerbose.Add(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);

            //UnityEngine.Debug.Log(error);

            var tuple = CalculateConvergenceDistanceAndCoords(player.transform.position, new Vector3(-x, y, z), ~((1 << 12) | (1 << 13)));
            HitLocations.Add(tuple.Item1.ToString("F8").Trim(toTrim).Replace(" ", ""));

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
            ConvergenceDistanceVerbose.Add(tuple.Item2);

            if (isFull)
            {
                float xL = left.gaze_direction_normalized.x;
                float yL = left.gaze_direction_normalized.y;
                float zL = left.gaze_direction_normalized.z;
                float xL0 = -ipd / 2.0f;//left.gaze_origin_mm.x;
                float yL0 = 0.0f; //left.gaze_origin_mm.y;
                float zL0 = 0.0f; //left.gaze_origin_mm.z;

                float xR = right.gaze_direction_normalized.x;
                float yR = right.gaze_direction_normalized.y;
                float zR = right.gaze_direction_normalized.z;
                float xR0 = ipd / 2.0f;//right.gaze_origin_mm.x;
                float yR0 = 0.0f; //right.gaze_origin_mm.y;
                float zR0 = 0.0f; //right.gaze_origin_mm.z;

                GazeLeftVerbose.Add(string.Join(",", xL, yL, zL));

                GazeLeftOriginVerbose.Add(string.Join(",", xL0, yL0, zL0));

                GazeRightVerbose.Add(string.Join(",", xR, yR, zR));

                GazeRightOriginVerbose.Add(string.Join(",", xR0, yR0, zR0));

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

                HitLocationsL2D.Add(LHit2D.ToString("F8").Trim(toTrim).Replace(" ", ""));
                HitLocationsR2D.Add(RHit2D.ToString("F8").Trim(toTrim).Replace(" ", ""));

                ConvergenceDistanceL.Add(tuple.Item2);
                ConvergenceDistanceR.Add(tuple.Item2);

                File.AppendAllText(contPath, string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22}", trial[0], trialTime[0], position[0], rotation[0], v[0], w[0], GazeLeftVerbose[0], GazeLeftOriginVerbose[0], HitLocationsL[0], ConvergenceDistanceL[0], HitLocationsL2D[0], GazeRightVerbose[0], GazeRightOriginVerbose[0], HitLocationsR[0], ConvergenceDistanceR[0], HitLocationsR2D[0], GazeCombVerbose[0], GazeOriginCombVerbose[0], HitLocations[0], ConvergenceDistanceVerbose[0], HitLocations2D[0], PupilDiamVerbose[0], OpennessVerbose[0]) + "\n");
                trial.Clear();
                trialTime.Clear();
                position.Clear();
                rotation.Clear();
                v.Clear();
                w.Clear();
                GazeCombVerbose.Clear();
                GazeOriginCombVerbose.Clear();
                HitLocations.Clear();
                ConvergenceDistanceVerbose.Clear();
                PupilDiamVerbose.Clear();
                OpennessVerbose.Clear();
                HitLocations2D.Clear();
                GazeLeftVerbose.Clear();
                GazeLeftOriginVerbose.Clear();
                HitLocationsL.Clear();
                HitLocationsL2D.Clear();
                ConvergenceDistanceL.Clear();
                GazeRightVerbose.Clear();
                GazeRightOriginVerbose.Clear();
                HitLocationsR.Clear();
                HitLocationsR2D.Clear();
                ConvergenceDistanceR.Clear();
            }
        }
        else
        {
            GazeCombVerbose.Add(string.Join(",", 0.0f, 0.0f, 0.0f));
            GazeOriginCombVerbose.Add(string.Join(",", 0.0f, 0.0f, 0.0f));
            HitLocations.Add(string.Join(",", 0.0f, 0.0f, 0.0f));
            ConvergenceDistanceVerbose.Add(0.0f);
            PupilDiamVerbose.Add(string.Join(",", 0.0f, 0.0f, 0.0f));
            OpennessVerbose.Add(string.Join(",", 0.0f, 0.0f, 0.0f));
            HitLocations2D.Add(string.Join(",", 0.0f, 0.0f));
            HitLocationsL2D.Add(string.Join(",", 0.0f, 0.0f));
            HitLocationsR2D.Add(string.Join(",", 0.0f, 0.0f));
            ConvergenceDistanceL.Add(0.0f);
            ConvergenceDistanceR.Add(0.0f);
            GazeLeftVerbose.Add(string.Join(",", 0.0f, 0.0f, 0.0f));
            GazeLeftOriginVerbose.Add(string.Join(",", 0.0f, 0.0f, 0.0f));
            GazeRightVerbose.Add(string.Join(",", 0.0f, 0.0f, 0.0f));
            GazeRightOriginVerbose.Add(string.Join(",", 0.0f, 0.0f, 0.0f));
        }

        if (Input.GetKey(KeyCode.Return))
        {
            // Environment.Exit(Environment.ExitCode);
            Save();
        }
    }

    // Update is called once per frame
    void Update()
    {
        print(Vector3.Distance(player.transform.position, screen1.transform.position));
    }

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

    public void Save()
    {
        try
        {
            StringBuilder csvCont = new StringBuilder();

            string firstLine;

            List<int> temp;

            if (!isFull)
            {
                firstLine = "TrialTime,Click,Reward,PosX,PosY,PosZ,RotX,RotY,RotZ,RotW,LinearVelocty,AngularVelocity,GazeX,GazeY,GazeZ,GazeX0,GazeY0,GazeZ0,HitX,HitY,HitZ,ConvergeDist,LeftPupilDiam,RightPupilDiam,LeftOpen,RightOpen";


                csvCont.AppendLine(firstLine);

                temp = new List<int>() {
                    trialTime.Count,
                    click.Count,
                    reward.Count,
                    position.Count,
                    rotation.Count,
                    v.Count,
                    w.Count,
                    GazeCombVerbose.Count,
                    GazeOriginCombVerbose.Count,
                    HitLocations.Count,
                    ConvergenceDistanceVerbose.Count,
                    PupilDiamVerbose.Count,
                    OpennessVerbose.Count,
                };

                temp.Sort();
                for (int i = 0; i < temp[0]; i++)
                {
                    var line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}", trialTime[i], click[i], reward[i], position[i], rotation[i], v[i], w[i], GazeCombVerbose[i], GazeOriginCombVerbose[i], HitLocations[i], ConvergenceDistanceVerbose[i], PupilDiamVerbose[i], OpennessVerbose[i]);
                    //UnityEngine.Debug.Log(i + " " + line);
                    csvCont.AppendLine(line);
                }
            }

            string contPath = path + "/continuous_data_" + PlayerPrefs.GetInt("Optic Flow Seed").ToString() + ".csv";

            File.WriteAllText(contPath, csvCont.ToString());

            string configPath = path + "/config_" + PlayerPrefs.GetInt("Optic Flow Seed").ToString() + ".xml";

            XmlWriter xmlWriter = XmlWriter.Create(configPath);

            xmlWriter.WriteStartDocument();

            xmlWriter.WriteStartElement("Settings");

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

            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("Setting");
            xmlWriter.WriteAttributeString("Type", "Data Collection Settings");

            xmlWriter.WriteStartElement("Path");
            xmlWriter.WriteString(PlayerPrefs.GetString("Path"));
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("FullON");
            xmlWriter.WriteString(PlayerPrefs.GetInt("Full ON").ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("Seed");
            xmlWriter.WriteString(seed.ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndDocument();
            xmlWriter.Close();

            SceneManager.LoadScene("MainMenu");
            SceneManager.UnloadSceneAsync("Human Arena");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log(e);
        }
    }
}