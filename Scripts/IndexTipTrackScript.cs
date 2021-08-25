using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using System.IO;
/*
Created by BlackFJ
*/
///<summary>
///
///</summary>
public class IndexTipTrackScript: MonoBehaviour
{
    private MixedRealityPose RightIndexTipPose;
    private MixedRealityPose RightThumbTipPose;
    private Vector3 RightThumbTipPosition, RightIndexTipPosition;//the position of right-handed indexTip and thumbTip 

    private List<Vector3> RightIndexTipPositionList;//save all the simpling point with indexTip position
    private Dictionary<int, int> RelationshipDict;//key: idx of rightIndexTipPositionList,value:  parent of idx  
    private Vector3 SomaPosition;//the world position of soma given

    [SerializeField]
    public float stepTime;//step time to control the simpling frequence
    public float distance2Limited;//the square of distance as threshold to limit the distance between indexTip and thumbTip 

    private float preTime;
    private float curTime;
    private Transform PaintingBoardTransform;//painting board
    private bool IsContinuous;//show whether indexTip fiting thumbtip 

    public int NumChosen;//the number of cylinder chosen by player
    public Vector3 PosChosen;//the 'a' position of chosen cylinder
    public int IdxChosen;//the idx of 'a' position
    public Material CreatedCylinderMaterial;//the material of cylinder created
    public Material ChosenMaterial;//the material of cylinder chosen

    private float Distance2(Vector3 a,Vector3 b)
    {
        return (a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) + (a.z - b.z) * (a.z - b.z);
    }

    private void CreateCylinder(Vector3 a,Vector3 b,Transform parentTransform,float radius,int idx)
    {
        GameObject newObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        float length = Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) + (a.z - b.z) * (a.z - b.z));
        Vector3 ab = (a - b).normalized;
        Vector3 y_axis = new Vector3(0, 1, 0);
        newObj.transform.localScale = new Vector3(radius, length / 2, radius);
        newObj.transform.Rotate(Vector3.Cross(ab, y_axis), -Mathf.Acos(Vector3.Dot(ab, y_axis)) * 180 / Mathf.PI);
        newObj.transform.position = (a + b) / 2;

        newObj.GetComponent<MeshRenderer>().material=CreatedCylinderMaterial;
        newObj.transform.parent = parentTransform;
        var chosenScript=newObj.AddComponent<ChosenScript>();
        chosenScript.ChosenMaterial = ChosenMaterial;
        chosenScript.Pos = a;
        chosenScript.Idx = idx;
    }

    private bool IsPainting()
    {
        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, Handedness.Right, out RightThumbTipPose)&&
            HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip,Handedness.Right,out RightIndexTipPose))
        {
            RightThumbTipPosition = RightThumbTipPose.Position;
            RightIndexTipPosition = RightIndexTipPose.Position;
            float tmpDistance2 = Distance2(RightIndexTipPosition, RightThumbTipPosition);
            if (tmpDistance2< distance2Limited)
            {
                return true;
            }
        }
        return false;

    }

    private string SomaCenteredString(Vector3 p)
    {
        var pc=p - SomaPosition;
        return pc.x.ToString() + " " + pc.y.ToString() + " " + pc.z.ToString() + " ";
    }

    private string Write2SWC(string radius = "0.5",string type="3")
    {
        string swc = "1 1 0 0 0 " + radius + " -1\n";
        for(int i = 1; i < RightIndexTipPositionList.Count; ++i)
        {
            string line = (i + 1).ToString() + " " + type + " " + SomaCenteredString(RightIndexTipPositionList[i]) 
                + radius + " " + (RelationshipDict[i] + 1) + "\n";
            swc += line;
        }
        return swc;
    }

    private void Start()
    {
        preTime = Time.time;
        PaintingBoardTransform = GameObject.Find("PaintingBoard").GetComponent<Transform>();
        RightIndexTipPositionList = new List<Vector3>();
        RelationshipDict = new Dictionary<int, int>();
        RelationshipDict[0] = -1;
        IsContinuous = false;
        NumChosen = 0;
        SomaPosition = GameObject.Find("Soma").transform.position;
        RightIndexTipPositionList.Add(SomaPosition);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            string swc = Write2SWC();
            File.WriteAllText("Assets/test.swc", swc);
            print("writing succeed!");
        }
        curTime = Time.time;
        if (curTime - preTime < stepTime) return;

        if (!IsContinuous)
        {
            if (IsPainting()&&NumChosen>0)
            {
                print("IsPainting!");
                    Debug.Log((RightIndexTipPosition - PosChosen).magnitude);
                    int curIdx = RightIndexTipPositionList.Count;
                if ((RightIndexTipPosition - PosChosen).magnitude > 0.0001)
                {
                    CreateCylinder(RightIndexTipPosition, PosChosen, PaintingBoardTransform, 0.01f, curIdx);
                    RelationshipDict[curIdx] = IdxChosen;
                    //Debug.Log(curIdx.ToString() + ":" + IdxChosen.ToString());
                    RightIndexTipPositionList.Add(RightIndexTipPosition);
                    IsContinuous = true;
                }
            }
        }
        else
        {
            if (!IsPainting())
            {
                IsContinuous = false;
            }
            else
            {
                int curIdx = RightIndexTipPositionList.Count;
                if ((RightIndexTipPosition - RightIndexTipPositionList[curIdx - 1]).magnitude > 0.0001)
                {
                    CreateCylinder(RightIndexTipPosition,
                        RightIndexTipPositionList[curIdx - 1], PaintingBoardTransform, 0.01f, curIdx);
                    RelationshipDict[curIdx] = curIdx - 1;
                    RightIndexTipPositionList.Add(RightIndexTipPosition);
                }

            }
        }
        preTime = curTime;

    }


}
