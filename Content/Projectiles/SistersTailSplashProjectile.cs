using System.Collections.Generic;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace DivineRoot.Content.Projectiles
{
    public class SistersTailSplashProjectile : ModProjectile, IPixelatedPrimitiveRenderer
    {
        public override string Texture =>
            "DivineRoot/Content/Projectiles/SistersTailProjectile";

        private bool effectApplied;
        private readonly List<Vector2> tipHistory = new List<Vector2>();

        public PixelationPrimitiveLayer LayerToRenderTo => PixelationPrimitiveLayer.BeforeProjectiles;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.IsAWhip[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.DefaultToWhip();

            Projectile.width = 18;
            Projectile.height = 18;

            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ownerHitCheck = true;
            Projectile.extraUpdates = 1;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void PostAI()
        {
            if (Main.dedServ)
                return;

            List<Vector2> controlPoints = new List<Vector2>();
            Projectile.FillWhipControlPoints(Projectile, controlPoints);

            SistersTailShaderDrawer.RecordTipHistory(Projectile, controlPoints, tipHistory, maxPoints: 28);
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            SistersTailShaderDrawer.RenderWhipPrimitives(Projectile, tipHistory, empowered: true);
        }

        public override void SendExtraAI(System.IO.BinaryWriter writer)
        {
            writer.Write(effectApplied);
        }

        public override void ReceiveExtraAI(System.IO.BinaryReader reader)
        {
            effectApplied = reader.ReadBoolean();
        }

        public override void OnHitNPC(
            NPC target,
            NPC.HitInfo hit,
            int damageDone)
        {
            if (effectApplied)
                return;

            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.Item62, target.Center);

                for (int i = 0; i < 30; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(10f, 10f);

                    Dust.NewDust(
                        target.position,
                        target.width,
                        target.height,
                        DustID.Blood,
                        velocity.X,
                        velocity.Y,
                        100,
                        default,
                        2f
                    );
                }
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            effectApplied = true;
            Projectile.netUpdate = true;

            Player player = Main.player[Projectile.owner];

            if (player.statLife < player.statLifeMax2)
            {
                player.statLife += 5;
                player.HealEffect(5);
            }

            const float blastRadius = 160f;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (!npc.active ||
                    npc.friendly ||
                    npc.whoAmI == target.whoAmI ||
                    npc.Distance(target.Center) >= blastRadius)
                {
                    continue;
                }

                npc.SimpleStrikeNPC(
                    (int)(hit.Damage * 1.5f),
                    hit.HitDirection,
                    hit.Crit
                );

                npc.netUpdate = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            List<Vector2> controlPoints = new List<Vector2>();
            Projectile.FillWhipControlPoints(Projectile, controlPoints);

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int frameHeight = texture.Height / 3;

            Color whipTint = Color.Lerp(lightColor, new Color(255, 120, 130), 0.55f);

            for (int i = 0; i < controlPoints.Count - 1; i++)
            {
                Vector2 current = controlPoints[i];
                Vector2 next = controlPoints[i + 1];
                Vector2 distance = next - current;

                int frame = 1;
                if (i == 0)
                    frame = 0;
                else if (i >= controlPoints.Count - 2)
                    frame = 2;

                Rectangle sourceRectangle = new Rectangle(
                    0,
                    frame * frameHeight,
                    texture.Width,
                    frameHeight
                );

                Vector2 origin = new Vector2(texture.Width / 2f, 0f);
                float rotation = distance.ToRotation() - MathHelper.PiOver2;
                Vector2 scale = new Vector2(1f, distance.Length() / frameHeight * 1.05f);

                Main.EntitySpriteDraw(
                    texture,
                    current - Main.screenPosition,
                    sourceRectangle,
                    whipTint,
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