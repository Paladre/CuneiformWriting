using CuneiformWriting.Gui;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace CuneiformWriting.Items
{
    public class stylus : Item
    {
        public override void OnHeldInteractStart(
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel,
        bool firstEvent,
        ref EnumHandHandling handling)
        {
            // Only trigger when offhand has tablet block item
            var offhand = byEntity.LeftHandItemSlot;

            if (offhand?.Itemstack?.Block is Blocks.BlockClayTablet)
            {
                if (byEntity.World.Side == EnumAppSide.Client)
                {
                    var capi = byEntity.World.Api as ICoreClientAPI;

                    //new GuiCuneiform(
                    //    capi,
                    //    null,               // no itemslot
                    //    offhand.Itemstack   // edit as portable tablet
                    //).TryOpen();
                }

                handling = EnumHandHandling.PreventDefault;
                return;
            }

            return;
        }

    }
}
