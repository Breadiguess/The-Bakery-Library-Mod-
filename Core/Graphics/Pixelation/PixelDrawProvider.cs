using Terraria.ModLoader;

namespace BreadLibrary.Core.Graphics.Pixelation
{
    [Autoload(Side = ModSide.Client)]
    public abstract class PixelDrawProvider : ModSystem, IPixelDrawProvider
    {
        public abstract void CollectPixelDraws(List<IDrawPixelated> results);
    }

    [Autoload(Side = ModSide.Client)]
    public abstract class PlayerPixelDrawProvider : ModSystem, IPlayerPixelDrawProvider
    {
        public abstract void CollectPixelDraws(Player player, List<IDrawPixelated> results);
    }
}