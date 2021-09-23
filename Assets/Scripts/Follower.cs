using UnityEngine;
using System.Collections;
using static Reward2D;

public class Follower : MonoBehaviour
{
	public static FPSDisplay FPScounter;

	float deltaTime = 0.0f;
	public Transform target;
	public Camera cam;
	public float offset = 0.15f;

	void Update()
	{
		deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
		target.position = cam.transform.position + cam.transform.forward * offset;
		target.rotation = cam.transform.rotation;
		//print(Vector3.forward * offset);
	}
}