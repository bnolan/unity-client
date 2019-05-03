using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.IO;
using System.IO.Compression;
using Zlib;

[System.Serializable]
public class Response
{
    public bool success;
    public ParcelDescription[] parcels;

    public static Response CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<Response>(jsonString);
    }
}

[System.Serializable]
public class ParcelDescription
{
    public int id;
    public int x1;
    public int x2;
    public int y1;
    public int y2;
    public int z1;
    public int z2;
    public string name;
    public string voxels;
    public FeatureDescription[] features;
}

[System.Serializable]
public class FeatureDescription
{
    //"type": "sign",
    //"scale": [0.61875, 0.5, 0.5],
    //"text": "COMING SOON",
    //"position": [-8.75, 2, 5.5],
    //"rotation": [0, 1.5707963267948966, 0],
    //"uuid": "8f230c58-519c-40a5-a330-f015703b52b8",
    //"fontSize": 24

    public string uuid;
    public float[] position;
    public float[] scale;
    public float[] rotation;
    public string text;
    public string url;
    public int fontSize;
    public string type;
}


public class Grid : MonoBehaviour
{
    public string url = "https://www.cryptovoxels.com/grid/parcels";
    public Camera camera;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        Debug.Log("Fetching URL");

        using (WWW www = new WWW(url))
        {
            yield return www;
            // Renderer renderer = GetComponent<Renderer>();
            // renderer.material.mainTexture = www.texture;

            //Debug.Log("Fetched " + url);
            //Debug.Log(www.text);

            Response r = Response.CreateFromJSON(www.text);

            Debug.Log("This many parcels:" + r.parcels.Length);

            foreach (ParcelDescription p in r.parcels) {
                if (p.id < 10)
                {
                    GameObject cube = new GameObject(); // .CreatePrimitive(PrimitiveType.Cube);
                                                        // cube.GetComponent<Renderer>().material = mat; // .color = new Color(0.9f, 0.9f, 0.9f, 1.0f);
                    cube.transform.position = new Vector3((p.x1 + p.x2) / 2f, -0.999f, (p.z1 + p.z2) / 2f);
                    cube.name = "parcel-" + p.id.ToString();

                    Parcel parcel = cube.AddComponent<Parcel>() as Parcel;
                    parcel.SetDescription(p);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
