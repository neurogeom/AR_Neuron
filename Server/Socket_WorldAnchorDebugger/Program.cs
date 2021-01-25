using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;

namespace socket_worldAnchor_debugger
{
    class Program
    {
        private static byte[] result = new byte[1024 * 1024];
        private static List<byte[]> bytesList = new List<byte[]>();
        private static string ipStr = "192.168.1.2";
        private const int port = 8088;
        private static Socket clientSocket_send=null;
        private static Socket clientSocket_receive=null;
        private static Socket serverSocket;
        public static int numAcception = 0;
        public static float totalMB = 0;
        static void Main(string[] args)
        {
            IPAddress ip = IPAddress.Parse(ipStr);
            IPEndPoint ip_end_point = new IPEndPoint(ip, port);

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(ip_end_point);

            serverSocket.Listen(10);
            Console.WriteLine("Listening {0} succeeded!", serverSocket.LocalEndPoint.ToString());

            serverSocket.BeginAccept(AcceptCallback, null);

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
            if (clientSocket_receive != null)
            {
                clientSocket_send = socket;
            }
            else
            {
                clientSocket_receive = socket;
            }
            socket.BeginReceive(result, 0, 1024 * 1024, SocketFlags.None, ReceiveCallback, socket);
            Console.WriteLine("Client Connected!"); 
            serverSocket.BeginAccept(AcceptCallback, null);
        }

        private static void ReceiveCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            if (current == clientSocket_send)
            {
                int received;
                try
                {
                    received = current.EndReceive(AR);
                }
                catch
                {
                    Console.WriteLine("Client Disconnected!");
                    current.Close();
                    return;
                }
                byte[] recBuffer = new byte[received];
                Array.Copy(result, recBuffer, received);
                if (clientSocket_receive != null)
                {
                    clientSocket_receive.Send(result);
                    Console.WriteLine("Send {0}th piece of byteStream data to another holoLens.");
                }
                bytesList.Add(recBuffer);
                //string text = Encoding.ASCII.GetString(recBuffer);
                //string text=BitConverter.ToString(recBuffer);

                totalMB += received * 1.0f / 1024.0f / 1024.0f;
                Console.WriteLine("{0}: {1}MB", numAcception, totalMB);
                numAcception++;
            }
            /*else if(current==clientSocket_receive)
            {
                if (bytesList.Count > 0)
                {
                    for (int i = 0; i < bytesList.Count; ++i)
                    {
                        current.Send(bytesList[i]);
                        Console.WriteLine("Send {0}th piece of byteStream data to holoLens.");
                    }
                }
            }*/
            current.BeginReceive(result, 0, 1024 * 1024, SocketFlags.None, ReceiveCallback, current);
        }
    }
}
