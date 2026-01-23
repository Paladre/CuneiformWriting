using Cairo;
using CuneiformWriting.Items;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using System.Drawing;

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
        ItemSlot sourceSlot;
        claytablet tabletItem;

        LoadedTexture strokeTexture;
        LoadedTexture hookTexture;
        LoadedTexture tabletbg;

        List<MeshRef> strokeMeshes = new List<MeshRef>();
        MeshRef ghostMesh;

        ElementBounds tabletBounds;

        List<CuneiformStroke> strokes = new List<CuneiformStroke>();
        Stack<CuneiformStroke> undoStack = new Stack<CuneiformStroke>();

        Vec2f strokeStart;
        Vec2f strokeEnd;

        bool isDragging = false;

        float currentLength;
        float currentAngle;

        StrokeType type = StrokeType.stroke;

        public GuiCuneiform(ICoreClientAPI capi, ItemSlot sourceSlot, claytablet tabletItem) : base(capi)
        {
            this.capi = capi;
            this.sourceSlot = sourceSlot;
            this.tabletItem = tabletItem;

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
                new AssetLocation("cuneiformwriting:textures/gui/tablet_bg.png"),
                ref tabletbg
            );
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

            //capi.ShowChatMessage("GUI with custom draw opened");
            strokes = tabletItem.LoadStrokes(sourceSlot.Itemstack);
            //RebuildMeshesFromStrokes();
        }

        public override void OnGuiClosed()
        {
            SendSaveToServer();

            foreach (var mesh in strokeMeshes)
                mesh.Dispose();

            base.OnGuiClosed();
        }

        public override void OnMouseDown(MouseEvent e)
        {
            if (isDragging) return;

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

            isDragging = true;
            e.Handled = true;
        }

        public override void OnMouseMove(MouseEvent e)
        {
            if (!isDragging) return;

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

            currentLength = length;
            currentAngle = angle;

            ghostMesh?.Dispose();

            //MeshData md = BuildStrokeMesh(
            //    currentLength,
            //    type,
            //    currentAngle
            //);

            //ghostMesh = capi.Render.UploadMesh(md);

            e.Handled = true;

        }

        public override void OnMouseUp(MouseEvent e)
        {
            if (!isDragging) return;

            float localX = (e.X - (float)tabletBounds.absX) / (float)tabletBounds.OuterWidth;
            float localY = (e.Y - (float)tabletBounds.absY) / (float)tabletBounds.OuterHeight;

            strokeEnd = new Vec2f(localX, localY);

            //if (ghostMesh != null)
            //{
            //    CuneiformStroke newStroke = new CuneiformStroke
            //    {
            //        x = strokeStart.X,
            //        y = strokeStart.Y,
            //        x2 = current.X,
            //        y2 = strokeEnd.Y,
            //        typeofstroke = type
            //    };
            //    undoStack.Push(newStroke);
            //    strokes.Add(newStroke);

            //    strokeMeshes.Add(ghostMesh);
            //    ghostMesh = null;
            //}

            CuneiformStroke newStroke = new CuneiformStroke
            {
                x1 = strokeStart.X,
                y1 = strokeStart.Y,
                x2 = strokeEnd.X,
                y2 = strokeEnd.Y,
                typeofstroke = type
            };
            undoStack.Push(newStroke);
            strokes.Add(newStroke);


            isDragging = false;
            e.Handled = true;
        }

        public override void OnRenderGUI(float deltaTime)
        {
            base.OnRenderGUI(deltaTime);

            //tabletGraphic.Dispose();

            //Vec2f start;
            //Vec2f end;

            //for (int i = 0; i < strokes.Count; i++)
            //{
            //    start = new Vec2f(strokes[i].x1, strokes[i].y1);
            //    end = new Vec2f(strokes[i].x2, strokes[i].y2);
            //    Vec2i[] indices = GetPolygonIndices(start, end, strokes[i].typeofstroke);
            //}
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

        public override void OnKeyDown(KeyEvent args)
        {
            if (args.CtrlPressed && args.KeyCode == (int)GlKeys.Z)
            {
                UndoLast();
                return;
            }
            return;
        }
        void UndoLast()
        {
            if (strokes.Count == 0) return;

            strokes.RemoveAt(strokes.Count - 1);

            //tabletItem.SaveStrokes(sourceSlot, strokes);
            SendSaveToServer();
        }

        void SendSaveToServer()
        {
            byte[] data = SerializeStrokes();
            int index = sourceSlot.Inventory.GetSlotId(sourceSlot);

            capi.Network.GetChannel("cuneiform")
                .SendPacket(new PacketSaveTablet { Data = data });
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
                tree.SetInt("t" + i, (int)s.typeofstroke);
            }

            return tree.ToBytes();
        }

        

        void OnDrawTablet(Context ctx, ImageSurface surface, ElementBounds bounds)
        {
            // Intentionally empty for now
            // Rendering will be done via Render2DTexture, not Cairo
        }

        

        
    }

}
