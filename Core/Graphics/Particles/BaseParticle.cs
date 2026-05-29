using BreadLibrary.Core.Graphics.Pixelation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria.Graphics.Renderers;

namespace BreadLibrary.Core.Graphics.Particles
{
    /// <summary>
    /// Controls how many Particles of the same Type can exist at the same time.
    /// </summary>
    /// <param name="capacity">The new capacity of the particle pool. </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class PoolCapacityAttribute(int capacity) : Attribute
    {
        /// <summary>
        /// The current Capacity of the particle pool.
        /// </summary>
        public int Capacity { get; } = capacity;
    }
    public abstract class BaseParticle<T> : ModType, IPooledParticle, IDrawPixelated, IBakeryParticleRenderInfo
    where T : IPooledParticle, new()
    {
        #region Pool stuff
        public const int DEFAULT_POOL_CAPACITY = 150;
        public static ParticlePool<T> Pool { get; } = new ParticlePool<T>(typeof(T).GetCustomAttribute<PoolCapacityAttribute>()?.Capacity ?? DEFAULT_POOL_CAPACITY, GetNewParticle);

        protected static T GetNewParticle() => new T();

        public bool IsRestingInPool { get; private set; }

        public virtual void FetchFromPool()
        {
            IsRestingInPool = false;
            ShouldBeRemovedFromRenderer = false;
        }

        public virtual void RestInPool()
        {
            IsRestingInPool = true;
        }
        #endregion
        protected sealed override void Register() { }

        public sealed override void SetupContent() => this.SetStaticDefaults();
        /// <summary>
        /// when this is true, the particle is removed from the renderer (and thus the world) at the end of the current frame.
        /// </summary>
        public bool ShouldBeRemovedFromRenderer { get; protected set; }
        
        /// <summary>
        /// The Particle Renderer that the particle is currently residing in.
        /// </summary>
        public BakeryParticleRenderer? OwningRenderer { get; set; }

        public ParticleRenderGroup RenderGroup { get; set; } = ParticleRenderGroup.Normal;

        public virtual PixelLayer DefaultPixelLayer => PixelLayer.AboveProjectiles;

        public PixelLayer PixelLayer { get; set; } = PixelLayer.AboveProjectiles;

        public bool HasExplicitPixelLayer { get; set; }

        public bool ShouldDrawPixelated => HasExplicitPixelLayer && !ShouldBeRemovedFromRenderer;

        /// <summary>
        /// Draws the particle using the provided renderer settings and sprite batch.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="spritebatch"></param>
        /// <remarks>If you're drawing this particle pixelated, make sure to use <see cref="PixelationSystem.PixelationMatrix"/> whenever you interact with spritebatch, otherwise it will not draw properly.</remarks>
        public virtual void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch) { }

        public virtual void Update(ref ParticleRendererSettings settings) { }
        void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch)
        {
            if (OwningRenderer is not null)
                Draw(ref OwningRenderer.Settings, spriteBatch);
        }
     


    }
}
