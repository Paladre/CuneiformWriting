using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace CuneiformWriting.Render
{
    public class TabletItemRenderer : EntityItemRenderer
    {
        public TabletItemRenderer(Entity entity, ICoreClientAPI api) : base(entity, api)
        {

        }
    }
}
