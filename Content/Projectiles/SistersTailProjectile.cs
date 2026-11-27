using System.Collections.Generic;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace DivineRoot.Content.Projectiles
{
    public class SistersTailProjectile : ModProjectile, IPixelatedPrimitiveRenderer
    {
        private readonly List<Vector2> tipHistory = new List<Vector2>();

        public PixelationPrimitiveLayer LayerToRenderTo => PixelationPrimitiveLayer.BeforeProjectiles;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.IsAWhip[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.DefaultToWhip();
            Projectile.WhipSettings.RangeMultiplier = 1f;
        }

        public override void OnHitNPC(
            NPC target,
            NPC.HitInfo hit,
            int damageDone)
        {
            target.AddBuff(311, 240);

            if (Projectile.owner >= 0 &&
                Projectile.owner < Main.maxPlayers)
            {
                Main.player[Projectile.owner]
                    .MinionAttackTargetNPC = target.whoAmI;
            }
        }

        public override void PostAI()
        {
            if (Main.dedServ)
                return;

            List<Vector2> controlPoints = new List<Vector2>();
            Projectile.FillWhipControlPoints(Projectile, controlPoints);

            SistersTailShaderDrawer.RecordTipHistory(Projectile, controlPoints, tipHistory, maxPoints: 20);

            for (int i = 1; i < controlPoints.Count; i++)
            {
                if (!Main.rand.NextBool(5))
                    continue;

                int dustIndex = Dust.NewDust(
                    controlPoints[i] - new Vector2(5f, 5f),
                    10,
                    10,
                    DustID.Blood,
                    0f,
                    0f,
                    100,
                    default,
                    1f
                );

                Dust dust = Main.dust[dustIndex];
                dust.noGravity = true;
                dust.velocity *= 0.5f;
                dust.scale *= 1.2f;
            }
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            SistersTailShaderDrawer.RenderWhipPrimitives(Projectile, tipHistory, empowered: false);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            List<Vector2> controlPoints = new List<Vector2>();
            Projectile.FillWhipControlPoints(Projectile, controlPoints);

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int frameHeight = texture.Height / 3;

            for (int i = 0; i < controlPoints.Count - 1; i += 2)
            {
                Vector2 current = controlPoints[i];
                Vector2 next = controlPoints[i + 1];
                Vector2 distance = next - current;

                int frame = 1;
                if (i == 0)
                    frame = 0;
                else if (i >= controlPoints.Count - 3)
                    frame = 2;

                Rectangle sourceRectangle = new Rectangle(
                    0,
                    frame * frameHeight,
                    texture.Width,
                    frameHeight
                );

                Vector2 origin = new Vector2(texture.Width / 2f, 0f);
                float rotation = distance.ToRotation() - MathHelper.PiOver2;
                Vector2 scale = new Vector2(1f, distance.Length() / frameHeight * 2.2f);

                Main.EntitySpriteDraw(
                    texture,
                    current - Main.screenPosition,
                    sourceRectangle,
                    lightColor,
                    rotation,
                    origin,
                    scale,
                    SpriteEffects.None,
                    0
                );
            }

            return false;
        }
    }
}