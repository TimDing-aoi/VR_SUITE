using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tracker : MonoBehaviour
{
    public static Tracker tracker;

    public GameObject cam;
    public GameObject player;
    public UnityEngine.UI.Scrollbar scrollx;
    public UnityEngine.UI.Scrollbar scrollz;
    private float x = 0.0f;
    private float z = 0.0f;
    private float phi = 0.0f;

    // Start is called before the first frame update
    public void Start()
    {
        Display.displays[1].Activate();
        tracker = this;
    }

    // Update is called once per frame
    void FixedUpdate()
    {

    }

    void Update()
    {
        scrollx.value = ((cam.transform.InverseTransformPoint(player.transform.position).x - x) + 4.0f) / 8.0f;
        scrollz.value = (cam.transform.InverseTransformPoint(player.transform.position).z - z) / 8.0f;
    }

    public void UpdateView()
    {
        phi = (phi + player.transform.rotation.y) % (Mathf.PI * 2.0f);
        cam.transform.rotation = player.transform.rotation;
        cam.transform.position = player.transform.position;
        x = cam.transform.InverseTransformPoint(player.transform.position).x;
        z = cam.transform.InverseTransformPoint(player.transform.position).z;
    }
}
