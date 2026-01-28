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
        CuneiformWritingModSystem modSystem;
        ICoreClientAPI capi;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            capi = api as ICoreClientAPI;
            modSystem = api.ModLoader.GetModSystem<CuneiformWritingModSystem>();
            // interactions
        }
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (byEntity.Controls.ShiftKey)
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
                return;
            }

            var player = (byEntity as EntityPlayer).Player;
            ItemSlot leftSlot = byEntity.LeftHandItemSlot;

            if (isRawClayTablet(leftSlot))
            {
                modSystem.BeginEdit(player, leftSlot);

                if (api.Side == EnumAppSide.Client)
                {
                    var dlg = new GuiCuneiform(api as ICoreClientAPI, leftSlot.Itemstack);
                    dlg.OnClosed += () =>
                    {
                        modSystem.EndEdit(player, dlg.data);
                    };
                    dlg.TryOpen();
                }
                handHandling = EnumHandHandling.PreventDefault;
                return;
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
