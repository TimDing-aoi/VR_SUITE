using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using static Reward2D;

public class Indicator : MonoBehaviour
{
    public GameObject sprite;
    public GameObject arrow;
    public GameObject mesh;
    private MeshRenderer rend;
    public GameObject firefly;
    public TMP_Text text;
    private float scale;
    private float height;
    private float distance = 0.0f;
    private float threshold;
    private bool off;
    

    // Start is called before the first frame update
    void Start()
    {
        rend = mesh.GetComponent<MeshRenderer>();
        threshold = PlayerPrefs.GetFloat("Reward Zone Radius");
        height = PlayerPrefs.GetFloat("Player Height");
        scale = PlayerPrefs.GetFloat("Triangle Height");
        off = PlayerPrefs.GetInt("Feedback ON") == 0;
        arrow.transform.localScale *= scale;
        text.transform.localScale *= scale;
        if (off)
        {
            arrow.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!off)
        {
            distance = Vector3.Distance(sprite.transform.position, firefly.transform.position);
            arrow.transform.position = sprite.transform.position - Vector3.up * height + sprite.transform.forward * 0.5f * scale;
            arrow.transform.rotation = Quaternion.LookRotation(-((sprite.transform.position - Vector3.up * height) - firefly.transform.position).normalized, Vector3.up);
            if (distance > threshold * 2.0f)
            {
                rend.material.SetColor("_Color", Color.red);
            }
            else
            {
                // rend.material.SetColor("_Color", Color.Lerp(Color.red, Color.green, 5.0f * Mathf.Exp((distance / threshold) - 1.0f)));

                rend.material.SetColor("_Color", Color.Lerp(Color.green, Color.red, distance / (threshold * 2.0f)));
            }
            text.text = distance.ToString() + "m";
            text.transform.rotation = new Quaternion(0.0f, Camera.main.transform.rotation.y, 0.0f, Camera.main.transform.rotation.w);
            text.transform.position = sprite.transform.position + sprite.transform.forward * 0.5f * scale;
        }
    }
}
