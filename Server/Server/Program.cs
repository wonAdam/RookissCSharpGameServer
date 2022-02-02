using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using ServerCore;

namespace Server
{
    class GameSession : Session
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            if(endPoint != null)
                Console.WriteLine($"[Server] OnConnected: {endPoint.ToString()}");
            else
                Console.WriteLine($"[Server] OnConnected");
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            if(endPoint != null)
                Console.WriteLine($"[Server] OnDisconnected: {endPoint.ToString()}");
            else
                Console.WriteLine($"[Server] OnDisconnected");
        }

        public override void OnRecv(Packet packet)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[From Client] {packet.ToString()}");

            //RS_TestMsg msg = new RS_TestMsg();
            //msg.msg = "Hello From Server ! ! !";
            //Send(msg);
        }

        public override void OnSend(int numOfBtyes)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[Server] Transferred bytes: {numOfBtyes}");
        }
    }

    class Program
    {
        static Listener _listener = new Listener();
        static void Main(string[] args)
        {
            _listener.Init(() => { return new GameSession(); });

            while (true)
            {
            }
        }
    }
}
