using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace CuneiformWriting.Blocks
{
    public class BlockClayTablet : Block
    {
        public override bool OnBlockInteractStart(
            IWorldAccessor world,
            IPlayer byPlayer,
            BlockSelection blockSel)
        {
            if (world.Side == EnumAppSide.Client)
            {
                var be = world.BlockAccessor
                    .GetBlockEntity(blockSel.Position) as BlockEntityClayTablet;

                be?.OpenGui(byPlayer);
            }

            return true;
        }
    }
}
