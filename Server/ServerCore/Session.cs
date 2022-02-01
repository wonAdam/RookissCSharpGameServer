using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    enum ESocketError
    { 
        None,
        BytesTransferedZero,
        SocketErrorNotSuccess,
        BufferNull,
    }


    public abstract class Session
    {
        Socket _socket;

        RecvBuffer _recvBuffer = new RecvBuffer(2^16);

        Queue<byte[]> _sendQueue = new Queue<byte[]>();
        object _sendLock = new object();
        List<ArraySegment<byte>> _sendBufferList = new List<ArraySegment<byte>>();

        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
        SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

        int _disconnected = 0;

        public abstract void OnConnected(EndPoint endPoint);
        public abstract void OnRecv(ArraySegment<byte> buffer);
        public abstract void OnSend(int numOfBtyes);
        public abstract void OnDisconnected(EndPoint endPoint);

        public void Start(Socket socket)
        {
            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecv_Internal);
            _socket = socket;
            
            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecv_Internal);
            ArraySegment<byte> segement = _recvBuffer.WriteSegment;
            _recvArgs.SetBuffer(segement.Array, segement.Offset, segement.Count);

            RegisterRecv(_recvArgs);
        }

        public void Disconnect()
        {
            // 이미 disconnect됐음
            if(Interlocked.Exchange(ref _disconnected, 1) == 1)
                return;

            OnDisconnected(_socket.RemoteEndPoint);
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        public void Send(byte[] sendBuff)
        {
            lock(_sendLock)
            {
                _sendQueue.Enqueue(sendBuff);

                if (_sendBufferList.Count == 0)
                    RegisterSend();
            }
        }

        void RegisterSend()
        {
            _sendBufferList.Clear();
            while (_sendQueue.Count > 0)
            {
                byte[] buff = _sendQueue.Dequeue();
                _sendBufferList.Add(new ArraySegment<byte>(buff, 0, buff.Length));
            }

            _sendArgs.SetBuffer(null);
            _sendArgs.BufferList = _sendBufferList;

            bool pending = _socket.SendAsync(_sendArgs);
            if (!pending)
                OnSend_Internal(null, _sendArgs);
        }

        void OnSend_Internal(object sender, SocketAsyncEventArgs args)
        {
            lock(_sendLock)
            {
                ESocketError error = CheckSendError(args);
                if (error == ESocketError.None)
                {
                    try
                    {
                        _sendBufferList.Clear();

                        OnSend(args.BytesTransferred);

                        if (_sendQueue.Count > 0)
                            RegisterSend();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                else
                {
                    Disconnect();
                }
            }
        }

        void RegisterRecv(SocketAsyncEventArgs args)
        {
            _recvBuffer.Clean();

            bool pending = _socket.ReceiveAsync(args);
            if(!pending)
                OnRecv_Internal(null, args);
        }

        void OnRecv_Internal(object sender, SocketAsyncEventArgs args)
        {
            ESocketError error = CheckRecvError(args);
            if (error == ESocketError.None)
            {
                // TODO
                try
                {
                    OnRecv(new ArraySegment<byte>(args.Buffer, args.Offset, args.BytesTransferred));
                    RegisterRecv(args);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

            }
            else
            {
                //TODO
            }
        }

        ESocketError CheckRecvError(SocketAsyncEventArgs args)
        {
            if(args.BytesTransferred <= 0)
                return ESocketError.BytesTransferedZero;
            if(args.SocketError != SocketError.Success)
                return ESocketError.SocketErrorNotSuccess;
            if (args.Buffer == null)
                return ESocketError.BufferNull;

            return ESocketError.None;
        }

        ESocketError CheckSendError(SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred <= 0)
                return ESocketError.BytesTransferedZero;
            if (args.SocketError != SocketError.Success)
                return ESocketError.SocketErrorNotSuccess;

            return ESocketError.None;
        }
    }
}
