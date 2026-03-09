using BreadLibrary.Core.Graphics.BreadLibrary.Core.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace BreadLibrary.Core.Graphics.PixelationShit
{
    [Autoload(Side = ModSide.Client)]
    public sealed class PlayerPixelRegistry : ModSystem
    {
        private static readonly List<IPlayerPixelatedDrawer> GlobalDrawers = new();

        public override void Load()
        {
            if (Main.dedServ)
                return;

            PixelationSystem.CollectPlayerPixelDrawersEvent += CollectForPlayer;
        }

        public override void Unload()
        {
            PixelationSystem.CollectPlayerPixelDrawersEvent -= CollectForPlayer;
            GlobalDrawers.Clear();
        }

        public static void Register(IPlayerPixelatedDrawer drawer)
        {
            if (drawer is not null && !GlobalDrawers.Contains(drawer))
                GlobalDrawers.Add(drawer);
        }

        public static void Unregister(IPlayerPixelatedDrawer drawer)
        {
            if (drawer is not null)
                GlobalDrawers.Remove(drawer);
        }

        private static void CollectForPlayer(Player player, List<IPlayerPixelatedDrawer> results)
        {
            for (int i = 0; i < GlobalDrawers.Count; i++)
            {
                IPlayerPixelatedDrawer drawer = GlobalDrawers[i];
                if (drawer is not null && drawer.IsActive(player))
                    results.Add(drawer);
            }
        }
    }
}