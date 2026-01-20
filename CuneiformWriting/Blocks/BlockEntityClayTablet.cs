using CuneiformWriting.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace CuneiformWriting.Blocks
{
    public class BlockEntityClayTablet : BlockEntity
    {
        public List<CuneiformStroke> strokes = new List<CuneiformStroke>();
        public void ApplySerialized(byte[] data)
        {
            TreeAttribute tree = TreeAttribute.CreateFromBytes(data);

            strokes = new List<CuneiformStroke>();

            int count = tree.GetInt("count");

            for (int i = 0; i < count; i++)
            {
                strokes.Add(new CuneiformStroke
                {
                    x = tree.GetFloat("x" + i),
                    y = tree.GetFloat("y" + i),
                    length = tree.GetFloat("l" + i),
                    angle = tree.GetFloat("a" + i),
                    typeofstroke = (StrokeType)tree.GetInt("t" + i)
                });
            }
        }

        BlockEntityClayTabletRenderer renderer;

        //public override void ToTreeAttributes(ITreeAttribute tree)
        //{
        //    base.ToTreeAttributes(tree);
        //    SaveStrokesToTree(tree);
        //}

        //public override void FromTreeAttributes(
        //    ITreeAttribute tree,
        //    IWorldAccessor world)
        //{
        //    base.FromTreeAttributes(tree, world);
        //    LoadStrokesFromTree(tree);
        //}

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            if (api.Side == EnumAppSide.Client)
            {
                renderer = new BlockEntityClayTabletRenderer(
                    this,
                    api as ICoreClientAPI
                );

                (api as ICoreClientAPI)
                    .Event.RegisterRenderer(
                        renderer,
                        EnumRenderStage.Opaque,
                        "cuneiformtablet"
                    );
            }
        }

        public void OpenGui(IPlayer byPlayer)
        {
            if (Api.Side != EnumAppSide.Client) return;

            var capi = Api as ICoreClientAPI;

            new Gui.GuiCuneiform(capi, this).TryOpen();
        }

        public void MarkDirtyAndRebuild()
        {
            MarkDirty(true);

            renderer?.RebuildTexture();
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            TreeAttribute sub = new TreeAttribute();

            sub.SetInt("count", strokes.Count);

            if (Api?.World != null)
                Api.World.Logger.Notification("TO TREE: " + strokes?.Count);

            for (int i = 0; i < strokes.Count; i++)
            {
                var s = strokes[i];

                sub.SetFloat("x" + i, s.x);
                sub.SetFloat("y" + i, s.y);
                sub.SetFloat("l" + i, s.length);
                sub.SetFloat("a" + i, s.angle);
                sub.SetInt("t" + i, (int)s.typeofstroke);
            }

            tree["cuneiform"] = sub;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor world)
        {
            base.FromTreeAttributes(tree, world);

            strokes.Clear();

            TreeAttribute sub = tree["cuneiform"] as TreeAttribute;


            if (sub == null)
            {
                world.Logger.Notification("FROM TREE: no data");
                return;
            }

            int count = sub.GetInt("count");

            world.Logger.Notification("FROM TREE: " + count);

            for (int i = 0; i < count; i++)
            {
                strokes.Add(new CuneiformStroke
                {
                    x = sub.GetFloat("x" + i),
                    y = sub.GetFloat("y" + i),
                    length = sub.GetFloat("l" + i),
                    angle = sub.GetFloat("a" + i),
                    typeofstroke = (StrokeType)sub.GetInt("t" + i)
                });
            }
        }
    }
}
