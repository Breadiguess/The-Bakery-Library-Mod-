using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace BreadLibrary.Core.Graphics.Metaballs.BuiltInMetaballs
{
    public sealed class BubbleMetaball : Metaball
    {
        private Asset<Effect> bubbleEffect;
        public override Color Color => Color.Azure;
        public override float Threshold => 0.45f;
        public override float EdgeSoftness => 0.18f;

        public override int MaxShaderInstances => 64;

        public override Effect PostProcessEffect => bubbleEffect?.Value;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            bubbleEffect = ModContent.Request<Effect>(
                "BreadLibrary/Assets/Effects/Metaballs/BubbleMetaballPostProcess",
                AssetRequestMode.ImmediateLoad
            );
        }

        public override void Unload()
        {
            bubbleEffect = null;
        }

        public override void ApplyPostProcessParameters(Effect effect, MetaballDrawContext context)
        {
            effect.Parameters["uTime"]?.SetValue(context.Time);
            effect.Parameters["uTextureSize"]?.SetValue(new Vector2(context.Target.Width, context.Target.Height));

            effect.Parameters["uBaseColor"]?.SetValue(Color.ToVector4());
            effect.Parameters["uRimColor"]?.SetValue(new Vector4(0.95f, 1f, 1f, 1f));
            effect.Parameters["uPinkColor"]?.SetValue(new Vector4(1f, 0.45f, 0.85f, 1f));
            effect.Parameters["uBlueColor"]?.SetValue(new Vector4(0.35f, 0.75f, 1f, 1f));
            effect.Parameters["uYellowColor"]?.SetValue(new Vector4(1f, 0.95f, 0.35f, 1f));

            effect.Parameters["uOpacity"]?.SetValue(0.78f);
            effect.Parameters["uRimPower"]?.SetValue(0.37f);
            effect.Parameters["uIridescenceStrength"]?.SetValue(10.55f);
        }

        public override void UpdateInstance(ref MetaballInstance instance, MetaballUpdateContext context)
        {
            float lifetimeCompletion = instance.LifetimeCompletion;
            float lifetimeRemaining = instance.LifetimeRemaining;

            instance.Center += instance.Velocity;

            instance.Velocity.X *= 0.985f;
            instance.Velocity.Y -= 0.035f;

            instance.Velocity.X += MathF.Sin(context.Time * 2.8f + instance.ai[0]) * 0.012f;
            instance.Velocity *= 0.988f;
            float wobble =
                MathF.Sin(context.Time * 5f + instance.ai[0]) * 0.045f +
                MathF.Sin(context.Time * 9f + instance.ai[1]) * 0.025f;

            float shrink = MathHelper.Lerp(1f, 0.15f, lifetimeCompletion * lifetimeCompletion);
            instance.Radius = instance.InitialRadius * shrink * (1f + wobble);

            instance.Strength = MathHelper.Lerp(1.2f, 0.65f, lifetimeCompletion);

            instance.Opacity = MathHelper.SmoothStep(1f, 0f, lifetimeCompletion);

            if (instance.TimeLeft > 0)
                instance.TimeLeft--;
        }
    }
}