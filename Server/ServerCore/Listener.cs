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
        public void Init(IPEndPoint endPoint, Func<Session> sessionFactory)
        {
            _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(endPoint);
            _listenSocket.Listen(10);

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
                    Console.WriteLine(e.Message);
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
