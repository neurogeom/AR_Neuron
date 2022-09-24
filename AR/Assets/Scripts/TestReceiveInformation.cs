using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.UI.BoundsControlTypes;

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TestReceiveInformation : MonoBehaviour
{

    private List<Vector3> swc_list;
    private Vector3 swc_average;
    private Dictionary<int, int> swc_map;
    float scale = 0.01f;
    private BoundsControl boundsControl;

    //Start is called before the first frame update
    void Start()
    {
        swc_average = new Vector3(0, 0, 0);
        swc_list = new List<Vector3>();
        swc_map = new Dictionary<int, int>();
        string swc_path = "C:/Users/x/Desktop/v3d-testdata/neuron01.tif.swc";
        if (!File.Exists(swc_path))
        {
            GameObject debugTip = GameObject.Find("DebugTip");
            if (debugTip != null)
            {
                debugTip.GetComponent<ToolTip>().ToolTipText = "Error on reading swc file!";
            }
            return;
        }
        string[] strs = File.ReadAllLines(swc_path);

        for (int i = 0; i < strs.Length; ++i)
        {
            if (strs[i].StartsWith("#")) continue;
            string[] words = strs[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Vector3 swc = new Vector3(float.Parse(words[2]), float.Parse(words[3]), float.Parse(words[4]));
            swc_list.Add(swc);

            swc_map.Add(int.Parse(words[0]), int.Parse(words[6]));
            swc_average += swc;

        }
        swc_average /= swc_list.Count;
        print("success");


        CreateNeuron();
        CreateBoundingBox();
    }

    private void CreateCylinder(Vector3 a, Vector3 b, float radius, GameObject parentObject)
    {
        GameObject newObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        float length = Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) + (a.z - b.z) * (a.z - b.z));
        Vector3 ab = (a - b).normalized;
        Vector3 y_axis = new Vector3(0, 1, 0);
        newObj.transform.localScale = new Vector3(radius, length / 2, radius);
        newObj.transform.Rotate(Vector3.Cross(ab, y_axis), -Mathf.Acos(Vector3.Dot(ab, y_axis)) * 180 / Mathf.PI);
        newObj.transform.position = (a + b) / 2;

        newObj.GetComponent<MeshRenderer>().material.color = Color.red;
        newObj.transform.parent = parentObject.transform;
        //GameObject.Find("ToolTip3").GetComponent<ToolTip>().ToolTipText = "cylinder!";
    }
    private void CreateSphere(Vector3 a, float radius, GameObject parentObject)
    {
        GameObject newObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        newObj.transform.position = a;
        newObj.GetComponent<MeshRenderer>().material.color = Color.green;
        newObj.transform.parent = parentObject.transform;
        newObj.transform.localScale = new Vector3(1, 1, 1) * radius;
        //GameObject.Find("ToolTip3").GetComponent<ToolTip>().ToolTipText = "sphere!";
    }

    private void CreateNeuron()
    {
        GameObject parentObject = this.gameObject;
        for (int i = 0; i < swc_list.Count; ++i)
        {
            CreateSphere((swc_list[i] - swc_average) * scale, scale, parentObject);
            int pid = swc_map[i + 1];
            if (pid == -1) continue;
            CreateCylinder((swc_list[i] - swc_average) * scale, (swc_list[pid - 1] - swc_average) * scale, scale, parentObject);
        }
        //GameObject.Find("ToolTip3").GetComponent<ToolTip>().ToolTipText = "neuron!";
    }

    private void CreateBoundingBox()
    {
        //boundsControl = this.gameObject.GetComponent<BoundsControl>();
        //boundsControl.BoundsControlActivation = BoundsControlActivationType.ActivateOnStart;
        //boundsControl.ScaleHandlesConfig.HandleSize = 0.1f;
        //boundsControl.RotationHandlesConfig.HandleSize = 0.1f;
        //boundsControl.TranslationHandlesConfig.HandleSize = 0.1f;

        //boundsControl.TranslateStarted.AddListener(TransferOwnership);
        //boundsControl.RotateStarted.AddListener(TransferOwnership);
        //boundsControl.ScaleStarted.AddListener(TransferOwnership);

        ObjectManipulator objectManipulator = this.gameObject.GetComponent<ObjectManipulator>();
        //objectManipulator.OnManipulationStarted.AddListener((ManipulationEventData data) => { TransferOwnership(); });
    }

    //public void OnPhotonInstantiate(PhotonMessageInfo info)
    //{
    //    //GameObject.Find("ToolTip2").GetComponent<ToolTip>().ToolTipText = "No information!";
    //    //GameObject connection = GameObject.Find("Connection");
    //    //string information = connection.GetComponent<hololens_client>().information;
    //    //swc_average = new Vector3(0, 0, 0);
    //    //swc_list = new List<Vector3>();
    //    //swc_map = new Dictionary<int, int>();
    //    //List<string> information_list = connection.GetComponent<hololens_client>().information_list;

    //    //GameObject.Find("ToolTip2").GetComponent<ToolTip>().ToolTipText = information;
    //    //for (int j = 0; j < information_list.Count; ++j)
    //    //{
    //    //    string infor = information_list[j];
    //    //    string[] words = infor.Split();
    //    //    if (words.Length % 7 != 1)
    //    //    {
    //    //        GameObject.Find("ToolTip1").GetComponent<ToolTip>().ToolTipText = "ErrorMod";
    //    //    }
    //    //    GameObject.Find("ToolTip1").GetComponent<ToolTip>().ToolTipText = infor;
    //    //    for (int i = 0; i < words.Length - 1; i += 7)
    //    //    {
    //    //        Vector3 swc = new Vector3(float.Parse(words[i + 2]), float.Parse(words[i + 3]), float.Parse(words[i + 4]));
    //    //        swc_list.Add(swc);
    //    //        swc_average += swc;
    //    //        swc_map.Add(int.Parse(words[i]), int.Parse(words[i + 6]));
    //    //    }
    //    //}
    //    //swc_average /= swc_list.Count;
    //    //GameObject.Find("ToolTip1").GetComponent<ToolTip>().ToolTipText =
    //    //    string.Format("total {0} pieces of information! total {1} nodes!", information_list.Count, swc_list.Count);


    //}

    //public void TransferOwnership()
    //{
    //    this.gameObject.GetPhotonView().TransferOwnership(PhotonNetwork.LocalPlayer);
    //}
}
