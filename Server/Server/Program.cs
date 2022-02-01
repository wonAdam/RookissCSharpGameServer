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
            Console.WriteLine($"[Server] OnConnected: {endPoint.ToString()}");
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"[Server] OnDisconnected: {endPoint.ToString()}");
        }

        public override void OnRecv(ArraySegment<byte> buffer)
        {
            string msg = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
            Console.WriteLine($"[From Client] {msg}");

            byte[] sendMsg = Encoding.UTF8.GetBytes("Hello From Server !");
            Send(sendMsg);
        }

        public override void OnSend(int numOfBtyes)
        {
            Console.WriteLine($"[Server] Transferred bytes: {numOfBtyes}");
        }
    }

    class Program
    {
        static Listener _listener = new Listener();
        static int listenPortNum = 7777;

        static IPEndPoint GetIPEndPoint()
        {
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0];
            return new IPEndPoint(ipAddr, listenPortNum);
        }

        static void Main(string[] args)
        {
            _listener.Init(GetIPEndPoint(), () => { return new GameSession(); });

            while (true)
            {
            }
        }
    }
}
