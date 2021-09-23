using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using UnityEngine;
using static FPSDisplay;

public class FireflySim : MonoBehaviour
{
    private List<float> t = new List<float>();
    private List<float> yaw = new List<float>();
    private List<float> px = new List<float>();
    private List<float> pz = new List<float>();
    private List<float> io = new List<float>();
    private List<float> fx = new List<float>();
    private List<float> fz = new List<float>();

    public GameObject firefly;
    public GameObject viktor;

    private int i = 0;
    private int k = 0;

    private float py = 0.1f;
    private float fy = 0.0001f;

    // Start is called before the first frame update
    void Start()
    {
        firefly.SetActive(false);

        var temp = File.ReadAllLines(@"F:\Work\trajectoriesintegrated_viktor.csv");

        var first = temp[0].Split(',');
        // float tOffset = float.Parse(first[0], CultureInfo.InvariantCulture.NumberFormat);
        float yawOffset = 360.0f * 4.0f;
        float pxOffset = float.Parse(first[2], CultureInfo.InvariantCulture.NumberFormat);
        float pyOffset = float.Parse(first[3], CultureInfo.InvariantCulture.NumberFormat);
        float fxOffset = float.Parse(first[5], CultureInfo.InvariantCulture.NumberFormat);
        float fyOffset = float.Parse(first[6], CultureInfo.InvariantCulture.NumberFormat);
 
        foreach(string line in temp)
        {
            var delimitedLine = line.Split(',');
            // t.Add(float.Parse(delimitedLine[0], CultureInfo.InvariantCulture.NumberFormat) - tOffset);
            yaw.Add(float.Parse(delimitedLine[1], CultureInfo.InvariantCulture.NumberFormat) - yawOffset);
            px.Add(float.Parse(delimitedLine[2], CultureInfo.InvariantCulture.NumberFormat) - pxOffset);
            pz.Add(float.Parse(delimitedLine[3], CultureInfo.InvariantCulture.NumberFormat) - pyOffset);
            io.Add(float.Parse(delimitedLine[4], CultureInfo.InvariantCulture.NumberFormat));
            fx.Add(float.Parse(delimitedLine[5], CultureInfo.InvariantCulture.NumberFormat) - pxOffset);
            fz.Add(float.Parse(delimitedLine[6], CultureInfo.InvariantCulture.NumberFormat) - pyOffset);
        }
        Application.targetFrameRate = 100;
    }

    // Update is called once per frame
    void Update()
    {
        if (i > 999)
        {
            viktor.transform.position = new Vector3(px[k] / 100, py, pz[k] / 100);
            viktor.transform.eulerAngles = new Vector3(0, yaw[k], 0);
            firefly.transform.position = new Vector3(fx[k] / 100, fy, fz[k] / 100);
            if (io[k] > 4)
            {
                firefly.SetActive(true);
            }
            else
            {
                firefly.SetActive(false);
            }
            k += (int)Mathf.Round(200 / FPSDisplay.FPScounter.GetFPS());
        }
        i++;
    }

    //void OnGUI()
    //{
    //    int w = Screen.width, h = Screen.height;

    //    GUIStyle style = new GUIStyle();

    //    Rect rect = new Rect(0, 0, w, h * 2 / 100);
    //    style.alignment = TextAnchor.UpperRight;
    //    style.fontSize = h / 20;
    //    style.normal.textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);
    //    float time = t[k];
    //    string text = string.Format("{0.0} seconds", time);
    //    GUI.Label(rect, text, style);
    //}
}
