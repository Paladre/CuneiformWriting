using CuneiformWriting.Blocks;
using CuneiformWriting.Items;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace CuneiformWriting
{
    public class CuneiformWritingModSystem : ModSystem
    {
        ICoreServerAPI sapi;

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterBlockClass("cuneiformwriting.claytablet", typeof(BlockClayTablet));
            api.RegisterBlockEntityClass("cuneiformwriting.claytabletentity", typeof(BlockEntityClayTablet));
            api.RegisterItemClass("cuneiformwriting.stylus", typeof(stylus));
            api.Network.RegisterChannel("cuneiform")
                .RegisterMessageType<PacketSaveTablet>();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.sapi = api;

            api.Network.GetChannel("cuneiform")
                .SetMessageHandler<PacketSaveTablet>(OnSaveFromClient);
        }

        public override void StartClientSide(ICoreClientAPI capi)
        {
            
        }

        void OnSaveFromClient(IServerPlayer fromPlayer, PacketSaveTablet packet)
        {
            BlockEntity be = sapi.World.BlockAccessor
                .GetBlockEntity(packet.Pos);

            if (be is BlockEntityClayTablet tablet)
            {
                sapi.Event.EnqueueMainThreadTask(() =>
                {
                    tablet.ApplySerialized(packet.Data);

                    tablet.MarkDirty(true);
                    tablet.MarkDirtyAndRebuild();

                }, "tablet-save");
            }
        }

    }
}
