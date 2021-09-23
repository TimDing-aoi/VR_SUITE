using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using static ViveSR.anipal.Eye.SRanipal_Eye_Framework;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler SharedInstance;

    public float lifeSpan;              // editable
    public List<GameObject> pooledObjects;
    public GameObject objectToPool;
    public GameObject player;
    private int amountToPool;
    private float phi;
    [ShowOnly] public float fill = 0.0f;
    private float add;
    public Camera cam;
    public float drawDistance;          // editable
    public float density;               // editable
    [ShowOnly] public bool start = false;
    [ShowOnly] public int seed;
    private RigidbodyFirstPersonControllerv2 rigidbodyFirstPersonControllerv2;

    void Start()
    {
        try
        {
            if (PlayerPrefs.GetString("Switch Mode") == "experiment")
            {
                seed = UnityEngine.Random.Range(1, 10000);
            }
            else if (PlayerPrefs.GetString("Switch Mode") == "replication")
            {
                seed = PlayerPrefs.GetInt("Optic Flow Seed");
            }
            else
            {
                throw new Exception("No seed available.");
            }
            UnityEngine.Random.InitState(seed);
            drawDistance = PlayerPrefs.GetFloat("Draw Distance");
            SharedInstance = this;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 300;
            lifeSpan = PlayerPrefs.GetFloat("Life Span");
            density = PlayerPrefs.GetFloat("Density");
            phi = cam.fieldOfView * cam.pixelWidth / cam.pixelHeight / 2f;
            amountToPool = (int)Mathf.Round(density * (Mathf.Pow(drawDistance, 2f) * Mathf.Atan2(cam.pixelHeight, cam.pixelWidth)) / 2.0f);
            //print(cam.pixelHeight);
            //print(cam.pixelWidth);
            //print(amountToPool);
            //print(phi);
            pooledObjects = new List<GameObject>();
            for (int i = 0; i < amountToPool; i++)
            {
                GameObject obj = (GameObject)Instantiate(objectToPool);
                obj.name = i.ToString();
                pooledObjects.Add(obj);
            }
            add = 1.0f / amountToPool;

            while (!SharedEye.ready) ;
            StartBlink();
        }
        catch (Exception e)
        {
            Debug.LogException(e, this);
        }
    }

    void Update()
    {
        //print(player.transform.localEulerAngles.y * Mathf.Deg2Rad + phi);
        //print(player.transform.localEulerAngles.y * Mathf.Deg2Rad - phi);
    }

    void StartBlink()
    {
        for (int i = 0; i < amountToPool; i++)
        {
            Blink(pooledObjects[i], lifeSpan);
            fill += add;
        }
        if (fill < 1.0f)
        {
            fill = 1.0f;
        }
        start = true;
    }

    /// <summary>
    /// Set object to inactive after a user-specified amount of time
    /// </summary>
    /// <param name="obj"> Object to blink </param>
    /// <param name="delay"> User specified amount of time </param>
    /// <returns></returns>
    async void Blink(GameObject obj, float delay)
    {
        int i = 0;
        while (Application.isPlaying)
        {
            float r = Mathf.Sqrt(Mathf.Pow(drawDistance, 2.0f) * UnityEngine.Random.Range(0.0f, 1.0f));
            Vector3 position = player.transform.position + Quaternion.AngleAxis(UnityEngine.Random.Range(-phi, phi), Vector3.up) * player.transform.forward * r;
            position.y = 0.0001f;
            Quaternion rotation = Quaternion.Euler(90, UnityEngine.Random.Range(0, 360), 90);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            if (i == 0)
            {
                float op = delay + UnityEngine.Random.Range(-(delay / 2f), (delay / 2f));
                await new WaitForSecondsRealtime(op);
                i++;
            }
            else
            {
                await new WaitForSecondsRealtime(delay);
            }
        }
    }
}
