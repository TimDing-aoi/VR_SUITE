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

        if (!ptb)
        {
            MaxSpeed = 20.0f * PlayerPrefs.GetFloat("Player Height");
            RotSpeed = 90.0f;
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
                int cammode = 0;

                moveY = PlayerPrefs.GetFloat("FixedYSpeed");
                //print(Vector3.Distance(new Vector3(0f, 0f, 0f), transform.position));

                bool self_motion = PlayerPrefs.GetInt("SelfMotionOn") == 1;
                if (Vector3.Distance(new Vector3(0f, 0f, 0f), transform.position) > (minR + maxR) / 2 || !self_motion && SharedReward.GFFPhaseFlag == 1
                    || !self_motion && SharedReward.GFFPhaseFlag == 2 || !self_motion && SharedReward.GFFPhaseFlag == 3 || SharedReward.isTimeout)
                //Out of circle(Feedback) OR No selfmotion's Preparation & Habituation & Observation OR Timed Out
                {
                    //print("out of ring");
                    if (worldcentric)
                    {
                        transform.rotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);
                    }
                    moveY = 0;                    
                    timeCounter = 0;
                    frameCounter = 0;
                    hbobCounter = 0;
                    circX = 0;
                }
                else if (self_motion && SharedReward.GFFPhaseFlag == 1)
                    //Selmotion preperation
                {
                    float fixedSpeed = PlayerPrefs.GetFloat("FixedYSpeed"); // in meter per second
                    float offset = fixedSpeed * 0.9f; //Offset for the player at start
                    transform.position = new Vector3(-offset, 0f, 0f);
                    if (cammode == 0) //Simply facing outward
                    {
                        transform.LookAt(new Vector3(0f, 0f, 0f));
                    }
                    transform.position = new Vector3(-offset, 1f, 0f);
                    circX = 0;
                }
                else if (self_motion && SharedReward.GFFPhaseFlag == 2 || self_motion && SharedReward.GFFPhaseFlag == 3) 
                //Selfmotion Habituation & Observation
                {
                    float fixedSpeed = PlayerPrefs.GetFloat("FixedYSpeed"); // in meter per second
                    float offset = fixedSpeed * 0.9f; //Offset for the player at start
                    float frameRate = 120.0f;
                    hbobCounter += fixedSpeed/frameRate;
                    float x = offset - hbobCounter;
                    if (worldcentric)
                    {
                        transform.position = new Vector3(-x, 0f, 0f);
                        if (cammode == 0) //Simply facing outward
                        {
                            transform.LookAt(new Vector3(0f, 0f, 0f));
                        }
                        transform.position = new Vector3(-x, 1f, 0f);
                    }
                    else
                    {
                        transform.position = new Vector3(0f, 0f, -x);
                        if (cammode == 0) //Simply facing outward
                        {
                            transform.LookAt(new Vector3(0f, 0f, 0f));
                        }
                        transform.position = new Vector3(0f, 1f, -x);
                    }
                    circX = 0;
                }
                else
                {
                    
                    if (worldcentric)
                    {
                        float fixedSpeed = PlayerPrefs.GetFloat("FixedYSpeed"); // in meter per second
                        //float maxDistance = 30.0f; // should come from PlayerPrefs.GetFloat("XYZ");
                        // set values
                        float maxJoyRotDeg = 50.0f;// 59.0f; // deg/s
                        float maxJoyRotRad = 30.0f; // rad/s
                        float frameRate = 120.0f; // frame rate
                        float joyConvRateDeg = maxJoyRotDeg / frameRate;
                        float joyConvRateRad = maxJoyRotRad / frameRate;

                        // Read input from joystick 

                        // for rad/s 
                        //float theta = joyConvRateRad * moveX * Mathf.Rad2Deg; // moveX consider to be in Radian so we use radian to degree convertion

                        // for deg/s
                        float theta = joyConvRateDeg * moveX; // moveX consider to be in degree; We use joyConvRate in Degree

                        // transfering phi from deg to rad if its necessary
                        theta = theta * Mathf.Deg2Rad;

                        //timeCounter += 0.005f * speedMultiplier;
                        frameCounter += 1;
                        frameCounterShared = frameCounter;
                        timeCounter += Time.smoothDeltaTime;
                        timeCounterShared = timeCounter;
                        //circX -= moveX * (float)Math.PI / 180;//Unrealistic steering
                        circX -= theta;//Unrealistic steering
                        //circX -= moveX * (float)Math.PI / (180 * timeCounter);//Realistic steering
                        float x = Mathf.Cos(circX);
                        float z = Mathf.Sin(circX);

                        tmpCnt += 1;
                        if (tmpCnt > 120)
                        {
                            tmpCnt = 0;
                            /*print("moveX");
                            print(moveX);
                            print("theta");
                            print(theta);
                            print("speedMultiplier");
                            print(speedMultiplier);
                            print("speedMultiplier2");
                            print(0.005f * speedMultiplier);
                            print("moveY");
                            print(moveY);
                            print("deltaTime");
                            print(Time.smoothDeltaTime);*/
                        }

                        

                        Vector3 previouspos = transform.position;
                        //transform.position = new Vector3(moveY * timeCounter * x, 0f, moveY * timeCounter * z);
                        transform.position = new Vector3(fixedSpeed * timeCounter * x, 0f, fixedSpeed * timeCounter * z);
                        FF = GameObject.Find("Firefly");
                        if (cammode == 0) //Simply facing outward
                        {
                            transform.LookAt(new Vector3(0f, 0f, 0f));
                            transform.Rotate(0f, 180f, 0f);
                        }
                        else if (cammode == 1) //Calculated Tangent
                        {
                            Vector3 lookatpos = new Vector3(timeCounter * x * 2, 1f, timeCounter * z * 2);
                            transform.LookAt(lookatpos);
                            transform.Rotate(0.0f, moveX * 180f / (float)Math.PI, 0.0f, Space.Self);
                        }
                        else if (cammode == 2) //Numerical Tangent
                        {
                            Vector3 lookatpos = 2 * transform.position - previouspos;
                            transform.LookAt(lookatpos);
                        }
                        //transform.position = new Vector3(moveY * timeCounter * x, 1f, moveY * timeCounter * z);
                        transform.position = new Vector3(fixedSpeed * timeCounter * x, 1f, fixedSpeed * timeCounter * z);
                        circXlast = circX;
                    }
                    else
                    {
                        //print("Egocentric");
                        float fixedSpeed = PlayerPrefs.GetFloat("FixedYSpeed"); // in meter per second
                        //float maxDistance = 30.0f; // should come from PlayerPrefs.GetFloat("XYZ");

                        frameCounter += 1;
                        frameCounterShared = frameCounter;
                        timeCounter += Time.smoothDeltaTime;
                        timeCounterShared = timeCounter;

                        // set values
                        float maxJoyRotDeg = 74.0f;// 85f; // deg/s
                        float maxJoyRotRad = 1000.0f; // rad/s
                        float frameRate = 120.0f; // frame rate
                        float joyConvRateDeg = maxJoyRotDeg / frameRate;
                        float joyConvRateRad = maxJoyRotRad / frameRate;

                        // Read input from joystick 

                        // for rad/s 
                        //float phi = joyConvRateRad * moveX * Mathf.Rad2Deg; // moveX consider to be in Radian so we use radian to degree convertion

                        // for deg/s
                        float phi = joyConvRateDeg * moveX; // moveX consider to be in degree

                        tmpCnt += 1;
                        if (tmpCnt > 120)
                        {
                            tmpCnt = 0;
                            print("phi");
                            print(phi);
                            print("moveY");
                            print(moveY);
                        }

                        // Rotate the camera based on joystick input
                        transform.Rotate(0.0f, phi, 0.0f, Space.Self);

                        // Read camre rotation (deg and radian)
                        float yRot_deg = transform.rotation.eulerAngles.y;
                        float yRot_rad = yRot_deg * Mathf.Deg2Rad;

                        // Pritn camera rotation
                        //print(yRot_deg);
                        //print(yRot_rad);

                        float timeBetweenFrames = Time.smoothDeltaTime;

                        // Calculate player location based on camera angle
                        //float x = transform.position.x + 0.005f * moveY * Mathf.Sin(yRot_rad);
                        //float z = transform.position.z + 0.005f * moveY * Mathf.Cos(yRot_rad);

                        //float x = transform.position.x + timeBetweenFrames * moveY * Mathf.Sin(yRot_rad);
                        //float z = transform.position.z + timeBetweenFrames * moveY * Mathf.Cos(yRot_rad);

                        float x = transform.position.x + timeBetweenFrames * fixedSpeed * Mathf.Sin(yRot_rad);
                        float z = transform.position.z + timeBetweenFrames * fixedSpeed * Mathf.Cos(yRot_rad);

                        // Update player location based on new calculated values
                        transform.position = new Vector3(x, 1f, z);
                       
                    }
                }
            }
            else
            {
                transform.position = transform.position + transform.forward * currentSpeed * Time.deltaTime;
                transform.Rotate(0f, currentRot * Time.deltaTime, 0f);
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