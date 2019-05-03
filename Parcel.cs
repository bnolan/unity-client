using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.IO;
using System.IO.Compression;
using Zlib;
using System.Web;
using TMPro;

public class Parcel : MonoBehaviour
{
    private UInt16[,,] field;
    private GameObject player;
    public ParcelDescription description;
    private bool loaded;

    public void SetDescription(ParcelDescription d)
    {
        description = d;
        loaded = false;
        field = new UInt16[Width() * 2, Height() * 2, Depth() * 2];
        player = GameObject.Find("main-player");
    }

    void Update()
    {
        if (loaded || !player)
        {
            return;
        }

        float InRangeDistance = 64.0f;

        if (Vector3.Distance(gameObject.transform.position, player.transform.position) < InRangeDistance)
        {
            loaded = true;
            Load();
        }
    }

    int Width()
    {
        return description.x2 - description.x1;
    }

    int Height()
    {
        return description.y2 - description.y1;
    }

    int Depth()
    {
        return description.z2 - description.z1;
    }

    public Mesh CreatePlane ()
    {
        Mesh mesh = new Mesh();

        float s = 0.5f;

        mesh.vertices = new Vector3[] {
            new Vector3(-s, -s, 0),
            new Vector3( s, -s, 0),
            new Vector3( s,  s, 0),
            new Vector3(-s,  s, 0),
        };

        mesh.uv = new Vector2[] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };

        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };

        mesh.RecalculateNormals();

        return mesh;

    }

    public void SetCommon (GameObject g, FeatureDescription d)
    {
        Vector3 offset = new Vector3(0.25f, 0.25f, -0.125f);

        g.name = "feature-" + d.uuid;
        g.transform.SetParent(transform, false);
        g.transform.localScale = new Vector3(d.scale[0], d.scale[1], d.scale[2]);
        g.transform.localPosition = new Vector3(d.position[0], d.position[1], d.position[2]) + offset;
        g.transform.localEulerAngles = new Vector3(d.rotation[0], d.rotation[1], d.rotation[2]) * Mathf.Rad2Deg;
        g.transform.localPosition -= g.transform.forward * 0.01f;
    }

    public IEnumerator LoadImage(FeatureDescription d)
    {
        Material imageMat = GameObject.Find("ImageCube").GetComponent<Renderer>().material as Material;
        Material mat = new Material(imageMat.shader);

        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.GetComponent<MeshFilter>().mesh = CreatePlane();
        plane.GetComponent<Renderer>().material = mat;
        plane.GetComponent<MeshCollider>().enabled = false;

        SetCommon(plane, d);

        string url = "https://img.cryptovoxels.com/img.php?url=" + HttpUtility.UrlEncode(d.url) + "&mode=color";
        using (WWW www = new WWW(url))
        {
            // Wait for download to complete
            yield return www;
            mat.mainTexture = www.texture;
        }
    }


    public void CreateSign(FeatureDescription d)
    {
        Material imageMat = GameObject.Find("ImageCube").GetComponent<Renderer>().material as Material;
        Material mat = new Material(imageMat.shader);

        GameObject go = Instantiate(GameObject.Find("SignText"));

        // plane.AddComponent<TextMeshPro>();
        // plane.GetComponent<MeshFilter>().mesh = CreatePlane();
        // plane.GetComponent<Renderer>().material = mat;
        // plane.GetComponent<MeshCollider>().enabled = false;

        TextMeshPro text = go.GetComponent<TextMeshPro>();
        text.text = d.text;
        text.fontSize = 1;

        SetCommon(go, d);

        float s = 0.05f;
        go.transform.localScale = new Vector3(s, s, s);

        // fixme remove nudge
        go.transform.localPosition += new Vector3(0, 0, -0.125f);
    }

    public void LoadFeatures()
    {
        foreach (FeatureDescription d in description.features)
        {
            if (d.type == "image")
            {
                IEnumerator coroutine = LoadImage(d);
                StartCoroutine(coroutine);
            } else if (d.type == "sign")
            {
                CreateSign(d);
            }
        }
    }

    public void Load()
    {
        Material atlasMaterial = GameObject.Find("ProtoCube").GetComponent<Renderer>().material as Material;
        Material glassMaterial = GameObject.Find("GlassCube").GetComponent<Renderer>().material as Material;

        Vector3 offset = new Vector3(0.5f, 0, 0.125f);

        GameObject solid = GameObject.CreatePrimitive(PrimitiveType.Cube);
        solid.name = "solid-voxels";
        solid.transform.SetParent(transform, false);
        solid.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        solid.transform.localPosition = new Vector3(-Width() / 2f, 0, -Depth() / 2f) + offset;
        solid.GetComponent<Renderer>().material = atlasMaterial;

        Mesh mesh = GenerateField(false);
        solid.GetComponent<MeshFilter>().mesh = mesh;

        MeshCollider collider = solid.AddComponent(typeof(MeshCollider)) as MeshCollider;
        collider.sharedMesh = mesh;

        GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
        glass.name = "glass-voxels";
        glass.transform.SetParent(transform, false);
        glass.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        glass.transform.localPosition = new Vector3(-Width() / 2f + 0, 0, -Depth() / 2f) + offset;
        glass.GetComponent<Renderer>().material = glassMaterial;

        mesh = GenerateField(true);
        glass.GetComponent<MeshFilter>().mesh = mesh;

        collider = solid.AddComponent(typeof(MeshCollider)) as MeshCollider;
        collider.sharedMesh = mesh;

        LoadFeatures();
    }

    long GetVoxel(int x, int y, int z)
    {
        if (x < 0 || y < 0 || z < 0 || x >= Width() * 2 || y >= Height() * 2 || z >= Depth() * 2)
        {
            return 0;
        }
        else
        {
            return (long)field[x, y, z];
        }
    }

    public void AddUvs(long index, bool transparent, List<Vector2> uvs)
    {
        if (transparent)
        {
            uvs.Add(new Vector3(0, 0));
            uvs.Add(new Vector3(1, 0));
            uvs.Add(new Vector3(0, 1));
            uvs.Add(new Vector3(1, 1));

            return;
        }

        float s = 1.0f / 16f / 128f * 128f;

        // long textureIndex = index - (1 << 15);
        long textureIndex = index % 16;

        // if (textureIndex >= 32) {
        //   // inverted
        //   textureIndex = (textureIndex % 32) * 2 + 1
        // } else {
        //   textureIndex = (textureIndex % 32) * 2
        // }

        float x = (float)1.0f / 4f * ((float)(textureIndex % 4) + 0.5f);
        float y = (float)1.0f / 4f * ((float)Math.Floor(textureIndex / 4.0f) + 0.5f);

        y = 1.0f - y;

        uvs.Add(new Vector3(x - s, y - s));
        uvs.Add(new Vector3(x + s, y - s));
        uvs.Add(new Vector3(x - s, y + s));
        uvs.Add(new Vector3(x + s, y + s));
    }

    private Boolean IsTransparent(long index) {
        return index <= 2;
    }

    private Boolean IsGeometry(long index, Boolean transparent) {
        return transparent ? index == 2 : index > 2;
    }

    public Mesh GenerateField (Boolean transparent) { 
        byte[] input = Convert.FromBase64String(description.voxels);
        byte[] output = Inflate(input);

        Buffer.BlockCopy(output, 0, field, 0, output.Length);

        List<Vector3> newVertices = new List<Vector3>();
        List<Vector2> newUV = new List<Vector2>();
        List<int> newTriangles = new List<int>();

        for (int x = -1; x < Width() * 2; x++)
        {
            for (int y = -1; y < Height() * 2; y++)
            {
                for (int z = -1; z < Depth() * 2; z++)
                {

                    long i = GetVoxel(x, y, z);

                    long nX = GetVoxel(x + 1, y, z);
                    long nY = GetVoxel(x, y + 1, z);
                    long nZ = GetVoxel(x, y, z + 1);

                    if (IsGeometry(i, transparent) != IsGeometry(nX, transparent))
                    {
                        int v = newVertices.Count;

                        newVertices.Add(new Vector3(x + 1, y, z));
                        newVertices.Add(new Vector3(x + 1, y + 1, z));
                        newVertices.Add(new Vector3(x + 1, y, z + 1));
                        newVertices.Add(new Vector3(x + 1, y + 1, z + 1));

                        if (i > nX)
                        {
                            newTriangles.Add(v + 0);
                            newTriangles.Add(v + 1);
                            newTriangles.Add(v + 2);

                            newTriangles.Add(v + 1);
                            newTriangles.Add(v + 3);
                            newTriangles.Add(v + 2);

                            AddUvs(i, transparent, newUV);
                        }
                        else
                        {
                            newTriangles.Add(v + 2);
                            newTriangles.Add(v + 1);
                            newTriangles.Add(v + 0);

                            newTriangles.Add(v + 2);
                            newTriangles.Add(v + 3);
                            newTriangles.Add(v + 1);

                            AddUvs(nX, transparent, newUV);
                        }
                    }

                    if (IsGeometry(i, transparent) != IsGeometry(nY, transparent))
                    {
                        int v = newVertices.Count;

                        newVertices.Add(new Vector3(x, y + 1, z));
                        newVertices.Add(new Vector3(x + 1, y + 1, z));
                        newVertices.Add(new Vector3(x, y + 1, z + 1));
                        newVertices.Add(new Vector3(x + 1, y + 1, z + 1));

                        if (i < nY)
                        {
                            newTriangles.Add(v + 0);
                            newTriangles.Add(v + 1);
                            newTriangles.Add(v + 2);

                            newTriangles.Add(v + 1);
                            newTriangles.Add(v + 3);
                            newTriangles.Add(v + 2);

                            AddUvs(i, transparent, newUV);
                        }
                        else
                        {
                            newTriangles.Add(v + 2);
                            newTriangles.Add(v + 1);
                            newTriangles.Add(v + 0);

                            newTriangles.Add(v + 2);
                            newTriangles.Add(v + 3);
                            newTriangles.Add(v + 1);

                            AddUvs(nY, transparent, newUV);
                        }
                    }

                    if (IsGeometry(i, transparent) != IsGeometry(nZ, transparent))
                    {
                        int v = newVertices.Count;

                        newVertices.Add(new Vector3(x, y, z + 1));
                        newVertices.Add(new Vector3(x + 1, y, z + 1));
                        newVertices.Add(new Vector3(x, y + 1, z + 1));
                        newVertices.Add(new Vector3(x + 1, y + 1, z + 1));

                        if (i > nZ)
                        {
                            newTriangles.Add(v + 0);
                            newTriangles.Add(v + 1);
                            newTriangles.Add(v + 2);

                            newTriangles.Add(v + 1);
                            newTriangles.Add(v + 3);
                            newTriangles.Add(v + 2);

                            AddUvs(i, transparent, newUV);
                        }
                        else
                        {
                            newTriangles.Add(v + 2);
                            newTriangles.Add(v + 1);
                            newTriangles.Add(v + 0);

                            newTriangles.Add(v + 2);
                            newTriangles.Add(v + 3);
                            newTriangles.Add(v + 1);

                            AddUvs(nZ, transparent, newUV);
                        }
                    }
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = newVertices.ToArray();
        mesh.uv = newUV.ToArray();
        mesh.triangles = newTriangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }


    public static byte[] Inflate(byte[] data)
    {
        int outputSize = 1024;
        byte[] output = new Byte[outputSize];
        bool expectRfc1950Header = true;
        using (MemoryStream ms = new MemoryStream())
        {
            ZlibCodec compressor = new ZlibCodec();
            compressor.InitializeInflate(expectRfc1950Header);

            compressor.InputBuffer = data;
            compressor.AvailableBytesIn = data.Length;
            compressor.NextIn = 0;
            compressor.OutputBuffer = output;

            foreach (var f in new FlushType[] { FlushType.None, FlushType.Finish })
            {
                int bytesToWrite = 0;
                do
                {
                    compressor.AvailableBytesOut = outputSize;
                    compressor.NextOut = 0;
                    compressor.Inflate(f);

                    bytesToWrite = outputSize - compressor.AvailableBytesOut;
                    if (bytesToWrite > 0)
                        ms.Write(output, 0, bytesToWrite);
                }
                while ((f == FlushType.None && (compressor.AvailableBytesIn != 0 || compressor.AvailableBytesOut == 0)) ||
                    (f == FlushType.Finish && bytesToWrite != 0));
            }

            compressor.EndInflate();

            return ms.ToArray();
        }
    }
}
