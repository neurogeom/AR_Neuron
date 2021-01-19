using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using Microsoft.MixedReality.Toolkit.UI;
using System.Text;
#if WINDOWS_UWP
using Windows.Networking.Sockets;
using Windows.Networking;
using Windows.Storage.Streams;
using Windows.Storage;
#endif

public class hololens_client : MonoBehaviour
{
    public string information="Error!";
    public List<string> information_list = new List<string>();
    public int IsConnected = -1;//-1:disconnected 0:connected 1:connected and scripts done
    byte[] buff = new byte[1024 * 64];
    // Start is called before the first frame update
    async void Start()
    {

#if WINDOWS_UWP
        using(var streamSocket=new StreamSocket()){
            var hostName=new HostName("192.168.1.2");
            await streamSocket.ConnectAsync(hostName,"8088");
            string request="ConnectionHoloLens_UWP:192.168.1.30:12345";
            /*using (Stream outputStream=streamSocket.OutputStream.AsStreamForWrite()){
                using(var streamWriter=new StreamWriter(outputStream)){
                    await streamWriter.WriteLineAsync(request);
                    await streamWriter.FlushAsync();
                    
                }
            }*/
            DataWriter writer=new DataWriter(streamSocket.OutputStream);
            byte[] sendBuff=Encoding.ASCII.GetBytes(request);
            writer.WriteBytes(sendBuff);
            await writer.StoreAsync();


            writer.DetachStream();
            writer.Dispose();

            
            //string response;
            
            using(Stream inputStream=streamSocket.InputStream.AsStreamForRead()){
                using(StreamReader streamReader=new StreamReader(inputStream)){
                    while(true){
                        information=await streamReader.ReadLineAsync();
                        information_list.Add(information);
                        /*if(information.EndsWith("EndSWC")){
                            
                            break;
                        }*/
                    }
                }
            }

            
            //DataReader reader=new DataReader(streamSocket.InputStream);
            //await reader.LoadAsync(1024*64);
            //information=reader.ReadString(reader.UnconsumedBufferLength);
            
            IsConnected=0;
        }
#endif
    }

}
