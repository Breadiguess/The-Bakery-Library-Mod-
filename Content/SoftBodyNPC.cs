using BreadLibrary.Core.SoftBodySim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace BreadLibrary.Content
{
    internal class SoftBodyNPC : ModNPC
    {
        SoftbodyInstance body;
        public override void SetDefaults()
        {
            NPC.lifeMax = 300;
            NPC.Size = new Vector2(40);
            NPC.aiStyle = NPCAIStyleID.Slime;
           
        }
        public override void OnSpawn(IEntitySource source)
        {

            var sim = new SoftbodySim();
            sim.Mat = MaterialLibrary.Jelly(); // or Jelly/Rubber/etc

            int w = 10, h = 10;
            float spacing = 9f;
            float meshWidth = (w - 1) * spacing;
            float meshHeight = (h - 1) * spacing;

            Vector2 origin = new Vector2(
                NPC.TopRight.X - meshWidth/2,
                NPC.Bottom.Y - meshHeight/2
            );
            int[,] grid = SoftbodyBuilder.CreateSquareLattice(
                 sim,
                 origin,
                 w,
                 h,
                 spacing,
                 60f,
                 6f
             );

            body = new SoftbodyInstance(sim, MaterialLibrary.Flesh());

            body.NodeGrid = grid;
            body.GridWidth = w;
            body.GridHeight = h;
            body.Sim.Viscosity = 0.002f;
            body.AttachCenterCrossToNPC(grid, NPC);

            // Attach top row of nodes to NPC
            List<int> anchors = new();

            for (int x = 0; x < 41; x++)
                anchors.Add(x * 41 + 0);

            body.AttachToNPC(NPC, -new Vector2(NPC.width/2, NPC.height), anchors);

            SoftbodySystem.Instances.Add(body);
        }


    }
}
