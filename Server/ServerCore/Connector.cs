using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore
{
    public abstract class Connector
    {
        private Func<Session> _sessionFactory;
        public abstract void OnConnect(IPEndPoint endPoint);
        public void Connect(IPEndPoint endPoint, Func<Session> sessionFactory, int count = 1)
        {
            _sessionFactory = sessionFactory;
            for (int i = 0; i < count; i++)
            {
                Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += OnConnect_Internal;
                args.RemoteEndPoint = endPoint;
                args.UserToken = socket;

                RegisterConnect(args);
            }
        }

        private void RegisterConnect(SocketAsyncEventArgs args)
        {
            Socket socket = args.UserToken as Socket;
            bool pending = socket.ConnectAsync(args);
            if (!pending)
                OnConnect_Internal(null, args);

        }

        private void OnConnect_Internal(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                Session session = _sessionFactory.Invoke();
                session.Start(args.ConnectSocket);
                session.OnConnected(args.RemoteEndPoint);
            }
            else
            {
                Console.WriteLine($"");
            }
        }
    }
}