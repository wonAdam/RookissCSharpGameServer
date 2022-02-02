using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace ServerCore
{
    public class Listener
    {
        Socket _listenSocket;
        Func<Session> _sessionFactory;

        public static int serverPortNum = 4040;
        public static IPEndPoint GetServerIPEndPoint()
        {
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0];
            return new IPEndPoint(ipAddr, serverPortNum);
        }
        public void Init(Func<Session> sessionFactory)
        {
            IPEndPoint endPoint = GetServerIPEndPoint();
            _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(endPoint);
            _listenSocket.Listen(10);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Listening on {endPoint.Address.ToString()} : {endPoint.Port}");

            _sessionFactory = sessionFactory;

            for (int i = 0; i < 10; ++i)
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
                RegisterAccept(args);
            }
        }

        public void RegisterAccept(SocketAsyncEventArgs args)
        {
            args.AcceptSocket = null;
            bool pending = _listenSocket.AcceptAsync(args);
            
            // Synchronous하게 완료됨
            if(!pending)
                OnAcceptCompleted(null, args);
        }

        public void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
        {
            // accept 성공
            if(args.SocketError == SocketError.Success)
            {
                try
                {
                    // 세션 시작
                    Session session = _sessionFactory.Invoke();
                    session.Start(args.AcceptSocket);
                    session.OnConnected(args.AcceptSocket.RemoteEndPoint);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[Exception] {e.Message}");
                }
                
            }
            else
            {
                // TODO: 
            }

            // 다시 listening
            RegisterAccept(args);
        }
    }
}
