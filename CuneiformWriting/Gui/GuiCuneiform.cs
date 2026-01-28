using Cairo;
using CuneiformWriting.Items;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace CuneiformWriting.Gui
{
    public enum StrokeType
    {
        stroke,
        hook
    }

    public class GuiCuneiform : GuiDialog
    {
        ICoreClientAPI capi;

        LoadedTexture strokeTexture;
        LoadedTexture hookTexture;
        LoadedTexture tabletbg;

        List<MeshRef> strokeMeshes = new List<MeshRef>();
        MeshRef ghostMesh;

        ElementBounds tabletBounds;

        List<CuneiformStroke> strokes = new List<CuneiformStroke>();
        public byte[] data;

        Vec2f strokeStart;
        Vec2f strokeEnd;

        bool isDrawing = false;

        float currentLength;
        float currentAngle;

        float baseThickness = 0.015f;
        float thicknessDelta;

        StrokeType type = StrokeType.stroke;

        public GuiCuneiform(ICoreClientAPI capi, ItemStack stack) : base(capi)
        {
            this.capi = capi;

            string code = stack.Collectible.Code;
            string color = code.Split('-')[1];
            string bgpath = "game:textures/block/clay/" + color + "clay.png";

            strokeTexture = new LoadedTexture(capi);
            capi.Render.GetOrLoadTexture(
                new AssetLocation("cuneiformwriting:textures/gui/stroke.png"),
                ref strokeTexture
            );
            hookTexture = new LoadedTexture(capi);
            capi.Render.GetOrLoadTexture(
                new AssetLocation("cuneiformwriting:textures/gui/hook.png"),
                ref hookTexture
            );
            tabletbg = new LoadedTexture(capi);
            capi.Render.GetOrLoadTexture(
                new AssetLocation(bgpath),
                ref tabletbg
            );
            if (stack.Attributes.HasAttribute("cuneiform"))
            {
                strokes = Utils.StrokesUtils.LoadStrokes(stack);
            }
        }

        public override string ToggleKeyCombinationCode => null;

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();

            float scale = capi.Settings.Float["guiScale"];

            float screenW = capi.Render.FrameWidth / scale;
            float screenH = capi.Render.FrameHeight / scale;

            float tabletH = screenH * 0.75f;
            float tabletW = tabletH * (3f / 4f);

            float posX = (screenW - tabletW) / 2f;
            float posY = (screenH - tabletH) / 2f;

            ElementBounds rootBounds =
                ElementBounds.Fixed(0, 0, screenW, screenH);

            tabletBounds =
                ElementBounds.Fixed(posX, posY, tabletW, tabletH);

            GuiElementCustomDraw drawElement =
                new GuiElementCustomDraw(capi, tabletBounds, OnDrawTablet);

            SingleComposer = capi.Gui.CreateCompo("cuneiform", rootBounds)
                .AddInteractiveElement(drawElement)
                .Compose();

            RebuildMeshesFromStrokes();
        }

        public override void OnGuiClosed()
        {
            //SendSaveToServer();

            foreach (var mesh in strokeMeshes)
                mesh.Dispose();
            data = SerializeStrokes();

            base.OnGuiClosed();
        }

        public override void OnMouseDown(MouseEvent e)
        {

            if (!isDrawing)
            {
                if (e.Button == EnumMouseButton.Right)
                {
                    type = StrokeType.hook;
                }
                else if (e.Button == EnumMouseButton.Left)
                {
                    type = StrokeType.stroke;
                }
                else return;

                if (!IsInsideTablet(e.X, e.Y))
                {
                    return;
                }

                float localX = (e.X - (float)tabletBounds.absX) / (float)tabletBounds.OuterWidth;
                float localY = (e.Y - (float)tabletBounds.absY) / (float)tabletBounds.OuterHeight;

                strokeStart = new Vec2f(localX, localY);

                thicknessDelta = 0.015f;

                isDrawing = true;
                e.Handled = true;
            } else
            {
                if (e.Button != EnumMouseButton.Left)
                {
                    ghostMesh?.Dispose();
                    ghostMesh = null;
                } else
                {
                    if (ghostMesh != null)
                    {
                        CuneiformStroke newStroke = new CuneiformStroke
                        {
                            x1 = strokeStart.X,
                            y1 = strokeStart.Y,
                            x2 = strokeEnd.X,
                            y2 = strokeEnd.Y,
                            thicknessDelta = thicknessDelta,
                            typeofstroke = type
                        };
                        strokes.Add(newStroke);
                        //SendSaveToServer();

                        strokeMeshes.Add(ghostMesh);
                        ghostMesh = null;
                    }

                }

                isDrawing = false;
                e.Handled = true;
            }

            
        }

        public override void OnMouseMove(MouseEvent e)
        {
            if (!isDrawing) return;

            if (!IsInsideTablet(e.X, e.Y)) return;

            float localX = (e.X - (float)tabletBounds.absX) / (float)tabletBounds.OuterWidth;
            float localY = (e.Y - (float)tabletBounds.absY) / (float)tabletBounds.OuterHeight;

            Vec2f current = new Vec2f(localX, localY);
            Vec2f delta = current - strokeStart;

            float dx = delta.X * (float)tabletBounds.OuterWidth;
            float dy = delta.Y * (float)tabletBounds.OuterHeight;

            float length = 0f;

            if (type == StrokeType.hook)
            {
                length = dx * 4f;
            }
            else
            {
                length = MathF.Sqrt(dx * dx + dy * dy) * 2f;
            }

            if (type == StrokeType.hook && (!IsHookInside(strokeStart.Y, delta.X * 4f) || dx * 4f < 0f)) return;

            float angle = (float)Math.Atan2(dy, dx);

            strokeEnd = current;

            currentLength = length;
            currentAngle = angle;

            ghostMesh?.Dispose();

            MeshData md = BuildStrokeMesh(
                currentLength,
                type,
                currentAngle,
                baseThickness + thicknessDelta
            );

            ghostMesh = capi.Render.UploadMesh(md);

            e.Handled = true;

        }

        public override void OnMouseWheel(MouseWheelEventArgs args)
        {
            if (!isDrawing)
            {
                args.SetHandled(true);
                return;
            }
            if (args.delta > 0)
            {
                thicknessDelta += 0.002f;
            } else
            {
                thicknessDelta -= 0.002f;
            }
            thicknessDelta = GameMath.Clamp(thicknessDelta, 0f, 0.030f);

            ghostMesh?.Dispose();

            MeshData md = BuildStrokeMesh(
                currentLength,
                type,
                currentAngle,
                baseThickness + thicknessDelta
            );

            ghostMesh = capi.Render.UploadMesh(md);
            args.SetHandled(true);
        }

        public override void OnRenderGUI(float deltaTime)
        {
            base.OnRenderGUI(deltaTime);

            capi.Render.Render2DTexture(
                tabletbg.TextureId,
                (float)tabletBounds.absX,
                (float)tabletBounds.absY,
                (float)tabletBounds.OuterWidth,
                (float)tabletBounds.OuterHeight,
                20f
            );

            for (int i = 0; i < strokes.Count; i++)
            {
                var s = strokes[i];
                var mr = strokeMeshes[i];

                float screenX = (float)tabletBounds.absX + s.x1 * (float)tabletBounds.OuterWidth;
                float screenY = (float)tabletBounds.absY + s.y1 * (float)tabletBounds.OuterHeight;

                if (s.typeofstroke == StrokeType.hook)
                {
                    capi.Render.Render2DTexture(
                        mr,
                        hookTexture.TextureId,

                        screenX,
                        screenY,

                        1f,
                        1f,

                        50f
                    );
                }
                else
                {
                    capi.Render.Render2DTexture(
                        mr,
                        strokeTexture.TextureId,

                        screenX,
                        screenY,

                        1f,
                        1f,

                        50f
                    );
                }

            }

            // Ghost preview
            if (ghostMesh != null)
            {
                if (type == StrokeType.hook)
                {
                    capi.Render.Render2DTexture(
                        ghostMesh,
                        hookTexture.TextureId,

                        (float)tabletBounds.absX + strokeStart.X * (float)tabletBounds.OuterWidth,
                        (float)tabletBounds.absY + strokeStart.Y * (float)tabletBounds.OuterHeight,

                        1f,
                        1f,

                        60f
                    );
                }
                else
                {
                    capi.Render.Render2DTexture(
                        ghostMesh,
                        strokeTexture.TextureId,

                        (float)tabletBounds.absX + strokeStart.X * (float)tabletBounds.OuterWidth,
                        (float)tabletBounds.absY + strokeStart.Y * (float)tabletBounds.OuterHeight,

                        1f,
                        1f,

                        60f
                    );
                }

            }
        }

        MeshData BuildStrokeMesh(float length, StrokeType typeofstroke, float angleRad, float thickness)
        {
            MeshData mesh = new MeshData();

            mesh.xyz = new float[12];
            mesh.Uv = new float[8];
            mesh.Indices = new int[6];
            mesh.VerticesCount = 4;
            mesh.IndicesCount = 6;
            mesh.Flags = new int[4];

            float cos = (float)Math.Cos(angleRad);
            float sin = (float)Math.Sin(angleRad);

            float currentThickness = thickness * (float)tabletBounds.OuterHeight;

            Vec2f[] baseVerts =
            {
                new Vec2f(0f, -0.5f),
                new Vec2f(1f, -0.5f),
                new Vec2f(1f,  0.5f),
                new Vec2f(0f,  0.5f)
            };

            if (typeofstroke == StrokeType.hook)
            {
                for (int i = 0; i < 4; i++)
                {
                    float x = baseVerts[i].X * length;
                    float y = baseVerts[i].Y * length;

                    mesh.xyz[i * 3 + 0] = x;
                    mesh.xyz[i * 3 + 1] = y;
                    mesh.xyz[i * 3 + 2] = 0f;
                }
            }
            else
            {
                

                for (int i = 0; i < 4; i++)
                {
                    float x = baseVerts[i].X * length;
                    float y = baseVerts[i].Y * currentThickness;

                    float rx = x * cos - y * sin;
                    float ry = x * sin + y * cos;

                    mesh.xyz[i * 3 + 0] = rx;
                    mesh.xyz[i * 3 + 1] = ry;
                    mesh.xyz[i * 3 + 2] = 0f;
                }

            }

            for (int i = 0; i < 4; i++)
            {
                mesh.Uv[i * 2 + 0] = (i == 1 || i == 2) ? 1f : 0f;
                mesh.Uv[i * 2 + 1] = (i >= 2) ? 1f : 0f;
            }

            mesh.Indices[0] = 0;
            mesh.Indices[1] = 1;
            mesh.Indices[2] = 2;

            mesh.Indices[3] = 0;
            mesh.Indices[4] = 2;
            mesh.Indices[5] = 3;

            return mesh;
        }

        void RebuildMeshesFromStrokes()
        {
            capi.Logger.Notification("Starting rebuild of {0} strokes", strokes.Count);

            int i = 0;

            foreach (var s in strokes)
            {
                i++;

                float length;

                float dx = (s.x2 - s.x1) * (float)tabletBounds.OuterWidth;
                float dy = (s.y2 - s.y1) * (float)tabletBounds.OuterHeight;

                if (s.typeofstroke == StrokeType.hook)
                {
                    length = dx * 4f;
                }
                else
                {
                    length = MathF.Sqrt(dx * dx + dy * dy) * 2f;
                }

                float angle = (float)Math.Atan2(dy, dx);

                if (float.IsNaN(s.x1) || float.IsNaN(s.y1) ||
                    float.IsNaN(s.x2) || float.IsNaN(s.y2))
                {
                    capi.Logger.Error("Stroke {0} contains NaN!", i);
                    continue;
                }

                if (length <= 0 || length > 5000)
                {
                    capi.Logger.Error("Stroke {0} bad length {1}", i, length);
                    continue;
                }

                capi.Logger.Notification("Building mesh {0}", i);

                MeshData md;
                try
                {
                    md = BuildStrokeMesh(
                        length,
                        s.typeofstroke,
                        angle,
                        baseThickness + s.thicknessDelta
                    );
                }
                catch (Exception e)
                {
                    capi.Logger.Error("BuildStrokeMesh crashed: " + e);
                    continue;
                }

                md.Flags = new int[4];


                capi.Logger.Notification("Uploading mesh {0}", i);

                MeshRef mr = capi.Render.UploadMesh(md);

                strokeMeshes.Add(mr);
            }

            capi.Logger.Notification("Rebuild finished");
        }

        bool IsInsideTablet(float x, float y)
        {
            return x >= (float)tabletBounds.absX &&
                   x <= (float)tabletBounds.absX + (float)tabletBounds.OuterWidth &&
                   y >= (float)tabletBounds.absY &&
                   y <= (float)tabletBounds.absY + (float)tabletBounds.OuterHeight;
        }

        bool IsHookInside(float y, float length)
        {
            return y + length / 4f <= 1f &&
                y - length / 4f >= 0f;
        }

        byte[] SerializeStrokes()
        {
            TreeAttribute tree = new TreeAttribute();

            tree.SetInt("count", strokes.Count);

            for (int i = 0; i < strokes.Count; i++)
            {
                var s = strokes[i];

                tree.SetFloat("x1" + i, s.x1);
                tree.SetFloat("y1" + i, s.y1);
                tree.SetFloat("x2" + i, s.x2);
                tree.SetFloat("y2" + i, s.y2);
                tree.SetFloat("thicknessDelta" + i, s.thicknessDelta);
                tree.SetInt("t" + i, (int)s.typeofstroke);
            }

            return tree.ToBytes();
        }

        

        void OnDrawTablet(Context ctx, ImageSurface surface, ElementBounds bounds)
        {

        }


    }

}
