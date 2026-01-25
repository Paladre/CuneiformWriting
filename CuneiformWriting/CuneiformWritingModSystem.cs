using CuneiformWriting.Items;
using CuneiformWriting.Render;
using HarmonyLib;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace CuneiformWriting
{
    public class CuneiformWritingModSystem : ModSystem
    {
        Harmony harmony;

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            api.RegisterItemClass(Mod.Info.ModID + ".claytablet", typeof(claytablet));
            api.RegisterItemClass(Mod.Info.ModID + ".stylus", typeof(stylus));

            api.Network.RegisterChannel("cuneiform")
                .RegisterMessageType<PacketSaveTablet>();
            harmony = new Harmony(Mod.Info.ModID + ".pitkiln");
            harmony.PatchAll();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            //Mod.Logger.Notification("Hello from template mod server side: " + Lang.Get("cuneiformwriting:hello"));
            api.Network.GetChannel("cuneiform")
                .SetMessageHandler<PacketSaveTablet>(OnSaveFromClient);
        }

        public override void StartClientSide(ICoreClientAPI capi)
        {
            base.StartClientSide(capi);
            capi.RegisterEntityRendererClass("claytablet", typeof(TabletItemRenderer));
        }

        private void OnSaveFromClient(IServerPlayer player, PacketSaveTablet msg)
        {
            var slot = player.InventoryManager.ActiveHotbarSlot;

            if (!player.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible.Attributes?.IsTrue("isClayTabletEditable") == true)
            {
                slot = player.InventoryManager.OffhandHotbarSlot;
            }
            


            if (slot == null) return;

            slot.Itemstack.Attributes.SetBytes("cuneiform", msg.Data);

            slot.MarkDirty();
        }

        //public override void AssetsFinalize(ICoreAPI api)
        //{
        //    foreach (Item i in api.World.Items)
        //    {
                
        //    }
        //}

        byte[] SerializeEmpty()
        {
            TreeAttribute tree = new TreeAttribute();

            tree.SetInt("count", 1);

            tree.SetFloat("x1", 0);
            tree.SetFloat("y1", 0);
            tree.SetFloat("x2", 0);
            tree.SetFloat("y2", 0);
            tree.SetInt("t", 0);

            return tree.ToBytes();
        }

        public Dictionary<string, TabletRenderCache> TabletCache = new();

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
