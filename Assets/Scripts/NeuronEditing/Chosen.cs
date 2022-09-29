using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Experimental.InteractiveElement;
/*
Created by BlackFJ
*/

///<summary>
///
///</summary>
public class Chosen : MonoBehaviour
{
    private Material baseMaterial;
    private Vector3 baseScale;
    private IndexTipTrackScript manager;

    public Material ChosenMaterial;
    //public bool IsChosen;
    public int Idx;//pos idx of list
    public Vector3 Pos;//cylinder 's a pos

    public SwcNode nodeA, nodeB;
    private void Start()
    {
        manager = GameObject.Find("HandTrackingManager").GetComponent<IndexTipTrackScript>();
        InteractiveElement interactiveElement = GetComponent<InteractiveElement>();
        if (interactiveElement == null)
        {
            interactiveElement = this.gameObject.AddComponent<InteractiveElement>();
        }
        interactiveElement.AddNewState(CoreInteractionState.Touch.ToString());

        NearInteractionTouchableVolume volume = this.gameObject.AddComponent<NearInteractionTouchableVolume>();

        TouchEvents touchEvents = interactiveElement.GetStateEvents<TouchEvents>("Touch");

        touchEvents.OnTouchStarted.AddListener((touchData) =>
        {
            baseScale = this.gameObject.transform.localScale;
            baseMaterial = this.gameObject.GetComponent<MeshRenderer>().material;
            changeMaterial(nodeB, manager.state==IndexTipTrackScript.States.Selecting, Color.green);
            //IsChosen = true;
            manager.NumChosen += 1;
            manager.chosenObject = this.gameObject;
        });

        touchEvents.OnTouchCompleted.AddListener((touchData) =>
        {
            this.gameObject.transform.localScale = baseScale;
            changeMaterial(nodeB, manager.state == IndexTipTrackScript.States.Selecting, Color.red);
            //IsChosen = false;
            manager.NumChosen -= 1;
        });
    }

    void changeMaterial(SwcNode node, bool isSelecting,Color color)
    {
        if (this.gameObject.name == "Soma")
        {
            this.gameObject.GetComponent<MeshRenderer>().material.color = color;
            return;
        }
        if (isSelecting)
        {
            SwcNode temp = node;
            while (node != null)
            {
                node.cylinder.GetComponent<MeshRenderer>().material.color = color;
                node.sphere.GetComponent<MeshRenderer>().material.color = color;
                if (node.isBranchRoot())
                {
                    break;
                }
                node = node.left;
            }

            node = temp.parent;
            while (!node.isBranchRoot() && node.parent!= null)
            {
                node.cylinder.GetComponent<MeshRenderer>().material.color = color;
                node.sphere.GetComponent<MeshRenderer>().material.color = color;
                node = node.parent;
            }
        }
        else
        {
            node.cylinder.GetComponent<MeshRenderer>().material.color = color;
            node.sphere.GetComponent<MeshRenderer>().material.color = color;
            node.parent.sphere.GetComponent<MeshRenderer>().material.color = color;
        }
    }
    
    public void setNode(SwcNode node)
    {
        nodeB = node;
        nodeA = node.parent;
    }

}
