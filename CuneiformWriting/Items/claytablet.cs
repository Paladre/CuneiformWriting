

using Cairo.Freetype;
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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CuneiformWriting.Items
{
    public class claytablet : Item, IHeldHandAnimOverrider
    {
        float baseThickness = 0.015f;

        Vec3f origin = new Vec3f(0,0,0);

        ICoreClientAPI cApi;

        int tabletH = 640;
        int tabletW = 480;

        private Dictionary<string, TabletRenderCache> _tabletCache = new Dictionary<string, TabletRenderCache>();

        public bool AllowHeldIdleHandAnim(Entity forEntity, ItemSlot slot, EnumHand hand)
        {
            return !isEditable(forEntity);
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            cApi = api as ICoreClientAPI;
            //this._emptyData = Utils.StrokesUtils.SerializeEmpty();
            //MeshData backMesh;
            //AssetLocation loc = new AssetLocation(CuneiformWritingModSystem.ModId + ":shapes/item/claytablet.json");
            //var shape = cApi.Assets.Get(loc).ToObject<Shape>();
            //cApi.Tesselator.TesselateItem(Clone(), out backMesh);
            //this._tabletMeshRef = cApi.Render.UploadMesh(backMesh);

            //if (Variant.ContainsKey("color") && Variant.ContainsKey("state"))
            //{
            //    //string color = Variant["color"];
            //    //string state = Variant["state"];
            //    //AssetLocation loc = CodeWithVariants(["color", "state"], [color, state]);
            //    //BlankVariant = api.World.GetItem(loc);
            //    //this._thiscache = new TabletRenderCache();
            //}
        }


        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
            
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {


            base.OnBeforeRender(capi, itemstack, target, ref renderinfo);

            if (target == EnumItemRenderTarget.Gui) return;

            byte[] cuneiformBytes = itemstack.Attributes.GetBytes("cuneiform");
            int contentHash = Fnv1a32(cuneiformBytes);

            string cacheId = $"{itemstack.Collectible.Code}:{(int)target}:{contentHash}";

            var baseModelRef = renderinfo.ModelRef;
            var baseMeshRef = baseModelRef?.meshrefs?.Length > 0 ? baseModelRef.meshrefs[0] : null;
            var baseTexId = baseModelRef?.textureids?.Length > 0 ? baseModelRef.textureids[0] : 0;

            if (!this._tabletCache.TryGetValue(cacheId, out TabletRenderCache cache))
            {
                cache = new TabletRenderCache();
                this._tabletCache[cacheId] = cache;
            }

            if (cache.ModelRef != null)
            {
                renderinfo.CullFaces = false;
                renderinfo.ModelRef = cache.ModelRef;
                renderinfo.NormalShaded = false;
            }

            bool hasWriting = cuneiformBytes != null && cuneiformBytes.Length > 0;
            bool shouldRefresh = itemstack.Attributes.TryGetBool("shouldTabletRefresh") == true;

            bool needsRebuild = shouldRefresh || (hasWriting && (cache.ModelRef == null || cache.LastHash != contentHash));
            if (!needsRebuild) return;

            RebuildBakedTexture(capi, itemstack, cache, cacheId);

            if (baseMeshRef != null && cache.MeshRef != null && cache.Texture != null)
            {
                cache.ModelRef = new MultiTextureMeshRef(
                    new[] { baseMeshRef, cache.MeshRef },
                    new[] { baseTexId, cache.Texture.TextureId }
                );
                cache.LastHash = contentHash;

                renderinfo.CullFaces = false;
                renderinfo.ModelRef = cache.ModelRef;
                renderinfo.NormalShaded = false;

                capi.Render.UpdateMesh(cache.ModelRef.meshrefs[1], cache.MeshData);
            }

            itemstack.Attributes.SetBool("shouldTabletRefresh", false);

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

        public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
        {
            EntityPlayer player = forEntity as EntityPlayer;
            if (player != null && player.LeftHandItemSlot.Empty)
            {
                return "tabletRead";
            }
            return base.GetHeldTpUseAnimation(activeHotbarSlot, forEntity);
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

        //private static bool canBeErased(EntityItem entityItem)
        //{
        //    if (entityItem.Itemstack?.Collectible.Attributes?.IsTrue("isClayTabletEditable") == true && entityItem.Itemstack.Attributes.HasAttribute("cuneiform")) return true;
        //    return false;
        //}

        void RebuildBakedTexture(ICoreClientAPI capi, ItemStack stack, TabletRenderCache cache, string cacheId)
        {

            int[] rgba = BakePixels(stack, tabletW, tabletH);

            // upload or update texture
            if (cache.Texture == null)
            {
                cache.Texture = new LoadedTexture(capi, 0, tabletW, tabletH);
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
            this._tabletCache[cacheId] = cache;
        }

        private static bool isEditable(Entity forEntity)
        {
            if (forEntity is EntityPlayer eplr && !eplr.RightHandItemSlot.Empty)
            {
                return eplr.RightHandItemSlot.Itemstack?.Collectible.Attributes?.IsTrue("isCuneiformStylus") == true && eplr.LeftHandItemSlot.Itemstack?.Collectible.Attributes?.IsTrue("isClayTabletEditable") == true;
            }
            return false;
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

            newPos.X = (int)(pos.X * tabletW);
            //Debug.WriteLine(newPos.X);
            newPos.Y = (int)(pos.Y * tabletH);
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

            float currentThickness = (baseThickness + s.thicknessDelta) / 4f;

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

        int[] BakePixels(ItemStack stack, int width, int height)
        {
            //AssetLocation texPath;
            //if (LastCodePart() == "raw")
            //{
            //    texPath = new AssetLocation("block/clay/" + LastCodePart(1) + "clay");
            //}
            //else
            //{
            //    texPath = new AssetLocation("block/clay/hardened/" + LastCodePart(1));
            //}

            //IAsset texAsset = cApi.Assets.TryGet(texPath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));

            //BitmapRef bmp = texAsset.ToBitmap(cApi);
            //int[] basePixels = bmp.Pixels;
            //int[] bgPixels = new int[24 * 32];

            //for (int i = 0; i < bgPixels.Length; i++)
            //{
            //    bgPixels[i] = basePixels[i % 24 + 32 * (i / 24)];
            //}

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

            if (!stack.Attributes.HasAttribute("cuneiform"))
            {
                return pixels;
            }

            //int coeff = height / 32;

            //for (int i = 0; i < pixels.Length; i++)
            //{

            //    int basepos = ((i / coeff) % 24) + 24 * (i / (coeff * width));
            //    if (basepos < 0 || basepos > bgPixels.Length)
            //    {
            //        cApi.Logger.Notification(" out of bounds basepos, value is : " + basepos + " at i = " + i);
            //    }

            //    pixels[i] = bgPixels[basepos];
            //    //pixels[i] = transparent;
            //}

            //  ink color 
            int ink = ColorUtil.ColorFromRgba(15,15,15,80);
            //int ink =
            //    (140 << 24) |
            //    (20 << 16) |
            //    (20 << 8) |
            //    255;

            //int ink = basePixels[24];

            List<CuneiformStroke> strokes = Utils.StrokesUtils.LoadStrokes(stack);
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

        static int Fnv1a32(byte[] data)
        {
            unchecked
            {
                const uint offset = 2166136261;
                const uint prime = 16777619;
                uint hash = offset;
                if (data != null)
                {
                    for (int i = 0; i < data.Length; i++)
                    {
                        hash ^= data[i];
                        hash *= prime;
                    }
                }
                // Keep it positive-ish for easier debugging; collisions are acceptable at this scale.
                return (int)hash;
            }
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            base.OnUnloaded(api);
            foreach (TabletRenderCache cache in this._tabletCache.Values)
            {
                if (cache.Texture != null)
                {
                    cache.Texture.Dispose();
                }
                if (cache.ModelRef != null)
                {
                    cache.ModelRef.Dispose();
                }
                if (cache.MeshRef != null) 
                { 
                    cache.MeshRef.Dispose(); 
                }
                if (cache.MeshData != null)
                {
                    cache.MeshData.Dispose();
                }
            }
            this._tabletCache.Clear();
        }

        
    }
}
