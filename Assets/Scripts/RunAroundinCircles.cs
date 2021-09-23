using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RunAroundinCircles : MonoBehaviour
{
    public GameObject firefly;
    private float phi = 0.0f;
    public float r = 1f;
    public float increment = 0.03f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        firefly.transform.position = new Vector3(0.0f, firefly.transform.position.y, 5f) + (new Vector3(Mathf.Sin(phi), 0.0f, Mathf.Cos(phi))) * r;
        phi += increment;
    }
}
