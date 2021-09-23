using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateTexture : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // Create a new 2x2 texture ARGB32 (32 bit with alpha) and no mipmaps
        var texture = new Texture2D(2048, 2048, TextureFormat.ARGB32, false);

        // set the pixel values

        for (int i = 0; i < 2048; i++)
        {
            for (int k = 0; k < 4; k++)
            {
                if (k == 0 || k == 2)
                {
                    for (int j = k * 512; j < (k + 1) * 512; j++)
                    {
                        texture.SetPixel(i, j, Color.black);
                    }
                }
                else
                {
                    for (int j = k * 512; j < (k + 1) * 512; j++)
                    {
                        texture.SetPixel(i, j, Color.white);
                    }
                }
            }
        }

        // Apply all SetPixel calls
        texture.Apply();

        // connect texture to material of GameObject this script is attached to
        GetComponent<Renderer>().material.mainTexture = texture;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
