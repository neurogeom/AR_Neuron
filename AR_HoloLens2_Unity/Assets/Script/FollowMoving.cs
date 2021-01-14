using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Created by BlackFJ
*/

///<summary>
///
///</summary>
public class FollowMoving : MonoBehaviour
{

    Vector3 screenPosition;
    Vector3 offset;

    private void Start()
    {
        StartCoroutine(OnMouseDown());
    }
    private IEnumerator OnMouseDown()
    {
        screenPosition = Camera.main.WorldToScreenPoint(transform.position);

        Vector3 mousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPosition.z);
        mousePos = Camera.main.ScreenToWorldPoint(mousePos);
        offset = transform.position - mousePos;

        while (Input.GetMouseButton(0))
        {
            Vector3 curMousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPosition.z);
            curMousePos = Camera.main.ScreenToWorldPoint(curMousePos);

            transform.parent.position = curMousePos + offset;
            yield return new WaitForFixedUpdate();
        }
    }


}
