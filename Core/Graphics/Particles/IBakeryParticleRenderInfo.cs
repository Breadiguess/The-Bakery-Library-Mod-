using BreadLibrary.Core.Graphics.Pixelation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreadLibrary.Core.Graphics.Particles
{
    public interface IBakeryParticleRenderInfo
    {
        BakeryParticleRenderer? OwningRenderer { get; set; }

        ParticleRenderGroup RenderGroup { get; set; }

        PixelLayer PixelLayer { get; set; }

        bool HasExplicitPixelLayer { get; set; }
    }
}
