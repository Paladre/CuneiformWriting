

//using Cairo.Freetype;
//using CuneiformWriting.Gui;
//using System.Collections.Generic;
//using System.IO;
//using Vintagestory.API.Client;
//using Vintagestory.API.Common;
//using Vintagestory.API.Datastructures;
//using Vintagestory.API.MathTools;

//namespace CuneiformWriting.Items
//{
//    public class claytablet : Item
//    {
//        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
//        {
//            if (byEntity.World.Side == EnumAppSide.Client)
//            {
//                ICoreClientAPI capi = byEntity.World.Api as ICoreClientAPI;

//                new GuiCuneiform(capi, slot, this).TryOpen();

//                handHandling = EnumHandHandling.PreventDefault;
//                var png = slot.Itemstack.Attributes.GetBytes("bakedtex");
//                api.Logger.Notification("OPEN: baked exists=" + (png != null));
//            }
//        }

//        public List<CuneiformStroke> LoadStrokes(ItemSlot slot)
//        {
//            List<CuneiformStroke> result = new List<CuneiformStroke>();

//            if (slot?.Itemstack == null)
//            {
//                return result;
//            }

//            var stack = slot.Itemstack;

//            // ------------------------------------------------------------
//            // 1) Check if any saved data exists
//            // ------------------------------------------------------------
//            if (stack.Attributes == null ||
//                !stack.Attributes.HasAttribute("cuneiform"))
//            {
//                return result;   // brand new tablet
//            }

//            // ------------------------------------------------------------
//            // 2) Get raw bytes safely
//            // ------------------------------------------------------------
//            byte[] bytes = stack.Attributes.GetBytes("cuneiform");

//            if (bytes == null || bytes.Length == 0)
//            {
//                return result;
//            }

//            // ------------------------------------------------------------
//            // 3) Deserialize with protection against corruption
//            // ------------------------------------------------------------
//            TreeAttribute tree;

//            try
//            {
//                tree = TreeAttribute.CreateFromBytes(bytes);
//            }
//            catch
//            {
//                // Corrupted or old-format data → start fresh
//                return result;
//            }

//            // ------------------------------------------------------------
//            // 4) Read strokes
//            // ------------------------------------------------------------
//            int count = tree.GetInt("count");

//            for (int i = 0; i < count; i++)
//            {
//                CuneiformStroke s = new CuneiformStroke();

//                s.x = tree.GetFloat("x" + i);
//                s.y = tree.GetFloat("y" + i);
//                s.length = tree.GetFloat("l" + i);
//                s.angle = tree.GetFloat("a" + i);

//                // Default to wedge if field missing (old tablets)
//                s.typeofstroke = (StrokeType)
//                    tree.GetInt("t" + i, (int)StrokeType.thick);

//                result.Add(s);
//            }

//            return result;
//        }

//        //public override void OnBeforeRender(
//        //    ICoreClientAPI capi,
//        //    ItemStack itemstack,
//        //    EnumItemRenderTarget target,
//        //    ref ItemRenderInfo renderinfo)
//        //{
//        //    byte[] pngData = itemstack.Attributes.GetBytes("bakedtex");

//        //    if (pngData == null) return;

//        //    IBitmap bmp = capi.Render.BitmapCreateFromPng(pngData);

//        //    LoadedTexture tex = new LoadedTexture(capi);
//        //    capi.Render.LoadTexture(bmp, ref tex);

//        //    // 🔥 IMPORTANT: apply to mesh material
//        //    renderinfo.TextureId = tex.TextureId;
//        //    renderinfo.NormalShaded = false;
//        //}

//        public override void OnHeldRenderOrtho(ItemSlot inSlot, IClientPlayer byPlayer)
//        {
//            base.OnHeldRenderOrtho(inSlot, byPlayer);

//            byte[] png = inSlot.Itemstack.Attributes.GetBytes("bakedtex");
//            if (png == null) return;

//            ICoreClientAPI capi = byPlayer.Entity.Api as ICoreClientAPI;

//            IBitmap bmp = capi.Render.BitmapCreateFromPng(png);

//            LoadedTexture tex = new LoadedTexture(capi);
//            capi.Render.LoadTexture(bmp, ref tex);

//            capi.Render.GlToggleBlend(true, EnumBlendMode.Standard);

//            capi.Render.Render2DTexture(
//                tex.TextureId,
//                capi.Render.FrameWidth * 0.45f,
//                capi.Render.FrameHeight * 0.45f,
//                capi.Render.FrameWidth * 0.1f,
//                capi.Render.FrameHeight * 0.1f,
//                0.02f
//            );

//            capi.Render.GlToggleBlend(false);
//        }
//    }
//}
