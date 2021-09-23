using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideWindow : MonoBehaviour
{
    public Canvas canvas;

    public void Hide()
    {
        canvas.enabled = false;
    }
}
