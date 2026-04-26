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

    public abstract class BaseParticle<T> : ModType, IDrawPixelated, IPooledParticle where T : IPooledParticle, new()
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

        public bool ShouldDrawPixelated => DrawsPixelated;
        /// <summary>
        /// Override this to have the particle be drawn in the pixelated renderer instead of the normal one.
        /// </summary>
        public virtual bool DrawsPixelated => false;
     

        protected sealed override void Register()
        {

        }

        public sealed override void SetupContent()
        {
            this.SetStaticDefaults();
        }
        /// <summary>
        /// when this is true, the particle is removed from the renderer (and thus the world) at the end of the current frame.
        /// </summary>
        public bool ShouldBeRemovedFromRenderer { get; protected set; }
        /// <summary>
        /// the PixelLayer this particle should draw to.
        /// Only relevant if <see cref="DrawsPixelated"/> is true.
        /// </summary>
        /// <remarks>Note: this is shared between all instances of the particle.</remarks>
        public virtual PixelLayer PixelLayer { get; }        


      
        /// <summary>
        /// Draws the particle using the provided renderer settings and sprite batch.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="spritebatch"></param>
        /// <remarks>If you're drawing this particle pixelated, make sure to use <see cref="PixelationSystem.PixelationMatrix"/> whenever you interact with spritebatch, otherwise it will not draw properly.</remarks>
        public virtual void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
        {
        }

        public virtual void Update(ref ParticleRendererSettings settings)
        {
        }

        protected void DrawPixelated(SpriteBatch spriteBatch)
        {
            var engine = ParticleEngine.GetRenderer(this);
            if (engine is not null)
            {
                this.Draw(ref engine.Settings, spriteBatch);
            }
        }

        void IDrawPixelated.DrawPixelated(SpriteBatch spriteBatch)
        {
            DrawPixelated(spriteBatch);
        }
    }
}
