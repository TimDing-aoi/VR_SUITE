using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateCam : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Display.displays[1].Activate();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
