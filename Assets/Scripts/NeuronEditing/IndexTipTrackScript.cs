using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using System.IO;
using System;
using System.Linq;
using Microsoft.MixedReality.Toolkit;
/*
Created by BlackFJ
*/
///<summary>
///
///</summary>
public class IndexTipTrackScript : MonoBehaviour
{
    private MixedRealityPose RightIndexTipPose;
    private MixedRealityPose RightThumbTipPose;
    private Vector3 RightThumbTipPosition, RightIndexTipPosition;//the position of right-handed indexTip and thumbTip 

    private List<Vector3> RightIndexTipPositionList;//save all the simpling point with indexTip position
    private Dictionary<int, int> RelationshipDict;//key: idx of rightIndexTipPositionList,value:  parent of idx  
    private Vector3 SomaPosition;//the world position of soma given

    [SerializeField]
    public float stepTime;//step time to control the sampling frequence
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
    private List<Marker> markerList = new List<Marker>();

    //for repairing
    private SwcNode repairSeed;
    private GameObject repairVectorCylinder;
    [SerializeField] Texture3D volume;

    public Vector3 Dims = new Vector3(512, 512, 512);
    public enum States { Selecting = 0, Drawing = 1, Reparing = 2, Waiting = 3, Drawing_2 = 4 };
    [SerializeField] public States state;

    public GameObject paintingTrack;
    private Vector3 preTrackPos;

    public int bkgThreshold;
    public Transform cubeTransform;


    private bool Finger_is_close()
    {
        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, Handedness.Right, out RightThumbTipPose) &&
            HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Right, out RightIndexTipPose))
        {
            RightThumbTipPosition = RightThumbTipPose.Position;
            RightIndexTipPosition = RightIndexTipPose.Position;
            float tmpDistance2 = Vector3.Distance(RightIndexTipPosition, RightThumbTipPosition);
            if (tmpDistance2 < distance2Limited)
            {
                return true;
            }
        }
        return false;

    }

    private string SomaCenteredString(Vector3 p)
    {
        var pc = p - SomaPosition;
        return pc.x.ToString() + " " + pc.y.ToString() + " " + pc.z.ToString() + " ";
    }

    private string Write2SWC(string radius = "0.5", string type = "3")
    {
        string swc = "1 1 0 0 0 " + soma.radius + " -1\n";
        soma.index = 1;
        int index = 2;
        Debug.Log(soma.children.Count);
        Debug.Log(soma.left == null);
        foreach (SwcNode node in soma.children)
        {
            PreOrder(node, ref swc, ref index);
        }

        return swc;
    }

    private void PreOrder(SwcNode node, ref string swc, ref int index)
    {
        string type = "2";
        node.index = index;
        if (node.parent.index == 0)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            temp.transform.position = node.position;
            temp.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        }
        string line = index.ToString() + " " + type + " " + SomaCenteredString(node.position)
                + " " + node.radius + " " + node.parent.index + "\n";
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
        //soma = (SwcSoma)GameObject.Find("Soma").GetComponent<Chosen>().nodeB;
        //SomaPosition = soma.position;
        //Chosen chosen= GameObject.Find("Soma").GetComponent<Chosen>();
        //chosen.Apos = soma;
        //chosen.Bpos = soma;
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

        switch (state)
        {
            case States.Drawing:
                if (!IsContinuous)
                {
                    if (Finger_is_close() && NumChosen > 0)
                    {
                        print("IsPainting!");

                        if (chosenObject.GetComponent<Chosen>().nodeB == soma)
                        {
                            //SwcNode newNode = new SwcNode(RightIndexTipPosition, 0.001f,PaintingBoardTransform);
                            //soma.AddChild(newNode);
                            //newNode.sphere = Primitive.CreateSphere(newNode, PaintingBoardTransform);
                            //newNode.cylinder = Primitive.CreateCylinder(newNode, PaintingBoardTransform);
                            //Chosen NewChosen = newNode.cylinder.AddComponent<Chosen>();
                            //NewChosen.setNode(newNode);

                            preNode = soma;

                            Debug.Log("create soma child");
                        }
                        else
                        {
                            //NodeB===========brokenNode=============NodeA
                            //                   //
                            //                  //
                            //                 //
                            //               newNode
                            //PosChosen = getBreakPosition(chosenObject);
                            //var brokenNode = new SwcNode(PosChosen, 0.001f, PaintingBoardTransform);// branch root
                            //var newNode = new SwcNode(RightIndexTipPosition, 0.001f, PaintingBoardTransform);

                            //var preChosen = chosenObject.GetComponent<Chosen>();
                            //var nodeA = preChosen.nodeA;
                            //var nodeB = preChosen.nodeB;

                            //nodeA.RemoveChild(preChosen.nodeB);
                            //nodeA.AddChild(brokenNode);
                            //brokenNode.AddChild(nodeB);
                            //brokenNode.AddChild(newNode);
                            //brokenNode.isBranch_root = true;
                            //brokenNode.sphere = Primitive.CreateSphere(brokenNode, PaintingBoardTransform);
                            //newNode.sphere = Primitive.CreateSphere(newNode, PaintingBoardTransform);

                            //Debug.Log(nodeB.parent.position);
                            //nodeB.cylinder = Primitive.CreateCylinder(nodeB, PaintingBoardTransform);
                            //var chosenB = nodeB.cylinder.AddComponent<Chosen>();
                            //chosenB.setNode(nodeB);
                            //brokenNode.cylinder = Primitive.CreateCylinder(brokenNode, PaintingBoardTransform);
                            //var chosenBroken = brokenNode.cylinder.AddComponent<Chosen>();
                            //chosenBroken.setNode(brokenNode);
                            //newNode.cylinder = Primitive.CreateCylinder(newNode, PaintingBoardTransform);
                            //var chosenNew = newNode.cylinder.AddComponent<Chosen>();
                            //chosenNew.setNode(newNode); 
                            PosChosen = getBreakPosition(chosenObject);
                            var preChosen = chosenObject.GetComponent<Chosen>();
                            var nodeA = preChosen.nodeA;
                            var nodeB = preChosen.nodeB;
                            var newNode = new SwcNode(PosChosen, nodeB.radius, PaintingBoardTransform);
                            nodeA.RemoveChild(preChosen.nodeB);
                            nodeA.AddChild(newNode);
                            newNode.AddChild(nodeB);
                            newNode.isBranch_root = true;

                            newNode.sphere = Primitive.CreateSphere(newNode, PaintingBoardTransform);
                            nodeB.cylinder = Primitive.CreateCylinder(nodeB, PaintingBoardTransform);
                            var chosenB = nodeB.cylinder.AddComponent<Chosen>();
                            chosenB.setNode(nodeB);

                            newNode.cylinder = Primitive.CreateCylinder(newNode, PaintingBoardTransform);
                            var chosenBroken = newNode.cylinder.AddComponent<Chosen>();
                            chosenBroken.setNode(newNode);

                            preNode = newNode;
                            GameObject.Destroy(chosenObject);

                            Debug.Log("create normal cylinder");

                        }
                        IsContinuous = true;
                    }
                }
                else
                {
                    if (!Finger_is_close())
                    {
                        IsContinuous = false;
                    }
                    else
                    {
                        if ((RightIndexTipPosition - preNode.sphere.transform.position).magnitude > 0.01)
                        {
                            var newNode = new SwcNode(RightIndexTipPosition, 0.002f, PaintingBoardTransform);
                            preNode.AddChild(newNode);
                            newNode.sphere = Primitive.CreateSphere(newNode, PaintingBoardTransform);
                            newNode.cylinder = Primitive.CreateCylinder(newNode, PaintingBoardTransform);
                            Chosen NewChosen = newNode.cylinder.AddComponent<Chosen>();
                            NewChosen.setNode(newNode);

                            preNode = newNode;

                            Debug.Log("create continuous cylinder");
                        }

                    }
                }
                break;
            case States.Selecting:
                if (chosenObject == null) break;
                if (Finger_is_close())
                {
                    Chosen c = chosenObject.GetComponent<Chosen>();
                    SwcNode node = c.nodeB;
                    while (!node.isBranchRoot() && !node.isLeaf())
                    {
                        node = node.left;
                    }

                    if (node.HasChild()) node.RemoveAllChild();
                    else GameObject.Destroy(node.sphere);

                    GameObject.Destroy(node.cylinder);
                    node = node.parent;
                    while (!node.isBranchRoot() && node.parent != null)
                    {
                        GameObject.Destroy(node.sphere);
                        GameObject.Destroy(node.cylinder);
                        if (node.parent.isBranchRoot())
                        {
                            node.parent.RemoveChild(node);
                        }
                        node = node.parent;
                    }
                }
                break;
            case States.Reparing:
                if (!IsContinuous)
                {
                    if (Finger_is_close() && NumChosen > 0)
                    {
                        var chosen = chosenObject.GetComponent<Chosen>();
                        if (chosen.nodeB.isLeaf())
                        {
                            repairSeed = chosen.nodeB;
                        }
                        else
                        {
                            var nodeA = chosen.nodeA;
                            var nodeB = chosen.nodeB;
                            SwcNode newNode = new SwcNode(getBreakPosition(chosenObject), chosen.nodeA.radius, PaintingBoardTransform);
                            nodeA.RemoveChild(nodeB);
                            nodeA.AddChild(newNode);
                            newNode.AddChild(nodeB);
                            newNode.isBranch_root = true;

                            newNode.sphere = Primitive.CreateSphere(newNode, PaintingBoardTransform);
                            nodeB.cylinder = Primitive.CreateCylinder(nodeB, PaintingBoardTransform);
                            var chosenB = nodeB.cylinder.AddComponent<Chosen>();
                            chosenB.setNode(nodeB);

                            newNode.cylinder = Primitive.CreateCylinder(newNode, PaintingBoardTransform);
                            var chosenBroken = newNode.cylinder.AddComponent<Chosen>();
                            chosenBroken.setNode(newNode);
                            GameObject.Destroy(chosenObject);

                            repairSeed = newNode;
                        }
                        print("Setting RepairVector!");
                        IsContinuous = true;
                    }
                }
                else
                {
                    Debug.Log("Continuous");
                    if (!Finger_is_close())
                    {
                        IsContinuous = false;
                        Vector3 pos = repairSeed.position;
                        pos += new Vector3(0.5f, 0.5f, 0.5f);
                        pos = new Vector3(pos.x * Dims.x, pos.y * Dims.y, pos.z * Dims.z);
                        Marker repairMarker = new Marker(pos);
                        Vector3 repairDirection = RightIndexTipPosition - repairSeed.sphere.transform.position;
                        repairDirection = PaintingBoardTransform.InverseTransformDirection(repairDirection).normalized;
                        List<Marker> list;

                        //list = FastMarching.FastMarching_repair(repairMarker, repairDirection, 512, 512, 512);
                        //Primitive.RepairTree(list, repairSeed, PaintingBoardTransform, volume.width,volume.height,volume.depth);


                        //Debug.Log(list.Count);
                        //byte[] img = volume.GetPixelData<byte>(0).ToArray();
                        //List<Marker> filtered_tree = HierarchyPrune.hierarchy_prune_repair(list, img, 512, 512, 512, 30, 1, false);
                        //Debug.Log(filtered_tree.Count);
                        //Primitive.RepairTree(filtered_tree, repairSeed, PaintingBoardTransform);

                        GameObject.Destroy(repairVectorCylinder);
                    }
                    else
                    {
                        Debug.Log("setting1");
                        if ((RightIndexTipPosition - repairSeed.sphere.transform.position).magnitude > 0.01)
                        {
                            Debug.Log("setting");
                            GameObject.Destroy(repairVectorCylinder);
                            repairVectorCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                            Vector3 a = PaintingBoardTransform.InverseTransformPoint(RightIndexTipPosition);
                            Vector3 b = PaintingBoardTransform.InverseTransformPoint(repairSeed.sphere.transform.position);
                            float length = Vector3.Distance(a, b);
                            Vector3 ab = (a - b).normalized;
                            Vector3 y_axis = new Vector3(0, 1, 0);
                            repairVectorCylinder.transform.parent = PaintingBoardTransform;
                            repairVectorCylinder.transform.localScale = new Vector3(0.001f, length / 2, 0.001f);
                            repairVectorCylinder.transform.Rotate(Vector3.Cross(ab, y_axis), -Mathf.Acos(Vector3.Dot(ab, y_axis)) * 180 / Mathf.PI);
                            repairVectorCylinder.transform.localPosition = (a + b) / 2;
                            repairVectorCylinder.GetComponent<MeshRenderer>().material.color = Color.white;
                        }

                    }
                }
                break;
            case States.Drawing_2:
                if (!IsContinuous)
                {
                    if (Finger_is_close())
                    {
                        markerList.Clear();
                        var transformedPos = cubeTransform.InverseTransformPoint(RightIndexTipPose.Position);
                        //pos = PaintingBoardTransform.worldToLocalMatrix * new Vector4(pos.x, pos.y, pos.z, 1);
                        transformedPos += new Vector3(0.5f, 0.5f, 0.5f);
                        var marker = new Marker(transformedPos);
                        Vector3 pos = marker.position;
                        if (pos.x >= 0 && pos.x < 1 && pos.y >= 0 && pos.y < 1 && pos.z >= 0 && pos.z < 1)
                        {
                            markerList.Add(marker);
                        }
                        preTrackPos = RightIndexTipPose.Position;
                        Debug.Log("start drawing_2");
                        IsContinuous = true;
                    }
                }
                else
                {
                    if (!Finger_is_close())
                    {
                        if (markerList.Count >= 2)
                        {
                            IsContinuous = false;
                            var result = VirutalFinger.RefineSketchCurve(markerList, volume, bkgThreshold, 10);
                            Primitive.CreateBranch(result, cubeTransform, volume.width, volume.height, volume.depth);
                        }
                        IsContinuous = false;
                        //for(int i = 0; i < paintingTrack.transform.childCount; i++)
                        //{
                        //    Destroy(paintingTrack.transform.GetChild(i).gameObject);
                        //}
                    }
                    else
                    {
                        if ((preTrackPos - RightIndexTipPose.Position).magnitude > 0.01)
                        {
                            var tmpSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            tmpSphere.transform.position = RightIndexTipPose.Position;
                            tmpSphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                            tmpSphere.transform.SetParent(paintingTrack.transform, true);
                            var transformedPos = cubeTransform.InverseTransformPoint(RightIndexTipPose.Position);
                            //pos = PaintingBoardTransform.worldToLocalMatrix * new Vector4(pos.x, pos.y, pos.z, 1);
                            transformedPos += new Vector3(0.5f, 0.5f, 0.5f);
                            var marker = new Marker(transformedPos);
                            Vector3 pos = marker.position;
                            if (pos.x >= 0 && pos.x < 1 && pos.y >= 0 && pos.y < 1 && pos.z >=0 && pos.z < 1)
                            {
                                markerList.Add(marker);
                                preTrackPos = RightIndexTipPose.Position;
                            }
                            //Debug.Log("create continuous cylinder");
                        }
                    }
                }
                break;
            case States.Waiting:
                break;
        }

        preTime = curTime;

    }

    private Vector3 getBreakPosition(GameObject chosenObject)
    {
        Chosen chosen = chosenObject.GetComponent<Chosen>();
        Vector3 posA = chosen.nodeA.sphere.transform.position;
        Vector3 posB = chosen.nodeB.sphere.transform.position;
        Vector3 direction = posB - posA;
        direction = direction.normalized;
        float t = (Vector3.Dot(RightIndexTipPosition, direction) - Vector3.Dot(posA, direction)) / Vector3.Dot(direction, direction);
        return (posA + t * direction);
    }
}
