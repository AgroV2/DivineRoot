  using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DivineRoot.Content.Projectiles
{
    public class BloodSpikeProjectile : ModProjectile
    {
        private int currentFrame;
        private int frameCounter;

        private const int TotalFrames = 6;
        private const int FrameWidth = 192 / TotalFrames;
        private const int FrameHeight = 200;

        private bool initialized;
        private int spawnDelay;
        private float spikeScale = 1f;

        private bool soundPlayed;

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;

            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;

            Projectile.timeLeft = 50;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
        }

        public override void AI()
        {
            if (!initialized)
            {
                initialized = true;

                spawnDelay = Main.rand.Next(0, 8);
                spikeScale = Main.rand.NextFloat(0.6f, 1.2f);

                Vector2 targetPosition =
                    new Vector2(
                        Projectile.ai[0],
                        Projectile.ai[1]
                    );

                Vector2 toTarget =
                    targetPosition - Projectile.Center;

                Projectile.rotation =
                    toTarget.ToRotation() +
                    MathHelper.PiOver2;

                Projectile.rotation +=
                    Main.rand.NextFloat(-0.15f, 0.15f);

                Projectile.netUpdate = true;
            }

            if (spawnDelay > 0)
            {
                spawnDelay--;
                return;
            }

            if (!soundPlayed)
            {
                soundPlayed = true;

                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(
                        SoundID.NPCHit13 with
                        {
                            Volume = 0.5f,
                            Pitch = Main.rand.NextFloat(-0.3f, 0.3f)
                        },
                        Projectile.Center
                    );

                    SpawnBloodBurst();
                }
            }

            if (Projectile.alpha > 0)
            {
                Projectile.alpha -= 70;

                if (Projectile.alpha < 0)
                    Projectile.alpha = 0;
            }

            if (currentFrame < TotalFrames - 1)
            {
                frameCounter++;

                if (frameCounter >= 2)
                {
                    frameCounter = 0;
                    currentFrame++;

                    if (!Main.dedServ)
                        SpawnGrowthBlood();

                    Projectile.netUpdate = true;
                }
            }

            Projectile.velocity = Vector2.Zero;

            if (!Main.dedServ)
            {
                if (Main.rand.NextBool(4))
                    SpawnBaseBlood();

                if (currentFrame >= 2 &&
                    Main.rand.NextBool(6))
                {
                    SpawnDripBlood();
                }
            }

            if (Projectile.timeLeft < 10)
            {
                Projectile.alpha += 26;

                if (Projectile.alpha > 255)
                    Projectile.alpha = 255;
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(initialized);
            writer.Write(spawnDelay);
            writer.Write(spikeScale);
            writer.Write(currentFrame);
            writer.Write(frameCounter);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            initialized = reader.ReadBoolean();
            spawnDelay = reader.ReadInt32();
            spikeScale = reader.ReadSingle();
            currentFrame = reader.ReadInt32();
            frameCounter = reader.ReadInt32();
        }

        private void SpawnBloodBurst()
        {
            Vector2 baseDirection =
                (Projectile.rotation -
                MathHelper.PiOver2).ToRotationVector2();

            for (int i = 0; i < 12; i++)
            {
                Vector2 dustVelocity =
                    baseDirection.RotatedBy(
                        Main.rand.NextFloat(-1.2f, 1.2f)
                    ) * Main.rand.NextFloat(1f, 4f);

                dustVelocity +=
                    baseDirection *
                    Main.rand.NextFloat(0.5f, 2f);

                Dust dust = Dust.NewDustDirect(
                    Projectile.Center - new Vector2(4f, 4f),
                    8,
                    8,
                    DustID.Blood,
                    dustVelocity.X,
                    dustVelocity.Y
                );

                dust.noGravity = Main.rand.NextBool(3);
                dust.scale = Main.rand.NextFloat(1.2f, 2f);
            }

            for (int i = 0; i < 5; i++)
            {
                int goreIndex = Gore.NewGore(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    GoreID.ChimneySmoke1
                );

                Gore gore = Main.gore[goreIndex];

                gore.velocity =
                    baseDirection.RotatedBy(
                        Main.rand.NextFloat(-0.8f, 0.8f)
                    ) * Main.rand.NextFloat(1f, 3f);

                gore.scale =
                    Main.rand.NextFloat(0.4f, 0.7f);
            }
        }

        private void SpawnGrowthBlood()
        {
            Vector2 direction =
                (Projectile.rotation -
                MathHelper.PiOver2).ToRotationVector2();

            float length =
                FrameHeight *
                spikeScale *
                ((currentFrame + 1f) / TotalFrames);

            Vector2 tipPosition =
                Projectile.Center +
                direction * length * 0.7f;

            for (int i = 0; i < 4; i++)
            {
                Vector2 dustVelocity =
                    direction.RotatedBy(
                        Main.rand.NextFloat(-0.5f, 0.5f)
                    ) * Main.rand.NextFloat(2f, 5f);

                Dust dust = Dust.NewDustDirect(
                    tipPosition,
                    4,
                    4,
                    DustID.Blood,
                    dustVelocity.X,
                    dustVelocity.Y
                );

                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(1f, 1.5f);
            }
        }

        private void SpawnBaseBlood()
        {
            Vector2 offset =
                new Vector2(
                    Main.rand.NextFloat(-10f, 10f),
                    Main.rand.NextFloat(-10f, 10f)
                );

            Dust dust = Dust.NewDustDirect(
                Projectile.Center + offset,
                4,
                4,
                DustID.Blood
            );

            dust.noGravity = true;
            dust.velocity *= 0.2f;
            dust.scale = Main.rand.NextFloat(0.8f, 1.3f);
        }

        private void SpawnDripBlood()
        {
            Vector2 direction =
                (Projectile.rotation -
                MathHelper.PiOver2).ToRotationVector2();

            float randomDistance =
                Main.rand.NextFloat(0.2f, 0.8f) *
                FrameHeight *
                spikeScale;

            Vector2 dripPosition =
                Projectile.Center +
                direction * randomDistance;

            Vector2 perpendicular =
                new Vector2(-direction.Y, direction.X);

            dripPosition +=
                perpendicular *
                Main.rand.NextFloat(-8f, 8f);

            Dust dust = Dust.NewDustDirect(
                dripPosition,
                2,
                2,
                DustID.Blood
            );

            dust.noGravity = false;
            dust.velocity = new Vector2(
                Main.rand.NextFloat(-0.5f, 0.5f),
                Main.rand.NextFloat(1f, 2f)
            );

            dust.scale = Main.rand.NextFloat(0.9f, 1.2f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.alpha >= 255)
                return false;

            Texture2D texture =
                Terraria.GameContent.TextureAssets
                .Projectile[Projectile.type].Value;

            Rectangle sourceRectangle =
                new Rectangle(
                    currentFrame * FrameWidth,
                    0,
                    FrameWidth,
                    FrameHeight
                );

            Vector2 origin =
                new Vector2(
                    FrameWidth / 2f,
                    FrameHeight
                );

            Color drawColor =
                Lighting.GetColor(
                    (int)(Projectile.Center.X / 16f),
                    (int)(Projectile.Center.Y / 16f)
                );

            drawColor *=
                (255f - Projectile.alpha) / 255f;

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                sourceRectangle,
                drawColor,
                Projectile.rotation,
                origin,
                spikeScale,
                SpriteEffects.None,
                0
            );

            return false;
        }

        public override bool? Colliding(
            Rectangle projHitbox,
            Rectangle targetHitbox)
        {
            if (Projectile.alpha > 200)
                return false;

            Vector2 direction =
                (Projectile.rotation -
                MathHelper.PiOver2).ToRotationVector2();

            float length =
                FrameHeight *
                spikeScale *
                ((currentFrame + 1f) / TotalFrames);

            Vector2 tip =
                Projectile.Center +
                direction * length;

            float point = 0f;

            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center,
                tip,
                14f * spikeScale,
                ref point
            );
        }

        public override void OnHitNPC(
            NPC target,
            NPC.HitInfo hit,
            int damageDone)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity =
                    Main.rand.NextVector2Circular(4f, 4f);

                Dust dust = Dust.NewDustDirect(
                    target.Center,
                    8,
                    8,
                    DustID.Blood,
                    velocity.X,
                    velocity.Y
                );

                dust.noGravity = Main.rand.NextBool();
                dust.scale = Main.rand.NextFloat(1f, 1.6f);
            }
        }
    }
}