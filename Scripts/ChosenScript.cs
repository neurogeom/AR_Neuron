﻿using System.Collections;
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
public class ChosenScript : MonoBehaviour
{
    private Material baseMaterial;
    private Vector3 baseScale;
    private IndexTipTrackScript manager;

    public Material ChosenMaterial;
    //public bool IsChosen;
    public int Idx;//pos idx of list
    public Vector3 Pos;//cylinder 's a pos

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
            this.gameObject.transform.localScale *= 2f;
            baseMaterial = this.gameObject.GetComponent<MeshRenderer>().material;
            this.gameObject.GetComponent<MeshRenderer>().material = ChosenMaterial;
            //IsChosen = true;
            manager.NumChosen += 1;
            manager.PosChosen = Pos;
            manager.IdxChosen = Idx;
        });

        touchEvents.OnTouchCompleted.AddListener((touchData) =>
        {
            this.gameObject.transform.localScale = baseScale;
            this.gameObject.GetComponent<MeshRenderer>().material = baseMaterial;
            //IsChosen = false;
            manager.NumChosen -= 1;
        });

    }

}
