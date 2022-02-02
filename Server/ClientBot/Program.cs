using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using ServerCore;
using ServerCore.Packets;

namespace ClientBot
{
    class ClientBotConnector : Connector
    {
        public override void OnConnect(IPEndPoint endPoint)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"OnConnected: {endPoint.ToString()}");
        }
    }

    class ClientSession : Session
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            if(endPoint != null)
                Console.WriteLine($"[Client] OnConnected: {endPoint.ToString()}");
            else
                Console.WriteLine($"[Client] OnConnected");

            try
            {
                while (true)
                {
                    RQ_TestMsg msg = new RQ_TestMsg();
                    msg.msg = "Hello From Client ! ! !";
                    Send(msg);
                    Thread.Sleep(5000);
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
            }
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            if(endPoint != null)
                Console.WriteLine($"[Client] OnDisconnected: {endPoint.ToString()}");
            else
                Console.WriteLine($"[Client] OnDisconnected");
        }

        public override void OnRecv(Packet packet)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;

            if(packet.PacketId == RS_TestMsg.Id)
                Console.WriteLine($"[From Server] {((RS_TestMsg)packet).msg}");
            else
                Console.WriteLine($"[From Server] {packet.ToString()}");
        }

        public override void OnSend(int numOfBtyes)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[Client] Transferred bytes: {numOfBtyes}");
        }
    }

    class Program
    {
        static ClientBotConnector _connector = new ClientBotConnector();

        static void Main(string[] args)
        {
            _connector.Connect(() =>
            {
                return new ClientSession();
            });

            while(true)
            {
            }
        }
    }
}
