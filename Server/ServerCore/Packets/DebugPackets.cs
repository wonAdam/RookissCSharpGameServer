using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore.Packets
{

    public class RQ_TestMsg : Packet
    {
        public override ushort PacketId { get => Id; }

        public override ushort PacketSize
        {
            get => (ushort)(sizeof(int) + sizeof(char) * msg.Length);
        }
        public const ushort Id = 2;
        public string msg;

        public override void Deserialize(ArraySegment<byte> buffer)
        {
            int cursor = 0;
            int msgByteLength = BitConverter.ToInt32(buffer.Array, buffer.Offset);
            cursor += sizeof(int);

            msg = Encoding.UTF8.GetString(buffer.Array, buffer.Offset + cursor, msgByteLength);
        }

        public override ArraySegment<byte> Serialize()
        {
            int msgByteLength = Encoding.UTF8.GetBytes(msg).Length;
            byte[] buff = new byte[sizeof(int) + msgByteLength];
            int cursor = 0;
            {
                byte[] msgLengthMember = BitConverter.GetBytes(msgByteLength);
                Array.Copy(msgLengthMember, 0, buff, cursor, sizeof(int));
                cursor += sizeof(int);
            }

            {
                byte[] msgBuff = Encoding.UTF8.GetBytes(msg);
                Array.Copy(msgBuff, 0, buff, cursor, msgBuff.Length * sizeof(byte));
            }

            return new ArraySegment<byte>(buff, 0, buff.Length);
        }
    }

    public class RS_TestMsg : Packet
    {
        public override ushort PacketId { get => Id; }
        public override ushort PacketSize
        {
            get => (ushort)(sizeof(int) + sizeof(char) * msg.Length);
        }
        public const ushort Id = 3;
        public string msg;

        public override void Deserialize(ArraySegment<byte> buffer)
        {
            int cursor = 0;
            int msgByteLength = BitConverter.ToInt32(buffer.Array, buffer.Offset);
            cursor += sizeof(int);

            msg = Encoding.UTF8.GetString(buffer.Array, buffer.Offset + cursor, msgByteLength);
        }

        public override ArraySegment<byte> Serialize()
        {
            int msgByteLength = Encoding.UTF8.GetBytes(msg).Length;
            byte[] buff = new byte[sizeof(int) + msgByteLength];
            int cursor = 0;
            {
                byte[] msgLengthMember = BitConverter.GetBytes(msgByteLength);
                Array.Copy(msgLengthMember, 0, buff, cursor, sizeof(int));
                cursor += sizeof(int);
            }

            {
                byte[] msgBuff = Encoding.UTF8.GetBytes(msg);
                Array.Copy(msgBuff, 0, buff, cursor, msgBuff.Length * sizeof(byte));
            }

            return new ArraySegment<byte>(buff, 0, buff.Length);
        }

    }
}
