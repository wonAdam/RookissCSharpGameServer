using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore.Packets
{
    public class RQ_Ping : Packet
    {
        public override ushort PacketId { get => Id; }

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
        public override ushort PacketId { get => Id; }
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


}
