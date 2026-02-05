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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CuneiformWriting.Items
{
    public class stylus : Item
    {
        CuneiformWritingModSystem modSystem;
        ICoreClientAPI capi;
        WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            capi = api as ICoreClientAPI;
            modSystem = api.ModLoader.GetModSystem<CuneiformWritingModSystem>();
            // interactions

            interactions = ObjectCacheUtil.GetOrCreate(api, "stylusInteractions", () =>
            {
                List<ItemStack> stylusStacks = new List<ItemStack>();
                foreach (var collobj in api.World.Collectibles)
                {
                    if (collobj.Attributes != null && collobj.Attributes.IsTrue("isClayTabletEditable"))
                    {
                        stylusStacks.Add(new ItemStack(collobj));
                    }
                }

                return new WorldInteraction[]
                {
                    new WorldInteraction
                    {
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = stylusStacks.ToArray(),
                        ActionLangCode = "cuneiformwriting:heldhelp-writetablet"
                    }
                };
            }
            );

        }
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
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

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            ItemSlot leftslot = byEntity.LeftHandItemSlot;

            //if (isErasing && canBeErased(leftslot) && secondsUsed > 1.5)
            //{
            //    leftslot.Itemstack = leftslot.Itemstack.GetEmptyClone();
            //    //leftslot.Itemstack.Attributes.SetBool("shouldTabletRefresh", true);
            //    isErasing = false;
            //    leftslot.MarkDirty();
            //    return true;
            //}
            return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
        }

        //public override void OnGroundIdle(EntityItem entityItem)
        //{
        //    base.OnGroundIdle(entityItem);

        //    IWorldAccessor world = entityItem.World;
        //    if (world.Side != EnumAppSide.Server) return;

        //    if (!canBeErased(entityItem)) return;

        //    if (entityItem.Swimming && world.Rand.NextDouble() < 0.01)
        //    {
        //        entityItem.Itemstack.Attributes.RemoveAttribute("cuneiform");
        //        entityItem.Itemstack.Attributes.SetBool("shouldTabletRefresh", true);
        //    }
        //}

        private static bool canBeErased(ItemSlot slot)
        {
            if (isRawClayTablet(slot) && slot.Itemstack.Attributes.HasAttribute("cuneiform")) return true;
            return false;
        }

        public static bool isRawClayTablet(ItemSlot slot)
        {
            return slot.Itemstack?.Collectible.Attributes?.IsTrue("isClayTabletEditable") == true;
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return interactions.Append(base.GetHeldInteractionHelp(inSlot));
        }

    }
}
