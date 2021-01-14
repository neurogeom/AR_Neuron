using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Net;
using Command;
using UnityEngine.UI;
using System.Threading;
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
    

    private void Start()
    {
        //Debug.Log("start1");
        //mSocket.ConnectServer("192.168.1.2", 8088);
        //Debug.Log("start2");
        //mSocket.SendMessage("ConnectionMobilePhone:192.168.1.2:1234");
        //Debug.Log("start3");
        //Debug.Log("SendMessageOver!");
        //mSocket.RequestLoop();
        GetComponent<Button>().onClick.AddListener(ButtonClick);
    }

    private void ButtonClick()
    {
        mSocket.ConnectServer("192.168.1.2", 8088);
        mSocket.SendMessage("ConnectionMobilePhone:192.168.1.2:1234");
        ThreadStart ts = new ThreadStart(ThreadWorkLoop);
        Thread tr = new Thread(ts);
        tr.Start();
        bool createdButton = false;
        while (true&&!createdButton)
        {
            if (swcList.Count > 0)
            {
                GameObject canvas = GameObject.Find("Canvas");
                for (int i = 0; i < swcList.Count; ++i)
                {
                    string swcSelected = swcList[i];
                    GameObject button = new GameObject("Button" + i.ToString(),
                        typeof(Button), typeof(RectTransform), typeof(Image));
                    button.transform.SetParent(canvas.transform);
                    button.transform.localPosition = new Vector3(i * 105 + 70, 100, 0);
                    button.GetComponent<Button>().onClick.AddListener(()=>ButtonClickForSelection(swcSelected));
                }
                createdButton = true;
            }
            
        }
    }
    private void ButtonClickForSelection(string swcSelected)
    {
        mSocket.SendMessage("FileSelection:" + swcSelected);
    }
    public void ThreadWorkLoop()
    {
        //mSocket.RequestLoop();
        
        while (true)
        {
            mSocket.RequestLoop();
            swcList = mSocket.swcList;
        }
        
        /*while (mSocket.IsConnected)
        {
            //string data = "";
            //SendMessage(data);

            try
            {
                string ans = mSocket.ReceiveResponse();
                //Debug.Log(ans);
                CommandAnalysis ca = new CommandAnalysis(ans);
                //ca.Show();
                switch (ca.commandType)
                {
                    case CommandType.FileListShow:
                        //Debug.Log("fileListShow");

                        for (int i = 0; i < ca.fileList.Length; ++i)
                        {

                            swcList.Add(ca.fileList[i]);
                        }
                        AddSelectionButton(swcList);
                        break;
                    default:
                        Debug.Log("Nothing happened!");
                        break;
                }
            }
            catch
            {
                mSocket.IsConnected = false;
            }
        }*/
    }

    /*public void AddSelectionButton(List<string> swcFile)
    {
        if (swcFile.Count == 0) return;
        GameObject canvas = GameObject.Find("Canvas");
        for(int i = 0; i < swcFile.Count; ++i)
        {
            GameObject button = new GameObject("Button" + i.ToString(),
                typeof(Button), typeof(RectTransform), typeof(Image));
            button.transform.SetParent(canvas.transform);
        }
    }*/

    /*private void Update()
    {
        if (mSocket.IsConnected)
        {
            try
            {
                string command=mSocket.ReceiveResponse();
                Debug.Log("Receive data: " + command);
                if (command != null)
                {
                    
                    CommandAnalysis ca = new CommandAnalysis(command);
                    
                    switch (ca.commandType)
                    {
                        case CommandType.FileListShow:
                            //Debug.Log("FileListShow!");
                            string returnCommand = "FileSelection:" + ca.fileList[0];
                            mSocket.SendMessage(returnCommand);
                            break;
                        case CommandType.SWCTransmition:

                            break;
                        default:
                            break;
                    }

                }
            }
            catch
            {
                mSocket.IsConnected = false;
            }
        }
    }*/
}
