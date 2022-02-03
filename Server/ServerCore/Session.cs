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

        RecvBuffer _recvBuffer = new RecvBuffer(65536);
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

            RegisterRecv();
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
            List<ArraySegment<byte>> buffs = new List<ArraySegment<byte>>();
            while (_sendQueue.Count > 0)
            {
                Packet packet = _sendQueue.Dequeue();
                ArraySegment<byte> buff = PacketConverter.Serialize(packet);
                buffs.Add(buff);
#if DEBUG
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[Send]: {packet.ToString()}");
#endif
                packetSizeSum += buff.Count;
            }

            ArraySegment<byte> sendBuffer = SendBufferHelper.Open(packetSizeSum);
            int cursor = 0;
            foreach(ArraySegment<byte> buff in buffs)
            {
                Array.Copy(buff.Array, 0, sendBuffer.Array, sendBuffer.Offset + cursor, buff.Count);
                cursor += buff.Count;
            }
            SendBufferHelper.Close(packetSizeSum);

            SocketError sockError;
            _socket.BeginSend(sendBuffer.Array, sendBuffer.Offset, sendBuffer.Count, SocketFlags.None, out sockError, new AsyncCallback(SendCallback), null);

            if(sockError != SocketError.Success)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"BeginSend Error: {sockError.ToString()}");
                Disconnect();
                return;
            }
        }

        void SendCallback(IAsyncResult ar)
        {
            try
            {
                int sendSize = _socket.EndSend(ar);

                if(sendSize == 0)
                {
                    Disconnect();
                    return;
                }

                lock (_sendLock)
                {
                    _sendAsync = false;
                    OnSend(sendSize);

                    if (_sendQueue.Count > 0)
                        RegisterSend();
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Exception] {e.Message}");

                if (_sendQueue.Count > 0)
                    RegisterSend();
            }
        }


        void OnSend_Internal(object sender, SocketAsyncEventArgs args)
        {
            if (!(args.BytesTransferred > 0 && args.SocketError == SocketError.Success))
            {
                Disconnect();
                return;
            }


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

        void RegisterRecv()
        {
            _recvBuffer.Clean();
            ArraySegment<byte> segement = _recvBuffer.WriteSegment;
            _recvArgs.SetBuffer(segement.Array, segement.Offset, segement.Count);

            _socket.BeginReceive(segement.Array, segement.Offset, segement.Count, SocketFlags.None, new AsyncCallback(RecvCallback), null);
            //bool pending = _socket.ReceiveAsync(args);
            //if (!pending)
            //{
            //    OnRecv_Internal(null, args);
            //}
        }

        void RecvCallback(IAsyncResult ar)
        {
            try
            {
                int receivedSize = _socket.EndReceive(ar);

                if (receivedSize == 0)
                {
                    RegisterRecv();
                    return;
                }

                if (!_recvBuffer.OnWrite(receivedSize))
                {
                    Disconnect();
                    return;
                }

                while (true)
                {
                    Packet packet;
                    ArraySegment<byte> readSegment = _recvBuffer.ReadSegment;
                    int deserializedSize;
                    EServerError error = PacketConverter.Deserialize(readSegment, out packet, out deserializedSize);
                    switch (error)
                    {
                        case EServerError.None:
                        {
                            OnRecv(packet);

                            if (!_recvBuffer.OnRead(deserializedSize))
                            {
                                Disconnect();
                                throw new Exception($"Recv Buffer Error");
                            }

                            RegisterRecv();
                            break;
                        }
                        case EServerError.PacketFragmentation:
                        {
                            RegisterRecv();
                            return;
                        }
                        case EServerError.UndefinedPacket:
                        default:
                        {
                            Disconnect();
                            throw new Exception($"Undefined Packet ServerError {error.ToString()}");
                        }
                    }
                }

            }
            catch (Exception e)
            {
                RegisterRecv();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Exception] {e.Message}");
            }
        }

        //void OnRecv_Internal(object sender, SocketAsyncEventArgs args)
        //{
        //    if (!(args.BytesTransferred > 0 && args.SocketError == SocketError.Success))
        //    {
        //        Disconnect();
        //        return;
        //    }

        //    if (args.SocketError != SocketError.Success)
        //    {
        //        Console.ForegroundColor = ConsoleColor.Red;
        //        Console.WriteLine($"[Socket Error] {args.SocketError.ToString()}");
        //        Disconnect();
        //        return;
        //    }

        //    if (args.BytesTransferred <= 0)
        //    {
        //        RegisterRecv();
        //        return;
        //    }

        //    try
        //    {
        //        while(true)
        //        {
        //            Packet packet;
        //            ArraySegment<byte> readSegment = _recvBuffer.ReadSegment;
        //            EServerError error = PacketConverter.Deserialize(new ArraySegment<byte>(readSegment.Array, readSegment.Offset, readSegment.Count), out packet);
        //            switch (error)
        //            {
        //                case EServerError.None:
        //                {
        //                    if (!_recvBuffer.OnWrite(args.BytesTransferred))
        //                    {
        //                        Disconnect();
        //                        return;
        //                    }

        //                    OnRecv(packet);

        //                    if (!_recvBuffer.OnRead(args.BytesTransferred))
        //                    {
        //                        Disconnect();
        //                        throw new Exception($"Recv Buffer Error");
        //                    }

        //                    RegisterRecv();
        //                    break;
        //                }
        //                case EServerError.PacketFragmentation:
        //                {
        //                    if (!_recvBuffer.OnWrite(args.BytesTransferred))
        //                    {
        //                        Disconnect();
        //                        return;
        //                    }

        //                    RegisterRecv();
        //                    return;
        //                }
        //                case EServerError.UndefinedPacket:
        //                default:
        //                {
        //                    if (!_recvBuffer.CancelWrite(args.BytesTransferred))
        //                    {
        //                        Disconnect();
        //                        return;
        //                    }

        //                    RegisterRecv();
        //                    throw new Exception($"Undefined ServerError {error.ToString()}");
        //                }
        //            }
        //        }

        //    }
        //    catch (Exception e)
        //    {
        //        Console.ForegroundColor = ConsoleColor.Red;
        //        Console.WriteLine($"[Exception] {e.Message}");
        //    }
        //}

    }
}
