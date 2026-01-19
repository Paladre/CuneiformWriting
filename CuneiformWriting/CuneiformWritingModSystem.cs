using CuneiformWriting.Items;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace CuneiformWriting
{
    public class CuneiformWritingModSystem : ModSystem
    {

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            api.RegisterItemClass("claytablet", typeof(claytablet));
            api.Network.RegisterChannel("cuneiform")
                .RegisterMessageType<PacketSaveTablet>();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification("Hello from template mod server side: " + Lang.Get("cuneiformwriting:hello"));
            api.Network.GetChannel("cuneiform")
                .SetMessageHandler<PacketSaveTablet>(OnSaveFromClient);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            Mod.Logger.Notification("Hello from template mod client side: " + Lang.Get("cuneiformwriting:hello"));
        }

        private void OnSaveFromClient(IServerPlayer player, PacketSaveTablet msg)
        {
            var slot = player.InventoryManager.ActiveHotbarSlot;

            if (slot == null) return;

            slot.Itemstack.Attributes.SetBytes("cuneiform", msg.Data);

            slot.MarkDirty();
        }

    }
}
