using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using Net;
using System.IO;
using Command;

namespace net_csharp
{
    public enum ConnectedTerminal
    {
        tDisconnected=0,
        tConnected,
        tMobilePhone,
        tHoloLens,
        tHoloLens_UWP,
        tOthers
    }

    class Program
    {
        private static byte[] result = new byte[1024*64];
        private const int port = 8088;
        private static string IpStr = "192.168.1.2";
        private static Socket serverSocket;
        private static List<Socket> clientSockets = new List<Socket>();
        private static List<ConnectedTerminal> clientTypes = new List<ConnectedTerminal>();
        
        private static string swcFolder = "../../../../swcFolder";
        private static SWCList swcList = new SWCList(swcFolder);

        private static List<string> swcFiles = swcList.GetSWCFilesName();
        //private static List<Socket> mobileSockets = new List<Socket>();

        static void Main(string[] args)
        {
            swcList.Show();
            //Console.WriteLine(swcList.ReadSelectedSWC("1.swc"));

            IPAddress ip = IPAddress.Parse(IpStr);
            IPEndPoint ip_end_point = new IPEndPoint(ip, port);

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            serverSocket.Bind(ip_end_point);

            serverSocket.Listen(10);
            Console.WriteLine("Listening {0} succeeded!", serverSocket.LocalEndPoint.ToString());

            serverSocket.BeginAccept(AcceptCallback, null);
            //Thread thread = new Thread(ClientConnectListen);
            //thread.Start();
            Console.ReadLine();
            serverSocket.Close();
        }

        private static void AcceptCallback(IAsyncResult AR)
        {
            Socket socket;
            try
            {
                socket = serverSocket.EndAccept(AR);
            }
            catch
            {
                return;
            }
            clientSockets.Add(socket);
            clientTypes.Add(ConnectedTerminal.tConnected);
            socket.BeginReceive(result, 0, 1024*64, SocketFlags.None, ReceiveCallback, socket);
            Console.WriteLine("Client Connected!");
            //socket.Send(WriteMessage("Connected!"));
            serverSocket.BeginAccept(AcceptCallback, null);
        }

        private static void ReceiveCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            int curIndex=clientSockets.IndexOf(current);
            int received;
            try
            {
                received = current.EndReceive(AR);
            }
            catch
            {
                Console.WriteLine("Client Disconnected!");
                current.Close();
                clientTypes[curIndex] = ConnectedTerminal.tDisconnected;
                clientSockets.Remove(current);
                return;
            }
            //ByteBuffer recBuffer = new ByteBuffer(result);
            //string text = recBuffer.ReadString();
            byte[] recBuffer = new byte[received];
            Array.Copy(result, recBuffer, received);
            string text = Encoding.ASCII.GetString(recBuffer);

            if (HandleCommand(text, curIndex))
            {
                Console.WriteLine("Received Text: " + text);
            }
                
            
            current.BeginReceive(result, 0, 1024*64, SocketFlags.None, ReceiveCallback, current);
        }


        private static bool HandleCommand(string text,int socketIndex)
        {
            CommandAnalysis ca = new CommandAnalysis(text);
            //ca.Show();
            switch (ca.commandType)
            {
                case CommandType.Error:
                    
                    clientSockets[socketIndex].Send(WriteMessage("Error Command!"));
                    break;
                
                case CommandType.Connection_MobilePhone:
                    clientTypes[socketIndex] = ConnectedTerminal.tMobilePhone;
                    string command = string.Format("FileList:{0} ", swcFiles.Count);
                    for(int i = 0; i < swcFiles.Count-1; ++i)
                    {
                        command += swcFiles[i] + " ";
                    }
                    command += swcFiles[swcFiles.Count - 1];
                    clientSockets[socketIndex].Send(WriteMessage(command));
                    Console.WriteLine("Transmitted Information: " + command);
                    break;

                case CommandType.Connection_HoloLens:
                    clientTypes[socketIndex] = ConnectedTerminal.tHoloLens;
                    Console.WriteLine("HoloLens Connecting!");
                    break;
                case CommandType.Connection_HoloLens_UWP:
                    clientTypes[socketIndex] = ConnectedTerminal.tHoloLens_UWP;
                    Console.WriteLine("HoloLens_UWP Connecting!");
                    int fileIndexx = swcFiles.IndexOf("1.swc");
                    if (fileIndexx == -1)
                    {
                        clientSockets[socketIndex].Send(WriteMessage("Error FileSelection!"));
                        Console.WriteLine("Transmitted Information: Error FileSelection!");
                    }
                    else
                    {
                        List<string> swcContentList = swcList.ReadSelectedSWCList_UWP("4.swc",30);
                        string whole = "";
                        for (int j = 0; j < swcContentList.Count; ++j)
                        {
                            whole += swcContentList[j];
                                    
                        }
                        Console.WriteLine(whole);
                        for (int i = 0; i < clientTypes.Count; ++i)
                        {
                            if (clientTypes[i] == ConnectedTerminal.tHoloLens_UWP)
                            {
                                clientSockets[i].Send(WriteMessage(whole));
                                //clientSockets[i].Send(WriteMessage(swcContentList[0]));
                                //clientSockets[i].Send(WriteMessage("EndSWC\n"));
                            }
                        }
                        /*for (int i = 0; i < swcContentList.Count; ++i)
                        {
                            Console.WriteLine("Transmitted Information "+i.ToString()+": " + swcContentList[i]);
                        }*/
                    }
                    
                    
                    break;
                case CommandType.Connection_Server:
                    clientTypes[socketIndex] = ConnectedTerminal.tOthers;
                    break;
                case CommandType.Connection_OtherClient:
                    clientTypes[socketIndex] = ConnectedTerminal.tOthers;
                    break;
                case CommandType.FileListShow:
                    break;

                case CommandType.FileSelection:
                    string fileSelected = ca.selectedFile;
                    int fileIndex = swcFiles.IndexOf(fileSelected);
                    if (fileIndex == -1)
                    {
                        clientSockets[socketIndex].Send(WriteMessage("Error FileSelection!"));
                        Console.WriteLine("Transmitted Infomation: Error FileSelection!");
                    }
                    else
                    {
                        string swcContent=swcList.ReadSelectedSWC(fileSelected);
                        for(int i = 0; i < clientTypes.Count; ++i)
                        {
                            if (clientTypes[i] == ConnectedTerminal.tHoloLens)
                            {
                                clientSockets[i].Send(WriteMessage(swcContent));
                            }
                        }
                        Console.WriteLine("Transmitted Information: " + swcContent);
                    }
                    break;

                default:
                    break;
            }
            if (ca.commandType == CommandType.Useless || ca.commandType == CommandType.Error)
            {
                return false;
            }
            return true;
        }
        private static byte[] WriteMessage(string message)
        {
            byte[] data = Encoding.ASCII.GetBytes(message);
            return data;
        }
        

        /*private static byte[] WriteMessage(byte[] message)
        {
            MemoryStream ms = null;
            using(ms=new MemoryStream())
            {
                ms.Position = 0;
                BinaryWriter writer = new BinaryWriter(ms);
                ushort msgLen = (ushort)message.Length;
                writer.Write(msgLen);
                writer.Write(message);
                writer.Flush();
                return ms.ToArray();
            }
        }*/

        /*private static void ReceiveMessage(object clientSocket)
        {
            Socket mClientSocket = (Socket)clientSocket;
            while (true)
            {
                try
                {
                    int receiveLen = mClientSocket.Receive(result);
                    Console.WriteLine("Received client {0}, total length {1}", mClientSocket.RemoteEndPoint.ToString(), receiveLen);
                    ByteBuffer buff = new ByteBuffer(result);

                    int len = buff.ReadShort();
                    string data = buff.ReadString();
                    Console.WriteLine("Data content:{0}", data);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    mClientSocket.Shutdown(SocketShutdown.Both);
                    mClientSocket.Close();
                    break;
                }
            }
        }*/

        /*private static void ClientConnectListen()
        {
            while (true)
            {
                Socket clientSocket = serverSocket.Accept();
                Console.WriteLine("Client {0} connecting succeeded!", clientSocket.RemoteEndPoint.ToString());

                ByteBuffer buffer = new ByteBuffer();
                string connectionServerCommand = String.Format("ConnectionServer:{0}:{1}", IpStr, port);
                buffer.WriteString(connectionServerCommand);
                clientSocket.Send(WriteMessage(buffer.ToBytes()));

                Thread thread = new Thread(ReceiveMessage);
                thread.Start(clientSocket);
            }
        }*/
    }
}
