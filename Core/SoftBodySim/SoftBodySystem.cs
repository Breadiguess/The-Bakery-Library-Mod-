using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreadLibrary.Core.SoftBodySim
{
    public class SoftbodySystem : ModSystem
    {
        public static List<SoftbodyInstance> Instances = new();

        public override void PostUpdateEverything()
        {
            for (int i = Instances.Count - 1; i >= 0; i--)
            {
                var s = Instances[i];

                if (s.AttachedEntity != null && !s.AttachedEntity.active)
                {
                    Instances.RemoveAt(i);
                    continue;
                }

                s.Update();
            }
        }

        public override void PostDrawTiles()
        {
            foreach (var s in Instances)
                s.Draw();
        }
    }
}
