using BreadLibrary.Core.Graphics.Pixelation;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria.Graphics.Renderers;

namespace BreadLibrary.Core.Graphics.Particles
{
    internal class ParticleRendererILEditSystem : ModSystem
    {
        public override void Load()
        {
            IL_ParticleRenderer.Draw += SkipPixelatedParticlesInVanillaRenderer;

        }

        public override void Unload()
        {
            IL_ParticleRenderer.Draw -= SkipPixelatedParticlesInVanillaRenderer;
        }

        private static void SkipPixelatedParticlesInVanillaRenderer(ILContext il)
        {
            ILCursor c = new(il);

            while (c.TryGotoNext(
                MoveType.After,
                i => i.MatchCallvirt<IParticle>("get_ShouldBeRemovedFromRenderer")))
            {
                // We want to transform the result so the effective condition also
                // respects the pixelated skip rule.

                // Move back a little so we can grab the particle instance load that
                // was used for get_ShouldBeRemovedFromRenderer.
                int startIndex = c.Index;

                c.Index = startIndex - 1;

                if (!c.TryGotoPrev(
                    MoveType.After,
                    i => i.MatchCallvirt<List<IParticle>>("get_Item") || i.MatchLdloc(out _)))
                {
                    c.Index = startIndex;
                    continue;
                }

                c.Index = startIndex;

                // At this point the bool from ShouldBeRemovedFromRenderer is on the stack.
                // we need to take should remove, and then transplant our method in as well.

                c.Emit(OpCodes.Ldarg_0); // ParticleRenderer this
                c.Emit(OpCodes.Ldarg_1); // SpriteBatch spriteBatch

                // We actually need the current particle, so we fetch it again from the renderer list + loop index.
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(ParticleRenderer).GetField(nameof(ParticleRenderer.Particles)));
                c.Emit(OpCodes.Ldloc_0);
                c.Emit(OpCodes.Callvirt, typeof(System.Collections.Generic.List<IParticle>).GetMethod("get_Item"));

                c.EmitDelegate<Func<bool, ParticleRenderer, Microsoft.Xna.Framework.Graphics.SpriteBatch, IParticle, bool>>(
                    static (shouldRemove, renderer, spriteBatch, particle) =>
                    {
                        return shouldRemove || ShouldSkipVanillaParticleDraw(particle);
                    });
            }
        }

        private static bool ShouldSkipVanillaParticleDraw(IParticle particle)
        {
            return particle is IDrawPixelated pixelated && pixelated.ShouldDrawPixelated;
        }
    }
}
