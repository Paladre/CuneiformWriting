using CuneiformWriting.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using static CuneiformWriting.CuneiformWritingModSystem;

namespace CuneiformWriting.Items
{
    public class stylus : Item
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);

            ItemSlot leftSlot = byEntity.LeftHandItemSlot;

            if (!isRawClayTablet(leftSlot)) return;

            if (byEntity.World.Side == EnumAppSide.Client)
            {
                ICoreClientAPI capi = byEntity.World.Api as ICoreClientAPI;

                new GuiCuneiform(capi, leftSlot, leftSlot.Itemstack.Item as claytablet).TryOpen();

                //capi.SendChatMessage("[Cuneiform Writing] Stylus OnHeldInteractStart");
                isWriting = true;

                handHandling = EnumHandHandling.PreventDefault;
            }


        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);

            ICoreClientAPI capi = byEntity.World.Api as ICoreClientAPI;
            //capi.SendChatMessage("[Cuneiform Writing] Stylus OnHeldInteractStop used for " + secondsUsed);
            isWriting = false;
        }

        public static bool isRawClayTablet(ItemSlot slot)
        {
            return slot.Itemstack?.Collectible.Attributes?.IsTrue("isClayTabletEditable") == true;
        }

        private static bool isWriting;
        

    }
}
