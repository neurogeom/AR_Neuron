using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Numpy;

public class num : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        int[,] arr = new int[2, 3] { { 5, 6, 7 }, { 8, 7, 6 } };
        NDarray ndarr = np.array(arr);
        Debug.Log(ndarr.ToString());
        Debug.Log("------------------------");
        //int[,] arr1 = np.arange(0, 9).reshape(3, 3);
        NDarray arr1 = np.array(new int[,] { { 1, 2, 3 }, { 4, 5, 6 } });
        var arr2 = np.matmul(arr1.T, arr1);
        Debug.Log(arr2.ToString());
        Debug.Log("------------------------");
        var arr3 = arr2[":,1:3"];
        
        Debug.Log(arr3.ToString());
        Debug.Log("***************");
        Debug.Log("***************");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
