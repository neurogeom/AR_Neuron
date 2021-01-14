using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Net;
using Command;
using UnityEngine.UI;
using System.Threading;
using System.IO;
/*
Created by BlackFJ
*/

///<summary>
///
///</summary>
public class TestSocket : MonoBehaviour
{
    public ClientSocket mSocket = new ClientSocket();
    public List<string> swcList;
    //public List<string> swcContentList = new List<string>();

    private void Start()
    {
        mSocket.ConnectServer("192.168.1.2", 8088);
        mSocket.SendMessage("ConnectionHoloLens:192.168.1.30:12345");
        string swcContent=mSocket.ReceiveResponse();
        Debug.Log(swcContent);
        string path = @"Assets\NeuronsPrefabs\tmp.swc";
        File.WriteAllText(path, swcContent);
        Debug.Log("Writing swc finished!");
        //swcContentList.Add(swcContent);
    }

    private void Update()
    {
        
    }


}
