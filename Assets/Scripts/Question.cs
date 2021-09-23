using UnityEngine;
using System.Collections;
using static Reward2D;

public class Question : MonoBehaviour
{
	public static FPSDisplay FPScounter;

	public Transform target;
	public Camera cam;
	public float offset = 0.02f;

	void Update()
	{
		target.position = cam.transform.position + cam.transform.forward * offset;
		target.rotation = new Quaternion(0.0f, cam.transform.rotation.y, 0.0f, cam.transform.rotation.w);
		//print(Vector3.forward * offset);
	}
}
