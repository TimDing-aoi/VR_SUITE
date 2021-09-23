using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    [Tooltip("SerialPort of your device.")]
    public string portName = "COM3";

    [Tooltip("Baudrate")]
    public int baudRate = 1000000;

    [Tooltip("Timeout")]
    public int ReadTimeout = 1;

    [Tooltip("QueueLength")]
    public int QueueLength = 1;

    wrmhl controller = new wrmhl();

    // Start is called before the first frame update
    void Start()
    {
        portName = PlayerPrefs.GetString("Port");
        controller.set(portName, baudRate, ReadTimeout, QueueLength);
        controller.connect();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
