using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public class RQ_Ping : Packet
    {
        public override ushort PacketId { get => 0; }

        public override ushort PacketSize { get => 0; }
        public const ushort Id = 0;

        public override void Deserialize(ArraySegment<byte> buffer)
        {
        }

        public override ArraySegment<byte> Serialize()
        {
            return new ArraySegment<byte>(new byte[0]);
        }
    }

    public class RS_Ping : Packet
    {
        public override ushort PacketId { get => 1; }
        public override ushort PacketSize { get => 0; }
        public const ushort Id = 1;

        public override void Deserialize(ArraySegment<byte> buffer)
        {
        }

        public override ArraySegment<byte> Serialize()
        {
            return new ArraySegment<byte>(new byte[0]);
        }
    }

    public class RQ_TestMsg : Packet
    {
        public override ushort PacketId { get => 2; }

        public override ushort PacketSize
        {
            get => (ushort)(sizeof(int) + sizeof(char) * msg.Length);
        }
        public const ushort Id = 2;
        public string msg;

        public override void Deserialize(ArraySegment<byte> buffer)
        {
            int cursor = 0;
            int msgLength = BitConverter.ToInt32(buffer.Array, 0);
            cursor += sizeof(int);

            msg = "";
            for (int i = 0; i < msgLength; ++i)
            {
                char c = BitConverter.ToChar(buffer.Array, cursor);
                msg.Append(c);
                cursor += sizeof(char);
            }
        }

        public override ArraySegment<byte> Serialize()
        {
            byte[] buff = new byte[sizeof(int) + sizeof(char) * msg.Length];
            int cursor = 0;

            {
                byte[] msgLength = BitConverter.GetBytes(msg.Length);
                Array.Copy(msgLength, 0, buff, cursor, sizeof(int));
                cursor += sizeof(int);
            }

            {
                foreach (char c in msg)
                {
                    byte[] cBuff = BitConverter.GetBytes(c);
                    Array.Copy(cBuff, 0, buff, cursor, sizeof(char));
                    cursor += sizeof(char);
                }
            }

            return new ArraySegment<byte>(buff, 0, buff.Length);
        }
    }

    public class RS_TestMsg : Packet
    {
        public override ushort PacketId { get => 3; }
        public override ushort PacketSize
        {
            get => (ushort)(sizeof(int) + sizeof(char) * msg.Length);
        }
        public const ushort Id = 3;
        public string msg;

        public override void Deserialize(ArraySegment<byte> buffer)
        {
            int cursor = 0;
            int msgLength = BitConverter.ToInt32(buffer.Array, 0);
            cursor += sizeof(int);

            msg = "";
            for (int i = 0; i < msgLength; ++i)
            {
                char c = BitConverter.ToChar(buffer.Array, cursor);
                msg.Append(c);
                cursor += sizeof(char);
            }
        }

        public override ArraySegment<byte> Serialize()
        {
            byte[] buff = new byte[sizeof(int) + sizeof(char) * msg.Length];
            int cursor = 0;

            {
                byte[] msgLength = BitConverter.GetBytes(msg.Length);
                Array.Copy(msgLength, 0, buff, cursor, sizeof(int));
                cursor += sizeof(int);
            }

            {
                foreach(char c in msg)
                {
                    byte[] cBuff = BitConverter.GetBytes(c);
                    Array.Copy(cBuff, 0, buff, cursor, sizeof(char));
                    cursor += sizeof(char);
                }
            }

            return new ArraySegment<byte>(buff, 0, buff.Length);
        }

    }

}
