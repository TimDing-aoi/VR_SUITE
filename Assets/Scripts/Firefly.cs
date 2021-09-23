using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Firefly : MonoBehaviour
{
    private static bool isTouched = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCollisionEnter(Collision collision)
    {
        print("Trigger On.");
        if (collision.gameObject.tag == "Character")
        {
            isTouched = true;
            print("Touching firefly.");
        }
    }

    void OnEnable()
    {
        print("Firefly active.");
    }

    void OnDisable()
    {
        print("Trigger off.");
        isTouched = false;
    }

    public static bool getStatus()
    {
        return isTouched;
    }
}
