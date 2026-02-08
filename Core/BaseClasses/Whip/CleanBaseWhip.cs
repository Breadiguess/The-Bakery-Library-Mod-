
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace BreadLibrary.Core.BaseClasses.Whip;

/// <summary>
///     A cleaned-up, easier-to-extend base class for whip projectiles.
///     Subclasses override virtual properties/methods to change visuals/behavior.
/// </summary>
/// <remarks>
///     Key improvements:
///     - owner item type is captured at spawn time (safer than string compares)
///     - control points are calculated only when needed
///     - clearer FlyProgress and HitboxActive semantics
///     - fewer allocations, clearer override points
/// </remarks>
public abstract class CleanBaseWhip : ModProjectile
{
    public virtual string GetTagEffectName => "";

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.IsAWhip[Projectile.type] = true;
    }

    public override void SetDefaults()
    {
        Segments = DefaultSegments;
        RangeMult = DefaultRangeMult;
        Projectile.penetrate = -1;
        Projectile.DefaultToWhip();
    }

    public override void OnSpawn(IEntitySource source)
    {
        base.OnSpawn(source);

        if (Main.player[Projectile.owner].HeldItem != null)
        {
            _ownerItemType = Main.player[Projectile.owner].HeldItem.type;
        }
    }

    public override bool PreAI()
    {
        var player = Main.player[Projectile.owner];

        if (!_initialized)
        {
            _initialized = true;

            float ft = 0;
            var segs = 0;
            float range = 0;
            ModifyWhipSettings(ref ft, ref segs, ref range);

            _flyTime = ft > 0 ? ft : ComputeFlyTime();

            Segments = segs > 0 ? segs : DefaultSegments;
            //Main.NewText(Segments);
            RangeMult = range > 0 ? range : DefaultRangeMult;
            //Main.NewText(RangeMult);
            Projectile.spriteDirection = player.direction;
        }

        var toProjectile = player.MountedCenter.AngleTo(Projectile.Center) - MathHelper.PiOver2;
        var flyProgress = FlyProgress; // normalized 0→1
        var stretch = GetAnimatedArmStretch(flyProgress);

        player.SetCompositeArmFront(true, stretch, toProjectile);
        //Main.NewText(player.compositeFrontArm.stretch.ToString());

        Time++;
        Projectile.Center = Main.GetPlayerArmPosition(Projectile) + Projectile.velocity * (Projectile.ai[0] - 1f);

        Projectile.rotation = Projectile.velocity.ToRotation();

        if (Projectile.spriteDirection == -1)
        {
            Projectile.rotation += MathHelper.Pi;
        }

        Projectile.Center = Main.GetPlayerArmPosition(Projectile) + Projectile.velocity * FlyProgress;

        player.heldProj = Projectile.whoAmI;

        if (Projectile.velocity.LengthSquared() > 0.0001f)
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        if (Time >= _flyTime)
        {
            Projectile.Kill();

            return false;
        }

        // Play sound at the tip of the whip
        //todo: make this a virtual thing so that the time at which the whipcrack sounds can be modified
        if (Math.Abs(Time - _flyTime / 2f) < 1f)
        {
            if (WhipSound != null && _controlPoints.Count > 0)
            {
                SoundEngine.PlaySound(WhipSound.Value, _controlPoints[^1]);
            }
        }

        if (Projectile.ai[0] == (int)(_flyTime / 2f))
        {
            Projectile.WhipPointsForCollision.Clear();
            Projectile.FillWhipControlPoints(Projectile, Projectile.WhipPointsForCollision);
            var position = Projectile.WhipPointsForCollision[^1];

            if (WhipSound != null)
            {
                SoundEngine.PlaySound(WhipSound.Value, position);
            }
        }

        if (HitboxActive(FlyProgress))
        {
            var half = new Vector2(Projectile.width * Projectile.scale * 0.5f, 0f);

            for (var i = 0; i < _controlPoints.Count; i++)
            {
                DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
                Utils.PlotTileLine(_controlPoints[i] - half, _controlPoints[i] + half, Projectile.height * Projectile.scale, DelegateMethods.CutTiles);
            }
        }

        WhipAI();

        return false;
    }

    private Player.CompositeArmStretchAmount GetAnimatedArmStretch(float t)
    {
        // t goes from 0 → 1 over the whip's flytime
        if (t < 0.2f)
        {
            return Player.CompositeArmStretchAmount.None; // 0%–20%
        }

        if (t < 0.35f)
        {
            return Player.CompositeArmStretchAmount.ThreeQuarters; // 20%–35%
        }

        if (t < 0.55f)
        {
            return Player.CompositeArmStretchAmount.Full; // 35%–55%
        }

        if (t < 0.75f)
        {
            return Player.CompositeArmStretchAmount.Full; // 55%–75%
        }

        if (t < 0.9f)
        {
            return Player.CompositeArmStretchAmount.Full; // 75%–90%
        }

        return Player.CompositeArmStretchAmount.None; // 90%–100%
    }

    protected virtual void WhipAI() { }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        // set player's target for minions — maintain old behavior
        Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI;

        // If we have owner item type saved, use that as the tag identity (safer than comparing strings)
        if (_ownerItemType != ItemID.None)
        {
            if (Main.player[Projectile.owner].HeldItem == null)
            {
                return;
            }

            /*
            // Example: apply a "whip tag" effect using owner item type
            var global = target.GetGlobalNPC<WhipDebuffNPC>();
            if (global != null)
            {
                // Check existing tags for this item type
                bool found = false;
                foreach (var tp in global.Tags)
                {
                    if (tp.ItemType == _ownerItemType) // using int ID
                    {
                        found = true;
                        // refresh time if needed (example assumes bw.TagTime accessible via item)
                        var maybeItem = Main.player[Projectile.owner].HeldItem.ModItem as BaseWhipItem;
                        if (maybeItem != null && tp.TimeLeft < maybeItem.TagTime)
                            tp.TimeLeft = maybeItem.TagTime;
                        break;
                    }
                }
                if (!found)
                {
                    var maybeItem = Main.player[Projectile.owner].HeldItem.ModItem as BaseWhipItem;
                    if (maybeItem != null)
                    {
                        global.Tags.Add(new WhipTag(_ownerItemType, maybeItem.TagTime, maybeItem.TagDamage, maybeItem.TagDamageMult, maybeItem.TagCritChance, GetTagEffectName));
                    }
                }
            }*/
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        // Build our custom control points
        List<Vector2> betterPoints = new();
        GetWhipSettingsBetter(Projectile, out var flyTime, out var segments, out var rangeMult);
        FillWhipControlPointsBetter(Projectile, betterPoints, segments, rangeMult, flyTime);

        // Check collision along each whip segment
        for (var i = 1; i < betterPoints.Count; i++)
        {
            var p1 = betterPoints[i - 1];
            var p2 = betterPoints[i];

            var collisionPoint = 0f;

            if (Collision.CheckAABBvLineCollision
                (
                    targetHitbox.TopLeft(),
                    targetHitbox.Size(),
                    p1,
                    p2,
                    Projectile.width,
                    ref collisionPoint
                ))
            {
                return true;
            }
        }

        return false;
    }

    #region Values

    public ref float Time => ref Projectile.ai[0];

    protected static int DefaultSegments => 12;

    protected static float DefaultRangeMult => 1.0f;

    protected virtual int DefaultHandleHeight => 20;

    protected virtual int DefaultSegHeight => 16;

    protected virtual int DefaultEndHeight => 20;

    protected virtual int DefaultSegTypes => 2;

    public virtual SoundStyle? WhipSound => SoundID.Item153;

    public virtual Color StringColor => Color.White;

    private bool _initialized;

    private int _ownerItemType = ItemID.None; // saved on spawn

    private float _flyTime; // in ticks (accounting for MaxUpdates below)

    private readonly List<Vector2> _controlPoints = new();

    public virtual int Segments { get; set; }

    public virtual float RangeMult { get; set; }

    protected virtual int handleHeight => DefaultHandleHeight;

    protected virtual int segHeight => DefaultSegHeight;

    protected virtual int endHeight => DefaultEndHeight;

    protected virtual int segTypes => DefaultSegTypes;

    #endregion

    #region Helpers

    /// <summary>
    ///     compute fly time once per relevant change
    /// </summary>
    /// <returns></returns>
    protected virtual float ComputeFlyTime()
    {
        var p = Main.player[Projectile.owner];

        var baseAnim = p.itemAnimationMax > 0 ? p.itemAnimationMax : 20;

        return baseAnim * Projectile.MaxUpdates;
    }

    public static void FillWhipControlPointsBetter(Projectile proj, List<Vector2> controlPoints, int segments, float rangeMultiplier, float flyTime)
    {
        controlPoints.Clear();
        var player = Main.player[proj.owner];

        var start = Main.GetPlayerArmPosition(proj);
        var dir = proj.velocity.SafeNormalize(Vector2.UnitX);

        var progress = Math.Clamp(proj.ai[0] / flyTime, 0f, 1f);
        var whipLength = proj.velocity.Length() * rangeMultiplier * progress;

        var tip = start + dir * whipLength;

        // Always add the base (arm)
        controlPoints.Add(start);

        // Add intermediate segments, including the tip
        for (var i = 1; i <= segments; i++)
        {
            var t = i / (float)segments; // goes 0..1
            var point = Vector2.Lerp(start, tip, t);

            // Example sag/curve (optional)
            var sag = (float)Math.Sin(t * MathHelper.Pi) * 20f * (1f - progress);
            point += dir.RotatedBy(MathHelper.PiOver2) * sag;

            controlPoints.Add(point);
        }
    }

    public float FlyProgress => _flyTime <= 0 ? 0f : Time / _flyTime;

    protected virtual bool HitboxActive(float progress)
    {
        return progress >= 0.1f && progress <= 0.7f;
    }

    public static void GetWhipSettingsBetter(Projectile proj, out float timeToFlyOut, out int segments, out float rangeMultiplier)
    {
        timeToFlyOut = Main.player[proj.owner].itemAnimationMax * proj.MaxUpdates;
        segments = DefaultSegments;
        rangeMultiplier = DefaultRangeMult;

        if (proj.ModProjectile is CleanBaseWhip cbw)
        {
            segments = cbw.Segments;
            rangeMultiplier = cbw.RangeMult;
        }
    }

    /// <summary>
    ///     Call me in PreAI to let subclasses alter control points before drawing/collision
    /// </summary>
    /// <param name="points"></param>
    public virtual void ModifyControlPoints(List<Vector2> points) { }

    /// <summary>
    ///     kind of important???
    /// </summary>
    /// <param name="outFlyTime"></param>
    /// <param name="outSegments"></param>
    /// <param name="outRangeMult"></param>
    protected virtual void ModifyWhipSettings(ref float outFlyTime, ref int outSegments, ref float outRangeMult)
    {
        outSegments = Segments;
        outRangeMult = RangeMult;
        outFlyTime = ComputeFlyTime();
        GetWhipSettingsBetter(Projectile, out var fly, out var segs, out var range);
        Projectile.GetWhipSettings(Projectile, out outFlyTime, out outSegments, out outRangeMult);
        // Replace with values from our system
    }

    #endregion

    #region DrawCode
    private void DrawWhipPrimitive(List<Vector2> points, BasicEffect whipEffect, float baseWidth = 6f)
    {
        if (points.Count < 2)
            return;

        GraphicsDevice gd = Main.graphics.GraphicsDevice;

        whipEffect ??= new BasicEffect(gd)
        {
            TextureEnabled = true,
            VertexColorEnabled = true,
            LightingEnabled = false
        };

        Texture2D texture = TextureAssets.MagicPixel.Value;

        whipEffect.Texture = texture;
        whipEffect.View = Main.GameViewMatrix.TransformationMatrix;
        whipEffect.Projection = Matrix.CreateOrthographicOffCenter(
            0, Main.screenWidth,
            Main.screenHeight, 0,
            -1f, 1f
        );
        whipEffect.World = Matrix.Identity;

        List<VertexPositionColorTexture> verts = new();

        float totalLength = 0f;
        for (int i = 0; i < points.Count - 1; i++)
            totalLength += Vector2.Distance(points[i], points[i + 1]);

        float accumulated = 0f;

        for (int i = 0; i < points.Count; i++)
        {
            Vector2 p = points[i];
            Vector2 dir;

            if (i == 0)
                dir = points[1] - p;
            else if (i == points.Count - 1)
                dir = p - points[i - 1];
            else
                dir = points[i + 1] - points[i - 1];

            if (dir.LengthSquared() < 0.001f)
                continue;

            dir.Normalize();

            Vector2 normal = dir.RotatedBy(MathHelper.PiOver2);

            float t = accumulated / totalLength;
            float width = baseWidth * MathHelper.Lerp(1.2f, 0.4f, t); // taper

            Color color = Color.Crimson.MultiplyRGB(Lighting.GetColor(p.ToTileCoordinates()));
            float u = t;

            Vector2 screen = p - Main.screenPosition;

            verts.Add(new VertexPositionColorTexture(
                new Vector3(screen + normal * width, 0f),
                color,
                new Vector2(u, 0f)
            ));

            verts.Add(new VertexPositionColorTexture(
                new Vector3(screen - normal * width, 0f),
                color,
                new Vector2(u, 1f)
            ));

            if (i < points.Count - 1)
                accumulated += Vector2.Distance(points[i], points[i + 1]);
        }

        if (verts.Count < 4)
            return;

        gd.RasterizerState = RasterizerState.CullNone;
        gd.BlendState = BlendState.AlphaBlend;
        gd.DepthStencilState = DepthStencilState.None;
        gd.SamplerStates[0] = SamplerState.LinearClamp;

        foreach (EffectPass pass in whipEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            gd.DrawUserPrimitives(
                PrimitiveType.TriangleStrip,
                verts.ToArray(),
                0,
                verts.Count - 2
            );
        }
    }

    private List<Vector2> ResamplePolyline(
    List<Vector2> points,
    float spacing
)
    {
        List<Vector2> result = new();
        if (points.Count < 2)
            return result;

        result.Add(points[0]);

        Vector2 prev = points[0];
        float carry = 0f;

        for (int i = 1; i < points.Count; i++)
        {
            Vector2 curr = points[i];
            Vector2 delta = curr - prev;
            float length = delta.Length();

            if (length <= 0.0001f)
                continue;

            Vector2 dir = delta / length;

            float dist = spacing - carry;
            while (dist <= length)
            {
                Vector2 sample = prev + dir * dist;
                result.Add(sample);
                dist += spacing;
            }

            carry = length - (dist - spacing);
            prev = curr;
        }

        // Ensure the tip is included
        if (result[^1] != points[^1])
            result.Add(points[^1]);

        return result;
    }
    private readonly BasicEffect whipEffect;
    public sealed override bool PreDraw(ref Color lightColor)
    {
        List<Vector2> list = new();
        ModifyControlPoints(list);
        if (list.Count == 0) return false;

        float lodSpacing = 1f;
        List<Vector2> dense = ResamplePolyline(list, lodSpacing);

        DrawWhipPrimitive(dense, whipEffect, baseWidth: 4f);



        return false;
    }
   

    public virtual float GetSegScale(int segIndex, int segCount)
    {
        return 1f;
    }

    #endregion
}