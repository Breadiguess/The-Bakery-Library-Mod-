using BreadLibrary.Core.Graphics.Particles;
using BreadLibrary.Core.Graphics.Pixelation;
using BreadLibrary.Core.SoftBodySim;
using BreadLibrary.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Graphics.Renderers;

namespace BreadLibrary.Content
{
#if DEBUG
    [PoolCapacity(500)]
    internal class Particle : BaseParticle<Particle>
    {
        public override void SetStaticDefaults()
        {
            ModContent.GetInstance<BreadLibrary>().Logger.Debug($"{this.GetPath} I ran SetStaticDefaults!");
        }

        public SoftbodyInstance Body;
        Vector2 Position;
        Vector2 Velocity;
        public int TimeLeft;
        public int TimeMax;
        public float rotation;
        public void Prepare(Vector2 Position, int TimeMax = 120, float rotation = -1)
        {
           this.Position = Position;
           this.TimeMax = TimeMax;
           TimeLeft = this.TimeMax;

            Velocity = Vector2.UnitY;
            if(rotation != -1)
                this.rotation = rotation;
            else
                rotation = Main.rand.NextFloat(MathHelper.TwoPi);


        }
        public override void Update(ref ParticleRendererSettings settings)
        {


            Position += Velocity;
            rotation += 0.2f;
            Velocity *= 1.1f;
            if(TimeLeft--<0)
                ShouldBeRemovedFromRenderer = true;
        }

        public override void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
        {
            var tex = TextureAssets.FishingLine.Value;

            Vector2 drawPos = Position - Main.screenPosition;

            Vector2 Origin = tex.Size() / 2;

            Main.EntitySpriteDraw(tex, drawPos, null, Color.White, rotation, Origin, 1, 0);
        }

       
    }
#endif
}
