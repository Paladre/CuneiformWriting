using CuneiformWriting.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using static CuneiformWriting.CuneiformWritingModSystem;

namespace CuneiformWriting.Items
{
    //public class stylus : Item
    //{
    //    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
    //    {
    //        base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);

    //        ItemSlot leftSlot = byEntity.LeftHandItemSlot;

    //        if (!isRawClayTablet(leftSlot)) return;

            

    //    }

    //    public static bool isRawClayTablet(ItemSlot slot)
    //    {
    //        return slot.Itemstack?.Collectible.Attributes?.IsTrue("isClayTabletEditable") == true;
    //    }
    //}
}
