

using CuneiformWriting.Gui;
using HarmonyLib;
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
    public class claytablet : Item, IHeldHandAnimOverrider
    {
        float baseThickness = 0.015f;

        Vec3f origin = new Vec3f(0,0,0);

        public bool AllowHeldIdleHandAnim(Entity forEntity, ItemSlot slot, EnumHand hand)
        {
            return !isEditable(forEntity);
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);

            ItemSlot leftSlot = byEntity.LeftHandItemSlot;

            if (isEditable(slot, leftSlot) && !byEntity.Controls.ShiftKey)
            {
                if (byEntity.World.Side == EnumAppSide.Client)
                {
                    ICoreClientAPI capi = byEntity.World.Api as ICoreClientAPI;

                    new GuiCuneiform(capi, slot, this).TryOpen();

                    handHandling = EnumHandHandling.PreventDefault;
                }
            }
            
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {

            base.OnBeforeRender(capi, itemstack, target, ref renderinfo);

            // Only render baked version in world / hand
            if (target == EnumItemRenderTarget.Gui) return;

            

            byte[] data = itemstack.Attributes.GetBytes("cuneiform");

            if (data == null || data.Length == 0)
            {

                return;
            }

            

            var system = capi.ModLoader.GetModSystem<CuneiformWritingModSystem>();
            var cacheDict = system.TabletCache;

            

            int hash = HashBytes(data);

            //int cacheId = GameMath.MurmurHash3(itemstack.Collectible.Id, hash, 1337);

            string cacheId = itemstack.TempAttributes.GetString("tabletCacheId");

            if (cacheId == null)
            {
                cacheId = Guid.NewGuid().ToString();
                itemstack.TempAttributes.SetString("tabletCacheId", cacheId);
            }

            capi.Logger.Notification($"[TabletRender] cacheId={cacheId} hash={hash}");

            //TabletRenderCache cache;

            //if (!cacheDict.TryGetValue(cacheId, out cache))
            //{
            //    cache = new TabletRenderCache();
            //    cacheDict[cacheId] = cache;
            //}

            TabletRenderCache cache;

            if (!cacheDict.TryGetValue(cacheId, out cache))
            {
                cache = new TabletRenderCache();
                cacheDict[cacheId] = cache;
            }

            // rebake if changed
            if (hash != cache.LastHash)
            {
                cache.LastHash = hash;

                cache.AttachedOverlay = false;

                RebuildBakedTexture(capi, itemstack, cache);

                capi.ShowChatMessage("[TabletRender] hash and LastHash different, Target is " + target + " for item : " + itemstack.Collectible.Code);
            }

            if (cache.ModelRef == null && data != null)
            {
                RebuildBakedTexture(capi, itemstack, cache);
                capi.ShowChatMessage("[TabletRender] Modelref is null, Target is " + target + "for item : " + itemstack.Collectible.Code);
            }

            if (!cache.AttachedOverlay)
            {
                cache.ModelRef = new MultiTextureMeshRef(
                    new[] { renderinfo.ModelRef.meshrefs[0], cache.MeshRef },
                    new[] { renderinfo.ModelRef.textureids[0], cache.Texture.TextureId }
                );

                cache.AttachedOverlay = true;
                capi.ShowChatMessage("[TabletRender] New Model Ref for " + itemstack.Collectible.Code);
            }

            

            renderinfo.ModelRef = cache.ModelRef;
            //capi.Render.BindTexture2d(cache.Texture.TextureId);
            //capi.Render.GlGenerateTex2DMipmaps();
            renderinfo.NormalShaded = true;
            renderinfo.CullFaces = true;

        }

        public override string GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
        {
            EntityPlayer player = forEntity as EntityPlayer;
            if (player != null && isEditable(forEntity))
            {
                return "claytabletDrawReady";
            }
            return base.GetHeldTpIdleAnimation(activeHotbarSlot, forEntity, hand);
        }

        void RebuildBakedTexture(ICoreClientAPI capi, ItemStack stack, TabletRenderCache cache)
        {
            // create new image pixels
            int sizeX = 1200;
            int sizeY = 1600;

            int[] rgba = BakePixels(stack, sizeX, sizeY);

            // upload or update texture
            if (cache.Texture == null)
            {
                cache.Texture = new LoadedTexture(capi, 0, sizeX, sizeY);
            }

            capi.Render.LoadOrUpdateTextureFromRgba(
                rgba,
                false,
                0,
                ref cache.Texture
            );
            capi.Render.BindTexture2d(cache.Texture.TextureId);
            capi.Render.GlGenerateTex2DMipmaps();

            // mesh
            if (cache.MeshData == null)
            {
                MeshData quad = QuadMeshUtil.GetQuad();

                quad.Rotate(origin, GameMath.PIHALF, 0, 0);
                quad.Scale(origin, 0.375f, 1f, 0.5f);
                quad.Translate(new Vec3f(0.5f, 0.0626f, 0.5f));

                quad.Flags = new int[quad.VerticesCount];
                quad.Rgba = new byte[quad.VerticesCount * 4];
                quad.Rgba.Fill(byte.MaxValue);

                quad.TextureIndices =
                    new byte[quad.VerticesCount / quad.VerticesPerFace];

                quad.AddTextureId(cache.Texture.TextureId);
                quad.AddRenderPass(0);

                cache.MeshData = quad;
                cache.MeshRef = capi.Render.UploadMesh(quad);

                cache.ModelRef = new MultiTextureMeshRef(
                    new[] { cache.MeshRef },
                    new[] { cache.Texture.TextureId }
                );
            }
            else
            {
                // Update texture id
                cache.MeshData.TextureIds[0] = cache.Texture.TextureId;

                capi.Render.UpdateMesh(cache.MeshRef, cache.MeshData);
            }
        }

        private static bool isEditable(ItemSlot slot, ItemSlot leftSlot)
        {
            return leftSlot.Itemstack?.Collectible.Attributes?.IsTrue("isCuneiformStylus") == true && slot.Itemstack?.Collectible.Attributes?.IsTrue("isClayTabletEditable") == true;
        }

        private static bool isEditable(Entity forEntity)
        {
            if (forEntity is EntityPlayer eplr && !eplr.RightHandItemSlot.Empty)
            {
                return eplr.RightHandItemSlot.Itemstack?.Collectible.Attributes?.IsTrue("isCuneiformStylus") == true && eplr.LeftHandItemSlot.Itemstack?.Collectible.Attributes?.IsTrue("isClayTabletEditable") == true;
            }
            return false;
        }


        public List<CuneiformStroke> LoadStrokes(ItemStack stack)
        {
            List<CuneiformStroke> result = new List<CuneiformStroke>();

            if (stack == null)
            {
                return result;
            }

            if (stack.Attributes == null ||
                !stack.Attributes.HasAttribute("cuneiform"))
            {
                return result;
            }

            byte[] bytes = stack.Attributes.GetBytes("cuneiform");

            if (bytes == null || bytes.Length == 0)
            {
                return result;
            }

            TreeAttribute tree;

            try
            {
                tree = TreeAttribute.CreateFromBytes(bytes);
            }
            catch
            {
                return result;
            }

            int count = tree.GetInt("count");

            for (int i = 0; i < count; i++)
            {
                CuneiformStroke s = new CuneiformStroke();

                s.x1 = tree.GetFloat("x1" + i);
                s.y1 = tree.GetFloat("y1" + i);
                s.x2 = tree.GetFloat("x2" + i);
                s.y2 = tree.GetFloat("y2" + i);
                s.thicknessDelta = tree.GetFloat("thicknessDelta" + i);
                s.typeofstroke = (StrokeType)tree.GetInt("t" + i, (int)StrokeType.stroke);

                result.Add(s);
            }

            return result;
        }

        void FillPolygon(int[] pixels, int W, int H, Vec2i[] poly, int color)
        {
            if (poly == null || poly.Length < 3) return;

            int minY = poly.Min(p => p.Y);
            int maxY = poly.Max(p => p.Y);

            minY = GameMath.Clamp(minY, 0, H - 1);
            maxY = GameMath.Clamp(maxY, 0, H - 1);

            for (int y = minY; y <= maxY; y++)
            {
                List<int> nodes = new();

                for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                {
                    int yi = poly[i].Y;
                    int yj = poly[j].Y;

                    if ((yi < y && yj >= y) || (yj < y && yi >= y))
                    {
                        int xi = poly[i].X;
                        int xj = poly[j].X;

                        int x = xi + (y - yi) * (xj - xi) / (yj - yi);
                        nodes.Add(x);
                    }
                }

                nodes.Sort();

                for (int i = 0; i + 1 < nodes.Count; i += 2)
                {
                    int x0 = GameMath.Clamp(nodes[i], 0, W - 1);
                    int x1 = GameMath.Clamp(nodes[i + 1], 0, W - 1);

                    for (int x = x0; x <= x1; x++)
                    {
                        pixels[x + y * W] = color;
                    }
                }
            }
        }

        Vec2i LocalFloatPosToInt(Vec2f pos)
        {
            Vec2i newPos = new Vec2i();

            newPos.X = (int)(pos.X * 1200f);
            //Debug.WriteLine(newPos.X);
            newPos.Y = (int)(pos.Y * 1600f);
            //Debug.WriteLine(newPos.Y);


            return newPos;
        }

        Vec2i[] GetPolygonIndices(CuneiformStroke s)
        {
            int i = s.typeofstroke == StrokeType.hook ? 4 : 3;
            Vec2i[] indices = new Vec2i[i];

            Vec2f start = new Vec2f(s.x1, s.y1);
            Vec2f end = new Vec2f(s.x2, s.y2);
            Vec2f delta = end - start;
            //float hookLength = s.x2 - s
            

            float dx = delta.X * 3f;
            float dy = delta.Y * 4f;

            float currentThickness = (baseThickness + s.thicknessDelta) / 2f;

            float angleRad = (float)Math.Atan2(dy, dx);

            float cos = (float)Math.Cos(angleRad);
            float sin = (float)Math.Sin(angleRad);

            if (s.typeofstroke == StrokeType.stroke)
            {
                indices[0] = LocalFloatPosToInt(new Vec2f(start.X + currentThickness * sin, start.Y - currentThickness * cos));
                indices[1] = LocalFloatPosToInt(new Vec2f(start.X - currentThickness * sin, start.Y + currentThickness * cos));
                indices[2] = LocalFloatPosToInt(end);
            }
            else
            {
                indices[0] = LocalFloatPosToInt(start);
                indices[1] = LocalFloatPosToInt(new Vec2f(end.X, start.Y - (delta.X * 3f/4f)));
                indices[2] = LocalFloatPosToInt(new Vec2f(start.X + 0.25f * delta.X, start.Y));
                indices[3] = LocalFloatPosToInt(new Vec2f(end.X, start.Y + (delta.X * 3f / 4f)));

            }

            return indices;
        }

        int HashBytes(byte[] data)
        {
            unchecked
            {
                int hash = 17;

                for (int i = 0; i < data.Length; i++)
                {
                    hash = hash * 31 + data[i];
                }

                return hash;
            }
        }

        int[] BakePixels(ItemStack stack, int width, int height)
        {
            List<CuneiformStroke> strokes = LoadStrokes(stack);


            int[] pixels = new int[width * height];

            int transparent = 0x00000000;

            //int clay =
            //    (255 << 24) |   // A
            //    (210 << 16) |   // R
            //    (180 << 8) |   // G
            //    140;

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = transparent;
            }

            //  ink color 
            int ink =
                (140 << 24) |
                (20 << 16) |
                (20 << 8) |
                20;

            //  rasterize strokes 
            foreach (var s in strokes)
            {
                Vec2i[] poly = GetPolygonIndices(s);

                if (poly == null || poly.Length < 3)
                    continue;

                FillPolygon(pixels, width, height, poly, ink);
            }

            return pixels;
        }

        
    }
}
