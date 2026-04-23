using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Terraria.Graphics.Renderers;

namespace BreadLibrary.Core.Graphics.Particles
{
    public sealed class ParticleLoaderSystem : ModSystem
    {
        private static readonly HashSet<Type> InitializedParticleTypes = new();

        public override void PostSetupContent()
        {
            foreach (Mod mod in ModLoader.Mods)
            {
                Assembly assembly = mod.Code;
                if (assembly is null)
                    continue;



                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsAbstract)
                        continue;

                    if (!typeof(IBaseParticle).IsAssignableFrom(type))
                        continue;

                    if (InitializedParticleTypes.Contains(type))
                        continue;

                    if (Activator.CreateInstance(type) is not IBaseParticle particle)
                        continue;

                    particle.SetStaticDefaults();
                    InitializedParticleTypes.Add(type);

                }
            }
        }

        public override void Unload()
        {
            InitializedParticleTypes?.Clear();
        }
    }
}
