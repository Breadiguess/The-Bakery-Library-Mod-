using BreadLibrary.Core.Graphics.PixelationShit;
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

    public abstract class BaseParticle<T> : IDrawPixelated, IPooledParticle where T : IPooledParticle, new()
    {
        public const int DEFAULT_POOL_CAPACITY = 150;


        public bool ShouldDrawPixelated => DrawsPixelated;
        public virtual bool DrawsPixelated => false;
        public static ParticlePool<T> Pool { get; } = new ParticlePool<T>(typeof(T).GetCustomAttribute<PoolCapacityAttribute>()?.Capacity ?? DEFAULT_POOL_CAPACITY, GetNewParticle);

        protected static T GetNewParticle() => new T();

        public bool IsRestingInPool { get; private set; }

        /// <summary>
        /// when this is true, the particle is removed from the renderer (and thus the world) at the end of the current frame.
        /// </summary>
        public bool ShouldBeRemovedFromRenderer { get; protected set; }
        public virtual PixelLayer PixelLayer { get; }        


        public virtual void FetchFromPool()
        {
            IsRestingInPool = false;
            ShouldBeRemovedFromRenderer = false;
        }

        public virtual void RestInPool()
        {
            IsRestingInPool = true;
        }

        public virtual void Draw(ref ParticleRendererSettings settings, SpriteBatch spritebatch)
        {
        }

        public virtual void Update(ref ParticleRendererSettings settings)
        {
        }

        public virtual void DrawPixelated(SpriteBatch spriteBatch)
        {
        }
    }
}
