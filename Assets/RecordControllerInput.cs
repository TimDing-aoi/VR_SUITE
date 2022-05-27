using System;
using System.Collections.Generic;
using UnityEngine;

public class RecordControllerInput : MonoBehaviour
{

    private string[] joystickNames;
    private float xAxis, yAxis;
    private List<string> lastPressedKeys = new List<string>();

    void Update()
    {
        joystickNames = Input.GetJoystickNames();
        xAxis = Input.GetAxis("Horizontal");
        yAxis = Input.GetAxis("Vertical");

        foreach (KeyCode curKey in Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(curKey))
            {
                lastPressedKeys.Add(curKey.ToString());
                if (lastPressedKeys.Count > 10)
                    lastPressedKeys.RemoveAt(0);
            }
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Joysticks:");
        foreach (var curName in joystickNames)
            GUILayout.Label(string.Format("   {0}", curName));
        GUILayout.Label(string.Format("Axes: ({0}, {1})", xAxis, yAxis));

        GUILayout.Label("Last pressed keys:");
        foreach (var curKeyName in lastPressedKeys)
            GUILayout.Label(string.Format("   {0}", curKeyName));
    }
}