using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreadLibrary.Core.Graphics.Metaballs
{

    public readonly struct MetaballUpdateContext
    {
        public readonly MetaballGroup Group;
        public readonly int InstanceIndex;
        public readonly float Time;

        public MetaballUpdateContext(MetaballGroup group, int instanceIndex, float time)
        {
            Group = group;
            InstanceIndex = instanceIndex;
            Time = time;
        }
    }
}
