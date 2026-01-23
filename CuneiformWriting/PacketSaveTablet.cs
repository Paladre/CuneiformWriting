using ProtoBuf;

namespace CuneiformWriting
{
    [ProtoContract]
    public class PacketSaveTablet
    {
        [ProtoMember(1)]
        public byte[] Data;

    }
}
