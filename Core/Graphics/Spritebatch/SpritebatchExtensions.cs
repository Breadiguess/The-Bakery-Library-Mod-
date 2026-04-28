using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BreadLibrary.Core.Graphics.Spritebatch
{
    /// <summary>
    ///     Provides <see cref="SpriteBatch" /> extension methods.
    /// </summary>
    public static class SpriteBatchExtensions
    {
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "sortMode")]
        private static extern ref SpriteSortMode SpriteSortMode(SpriteBatch batch);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "blendState")]
        private static extern ref BlendState BlendState(SpriteBatch batch);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "samplerState")]
        private static extern ref SamplerState SamplerState(SpriteBatch batch);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "depthStencilState")]
        private static extern ref DepthStencilState DepthStencilState(SpriteBatch batch);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "rasterizerState")]
        private static extern ref RasterizerState RasterizerState(SpriteBatch batch);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "spriteEffect")]
        private static extern ref Effect Effect(SpriteBatch batch);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "transformMatrix")]
        private static extern ref Matrix TransformMatrix(SpriteBatch batch);

        /// <summary>
        ///     Captures the current rendering parameters of the specified <see cref="SpriteBatch" />.
        /// </summary>
        /// <param name="spriteBatch">The <see cref="SpriteBatch" /> instance to capture.</param>
        /// <returns>
        ///     A <see cref="SpriteBatchParameters" /> instance with the captured rendering parameters of
        ///     the specified <see cref="SpriteBatch" />.
        /// </returns>
        public static SpriteBatchParameters Capture(this SpriteBatch spriteBatch)
        {
            return new SpriteBatchParameters
            (
                SpriteSortMode(spriteBatch),
                BlendState(spriteBatch),
                SamplerState(spriteBatch),
                DepthStencilState(spriteBatch),
                RasterizerState(spriteBatch),
                Effect(spriteBatch),
                TransformMatrix(spriteBatch)
            );
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="spriteBatch">The <see cref="SpriteBatch" /> instance to begin.</param>
        /// <param name="parameters">
        ///     The <see cref="SpriteBatchParameters" /> that define the rendering
        ///     parameters of the <see cref="SpriteBatch" />.
        /// </param>
        public static void Begin(this SpriteBatch spriteBatch, in SpriteBatchParameters parameters)
        {
            spriteBatch.Begin
            (
                parameters.SpriteSortMode,
                parameters.BlendState,
                parameters.SamplerState,
                parameters.DepthStencilState,
                parameters.RasterizerState,
                parameters.Effect,
                parameters.TransformMatrix
            );
        }
    }
}
