using BreadLibrary.Core.Graphics.Metaballs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

public sealed class ToxicSludgeMetaball : Metaball
{
    private Asset<Effect> toxicEffect;

    public override Color Color => Color.DarkOliveGreen;

    public override float Threshold => 0.9f;
    public override float EdgeSoftness => 0.1f;

    public override int MaxShaderInstances => 64;



    public override Effect PostProcessEffect => toxicEffect?.Value;

    public override void Load()
    {
        if (Main.dedServ)
            return;

        toxicEffect = ModContent.Request<Effect>(
            "BreadLibrary/Assets/Effects/Metaballs/ToxicMetaballPostProcess",
            AssetRequestMode.ImmediateLoad
        );
    }

    public override void Unload()
    {
        toxicEffect = null;
    }

    public override void ApplyShaderParameters(Effect effect, MetaballDrawContext context)
    {
        effect.Parameters["uTime"]?.SetValue(context.Time);
    }

    public override void ApplyPostProcessParameters(Effect effect, MetaballDrawContext context)
    {
        effect.Parameters["uTime"]?.SetValue(context.Time);
        effect.Parameters["uScreenSize"]?.SetValue(context.ScreenSize);
        effect.Parameters["uPrimaryColor"]?.SetValue(Color.ToVector4());
        effect.Parameters["uSecondaryColor"]?.SetValue(new Color(25, 110, 45).ToVector4());
        effect.Parameters["uHighlightColor"]?.SetValue(new Color(200, 255, 220).ToVector4());
    }

    public override void UpdateInstance(ref MetaballInstance instance, MetaballUpdateContext context)
    {
        float lifetimeCompletion = instance.LifetimeCompletion;

        instance.Center += Collision.TileCollision(instance.Center, instance.Velocity, 10, 10, true, true);

        instance.Velocity *= 0.96f;
        instance.Velocity.Y += Main.LocalPlayer.gravity;
        instance.Strength = 1+4 * lifetimeCompletion;
        float wobble = MathF.Sin(context.Time * 8f + instance.ai[0]) * 0.08f;
        float shrink = instance.LifetimeRemaining;

        instance.Radius = instance.InitialRadius * shrink * (1f + wobble);
        instance.Opacity = MathHelper.Lerp(1f, 0f, lifetimeCompletion);

        if (instance.TimeLeft > 0)
            instance.TimeLeft--;
    }
}