using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
/*
Created by BlackFJ
*/

///<summary>
///
///</summary>s
public class FollowMovingNeuron : MonoBehaviour
{
    int flag;

    private void OnEnable()
    {
        flag = GameObject.Find("SWCLoader").GetComponent<SWCLoader>().neuronFollowing;
        if (flag == 0)
        {
            GameObject.Find("SWCParent").GetComponent<RadialView>().enabled = true;
            GameObject.Find("SWCLoader").GetComponent<SWCLoader>().neuronFollowing = 1;
            gameObject.SetActive(false);
        }
        else
        {

            GameObject.Find("SWCParent").GetComponent<RadialView>().enabled = false;
            GameObject.Find("SWCLoader").GetComponent<SWCLoader>().neuronFollowing = 0;
            gameObject.SetActive(false);
        }
    }
}
