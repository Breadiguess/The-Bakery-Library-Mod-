using BreadLibrary.Core.Graphics.Pixelation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Graphics.Renderers;

namespace BreadLibrary.Core.Graphics.Particles
{
    public class BakeryParticleRenderer : ParticleRenderer
    {
        public PixelLayer DefaultPixelLayer { get; set; } = PixelLayer.AboveProjectiles;

        public ParticleRenderGroup RenderGroup { get; }

        public BakeryParticleRenderer(ParticleRenderGroup renderGroup, PixelLayer defaultPixelLayer)
        {
            RenderGroup = renderGroup;
            DefaultPixelLayer = defaultPixelLayer;
        }

        /// <summary>
        /// Adds a particle to the renderer.
        /// </summary>
        /// <param name="particle"></param>
        /// <remarks> <seealso cref="Add(IParticle, PixelLayer)"/> for easily telling what particle to go where.</remarks>
        public new void Add(IParticle particle)
        {
            if (particle is IBakeryParticleRenderInfo info)
            {
                info.OwningRenderer = this;
                info.RenderGroup = RenderGroup;

                if (!info.HasExplicitPixelLayer)
                    info.PixelLayer = DefaultPixelLayer;
            }

            base.Add(particle);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="particle"></param>
        /// <param name="pixelLayer">explicitly sets the pixel layer of the particle.</param>
        public void Add(IParticle particle, PixelLayer pixelLayer)
        {
            if (particle is IBakeryParticleRenderInfo info)
            {
                info.PixelLayer = pixelLayer;
                info.HasExplicitPixelLayer = true;
            }

            Add(particle);
        }
    }
}
