

using CuneiformWriting.Gui;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace CuneiformWriting.Items
{
    public class claytablet : Item
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (byEntity.World.Side == EnumAppSide.Client)
            {
                ICoreClientAPI capi = byEntity.World.Api as ICoreClientAPI;

                new GuiCuneiform(capi, slot, this).TryOpen();

                handHandling = EnumHandHandling.PreventDefault;
                api.Logger.Notification(
                    "OPEN -> slot hash:" + slot.Itemstack?.GetHashCode()
                );
            }
        }

        //public void SaveStrokes(ItemSlot slot, List<CuneiformStroke> strokes)
        //{
        //    ItemStack stack = slot.Itemstack;
        //    TreeAttribute tree = new TreeAttribute();

        //    tree.SetInt("count", strokes.Count);

        //    for (int i = 0; i < strokes.Count; i++)
        //    {
        //        var s = strokes[i];

        //        tree.SetFloat("x" + i, s.x);
        //        tree.SetFloat("y" + i, s.y);
        //        tree.SetFloat("l" + i, s.length);
        //        tree.SetFloat("a" + i, s.angle);
        //        tree.SetInt("t" + i, (int)s.typeofstroke);
        //    }

        //    stack.Attributes["cuneiform"] = tree;

        //    slot.MarkDirty();

        //    slot.Inventory?.DidModifyItemSlot(slot);

        //    api.Logger.Notification(
        //        "SAVE -> slot id: " + slot.Inventory?.InventoryID +
        //        " hash:" + slot.Itemstack?.GetHashCode()
        //    );
        //}

        //public List<CuneiformStroke> LoadStrokes(ItemSlot slot)
        //{
        //    //ItemStack stack = slot.Itemstack;
        //    var result = new List<CuneiformStroke>();

        //    //if (!stack.Attributes.HasAttribute("cuneiform"))
        //    //    return result;

        //    //var tree = stack.Attributes["cuneiform"] as TreeAttribute;

        //    var stack = slot.Itemstack;

        //    byte[] bytes = stack.Attributes.GetBytes("cuneiform");

        //    if (bytes != null || bytes.Length == 0) return new List<CuneiformStroke>();

        //    TreeAttribute tree = TreeAttribute.CreateFromBytes(bytes);

        //    int count = tree.GetInt("count");

        //    for (int i = 0; i < count; i++)
        //    {
        //        result.Add(new CuneiformStroke
        //        {
        //            x = tree.GetFloat("x" + i),
        //            y = tree.GetFloat("y" + i),
        //            length = tree.GetFloat("l" + i),
        //            angle = tree.GetFloat("a" + i),
        //            typeofstroke = (StrokeType)tree.GetInt("t" + i)
        //        });
        //    }

        //    return result;
        //}

        public List<CuneiformStroke> LoadStrokes(ItemSlot slot)
        {
            List<CuneiformStroke> result = new List<CuneiformStroke>();

            if (slot?.Itemstack == null)
            {
                return result;
            }

            var stack = slot.Itemstack;

            // ------------------------------------------------------------
            // 1) Check if any saved data exists
            // ------------------------------------------------------------
            if (stack.Attributes == null ||
                !stack.Attributes.HasAttribute("cuneiform"))
            {
                return result;   // brand new tablet
            }

            // ------------------------------------------------------------
            // 2) Get raw bytes safely
            // ------------------------------------------------------------
            byte[] bytes = stack.Attributes.GetBytes("cuneiform");

            if (bytes == null || bytes.Length == 0)
            {
                return result;
            }

            // ------------------------------------------------------------
            // 3) Deserialize with protection against corruption
            // ------------------------------------------------------------
            TreeAttribute tree;

            try
            {
                tree = TreeAttribute.CreateFromBytes(bytes);
            }
            catch
            {
                // Corrupted or old-format data → start fresh
                return result;
            }

            // ------------------------------------------------------------
            // 4) Read strokes
            // ------------------------------------------------------------
            int count = tree.GetInt("count");

            for (int i = 0; i < count; i++)
            {
                CuneiformStroke s = new CuneiformStroke();

                s.x = tree.GetFloat("x" + i);
                s.y = tree.GetFloat("y" + i);
                s.length = tree.GetFloat("l" + i);
                s.angle = tree.GetFloat("a" + i);

                // Default to wedge if field missing (old tablets)
                s.typeofstroke = (StrokeType)
                    tree.GetInt("t" + i, (int)StrokeType.thick);

                result.Add(s);
            }

            return result;
        }
    }
}
