using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using static ObjectPooler;

public class WaitUntilFilled : MonoBehaviour
{
    //public GameObject player;
    public RigidbodyFirstPersonControllerv2 rigidbodyFirstPersonController;

    // Start is called before the first frame update
    void Start()
    {
        //rigidbodyFirstPersonController = player.GetComponent<RigidbodyFirstPersonController>();
        rigidbodyFirstPersonController.movementSettings.ForwardSpeed = 0.0f;
        rigidbodyFirstPersonController.mouseLook.XSensitivity = 0.0f;
        rigidbodyFirstPersonController.mouseLook.YSensitivity = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (ObjectPooler.SharedInstance.fill >= 1)
        {
            rigidbodyFirstPersonController.movementSettings.ForwardSpeed = 2.0f;
            rigidbodyFirstPersonController.mouseLook.XSensitivity = 0.5f;
            rigidbodyFirstPersonController.mouseLook.YSensitivity = 0.5f;
        }
    }
}
