using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Created by BlackFJ
*/

///<summary>
///
///</summary>
public class MovingCamera : MonoBehaviour
{
    private void Update()
    {
        if(Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            gameObject.transform.Translate(new Vector3(0, 0, Input.GetAxis("Mouse ScrollWheel")) * Time.deltaTime);
        }
        if (Input.GetMouseButton(2))
        {
            gameObject.transform.Translate(Vector3.left * Input.GetAxis("Mouse X"));
            gameObject.transform.Translate(Vector3.up * Input.GetAxis("Mouse Y"));
        }
        if (Input.GetMouseButton(1))
        {
            transform.RotateAround(gameObject.transform.position, Vector3.up, (Input.GetAxis("Mouse X") * 5));
            transform.RotateAround(gameObject.transform.position, Vector3.right, (Input.GetAxis("Mouse Y") * 5));
        }
    }
}
