using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CuneiformWriting
{
    [ProtoContract]
    public class PacketSaveTablet
    {
        [ProtoMember(1)]
        public byte[] Data;

    }
}
