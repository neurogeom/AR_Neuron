using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Created by BlackFJ
*/

///<summary>
///
///</summary>
public class ShowSphere : MonoBehaviour
{
    int flag;
    private void OnEnable()
    {
        flag = GameObject.Find("SWCLoader").GetComponent<SWCLoader>().justShowSphere;
        if (flag == 0)
        {
            var parentObject = GameObject.Find("SWCParent");
            Transform[] childrenObject = parentObject.GetComponentsInChildren<Transform>();
            for (int i = 0; i < childrenObject.Length; ++i)
            {
                if (childrenObject[i].name == "Cylinder")
                {
                    childrenObject[i].gameObject.SetActive(false);
                }
            
            }
            GameObject.Find("SWCLoader").GetComponent<SWCLoader>().justShowSphere = 1;
            
            gameObject.SetActive(false);
            
        }
        else
        {
            var parentObject = GameObject.Find("SWCParent");
            Transform[] childrenObject = parentObject.GetComponentsInChildren<Transform>(true);
            
            for (int i = 0; i < childrenObject.Length; ++i)
            {
                if (childrenObject[i].name == "Cylinder")
                {
                    childrenObject[i].gameObject.SetActive(true);
                }
            }
            GameObject.Find("SWCLoader").GetComponent<SWCLoader>().justShowSphere = 0;
            
            gameObject.SetActive(false);
            
        }

    }
}
