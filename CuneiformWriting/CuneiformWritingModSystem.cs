using CuneiformWriting.Items;
using CuneiformWriting.Render;
using HarmonyLib;
using System.Collections.Generic;
using System.Net.Sockets;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace CuneiformWriting
{
    public class CuneiformWritingModSystem : ModSystem
    {
        public static string ModId;

        ICoreAPI Api;

        private static Harmony Harmony { get; set; }

        Dictionary<string, ItemSlot> nowEditing = new Dictionary<string, ItemSlot>();

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            api.RegisterItemClass(CuneiformWritingModSystem.ModId + ".claytablet", typeof(claytablet));
            api.RegisterItemClass(CuneiformWritingModSystem.ModId + ".stylus", typeof(stylus));
            api.Network.RegisterChannel(CuneiformWritingModSystem.ModId).RegisterMessageType<PacketSaveTablet>();
            base.Start(api);
        }

        public override void StartServerSide(ICoreServerAPI sapi)
        {
            //Mod.Logger.Notification("Hello from template mod server side: " + Lang.Get("cuneiformwriting:hello"));
            base.StartServerSide(sapi);
            sapi.Network.GetChannel(CuneiformWritingModSystem.ModId).SetMessageHandler<PacketSaveTablet>(OnSaveFromClient);
            
            //sapi.Network.RegisterChannel(CuneiformWritingModSystem.ModId).RegisterMessageType(typeof(PacketSaveTablet)).SetMessageHandler<CuneiformWriting.PacketSaveTablet>(new NetworkClientMessageHandler<CuneiformWriting.PacketSaveTablet>(this.OnSaveFromClient));
        }

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            this.Api = api;
            CuneiformWritingModSystem.ModId = base.Mod.Info.ModID;
            CuneiformWritingModSystem.Harmony = new Harmony(CuneiformWritingModSystem.ModId);
            CuneiformWritingModSystem.Harmony.PatchAll();

        }

        public override void StartClientSide(ICoreClientAPI capi)
        {
            base.StartClientSide(capi);
        }

        private void OnSaveFromClient(IServerPlayer fromPlayer, PacketSaveTablet packet)
        {
            //var slot = player.InventoryManager.OffhandHotbarSlot;

            if (nowEditing.TryGetValue(fromPlayer.PlayerUID, out var slot))
            {
                EndEdit(fromPlayer, packet.Data);
            }

            //if (slot == null) return;

            //slot.Itemstack.Attributes.SetBytes("cuneiform", msg.Data);
            //slot.Itemstack.Attributes.SetBool("shouldTabletRefresh", true);

            //slot.MarkDirty();
        }

        //private void OnEraseFromClient(IServerPlayer player)
        //{
        //    var slot = player.InventoryManager.OffhandHotbarSlot;

        //    if (slot == null) return;

        //    slot.Itemstack.Attributes.RemoveAttribute("cuneiform");
        //    slot.Itemstack.Attributes.SetBool("shouldTabletRefresh", true);
        //    slot.MarkDirty();
        //}

        public void BeginEdit(IPlayer player, ItemSlot slot)
        {
            nowEditing[player.PlayerUID] = slot;
        }

        public void EndEdit(IPlayer player, byte[] data)
        {
            if (nowEditing.TryGetValue(player.PlayerUID, out var slot))
            {
                slot.Itemstack.Attributes.SetBytes("cuneiform", data);
                slot.Itemstack.Attributes.SetBool("shouldTabletRefresh", true);

                slot.MarkDirty();

                if (Api is ICoreClientAPI capi)
                {
                    capi.Network.GetChannel(CuneiformWritingModSystem.ModId).SendPacket(new PacketSaveTablet() { Data = data });
                }
            }
            nowEditing.Remove(player.PlayerUID);
        }

        //public void EraseTablet(IPlayer player)
        //{
        //    if (Api is ICoreClientAPI capi)
        //    {
        //        capi.Network.GetChannel(CuneiformWritingModSystem.ModId).SendPacket(new PacketSaveTablet());
        //        // play sound ? (see ModSystemEditableBook)
        //    }
        //}

        public class TabletRenderCache
        {
            public LoadedTexture Texture;
            public MeshData MeshData;
            public MeshRef MeshRef;
            public MultiTextureMeshRef ModelRef;
            public int LastHash;
            public bool AttachedOverlay;
        }

    }
}
