using Cairo;
using CuneiformWriting.Gui;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace CuneiformWriting
{
    public class TabletBaker
    {
        ICoreClientAPI capi;

        public TabletBaker(ICoreClientAPI capi)
        {
            this.capi = capi;
        }

        // ------------------------------------------------------------

        public byte[] BakeToPng(List<CuneiformStroke> strokes, int size = 512)
        {
            ImageSurface surface =
                new ImageSurface(Format.Argb32, size, size);

            Context g = new Context(surface);

            g.Operator = Operator.Clear;
            g.Paint();
            g.Operator = Operator.Over;

            foreach (var s in strokes)
            {
                DrawStroke(g, s, size);
            }

            // ----------------------------------------------------
            // REALISTIC API WORKAROUND
            // ----------------------------------------------------

            string path = System.IO.Path.Combine(
                capi.DataBasePath,
                "tablet_tmp.png"
            );

            surface.WriteToPng(path);

            byte[] bytes = System.IO.File.ReadAllBytes(path);

            System.IO.File.Delete(path);

            return bytes;
        }

        // ------------------------------------------------------------

        void DrawStroke(Context g, CuneiformStroke s, int texSize)
        {
            float x = s.x * texSize;
            float y = s.y * texSize;
            float length = s.length * texSize;

            g.SetSourceRGBA(0.05, 0.03, 0.02, 1);

            if (s.typeofstroke == StrokeType.hook)
            {
                DrawHook(g, x, y, length);
            }
            else
            {
                float thickness = 0.03f * texSize;
                DrawWedge(g, x, y, length, thickness, s.angle);
            }
        }

        // ------------------------------------------------------------

        void DrawHook(Context g, float x, float y, float size)
        {
            float half = size / 2f;

            g.Rectangle(
                x - half,
                y - half,
                size,
                size
            );

            g.Fill();
        }

        // ------------------------------------------------------------

        void DrawWedge(Context g,
                       float x,
                       float y,
                       float length,
                       float thickness,
                       float angle)
        {
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);

            float[] vx = new float[4];
            float[] vy = new float[4];

            for (int i = 0; i < 4; i++)
            {
                float bx = (i == 1 || i == 2) ? length : 0f;
                float by = (i >= 2 ? 0.5f : -0.5f) * thickness;

                vx[i] = bx * cos - by * sin + x;
                vy[i] = bx * sin + by * cos + y;
            }

            g.MoveTo(vx[0], vy[0]);
            g.LineTo(vx[1], vy[1]);
            g.LineTo(vx[2], vy[2]);
            g.LineTo(vx[3], vy[3]);
            g.ClosePath();

            g.Fill();
        }
    }
}