using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;

public class TestReceiveInformation : MonoBehaviour
{

    private List<Vector3> swc_list;
    private Vector3 swc_average;
    private Dictionary<int, int> swc_map;
    float scale = 1f;
    // Start is called before the first frame update
    void Start()
    {
        string information=GetComponent<hololens_client>().information;
        swc_average = new Vector3(0, 0, 0);
        swc_list = new List<Vector3>();
        swc_map = new Dictionary<int, int>();
        List<string> information_list = GetComponent<hololens_client>().information_list;
        
        GameObject.Find("ToolTip2").GetComponent<ToolTip>().ToolTipText = information;
        for (int j = 0; j < information_list.Count; ++j)
        {
            string infor = information_list[j];
            string[] words = infor.Split();
            if (words.Length % 7 != 1)
            {
                GameObject.Find("ToolTip1").GetComponent<ToolTip>().ToolTipText = "ErrorMod";
            }
            //GameObject.Find("ToolTip1").GetComponent<ToolTip>().ToolTipText = infor;
            for (int i = 0; i < words.Length - 1; i += 7)
            {
                Vector3 swc = new Vector3(float.Parse(words[i + 2]), float.Parse(words[i + 3]), float.Parse(words[i + 4]));
                swc_list.Add(swc);
                swc_average += swc;
                swc_map.Add(int.Parse(words[i]), int.Parse(words[i + 6]));

            }
        }
        swc_average /= swc_list.Count;
        GameObject.Find("ToolTip1").GetComponent<ToolTip>().ToolTipText =
            string.Format("total {0} pieces of information! total {1} nodes!",information_list.Count,swc_list.Count);
        CreateNeuron();
    }

    private void CreateCylinder(Vector3 a,Vector3 b,float radius,GameObject parentObject)
    {
        GameObject newObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        float length = Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) + (a.z - b.z) * (a.z - b.z));
        Vector3 ab = (a - b).normalized;
        Vector3 y_axis = new Vector3(0, 1, 0);
        newObj.transform.localScale = new Vector3(radius, length / 2, radius)*scale;
        newObj.transform.Rotate(Vector3.Cross(ab, y_axis), -Mathf.Acos(Vector3.Dot(ab, y_axis)) * 180/ Mathf.PI);
        newObj.transform.position = (a + b) / 2;

        newObj.GetComponent<MeshRenderer>().material.color = Color.red;
        newObj.transform.parent = parentObject.transform;
        GameObject.Find("ToolTip3").GetComponent<ToolTip>().ToolTipText = "cylinder!";
    }
    private void CreateSphere(Vector3 a,float radius,GameObject parentObject)
    {
        GameObject newObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        newObj.transform.position = a;
        newObj.GetComponent<MeshRenderer>().material.color = Color.green;
        newObj.transform.parent = parentObject.transform;
        newObj.transform.localScale = new Vector3(1, 1, 1) * radius*scale;
        GameObject.Find("ToolTip3").GetComponent<ToolTip>().ToolTipText = "sphere!";
    }

    private void CreateNeuron()
    {
        GameObject parentObject = GameObject.Find("SWCParent");
        for(int i = 0; i < swc_list.Count; ++i)
        {
            CreateSphere((swc_list[i] - swc_average), 1, parentObject);
            int pid = swc_map[i + 1];
            if (pid == -1) continue;
            CreateCylinder((swc_list[i] - swc_average), (swc_list[pid - 1] - swc_average), 1, parentObject);
        }
        GameObject.Find("ToolTip3").GetComponent<ToolTip>().ToolTipText = "neuron!";
    }
}
