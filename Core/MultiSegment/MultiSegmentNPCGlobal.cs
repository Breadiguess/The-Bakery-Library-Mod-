using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader.IO;

namespace BreadLibrary.Core.MultiSegment
{
    internal sealed class MultiSegmentNPCGlobal : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public override void PostAI(NPC npc)
        {
            if (npc.ModNPC is IMultiSegmentNPC multiSegment)
            {
                multiSegment.UpdateSegments();
            }
        }
        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            if (npc.ModNPC is IMultiSegmentNPC multiSegment)
            {
                multiSegment.NetSendExtraHitboxes(binaryWriter);
            }
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            if (npc.ModNPC is IMultiSegmentNPC multiSegment)
            {
                multiSegment.NetReceiveExtraHitboxes(binaryReader);
            }
        }
    }
}
