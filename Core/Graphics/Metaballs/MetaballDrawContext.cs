using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BreadLibrary.Core.Graphics.Metaballs
{
    public readonly struct MetaballDrawContext
    {
        public readonly GraphicsDevice GraphicsDevice;
        public readonly RenderTarget2D Target;
        public readonly Vector2 ScreenPosition;
        public readonly Vector2 ScreenSize;
        public readonly float Time;

        public MetaballDrawContext(
            GraphicsDevice graphicsDevice,
            RenderTarget2D target,
            Vector2 screenPosition,
            Vector2 screenSize,
            float time)
        {
            GraphicsDevice = graphicsDevice;
            Target = target;
            ScreenPosition = screenPosition;
            ScreenSize = screenSize;
            Time = time;
        }
    }
}