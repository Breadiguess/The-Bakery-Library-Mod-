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
        public SoftbodyInstance Body;

        // Used so we can move the body by NPC motion instead of hard teleporting every frame.
        private Vector2 _lastCenter;
        private bool _bodyInitialized;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 42;
            NPC.height = 22;
            NPC.damage = 20;
            NPC.defense = 4;
            NPC.lifeMax = 120;
            NPC.knockBackResist = 0.6f;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.aiStyle = NPCAIStyleID.Slime;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            CreateBody();
            _lastCenter = NPC.Center;
            _bodyInitialized = true;
        }

        private void CreateBody()
        {
            SoftbodySim sim = new();

            sim.Mat.Iterations = 3;
            sim.Mat.Damping = 0.95f;
            sim.Mat.ShapeMatchingStiffness = 0.29f;
            sim.Mat.StructuralStiffness = 0.05f;
            sim.Mat.BendStiffness = 0.07f;
           
            Body = new SoftbodyInstance(sim);
          
            Body.CreateEllipseBody(
                center: NPC.Center,
                count: 40,
                radiusX: 60f,
                radiusY: 20f,
                mass: 3f,
                nodeRadius: 4f
            );

            Body.SetCenter(NPC.Center, preserveVelocity: false);
            Body.DriverMode = SoftbodyInstance.TransformDriverMode.EntityCenter;
            Body.DriverEntity = this.NPC;
            Body.Collision.CollideWithPlayers = true;
            Body.Collision.EntityBounce = 0f;
            Body.Collision.IgnoreDriverEntity = true;
            Body.Collision.PlayerPushFactor = 0;
            SoftbodySystem.Instances.Add(Body);
        }
        public override void AI()
        {
            if (!_bodyInitialized || Body == null)
            {
                CreateBody();
                _lastCenter = NPC.Center;
                _bodyInitialized = true;
            }
            Body.Translate(_lastCenter);

        }

        private void DoMovement()
        {
          
        }

        public override bool PreDraw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            
            return false;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (Body == null)
                return;

            // Kick all nodes a little on hit for extra squish.
            Vector2 impulse = hit.HitDirection * Vector2.UnitX * 4f;

            for (int i = 0; i < Body.Sim.Nodes.Count; i++)
            {
                ref var node = ref Body.Sim.GetNodeRef(i);
                if (node.InvMass <= 0f)
                    continue;

                // Verlet-style impulse: offset previous position backward.
                node.PrevPos -= impulse;
            }
        }

        public override void OnKill()
        {
            Body = null;
            _bodyInitialized = false;
        }
    }
}
