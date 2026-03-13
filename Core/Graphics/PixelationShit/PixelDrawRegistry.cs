using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreadLibrary.Core.Graphics.PixelationShit
{
    [Autoload(Side = ModSide.Client)]
    public sealed class PixelDrawRegistry : ModSystem
    {
        private static readonly List<IDrawPixellated> GlobalDrawers = new();

        public override void Load()
        {
            if (Main.dedServ)
                return;

            PixelationSystem.CollectPixelDrawsEvent += CollectGlobalDrawers;
        }

        public override void Unload()
        {
            PixelationSystem.CollectPixelDrawsEvent -= CollectGlobalDrawers;
            GlobalDrawers.Clear();
        }

        public static void Register(IDrawPixellated drawer)
        {
            if (drawer is not null && !GlobalDrawers.Contains(drawer))
                GlobalDrawers.Add(drawer);
        }

        public static void Unregister(IDrawPixellated drawer)
        {
            if (drawer is not null)
                GlobalDrawers.Remove(drawer);
        }

        private static void CollectGlobalDrawers(List<IDrawPixellated> results)
        {
            for (int i = 0; i < GlobalDrawers.Count; i++)
            {
                IDrawPixellated drawer = GlobalDrawers[i];
                if (drawer is not null && drawer.ShouldDrawPixelated)
                    results.Add(drawer);
            }
        }
    }
}
