using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO.Ports;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using static Reward2D;
using UnityEngine.InputSystem.LowLevel;
using static timelinestamps;

public class AlloEgoJoystick : MonoBehaviour
{
    wrmhl joystick = new wrmhl();

    [Tooltip("SerialPort of your device.")]
    public string portName = "COM4";

    [Tooltip("Baudrate")]
    public int baudRate = 1000000;

    [Tooltip("Timeout")]
    public int ReadTimeout = 5000;

    [Tooltip("QueueLength")]
    public int QueueLength = 1;

    public static AlloEgoJoystick SharedJoystick;

    public float moveX;
    public float moveY;
    public float circX;
    public float circXlast;
    public int press;
    [ShowOnly] public float currentSpeed = 0.0f;
    [ShowOnly] public float currentRot = 0.0f;
    public float RotSpeed = 0.0f;
    public float MaxSpeed = 0.0f;

    public float phiShared = 0.0f;

    public bool worldcentric = true;

    //readonly List<float> t = new List<float>();
    //readonly List<bool> isPtb = new List<bool>();
    //readonly List<float> rawX = new List<float>();
    //readonly List<float> rawY = new List<float>();
    //readonly List<float> v = new List<float>();
    //readonly List<float> w = new List<float>();
    //readonly List<float> vAddPtb = new List<float>();
    //readonly List<float> wAddPtb = new List<float>();

    private System.Random rand;
    [ShowOnly] public int seed;

    const int count = 90;
    const float R = 7.69711747013104972f;
    const float A = 0.00492867323399f;
    const ulong MAXINT = (1UL << 53) - 1;
    const double INCR = 1.0 / MAXINT;

    float[] x = new float[count + 1];
    float[] y = new float[count + 1];
    ulong[] xcomp;
    float aDivY0;

    public float timeCounterShared = 0;
    private float timeCounter = 0;
    public float frameCounterShared = 0;
    private float frameCounter = 0;
    private float hbobCounter = 0;
    private float accelCounter = 0;
    private float decelCounter = 0;
    private float tmpCnt = 0.0f;

    public GameObject FF;

    [HideInInspector] public bool ptb;

    [HideInInspector] public float gainVel = 0.0f;
    [HideInInspector] public float gainRot = 0.0f;

    [HideInInspector] public float meanDist;
    [HideInInspector] public float meanTime;
    [HideInInspector] public float meanAngle;
    [HideInInspector] public bool flagPTBType;
    [HideInInspector] public float minTau;
    [HideInInspector] public float maxTau;
    [HideInInspector] public float numTau;
    [HideInInspector] public float meanLogSpace;
    [HideInInspector] public float stdDevLogSpace;
    [HideInInspector] public float timeConstant;
    [HideInInspector] public float filterTau;
    [HideInInspector] public float velFilterGain;
    [HideInInspector] public float rotFilterGain;
    [HideInInspector] public float gamma;
    [HideInInspector] public float meanNoise;
    [HideInInspector] public float stdDevNoise;
    [HideInInspector] public float logSample;

    [HideInInspector] public float velKsi = 0.0f;
    [HideInInspector] public float prevVelKsi = 0.0f;
    [HideInInspector] public float rotKsi = 0.0f;
    [HideInInspector] public float prevRotKsi = 0.0f;
    [HideInInspector] public float velEta = 0.0f;
    [HideInInspector] public float prevVelEta = 0.0f;
    [HideInInspector] public float rotEta = 0.0f;
    [HideInInspector] public float prevRotEta = 0.0f;
    [HideInInspector] public float cleanVel = 0.0f;
    [HideInInspector] public float prevCleanVel = 0.0f;
    [HideInInspector] public float cleanRot = 0.0f;
    [HideInInspector] public float prevCleanRot = 0.0f;

    public float currentTau;
    public List<float> taus = new List<float>();

    private float prevX;
    private float prevY;

    //Causal Inference Max rot speed
    private float maxJoyRotDeg = 60.0f;// deg/s

    private float frameRate = 90.0f; // frame rate

    // Start is called before the first frame update
    void Awake()
    {
        SharedJoystick = this;
    }

    void Start()
    {
        seed = UnityEngine.Random.Range(1, 10000);
        rand = new System.Random(seed);

        circX = 0;

        ptb = PlayerPrefs.GetInt("Type") != 2;

        //print(PlayerPrefs.GetInt("Type"));
        //print(ptb);

        if (!ptb && PlayerPrefs.GetFloat("FixedYSpeed") == 0)
        {
            MaxSpeed = 20.0f * PlayerPrefs.GetFloat("Player Height");
            RotSpeed = 90.0f;
        }
        else if(PlayerPrefs.GetFloat("FixedYSpeed") != 0)
        {
            MaxSpeed = PlayerPrefs.GetFloat("FixedYSpeed") * 0.1f;
            RotSpeed = 60.0f;
        }

        meanDist = PlayerPrefs.GetFloat("Mean Distance");
        meanTime = PlayerPrefs.GetFloat("Mean Travel Time");
        meanAngle = 3.0f * PlayerPrefs.GetFloat("Max Angle");
        flagPTBType = PlayerPrefs.GetInt("Type") != 2;
        minTau = PlayerPrefs.GetFloat("Min Tau");
        maxTau = PlayerPrefs.GetFloat("Max Tau");
        numTau = (int)PlayerPrefs.GetFloat("Number Of Taus");
        timeConstant = PlayerPrefs.GetFloat("Tau Tau");
        filterTau = PlayerPrefs.GetFloat("Filter Tau");
        velFilterGain = PlayerPrefs.GetFloat("Velocity Noise Gain");
        rotFilterGain = PlayerPrefs.GetFloat("Rotation Noise Gain");

        x[0] = R;
        y[0] = GaussianPDFDenorm(R);

        x[1] = R;
        y[1] = y[0] + (A / x[1]);

        for (int i = 2; i < count; i++)
        {
            x[i] = GaussianPDFDenormInv(y[i - 1]);
            y[i] = y[i - 1] + (A / x[i]);
        }

        x[count] = 0.0f;

        aDivY0 = A / y[0];
        xcomp = new ulong[count];

        xcomp[0] = (ulong)(R * y[0] / A * (double)MAXINT);

        for (int i = 1; i < count - 1; i++)
        {
            xcomp[i] = (ulong)(x[i + 1] / x[i] * (double)MAXINT);
        }

        xcomp[count - 1] = 0;

        gamma = Mathf.Exp(-1f / timeConstant);
        
        if (flagPTBType)
        {
            var linspace = (maxTau - minTau) / (numTau - 1);

            for (int i = 0; i < numTau; i++)
            {
                taus.Add(minTau + (i * linspace));
                print(string.Format("tau{0} = {1}", i, taus[i]));
            }

            if (taus[taus.Count - 1] != maxTau)
            {
                taus[taus.Count - 1] = maxTau;
            }
        }
        else
        {
            meanNoise = 0.5f * (Mathf.Log(minTau) + Mathf.Log(maxTau));
            stdDevNoise = 0.5f * (meanNoise - Mathf.Log(minTau));
            meanLogSpace = meanNoise * (1.0f - gamma);
            stdDevLogSpace = stdDevNoise * Mathf.Sqrt(1.0f - (gamma * gamma));
            //print(string.Format("muPhi = {0}, sigPhi = {1}, muEta = {2}, sigEta = {3}", meanNoise, stdDevNoise, meanLogSpace, stdDevLogSpace));
        }
    }

    private void FixedUpdate()
    {
        try
        {
            CTIJoystick joystick = CTIJoystick.current;
            moveX = joystick.x.ReadValue();
            moveY = joystick.y.ReadValue();
            /*moveX = Input.GetAxis("Vertical");
            if (Mathf.Abs(moveX) < 0.05f)
            {
                moveX = 0;
            }
            moveY = 1.0f;*/

            if (moveX < 0.0f)
            {
                moveX += 1.0f;
            }
            else if (moveX > 0.0f)
            {
                moveX -= 1.0f;
            }
            else if (moveX == 0)
            {
                if (prevX < 0.0f)
                {
                    moveX -= 1.0f;
                }
                else if (prevX > 0.0f)
                {
                    moveX += 1.0f;
                }
            }
            prevX = moveX;

            if (moveY < 0.0f)
            {
                moveY += 1.0f;
            }
            else if (moveY > 0.0f)
            {
                moveY -= 1.0f;
            }
            else if (moveY == 0)
            {
                if (prevY < 0.0f)
                {
                    moveY -= 1.0f;
                }
                else if (prevY > 0.0f)
                {
                    moveY += 1.0f;
                }
            }
            prevY = moveY;

            float minR = PlayerPrefs.GetFloat("Minimum Firefly Distance");
            float maxR = PlayerPrefs.GetFloat("Maximum Firefly Distance");

            if (ptb)
            {
                ProcessNoise();
                //print(currentSpeed);
            }
            else
            {
                currentRot = moveX * RotSpeed;
                if (PlayerPrefs.GetFloat("FixedYSpeed") != 0)
                {
                    if (Vector3.Distance(new Vector3(0f, 0f, 0f), transform.position) > (minR + maxR) / 2)
                    {
                        currentSpeed = 0.0f;
                        SharedReward.currPhase = Phases.check;
                    }
                    else
                    {
                        currentSpeed = 1.0f;
                    }
                }
                else
                {
                    currentSpeed = moveY * MaxSpeed;
                }
                cleanVel = currentSpeed;
                cleanRot = currentRot;
            }
            //transform.position = transform.position + transform.forward * currentSpeed * Time.fixedDeltaTime;
            //transform.Rotate(0f, currentRot * Time.fixedDeltaTime, 0f);
            if (PlayerPrefs.GetFloat("FixedYSpeed") != 0)
            {
                moveY = PlayerPrefs.GetFloat("FixedYSpeed");
                //print(Vector3.Distance(new Vector3(0f, 0f, 0f), transform.position));

                bool self_motion = SharedReward.selfmotiontrial;
                if (Vector3.Distance(new Vector3(0f, 0f, 0f), transform.position) > (minR + maxR) / 2 || SharedReward.GFFPhaseFlag == 1
                    || SharedReward.GFFPhaseFlag == 2 || !self_motion && SharedReward.GFFPhaseFlag == 3 || SharedReward.isTimeout)
                //Out of circle(Feedback) OR Preparation & Habituation & No selfmotion's Observation OR Timed Out
                {
                    transform.rotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);
                    moveY = 0;                    
                    timeCounter = 0;
                    accelCounter = 0;
                    decelCounter = 0;
                    frameCounter = 0;
                    hbobCounter = 0;
                    circX = 0;
                }
                else if (self_motion && SharedReward.GFFPhaseFlag == 2.5)
                //Selfmotion Ramp Up
                {
                    float updur = PlayerPrefs.GetFloat("RampUpDur");
                    float accelTime = Mathf.Ceil(updur * 90);
                    float SMspeed = SharedReward.SelfMotionSpeed;
                    float acceleration = (accelCounter / accelTime) * SMspeed;
                    transform.rotation = Quaternion.Euler(0.0f, 90.0f + hbobCounter, 0.0f);
                    moveY = 0;
                    timeCounter = 0;
                    accelCounter++;
                    frameCounter = 0;
                    hbobCounter += acceleration/frameRate;
                    if (hbobCounter > 0)
                    {
                        circX = (360 - hbobCounter) * Mathf.Deg2Rad;
                    }
                    else
                    {
                        circX = -hbobCounter * Mathf.Deg2Rad;
                    }
                }
                else if (self_motion && SharedReward.GFFPhaseFlag == 3.5)
                //Selfmotion Ramp Down
                {
                    float downdur = PlayerPrefs.GetFloat("RampDownDur");
                    float decelTime = Mathf.Ceil(downdur * 90);
                    float SMspeed = SharedReward.SelfMotionSpeed;
                    float deceleration = (1-(decelCounter / decelTime)) * SMspeed;
                    transform.rotation = Quaternion.Euler(0.0f, 90.0f + hbobCounter, 0.0f);
                    moveY = 0;
                    timeCounter = 0;
                    decelCounter++;
                    frameCounter = 0;
                    hbobCounter += deceleration/frameRate;
                    if(hbobCounter > 0)
                    {
                        circX = (360 - hbobCounter) * Mathf.Deg2Rad;
                    }
                    else
                    {
                        circX = -hbobCounter * Mathf.Deg2Rad;
                    }
                }
                else if (self_motion && SharedReward.GFFPhaseFlag == 3) 
                //Selfmotion Observation
                {
                    float SMspeed = SharedReward.SelfMotionSpeed/frameRate;
                    transform.rotation = Quaternion.Euler(0.0f, 90.0f + hbobCounter, 0.0f);
                    moveY = 0;
                    timeCounter = 0;
                    frameCounter = 0;
                    hbobCounter += SMspeed;
                    if (hbobCounter > 0)
                    {
                        circX = (360 - hbobCounter) * Mathf.Deg2Rad;
                    }
                    else
                    {
                        circX = -hbobCounter * Mathf.Deg2Rad;
                    }
                }
                else if(SharedReward.GFFPhaseFlag == 4)
                //action
                {
                    int framcntTemp = Time.frameCount;
                    float fixedSpeed = PlayerPrefs.GetFloat("FixedYSpeed"); // in meter per second
                    float joyConvRateDeg = maxJoyRotDeg / frameRate;

                    // for deg/s
                    float theta = joyConvRateDeg * moveX; // moveX consider to be in degree; We use joyConvRate in Degree

                    // transfering theta from deg to rad if its necessary
                    theta = theta * Mathf.Deg2Rad;

                    //timeCounter += 0.005f * speedMultiplier;
                    frameCounter += 1;
                    frameCounterShared = frameCounter;
                    timeCounter += Time.deltaTime;
                    timeCounterShared = timeCounter;
                    circX -= theta;//Unrealistic steering
                    float x = Mathf.Cos(circX);
                    float z = Mathf.Sin(circX);

                    tmpCnt += 1;
                    if (tmpCnt > 90)
                    {
                        tmpCnt = 0;
                    }
                    Vector3 previouspos = transform.position;
                    //transform.position = new Vector3(moveY * timeCounter * x, 0f, moveY * timeCounter * z);
                    transform.position = new Vector3(fixedSpeed * timeCounter * x, 0f, fixedSpeed * timeCounter * z);
                    FF = GameObject.Find("Firefly");
                    transform.LookAt(new Vector3(0f, 0f, 0f));
                    transform.Rotate(0f, 180f, 0f);
                    //transform.position = new Vector3(moveY * timeCounter * x, 1f, moveY * timeCounter * z);
                    transform.position = new Vector3(fixedSpeed * timeCounter * x, 1f, fixedSpeed * timeCounter * z);
                    circXlast = circX;
                }
            }
            else
            {
                if(SharedReward.isTrial)
                {
                    transform.position = transform.position + transform.forward * currentSpeed * Time.deltaTime;
                    transform.Rotate(0f, currentRot * Time.deltaTime, 0f);
                }
            }
        }
        catch (Exception e)
        {
            // It's gonna be the same exception everytime, but I'm purposely doing this.
            // It's just that this code will read serial in faster than it's actually
            // coming in so there'll be an error saying there's no object or something
            // like that.
        }
        //print(line);
    }

    void Update()
    {

    }

    public float[] MakeProfile()
    {
        float sig = 1.0f;
        int size = 180; // 3 seconds

        float[] temp = new float[size];

        for (int i = 0; i < size; i++)
        {
            // Gaussian Curve Equation
        }

        return temp;
    }

    public void DiscreteTau()
    {
        if (rand.NextDouble() > gamma)
        {
            var temp = currentTau;

            do
            {
                currentTau = taus[rand.Next(0, taus.Count)];
            } while (temp == currentTau);
        }

        CalculateMaxValues();
    }

    public void ContinuousTau()
    {
        logSample = gamma * logSample + ((stdDevLogSpace * BoxMullerGaussianSample()) + meanLogSpace);
        currentTau = Mathf.Exp(logSample);
        //print(currentTau);
        CalculateMaxValues();
    }

    private void CalculateMaxValues()
    {
        MaxSpeed = (meanDist / meanTime) * (1.0f / (-1.0f + (2 * (currentTau / meanTime)) * Mathf.Log((1 + Mathf.Exp(meanTime / currentTau)) / 2.0f)));
        RotSpeed = (meanAngle / meanTime) * (1.0f / (-1.0f + (2 * (currentTau / meanTime)) * Mathf.Log((1 + Mathf.Exp(meanTime / currentTau)) / 2.0f)));
    }

    private void ProcessNoise()
    {
        float alpha = Mathf.Exp(-Time.fixedDeltaTime / currentTau);
        float beta = (1.0f - alpha);
        float gamma = Mathf.Exp(-Time.fixedDeltaTime / filterTau);
        float delta = (1.0f - gamma);

        //print(string.Format("{0},{1},{2},{3}", alpha, beta, delta, gamma));
        //print(currentTau);

        velKsi = gamma * prevVelKsi + delta * BoxMullerGaussianSample();
        velEta = gamma * prevVelEta + delta * velFilterGain * velKsi;
        cleanVel = alpha * prevCleanVel + MaxSpeed * beta * moveY;
        prevCleanVel = cleanVel;
        prevVelKsi = velKsi;
        prevVelEta = velEta;

        rotKsi = gamma * prevRotKsi + delta * BoxMullerGaussianSample();
        rotEta = gamma * prevRotEta + delta * rotFilterGain * rotKsi;
        cleanRot = alpha * prevCleanRot + RotSpeed * beta * moveX;
        prevCleanRot = cleanRot;
        prevRotKsi = rotKsi;
        prevRotEta = rotEta;

        currentSpeed = (1.0f + velEta) * cleanVel;
        currentRot = (1.0f + rotEta) * cleanRot;
    }

    public float BoxMullerGaussianSample()
    {
        float u1, u2, S;
        do
        {
            u1 = 2.0f * (float)rand.NextDouble() - 1.0f;
            u2 = 2.0f * (float)rand.NextDouble() - 1.0f;
            S = u1 * u1 + u2 * u2;
        }
        while (S >= 1.0f);
        return u1 * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);
    }

    public float ZigguratGaussianSample()
    {
        byte[] bytes = new byte[8];
        rand.NextBytes(bytes);
        for(; ; )
        {
            ulong u = BitConverter.ToUInt64(bytes, 0);

            int s = (int)((u >> 3) & 0x7f);

            float sign = ((u & 0x400) == 0) ? 1.0f : -1.0f;

            ulong u2 = u >> 11;

            if (0 == s)
            {
                if (u2 < xcomp[0])
                {
                    return (float)(u2 * INCR * aDivY0 * sign);
                }
                return SampleTail() * sign;
            }

            if (u2 < xcomp[s])
            {
                return (float)(u2 * INCR * x[s] * sign);
            }

            float _x = (float)(u2 * INCR * x[s]);

            if ((y[s - 1] + ((y[s] - y[s - 1]) * (float)rand.NextDouble()) < GaussianPDFDenorm(_x)))
            {
                return _x * sign;
            }
        }
    }

    public float GaussianPDFDenorm(float x)
    {
        return Mathf.Exp(-(x * x / 2.0f));
    }

    public float GaussianPDFDenormInv(float y)
    {
        return Mathf.Sqrt(-2.0f * Mathf.Log(y));
    }

    public float SampleTail()
    {
        float x, y;
        do
        {
            x = -Mathf.Log((float)rand.NextDouble()) / R;
            y = -Mathf.Log((float)rand.NextDouble());
        }
        while(y + y < x * x && (x == 0 || y == 0));
        return R + x;
    }
}