using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using ServerCore;

namespace ClientBot
{
    class ClientBotConnector : Connector
    {
        public override void OnConnect(IPEndPoint endPoint)
        {
            Console.WriteLine($"OnConnected: {endPoint.ToString()}");
        }
    }

    class ClientSession : Session
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"[Client] OnConnected: {endPoint.ToString()}");
            try
            {
                while (true)
                {
                    byte[] msgByte = Encoding.UTF8.GetBytes("Hello World !");
                    Send(msgByte);
                    Thread.Sleep(500);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"[Client] OnDisconnected: {endPoint.ToString()}");
        }

        public override void OnRecv(ArraySegment<byte> buffer)
        {
            string recvMsg = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
            Console.WriteLine($"[From Server] {recvMsg}");
        }

        public override void OnSend(int numOfBtyes)
        {
            Console.WriteLine($"[Client] Transferred bytes: {numOfBtyes}");
        }
    }

    class Program
    {
        static int portNum = 7777;
        static ClientBotConnector _connector = new ClientBotConnector();
        static IPEndPoint GetIPEndPoint()
        {
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0];
            return new IPEndPoint(ipAddr, portNum);
        }

        static void Main(string[] args)
        {
            IPEndPoint endPoint = GetIPEndPoint();
            _connector.Connect(endPoint, () =>
            {
                return new ClientSession();
            });

            while(true)
            {
            }
        }
    }
}
