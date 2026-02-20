using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreadLibrary.Core.SoftBodySim
{
    public sealed class Material
    {
        public int Iterations = 6;
        public float Damping = 0.995f;      
        public float GravityScale = 1f;

        // Collision response
        public float Friction = 0.15f;      
        public float Bounce = 0.0f;         

        // Global constraint stiffness multipliers 
        public float StructuralStiffness = 1f;
        public float BendStiffness = 1f;
        public float AreaStiffness = 1f;

        public float ShearStiffness { get; internal set; }
        public float AttachmentStiffness { get; internal set; }
    }
}
