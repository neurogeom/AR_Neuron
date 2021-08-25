using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using System.IO;
using System;
/*
Created by BlackFJ
*/
///<summary>
///
///</summary>
public class IndexTipTrackScript2: MonoBehaviour
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

    private SwcSoma soma;
    public GameObject chosenObject;
    private SwcNode preNode;

    private float Distance2(Vector3 a,Vector3 b)
    {
        return (a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) + (a.z - b.z) * (a.z - b.z);
    }

    private GameObject CreateCylinder(Vector3 a,Vector3 b,Transform parentTransform,float radius)
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
        var chosenScript=newObj.AddComponent<Chosen>();
        chosenScript.ChosenMaterial = ChosenMaterial;
        return newObj;
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

        soma.Index = 1;
        int index = 2;
        foreach (SwcNode node in soma.children)
        {
            PreOrder(node, ref swc, ref index);
        }
        
        return swc;
    }

    private void PreOrder(SwcNode node,ref string swc,ref int index)
    {
        string type = "2";
        node.Index = index;
        if(node.Parent.Index == 0)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            temp.transform.position = node.Position;
            temp.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        }
        string line = index.ToString() + " " + type + " " + SomaCenteredString(node.Position)
                + " " + node.radius + " " + node.Parent.Index + "\n";
        swc += line;
        index++;
        if (node.left != null) PreOrder(node.left, ref swc, ref index);
        if (node.right != null) PreOrder(node.right, ref swc, ref index);

    }

    private void Start()
    {
        preTime = Time.time;
        PaintingBoardTransform = GameObject.Find("PaintingBoard").GetComponent<Transform>();
        //RightIndexTipPositionList = new List<Vector3>();
        //RelationshipDict = new Dictionary<int, int>();
        //RelationshipDict[0] = -1;
        IsContinuous = false;
        NumChosen = 0;
        SomaPosition = GameObject.Find("Soma").transform.position;
        soma = new SwcSoma(SomaPosition, 0.5f);
        Chosen chosen= GameObject.Find("Soma").GetComponent<Chosen>();
        chosen.Apos = soma;
        chosen.Bpos = soma;
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
            if (IsPainting() && NumChosen > 0)
            {
                print("IsPainting!");

                if (chosenObject.transform.position == SomaPosition)
                {
                    GameObject Cylinder = CreateCylinder(RightIndexTipPosition, SomaPosition, PaintingBoardTransform, 0.01f);
                    Chosen NewChosen = Cylinder.GetComponent<Chosen>();
                    NewChosen.Apos = soma;
                    NewChosen.Bpos = new SwcNode(RightIndexTipPosition, 0.5f);
                    soma.AddChild(NewChosen.Bpos);

                    preNode = NewChosen.Bpos;

                    Debug.Log("create soma child");
                }
                else
                {
                    PosChosen = getChosenPosition(chosenObject);
                    GameObject Cylinder = CreateCylinder(RightIndexTipPosition, PosChosen, PaintingBoardTransform, 0.01f);
                    Chosen NewChosen = Cylinder.GetComponent<Chosen>();
                    Chosen chosenParent = chosenObject.GetComponent<Chosen>();
                    NewChosen.Apos = new SwcNode(PosChosen, 0.5f);
                    chosenParent.Apos.AddChild(NewChosen.Apos);
                    NewChosen.Bpos = new SwcNode(RightIndexTipPosition, 0.5f);
                    NewChosen.Apos.AddChild(NewChosen.Bpos);

                    GameObject CylinderA = CreateCylinder(chosenParent.Apos.Position, PosChosen, PaintingBoardTransform, 0.01f);
                    GameObject CylinderB = CreateCylinder(PosChosen, chosenParent.Bpos.Position, PaintingBoardTransform, 0.01f);

                    Chosen chosenA = CylinderA.GetComponent<Chosen>();
                    chosenA.Apos = chosenParent.Apos;
                    chosenA.Bpos = NewChosen.Apos;
                    chosenA.Apos.left = null;
                    chosenA.Apos.AddChild(chosenA.Bpos);

                    Chosen chosenB = CylinderB.GetComponent<Chosen>();
                    chosenB.Apos = NewChosen.Apos;
                    chosenB.Bpos = chosenParent.Bpos;
                    chosenB.Apos.AddChild(chosenB.Bpos);
                        
                    preNode = NewChosen.Bpos;

                    Destroy(chosenObject);

                    Debug.Log("create normal cylinder");

                }
                IsContinuous = true;
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
                if ((RightIndexTipPosition - preNode.Position).magnitude > 0.0001)
                {
                    GameObject Cylinder = CreateCylinder(RightIndexTipPosition, preNode.Position, PaintingBoardTransform, 0.01f);
                    Chosen NewChosen = Cylinder.GetComponent<Chosen>();
                    NewChosen.Apos = preNode;
                    NewChosen.Bpos = new SwcNode(RightIndexTipPosition, 0.01f);
                    NewChosen.Apos.AddChild(NewChosen.Bpos);

                    preNode = NewChosen.Bpos;

                    Debug.Log("create continuous cylinder");
                }

            }
        }
        preTime = curTime;

    }

    private Vector3 getChosenPosition(GameObject chosenObject)
    {
        Chosen chosen = chosenObject.GetComponent<Chosen>();
        Vector3 direction = chosen.Bpos.Position - chosen.Apos.Position;
        direction = direction.normalized;
        float t = (Vector3.Dot(RightIndexTipPosition, direction) - Vector3.Dot(chosen.Apos.Position, direction)) / Vector3.Dot(direction, direction);
        return (chosen.Apos.Position+t*direction);
    }
}
