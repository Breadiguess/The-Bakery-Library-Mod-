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

                foreach (Type type in GetLoadableTypes(assembly, mod))
                {
                    if (type is null)
                        continue;

                    if (type.IsAbstract || type.IsInterface)
                        continue;

                    if (!typeof(IBaseParticle).IsAssignableFrom(type))
                        continue;

                    if (!InitializedParticleTypes.Add(type))
                        continue;

                    IBaseParticle particle;

                    try
                    {
                        particle = (IBaseParticle)Activator.CreateInstance(type);
                    }
                    catch (Exception ex)
                    {
                        Mod.Logger.Warn($"Failed to create particle type '{type.FullName}' from mod '{mod.Name}'.", ex);
                        continue;
                    }

                    try
                    {
                        particle.SetStaticDefaults();
                    }
                    catch (Exception ex)
                    {
                        Mod.Logger.Warn($"Failed to run SetStaticDefaults for particle type '{type.FullName}' from mod '{mod.Name}'.", ex);
                    }
                }
            }
        }

        private IEnumerable<Type> GetLoadableTypes(Assembly assembly, Mod ownerMod)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (Exception loaderException in ex.LoaderExceptions.Where(e => e is not null))
                {
                    Mod.Logger.Warn(
                        $"Could not load one or more types from mod '{ownerMod.Name}' while scanning for particles: " +
                        loaderException.Message
                    );
                }

                return ex.Types.Where(t => t is not null)!;
            }
            catch (Exception ex)
            {
                Mod.Logger.Warn($"Failed to scan assembly for mod '{ownerMod.Name}'.", ex);
                return Enumerable.Empty<Type>();
            }
        }


        public override void Unload()
        {
            InitializedParticleTypes?.Clear();
        }
    }
}
