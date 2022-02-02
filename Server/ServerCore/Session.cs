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
    public enum EServerError
    { 
        None,
        UndefinedPacket,
        PacketFragmentation,
    }


    public abstract class Session
    {
        Socket _socket;

        RecvBuffer _recvBuffer = new RecvBuffer(2^16);
        SendBuffer _sendBuffer = new SendBuffer(SendBufferHelper.ChunkSize);

        Queue<Packet> _sendQueue = new Queue<Packet>();
        object _sendLock = new object();
        bool _sendAsync = false;

        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
        SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

        int _disconnected = 0;

        public abstract void OnConnected(EndPoint endPoint);
        public abstract void OnRecv(Packet packet);
        public abstract void OnSend(int numOfBtyes);
        public abstract void OnDisconnected(EndPoint endPoint);

        public void Start(Socket socket)
        {
            _socket = socket;
            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecv_Internal);
            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecv_Internal);

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

        public void Send(Packet packet)
        {
            lock (_sendLock)
            {
                _sendQueue.Enqueue(packet);

                if (!_sendAsync)
                    RegisterSend();
            }
        }

        void RegisterSend()
        {
            _sendAsync = true;
            int packetSizeSum = 0;
            while (_sendQueue.Count > 0)
            {
                Packet packet = _sendQueue.Dequeue();
                ArraySegment<byte> buff = PacketConverter.Serialize(packet);
#if DEBUG
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[Send]: {packet.ToString()}");
#endif
                packetSizeSum += buff.Count;
            }

            ArraySegment<byte> sendBuffer = SendBufferHelper.Open(packetSizeSum);
            _sendArgs.SetBuffer(sendBuffer.Array, sendBuffer.Offset, sendBuffer.Count);
            SendBufferHelper.Close(packetSizeSum);

            bool pending = _socket.SendAsync(_sendArgs);
            if (!pending)
                OnSend_Internal(null, _sendArgs);
        }

        void OnSend_Internal(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError != SocketError.Success)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Socket Error] {args.SocketError.ToString()}");
                Disconnect();
                return;
            }

            if (args.BytesTransferred <= 0)
                return;

            lock (_sendLock)
            {
                try
                {
                    _sendAsync = false;
                    OnSend(args.BytesTransferred);

                    if (_sendQueue.Count > 0)
                        RegisterSend();
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[Exception] {e.Message}");
                }
            }
        }

        void RegisterRecv(SocketAsyncEventArgs args)
        {
            _recvBuffer.Clean();
            ArraySegment<byte> segement = _recvBuffer.WriteSegment;
            _recvArgs.SetBuffer(segement.Array, segement.Offset, segement.Count);
            
            bool pending = _socket.ReceiveAsync(args);
            if (!pending)
            {
                OnRecv_Internal(null, args);
            }
        }

        void OnRecv_Internal(object sender, SocketAsyncEventArgs args)
        {
            if(args.SocketError != SocketError.Success)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Socket Error] {args.SocketError.ToString()}");
                Disconnect();
                return;
            }

            if (args.BytesTransferred <= 0)
            {
                RegisterRecv(args);
                return;
            }

            try
            {
                while(true)
                {
                    Packet packet;
                    ArraySegment<byte> readSegment = _recvBuffer.ReadSegment;
                    EServerError error = PacketConverter.Deserialize(new ArraySegment<byte>(readSegment.Array, readSegment.Offset, readSegment.Count), out packet);
                    switch (error)
                    {
                        case EServerError.None:
                        {
                            if (!_recvBuffer.OnWrite(args.BytesTransferred))
                            {
                                Disconnect();
                                return;
                            }

                            OnRecv(packet);

                            if (!_recvBuffer.OnRead(args.BytesTransferred))
                            {
                                Disconnect();
                                throw new Exception($"Recv Buffer Error");
                            }

                            RegisterRecv(args);
                            break;
                        }
                        case EServerError.PacketFragmentation:
                        {
                            if (!_recvBuffer.OnWrite(args.BytesTransferred))
                            {
                                Disconnect();
                                return;
                            }

                            RegisterRecv(args);
                            return;
                        }
                        case EServerError.UndefinedPacket:
                        default:
                        {
                            if (!_recvBuffer.CancelWrite(args.BytesTransferred))
                            {
                                Disconnect();
                                return;
                            }

                            RegisterRecv(args);
                            throw new Exception($"Undefined ServerError {error.ToString()}");
                        }
                    }
                }

            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Exception] {e.Message}");
            }
        }

    }
}
