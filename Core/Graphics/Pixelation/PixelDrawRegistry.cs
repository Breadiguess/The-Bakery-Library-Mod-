using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreadLibrary.Core.Graphics.Pixelation
{
    [Autoload(Side = ModSide.Client)]
    public sealed class PixelDrawRegistry : ModSystem
    {
        private static readonly List<IDrawPixelated> GlobalDrawers = new();

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

        public static void Register(IDrawPixelated drawer)
        {
            if (drawer is not null && !GlobalDrawers.Contains(drawer))
                GlobalDrawers.Add(drawer);
        }

        public static void Unregister(IDrawPixelated drawer)
        {
            if (drawer is not null)
                GlobalDrawers.Remove(drawer);
        }

        private static void CollectGlobalDrawers(List<IDrawPixelated> results)
        {
            for (int i = 0; i < GlobalDrawers.Count; i++)
            {
                IDrawPixelated drawer = GlobalDrawers[i];
                if (drawer is not null && drawer.ShouldDrawPixelated)
                    results.Add(drawer);
            }
        }
    }
}
