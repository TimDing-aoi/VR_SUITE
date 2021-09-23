using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridLines : MonoBehaviour
{
    public GameObject obj;
    public GameObject player;
    public float scale = 0.5f;
    
    private List<GameObject> gameObjects = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        for (int i = -5; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                for (int k = 0; k < 20; k++)
                {
                    GameObject newObj = GameObject.Instantiate(obj);
                    newObj.transform.position = new Vector3(i * scale, j * scale, k * scale);
                    gameObjects.Add(newObj);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        foreach (GameObject listObj in gameObjects)
        {
            if (Vector3.Distance(listObj.transform.position, player.transform.position) < 10f)
            {
                listObj.transform.position = new Vector3(listObj.transform.position.x + 5f, listObj.transform.position.y, listObj.transform.position.z + 5f);
            }
        }
    }
}
