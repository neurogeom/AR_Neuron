using System;
using System.Net;
using System.Net.Sockets;

namespace simulation_clientSocket
{
    class Program
    {
        private static string ipStr = "192.168.1.2";
        private const int port = 8088;
        private static byte[] result = new byte[1024 * 1024];
        private static int num = 0;
        static void Main(string[] args)
        {
            Socket clientSocket_receive = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPAddress ip = IPAddress.Parse(ipStr);
            IPEndPoint ip_end_point = new IPEndPoint(ip, port);

            clientSocket_receive.Connect(ip_end_point);
            while (true)
            {
                clientSocket_receive.Receive(result, 0, 1024 * 1024, SocketFlags.None);
                string text = BitConverter.ToString(result);
                Console.WriteLine(num.ToString()+" : "+text);
                Console.WriteLine();
                result = new byte[1024 * 1024];
                num++;
            }
        }
    }
}
