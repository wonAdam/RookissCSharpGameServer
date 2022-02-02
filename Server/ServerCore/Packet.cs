using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public class PacketHeader
    {
        public ushort _packetSize;
        public ushort _packetId;
        public static int Size { 
            get => sizeof(ushort) + sizeof(ushort); 
        }
        public PacketHeader(Packet packet)
        {
            _packetSize = packet.PacketSize;
            _packetId = packet.PacketId;
        }

        public void Deserialize(ArraySegment<byte> buffer)
        {
            int bufferCursor = 0;
            _packetSize = BitConverter.ToUInt16(buffer.Array, bufferCursor);
            bufferCursor += sizeof(ushort);
            _packetId = BitConverter.ToUInt16(buffer.Array, bufferCursor);
            bufferCursor += sizeof(ushort);
        }

        public ArraySegment<byte> Serialize()
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[2 ^ 10]);
            int bufferCursor = 0;
            {
                byte[] member = BitConverter.GetBytes(_packetSize);
                Array.Copy(member, 0, buffer.Array, buffer.Offset + bufferCursor, sizeof(ushort));
                bufferCursor += sizeof(ushort);
            }

            {
                byte[] member = BitConverter.GetBytes(_packetId);
                Array.Copy(member, 0, buffer.Array, buffer.Offset + bufferCursor, sizeof(ushort));
                bufferCursor += sizeof(ushort);
            }

            return buffer;
        }
    }

    public static class PacketConverter
    {
        /// <summary>
        /// 패킷을 받아 
        /// 헤더와 패킷내용을 바이너리로 byte[]로 만들거 반환합니다.
        /// </summary>
        /// <param name="packet">packet without header</param>
        /// <returns></returns>
        public static ArraySegment<byte> Serialize(Packet packet)
        {
            PacketHeader header = new PacketHeader(packet);

            ArraySegment<byte> headerBuffer = header.Serialize();
            ArraySegment<byte> packetBuffer = packet.Serialize();

            byte[] resultBuffer = new byte[headerBuffer.Count + packetBuffer.Count];

            Array.Copy(headerBuffer.Array, 0, resultBuffer, 0, headerBuffer.Count);
            Array.Copy(packetBuffer.Array, 0, resultBuffer, headerBuffer.Count, packetBuffer.Count);

            return new ArraySegment<byte>(resultBuffer, 0, resultBuffer.Length);
        }

        /// <summary>
        /// 패킷(헤더 + 패킷내용)을 받아
        /// 패킷을 out으로 반환하거나 
        /// 패킷을 만들지 못했다면 EServerError를 반환합니다.
        /// </summary>
        /// <param name="packet">bytes packet with header</param>
        /// <returns></returns>
        public static EServerError Deserialize(ArraySegment<byte> packetBuffer, out Packet packet)
        {
            packet = null;

            // not enough size for parsing header
            if (packetBuffer.Count < PacketHeader.Size)
                return EServerError.PacketFragmentation;

            int readCursor = 0;
            ushort packetSize = BitConverter.ToUInt16(packetBuffer.Array, packetBuffer.Offset + readCursor);
            readCursor += sizeof(ushort);
            ushort packetId = BitConverter.ToUInt16(packetBuffer.Array, packetBuffer.Offset + readCursor);
            readCursor += sizeof(ushort);

            // not enough size for parsing packet
            if (packetBuffer.Count < packetSize + readCursor)
                return EServerError.PacketFragmentation;

            // TODO: packetId에 따라 Packet객체 생성후 Deserialize
            // 자동 생성 코드가 필요
            ArraySegment<byte> ContentBuffer = new ArraySegment<byte>(packetBuffer.Array, packetBuffer.Offset + readCursor, packetSize);

            switch (packetId)
            {
                case RQ_Ping.Id:
                {
                    packet = new RQ_Ping();
                    packet.Deserialize(ContentBuffer);
                    break;
                }
                case RS_Ping.Id:
                {
                    packet = new RS_Ping();
                    packet.Deserialize(ContentBuffer);
                    break;
                }
                case RQ_TestMsg.Id:
                {
                    packet = new RQ_TestMsg();
                    packet.Deserialize(ContentBuffer);
                    break;
                }
                case RS_TestMsg.Id:
                {
                    packet = new RS_TestMsg();
                    packet.Deserialize(ContentBuffer);
                    break;
                }
                default:
                    return EServerError.UndefinedPacket;
            }


            return EServerError.None;
        }

    }

    public abstract class Packet
    {
        public abstract ushort PacketId { get; }
        public abstract ushort PacketSize { get; }

        /// <summary>
        /// 패킷내용 부분을 ArraySegment로 만들어 반환합니다.
        /// 웬만해서는 Header를 같이 붙여 사용해야하므로 
        /// PacketHelper에서만 호출해야합니다.
        /// </summary>
        public abstract ArraySegment<byte> Serialize();
        /// <summary>
        /// 패킷내용 부분을 ArraySegment로 만들어 반환합니다.
        /// 웬만해서는 Header를 같이 붙여 사용해야하므로 
        /// PacketHelper에서만 호출해야합니다.
        /// </summary>
        public abstract void Deserialize(ArraySegment<byte> buffer);
    }
}
