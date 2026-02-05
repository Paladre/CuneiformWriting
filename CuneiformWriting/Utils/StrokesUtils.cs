using CuneiformWriting.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace CuneiformWriting.Utils
{
    public static class StrokesUtils
    {
        public static List<CuneiformStroke> LoadStrokes(ItemStack stack)
        {
            List<CuneiformStroke> result = new List<CuneiformStroke>();

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

        public static byte[] SerializeStrokes(List<CuneiformStroke> strokes)
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
    }
    
}
