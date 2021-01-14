using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System;
using System.Text;
using Command;
using UnityEngine.UI;
/*
Created by BlackFJ
*/

///<summary>
///
///</summary>

namespace Net
{
    public class ClientSocket
    {
        private static byte[] result = new byte[1024*64];
        private static Socket clientSocket;
        //是否已连接的标识
        public bool IsConnected = false;
        public List<string> swcList = new List<string>();

        public ClientSocket()
        {
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void ConnectServer(string ip, int port)
        {
            IPAddress mIp = IPAddress.Parse(ip);
            IPEndPoint ip_end_point = new IPEndPoint(mIp, port);

            try
            {
                clientSocket.Connect(ip_end_point);
                IsConnected = true;
                Debug.Log("连接服务器成功");
            }
            catch
            {
                IsConnected = false;
                Debug.Log("连接服务器失败");
                return;
            }
            //服务器下发数据长度
            /*int receiveLength = clientSocket.Receive(result);
            ByteBuffer buffer = new ByteBuffer(result);
            //int len = buffer.ReadShort();
            string data = buffer.ReadString();
            Debug.Log("服务器返回数据：" + data);*/
        }

        public void RequestLoop()
        {
            bool GotInfo = false;
            while (IsConnected&&!GotInfo)
            {
                //string data = "";
                //SendMessage(data);
              
                try
                {
                    string ans = ReceiveResponse();
                    //Debug.Log(ans);
                    CommandAnalysis ca = new CommandAnalysis(ans);
                    //ca.Show();
                    switch (ca.commandType)
                    {
                        case CommandType.FileListShow:
                            //Debug.Log("fileListShow");
                           

                            for(int i = 0; i < ca.fileList.Length; ++i)
                            {
                                swcList.Add(ca.fileList[i]);
                                //Debug.Log(ca.fileList[i]);
                                
                            }
                            GotInfo = true;
                            break;
                        default:
                            Debug.Log("Nothing happened!");
                            break;
                    }
                }
                catch
                {
                    IsConnected = false;
                }
            }
        }

        public string ReceiveResponse()
        {
            int receiveLength = clientSocket.Receive(result);
            if (receiveLength == 0) return null;

            ByteBuffer byteBuffer = new ByteBuffer(result);
            /*var buffer = new byte[1024];
            int received = clientSocket.Receive(buffer, SocketFlags.None);
            if (received == 0) return;
            var data = new byte[received];
            Array.Copy(buffer, data, received);
            string text = Encoding.ASCII.GetString(data);*/
            string text = byteBuffer.ReadString();
            //Debug.Log("Receive data:" + text);
            result = new byte[1024*64];
            return text;
        }

        public void SendMessage(string data)
        {
            if (IsConnected == false||data=="")
                return;
            try
            {
                ByteBuffer buffer = new ByteBuffer();
                buffer.WriteString(data);
                clientSocket.Send(WriteMessage(buffer.ToBytes()));
            }
            catch
            {
                IsConnected = false;
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
        }

        private static byte[] WriteMessage(byte[] message)
        {
            MemoryStream ms = null;
            using (ms = new MemoryStream())
            {
                ms.Position = 0;
                BinaryWriter writer = new BinaryWriter(ms);
                //ushort msglen = (ushort)message.Length;
                //writer.Write(msglen);
                writer.Write(message);
                writer.Flush();
                return ms.ToArray();
            }
        }
    }
}

