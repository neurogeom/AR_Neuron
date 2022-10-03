using Microsoft.MixedReality.Toolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics;
using System.IO;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Double;

public class EyeCollisionDetect : MonoBehaviour
{
    Material mMaterial;

    GameObject gameObject;
    private float defaultDistanceInMeters = 2;
    public GameObject paintingBoard;
    int maxHitCount = 1;

    float[] mPoints = new float[1024 * 3];
    int mHitCount = 0;
    DateTime preTime;
    DateTime preComputeTime;
    DateTime preHitTime;

    int scanCount = 0;
    Vector3[] scanPoints = new Vector3[5000];
    int[] scanPathLengthCount = new int[100];
    Vector3 scanCenter = Vector3.zero;

    public int bkgThresh;
    public bool isRepairing = false;

    bool flag = true;

    Vector3 preLocalHitPos = Vector3.zero;

    public Texture3D volume;
    // Start is called before the first frame update
    List<int> targetIndexs = new List<int>();

    List<Vector3> test = new List<Vector3>();

    int fileLength = 0;
    
    int sz0,sz1, sz2;

    public App2 app2;
    void Start()
    {
       

        //mMaterial = GameObject.Find("Cube").GetComponent<MeshRenderer>().materials[1];
        preTime = CoreServices.InputSystem.EyeGazeProvider.Timestamp;
        preComputeTime = CoreServices.InputSystem.EyeGazeProvider.Timestamp;
        preHitTime = CoreServices.InputSystem.EyeGazeProvider.Timestamp;
        //gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //gameObject.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        //gameObject.GetComponent<SphereCollider>().enabled = false;

        sz0 = volume.width;
        sz1 = volume.height;
        sz2 = volume.depth;
    }

    // Update is called once per frame
    void Update()
    {
        var eyeGazeProvider = CoreServices.InputSystem.EyeGazeProvider;
        //gameObject.transform.position = CoreServices.InputSystem.EyeGazeProvider.GazeOrigin +
        //    CoreServices.InputSystem.EyeGazeProvider.GazeDirection.normalized * defaultDistanceInMeters;
        var gazeOrigin = CoreServices.InputSystem.EyeGazeProvider.GazeOrigin;
        var gazeDirection = CoreServices.InputSystem.EyeGazeProvider.GazeDirection.normalized;
        var localGazeOrigin = paintingBoard.transform.InverseTransformPoint(gazeOrigin);
        var localHitPos = paintingBoard.transform.InverseTransformPoint(eyeGazeProvider.HitPosition);
        if (preLocalHitPos == Vector3.zero) preLocalHitPos = localHitPos;

        if (eyeGazeProvider.HitInfo.raycastValid)
        {
            var timestamp = eyeGazeProvider.Timestamp;
            if ((timestamp - preTime).TotalMilliseconds > 1000 / 60)
            {
                if ((timestamp - preHitTime).TotalMilliseconds > 1000 / 20)
                {
                    preHitTime = timestamp;
                    //addHitPoint(localHitPos);
                }
                    
                addScanPoint(localHitPos);
                preTime = timestamp;
                preLocalHitPos = localHitPos;

                if ((timestamp - preComputeTime).TotalMilliseconds >= 667)
                {
                    double gamma = fittingDistribution();
                    if (gamma > 0.85)
                    {
                        scanCenter.x /= scanCount;
                        scanCenter.y /= scanCount;
                        scanCenter.z /= scanCount;
                        //addHitPoint(scanCenter);
                        getSeed(scanCenter, localGazeOrigin);
                        app2.targets = targetIndexs.ToArray();

                    }
                    //recordEyeData(gamma);
                    scanCenter = Vector3.zero;
                    Array.Clear(scanPoints, 0, scanPoints.Length);
                    Array.Clear(scanPathLengthCount, 0, scanPathLengthCount.Length);
                    scanCount = 0;
                    //Debug.Log(timestamp + " " + gamma.ToString("f6"));
                    preComputeTime = timestamp;
                }
            }
        }
        //else Debug.Log("raycast unValid");
    }

    private void recordEyeData(double gamma)
    {
        string path = Application.dataPath + "/eyeData.txt";
        FileStream fs = new FileStream(path, FileMode.Open);
        fs.Position = fs.Length;
        string s = "";
        for(int i = 0; i < scanPathLengthCount.Length; i++)
        {
            double frequcy = (double)scanPathLengthCount[i] / scanCount;
            s += i.ToString() + " " + frequcy.ToString("f6") + "\n";
        }
        s+= gamma + "\n";
        byte[] bytes = new UTF8Encoding().GetBytes(s.ToString());
        fs.Write(bytes, 0, bytes.Length);
        fs.Close();
    }

    private void getSeed(Vector3 scanCenter, Vector3 gazeOrigin)
    {
        Vector3 direction = (scanCenter - gazeOrigin).normalized;
        byte[] volumeData = volume.GetPixelData<byte>(0).ToArray();
        float min_x = -0.5f, min_y = -0.5f, min_z = -0.5f;
        float max_x = 0.5f, max_y = 0.5f, max_z = 0.5f;
        float tmin_x = (min_x - scanCenter.x) / direction.x;
        float tmin_y = (min_y - scanCenter.y) / direction.y;
        float tmin_z = (min_z - scanCenter.z) / direction.z;
        float tmax_x = (max_x - scanCenter.x) / direction.x;
        float tmax_y = (max_y - scanCenter.y) / direction.y;
        float tmax_z = (max_z - scanCenter.z) / direction.z;
        if (tmax_x < tmin_x) (tmax_x, tmin_x) = (tmin_x, tmax_x);
        if (tmax_y < tmin_y) (tmax_y, tmin_y) = (tmin_y, tmax_y);
        if (tmax_z < tmin_z) (tmax_z, tmin_z) = (tmin_z, tmax_z);
        float tmin = Mathf.Max(tmin_x, Mathf.Max(tmin_y, tmin_z));
        float tmax = Mathf.Min(tmax_x, Mathf.Min(tmax_y, tmax_z));
        float max_length = tmax - tmin;
        float dt = 0.001f;
        Vector3 p = scanCenter + new Vector3(.5f, .5f, .5f);
        float distance = 0;
        int max_index =0;
        byte max_intensity=0;
        for(int t = 0; t < 1000; t++)
        {
            int x = (int)(p.x * sz0);
            int y = (int)(p.y * sz1);
            int z = (int)(p.z * sz2);
            int offset = 1;
            for(int offsetX = -offset; offsetX <= offset; offsetX++)
            {
                for (int offsetY = -offset; offsetY <= offset; offsetY++)
                {
                    for (int offsetZ = -offset; offsetZ <= offset; offsetZ++)
                    {
                        int w = x + offsetX;
                        int h = y + offsetY;
                        int d = z + offsetZ;
                        if (w >= 0 && w < sz0 && h >= 0 && h < sz1 && d >= 0 && d < sz2)
                        {
                            int index = w + (h * sz0) + (d * sz0 * sz1);
                            byte intesity = volumeData[index];
                            if (intesity > max_intensity)
                            {
                                max_intensity = intesity;
                                max_index = index;
                            }
                        }
                    }
                }
            }

            p += direction * dt;
            distance += dt;
            if (distance > max_length) break;
        }
        //Debug.Log("max_intesity" + max_intensity);
        

        int i = (int)(max_index % sz0);
        int j = (int)((max_index / sz0) % sz1);
        int k = (int)((max_index / (sz0*sz1) % sz2));
        Vector3 pos = new Vector3();
        pos.x = i / (float)sz0;
        pos.y = j / (float)sz1;
        pos.z = k / (float)sz2;
        pos = pos - new Vector3(.5f, .5f, .5f);
        pos = paintingBoard.transform.TransformPoint(pos);

        if (max_intensity >= bkgThresh)
        {
            targetIndexs.Add(max_index);
            if (isRepairing == true) app2.TargetProcess(max_index);
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localScale = new Vector3(.06f, .06f, .06f);
            sphere.transform.position = pos;
            sphere.GetComponent<MeshRenderer>().material.color = Color.red;
            GameObject KeyPoints = GameObject.Find("KeyPoints");
            sphere.transform.parent = KeyPoints.transform;
        }
    }

    private double fittingDistribution()
    {
        double[] pathLength = new double[100];
        double[] frequency = new double[100];
        for (int i = 0; i < 100; i++)
        {
            frequency[i] = (double)scanPathLengthCount[i] / scanCount;
            pathLength[i] = 1 / ((double)(i + 1.0d) * (double)(i + 1.0d));
            //Debug.Log("x:" + pathLength[i] + " y:" + frequency[i]);
        }

        var s = Fit.LineThroughOrigin(pathLength, frequency);
        //Debug.Log("fitting A:" + s);
        return s;
    }

    private void addScanPoint(Vector3 localHitPosition)
    {
        //scanPoints[scanCount++] = localHitPosition;
        scanCount++;
        scanCenter += localHitPosition;
        int length = (int)(Vector3.Distance(localHitPosition, preLocalHitPos) * 100);
        length = Math.Min(99, length);
        scanPathLengthCount[length]++;
    }

    public void addHitPoint(Vector3 pos)
    {
        int index = maxHitCount / 1024;
        mPoints[mHitCount * 3] = pos.x;
        mPoints[mHitCount * 3 + 1] = pos.y;
        mPoints[mHitCount * 3 + 2] = pos.z;

        //Debug.Log("add hit:" + pos.ToString("f4"));
        mHitCount++;
        mHitCount %= 1024;

        mMaterial.SetFloatArray("_Hits", mPoints);
        mMaterial.SetInt("_HitCount", mHitCount);

    }
}