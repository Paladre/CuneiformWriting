using CuneiformWriting.Blocks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace CuneiformWriting
{
    public class BlockEntityClayTabletRenderer : IRenderer
    {
        public double RenderOrder => 0.5;
        public int RenderRange => 24;

        ICoreClientAPI capi;
        BlockEntityClayTablet be;

        LoadedTexture texture;

        public BlockEntityClayTabletRenderer(
            BlockEntityClayTablet be,
            ICoreClientAPI capi)
        {
            this.be = be;
            this.capi = capi;

            texture = new LoadedTexture(capi);

            RebuildTexture();
        }

        // ---------------------------------------------------------

        public void OnRenderFrame(
            float deltaTime,
            EnumRenderStage stage)
        {
            if (stage != EnumRenderStage.Opaque)
                return;

            if (be == null) return;

            DrawOnBlockFace();
        }

        // ---------------------------------------------------------

        void DrawOnBlockFace()
        {
            if (texture.TextureId == 0) return;

            // Bind texture for 3D
            capi.Render.BindTexture2d(texture.TextureId);

            // Build quad
            MeshData quad = QuadMesh();

            // Move to block position
            for (int i = 0; i < 4; i++)
            {
                quad.xyz[i * 3 + 0] += be.Pos.X + 0.5f;
                quad.xyz[i * 3 + 1] += be.Pos.Y + 0.5f;
                quad.xyz[i * 3 + 2] += be.Pos.Z + 0.51f;
            }

            MeshRef mr = capi.Render.UploadMesh(quad);

            capi.Render.RenderMesh(mr);

            mr.Dispose();
        }

        MeshData QuadMesh()
        {
            MeshData md = new MeshData();

            md.xyz = new float[]
            {
        -0.4f, -0.3f, 0.01f,
         0.4f, -0.3f, 0.01f,
         0.4f,  0.3f, 0.01f,
        -0.4f,  0.3f, 0.01f
            };

            md.Uv = new float[]
            {
        0,0,
        1,0,
        1,1,
        0,1
            };

            md.Indices = new int[] { 0, 1, 2, 0, 2, 3 };

            md.VerticesCount = 4;
            md.IndicesCount = 6;

            return md;
        }

        // ---------------------------------------------------------

        public void RebuildTexture()
        {
            if (be.strokes == null || be.strokes.Count == 0)
                return;

            TabletBaker baker = new TabletBaker(capi);

            byte[] png = baker.BakeToPng(be.strokes, 512);

            IBitmap bmp = capi.Render.BitmapCreateFromPng(png);

            capi.Render.LoadTexture(bmp, ref texture);
        }

        // ---------------------------------------------------------

        public void Dispose()
        {
            texture?.Dispose();
        }
    }
}
