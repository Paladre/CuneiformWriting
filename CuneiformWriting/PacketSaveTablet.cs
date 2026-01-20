using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace CuneiformWriting
{
    // [ProtoContract]
    public class PacketSaveTablet
    {
        //[ProtoMember(1)]
        public BlockPos Pos;
        public byte[] Data;

        //[ProtoMember(2)]
        //public byte[] Baked;

    }
}
