using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreadLibrary.Core.SoftBodySim
{
    public static class MaterialLibrary
    {
        public static Material Cloth()
        {
            return new()
            {
                Iterations = 5,
                Damping = 0.99f,
                GravityScale = 1f,
                Friction = 0.1f,
                Bounce = 0f,

                StructuralStiffness = 0.6f,
                ShearStiffness = 0.5f,
                BendStiffness = 0.2f,
                AreaStiffness = 0f,

                AttachmentStiffness = 0.35f
            };
        }

        public static Material Jelly()
        {
            return new()
            {
                Iterations = 4,
                Damping = 0.8f,
                GravityScale = 0,
                Friction = 0.2f,
                Bounce = 1f,

                StructuralStiffness = 0.04f,
                ShearStiffness = 0.05f,
                BendStiffness = 0f,
                AreaStiffness = 0.06f,

                AttachmentStiffness = 0.5f
            };
        }

        public static Material Rubber()
        {
            return new()
            {
                Iterations = 6,
                Damping = 0.995f,
                GravityScale = 1f,
                Friction = 0.3f,
                Bounce = 0.2f,

                StructuralStiffness = 0.75f,
                ShearStiffness = 0.6f,
                BendStiffness = 0.3f,
                AreaStiffness = 0f,

                AttachmentStiffness = 0.5f
            };
        }

        public static Material Flesh()
        {
            return new()
            {
                Iterations = 4,
                Damping = 0.98f,
                GravityScale = 1f,
                Friction = 0.4f,
                Bounce = 0f,

                StructuralStiffness = 0.5f,
                ShearStiffness = 0.4f,
                BendStiffness = 0.1f,
                AreaStiffness = 0.3f,

                AttachmentStiffness = 0.3f
            };
        }
    }
}
