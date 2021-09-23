using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

public class LoadCTIJoystick : MonoBehaviour
{
    float prevX;
    float prevY;
    float x;
    float y;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        var joystick = CTIJoystick.current;

        x = joystick.x.ReadValue();
        y = joystick.y.ReadValue();

        if (x < 0.0f)
        {
            x += 1.0f;
        }
        else if (x > 0.0f)
        {
            x -= 1.0f;
        }
        else if (x == 0)
        {
            if (prevX < 0.0f)
            {
                x -= 1.0f;
            }
            else if (prevX > 0.0f)
            {
                x += 1.0f;
            }
        }

        prevX = x;

        if (y < 0.0f)
        {
            y += 1.0f;
        }
        else if (y > 0.0f)
        {
            y -= 1.0f;
        }
        else if (y == 0)
        {
            if (prevY < 0.0f)
            {
                y -= 1.0f;
            }
            else if (prevY > 0.0f)
            {
                y += 1.0f;
            }
        }

        prevY = y;
    }

    void OnGUI()
    {
        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(0, 0, w, h);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 72;
        style.normal.textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);
        string text = string.Format("X: {0:F7}, Y: {1:F7})", x, y);
        GUI.Label(rect, text, style);
    }
}
