using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.UI.BoundsControlTypes;

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TestReceiveInformation : MonoBehaviour
{

    private List<Vector3> swcList;
    private List<float> radiusList;
    private List<int> parentList;
    private Vector3 swc_average;
    //private Dictionary<int, int> swc_map;
    float scale = 1 / 2048.0f;
    private BoundsControl boundsControl;

    //Start is called before the first frame update
    void Start()
    {
        swc_average = new Vector3(0, 0, 0);

        //F:\gold166\p_checked6_mouse_RGC_uw\ho_091201c1
        string swc_path = "F:/gold166/p_checked6_mouse_RGC_uw/sv_080926a/080926a.tif.v3dpbd.swc";
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
        int length = strs.Length;
        swcList = new List<Vector3>();
        radiusList = new List<float>();
        parentList = new List<int>();
        for (int i = 0; i < strs.Length; ++i)
        {
            if (strs[i].StartsWith("#")) continue;
            string[] words = strs[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Vector3 swc = new Vector3(float.Parse(words[2]), float.Parse(words[3]), float.Parse(words[4]));
            swcList.Add(swc);
            radiusList.Add(float.Parse(words[5]));
            parentList.Add(int.Parse(words[6]) - 1);
            swc_average += swc;

        }
        swc_average /= swcList.Count;
        print("success");


        CreateNeuron();
        //CreateBoundingBox();
    }

    private void CreateCylinder(Vector3 a, Vector3 b, float radiusA,float radiusB, GameObject parentObject)
    {
        float length = Vector3.Distance(a, b);
        GameObject newObj = Primitive.MyCylinder(radiusA, radiusB, length);

        Vector3 ab = (a - b).normalized;
        newObj.transform.up = ab;
        newObj.transform.position = (a + b) / 2;

        newObj.GetComponent<MeshRenderer>().material.color = Color.red;
        newObj.transform.parent = parentObject.transform;
        //GameObject.Find("ToolTip3").GetComponent<ToolTip>().ToolTipText = "cylinder!";
    }
    private void CreateSphere(Vector3 a, float radius, GameObject parentObject)
    {
        GameObject newObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        newObj.transform.position = a;
        newObj.GetComponent<MeshRenderer>().material.color = Color.red;
        newObj.transform.parent = parentObject.transform;
        newObj.transform.localScale = new Vector3(1, 1, 1) * radius;
        //GameObject.Find("ToolTip3").GetComponent<ToolTip>().ToolTipText = "sphere!";
    }

    private void CreateNeuron()
    {
        GameObject parentObject = this.gameObject;
        for (int i = 0; i < swcList.Count; ++i)
        {
            Vector3 position = (swcList[i] - swc_average) * scale;
            CreateSphere(position, radiusList[i]*scale, parentObject);
            int pid = parentList[i];
            if (pid == -2) continue;
            Vector3 parentPostion = (swcList[pid] - swc_average) * scale;
            CreateCylinder(position, parentPostion, radiusList[i]*scale, radiusList[pid]*scale, parentObject);
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
