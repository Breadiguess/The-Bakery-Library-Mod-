using BreadLibrary.Core.Graphics.Metaballs;
using BreadLibrary.Core.Graphics.Metaballs.BuiltInMetaballs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreadLibrary.Content
{
    #if DEBUG
    internal class ExampleGun : ModItem 
    {
        private static float BulletCount => 4f;
        public override void SetDefaults()
        {
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 40;
            Item.useTime = 40;
            Item.noMelee = true;
            Item.shootSpeed = 10;
            Item.shoot = ModContent.ProjectileType<ExampleBullet>();
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {

            for(int i = 0; i< BulletCount; i++)
            {
                Vector2 Adj = velocity.RotatedBy(MathHelper.PiOver2* i/BulletCount - MathHelper.PiOver4);
                Adj = Adj.RotatedByRandom(MathHelper.ToRadians(10));
                Projectile.NewProjectile(source, position, Adj, type, damage, knockback);
            }



            return false;
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            position += Vector2.UnitY * -15;
        }
        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            player.ChangeDir(Math.Sign((Main.MouseWorld - player.Center).X));
            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;

            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 7f;
            Vector2 itemSize = new Vector2(50, 24);
            Vector2 itemOrigin = new Vector2(-17, 3);


            Vector2 origin = itemOrigin;
            origin.X *= player.direction;
            origin.Y *= player.gravDir;

            player.itemRotation = itemRotation;

                player.itemRotation *= player.direction;
            if (player.direction < 0)
                player.itemRotation += MathHelper.Pi;
            Vector2 consistentCenterAnchor = player.itemRotation.ToRotationVector2() * (itemSize.X / -2f - 10f) * player.direction;

            Vector2 consistentAnchor = consistentCenterAnchor - origin.RotatedBy(player.itemRotation);
            Vector2 offsetAgain = itemSize * -0.5f;
            Vector2 finalPosition = itemPosition + offsetAgain + consistentAnchor;
            player.itemLocation = finalPosition + new Vector2(itemSize.X * 0.5f, 0);
        }
        public override void UseItemFrame(Player player)
        {
            player.ChangeDir(Math.Sign((Main.MouseWorld - player.Center).X));

            float animProgress = 1 - player.itemTime / (float)player.itemTimeMax;
            float rotation = (player.Center - Main.MouseWorld).ToRotation() * player.gravDir + MathHelper.PiOver2;
            if (animProgress < 0.4f)
                rotation += -0.45f * (float)Math.Pow((0.4f - animProgress) / 0.4f, 2) * player.direction;

            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
        }
    }

    internal class ExampleBullet : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.hide = true;
            Projectile.timeLeft = 160;
        }
        public override void AI()
        {
            {
                MetaballSystem.Create<BubbleMetaball>(
                        Projectile.Center,
                        radius: 40f,
                        strength: 2f,
                        opacity: 1.1f,
                        timeLeft: 100,
                        velocity: Main.rand.NextVector2Unit() * Main.rand.NextFloat(1, 4) - Projectile.velocity,
                        initializer: (ref MetaballInstance instance) =>
                        {
                            instance.ai[0] = Main.rand.NextFloat(MathHelper.TwoPi);
                            instance.ai[1] = Main.rand.NextFloat(0.8f, 1.2f)*10;
                        });
            }
         

            if (Projectile.timeLeft < 100)
            {
                Projectile.velocity.Y += 0.2f;
            }
            Projectile.velocity *= 0.99f;

        }
    }
    #endif
}
