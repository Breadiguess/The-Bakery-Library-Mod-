using System.Collections.Generic;
using Terraria;

namespace BreadLibrary.Core.Graphics.Pixelation
{
    /// <summary>
    /// An Interface that can be added to a class to allow it to be drawn in a pixellated variety rather than normally.
    /// </summary>
    public interface IDrawPixelated
    {
        PixelLayer PixelLayer { get; }
        bool ShouldDrawPixelated => true;
        void DrawPixelated(SpriteBatch spriteBatch);
    }


    /// <summary>
    /// Use this for player-bound visuals instead of trying to force ordinary PlayerDrawLayers into the RT.
    /// </summary>
    public interface IPlayerPixelatedDrawer
    {
        PixelLayer PixelLayer { get; }
        bool IsActive(Player player);
        void DrawPixelated(Player player, SpriteBatch spriteBatch);
    }
    // IT JUST WORKS

    public interface IPixelDrawableSource
    {
        void CollectPixelDraws(List<IDrawPixelated> results);
    }

    /// <summary>
    /// Global/stateless collector. Good for systems, managers, cached world visuals, etc.
    /// </summary>
    public interface IPixelDrawProvider
    {
        void CollectPixelDraws(List<IDrawPixelated> results);
    }

    /// <summary>
    /// Player-bound collector. Good for player auras, overlays, held effects, etc.
    /// </summary>
    public interface IPlayerPixelDrawProvider
    {
        void CollectPixelDraws(Player player, List<IDrawPixelated> results);
    }
}