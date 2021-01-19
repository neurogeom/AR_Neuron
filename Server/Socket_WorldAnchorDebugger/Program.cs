using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
namespace socket_worldAnchor_debugger
{
    class Program
    {
        private static byte[] result = new byte[1024 * 1024];
        //private static List<byte[]> bytesList = new List<byte[]>();
        private static string ipStr = "192.168.1.2";
        private const int port = 8088;
        private static Socket clientSocket;
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
            clientSocket=socket;
            socket.BeginReceive(result, 0, 1024 * 1024, SocketFlags.None, ReceiveCallback, socket);
            Console.WriteLine("Client Connected!");
        }

        private static void ReceiveCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
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
            //string text = Encoding.ASCII.GetString(recBuffer);
            //string text=BitConverter.ToString(recBuffer);

            totalMB += received * 1.0f / 1024.0f / 1024.0f;
            Console.WriteLine("{0}: {1}MB", numAcception, totalMB);
            numAcception++;
            current.BeginReceive(result, 0, 1024 * 1024, SocketFlags.None, ReceiveCallback, current);
        }
    }
}
