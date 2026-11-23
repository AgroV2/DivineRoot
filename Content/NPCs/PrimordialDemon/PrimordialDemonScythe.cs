using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DivineRoot.Content.Systems;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace DivineRoot.Content.NPCs.PrimordialDemon
{
  public class PrimordialDemonScythe : ModProjectile
  {
    public const float ThrowSpeed = 15f;
    public const float MaxDistance = 42f * 16f;
    public const float ExplosionRadius = 7f * 16f;
    private const int ExplosionDuration = 22;
    private const int ExplosionDamageWindow = 12;

    private bool exploded;

    public override string Texture => "DivineRoot/Content/Items/Weapons/scytheEdem";

    private Vector2 Origin => new Vector2(Projectile.ai[0], Projectile.ai[1]);
    private ref float ExplosionTimer => ref Projectile.ai[2];
    private bool Exploding => ExplosionTimer > 0f;

    private float CurrentRadius => ExplosionRadius * MathHelper.Clamp(ExplosionTimer / 6f, 0.35f, 1f);

    public override void SetDefaults()
    {
      Projectile.width = 54;
      Projectile.height = 54;
      Projectile.hostile = true;
      Projectile.friendly = false;
      Projectile.penetrate = -1;
      Projectile.timeLeft = 600;
      Projectile.tileCollide = false;
      Projectile.ignoreWater = true;
      Projectile.aiStyle = -1;
      Projectile.netImportant = true;
      CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public override void AI()
    {
      if (Exploding)
      {
        Projectile.velocity = Vector2.Zero;
        ExplosionTimer++;

        if (!Main.dedServ)
        {
          float progress = ExplosionTimer / ExplosionDuration;
          Lighting.AddLight(Projectile.Center, 2f * (1f - progress), 0.6f * (1f - progress), 0.1f);

          for (int i = 0; i < 4; i++)
          {
            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
            Vector2 offset = angle.ToRotationVector2() * CurrentRadius * Main.rand.NextFloat(0.2f, 1f);
            Dust ember = Dust.NewDustPerfect(Projectile.Center + offset,
              Main.rand.NextBool(3) ? DustID.YellowTorch : DustID.Torch,
              offset * 0.04f, 0, default, Main.rand.NextFloat(1.4f, 2.6f));
            ember.noGravity = true;
          }
        }

        if (ExplosionTimer >= ExplosionDuration)
          Projectile.Kill();
        return;
      }

      Projectile.rotation += 0.42f;

      if (Vector2.Distance(Origin, Projectile.Center) >= MaxDistance)
      {
        Explode();
        return;
      }

      if (Main.dedServ)
        return;

      Lighting.AddLight(Projectile.Center, 1f, 0.3f, 0.05f);

      if (Main.rand.NextBool(2))
      {
        Dust ember = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(22f, 22f),
          Main.rand.NextBool(3) ? DustID.YellowTorch : DustID.Torch,
          -Projectile.velocity * 0.1f, 0, default, Main.rand.NextFloat(1.2f, 2.1f));
        ember.noGravity = true;
      }
    }

    private void Explode()
    {
      if (exploded)
        return;

      exploded = true;
      ExplosionTimer = 1f;
      Projectile.velocity = Vector2.Zero;
      Projectile.netUpdate = true;

      if (!Main.dedServ)
      {
        SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.9f, Pitch = -0.4f }, Projectile.Center);
        PrimordialDemonScreenShakeSystem.AddShake(Projectile.Center, 26f);

        for (int i = 0; i < 60; i++)
        {
          float angle = MathHelper.TwoPi * i / 60f + Main.rand.NextFloat(-0.05f, 0.05f);
          Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(3f, 9f);
          Dust ember = Dust.NewDustPerfect(Projectile.Center,
            i % 3 == 0 ? DustID.YellowTorch : DustID.Torch, velocity, 0, default,
            Main.rand.NextFloat(1.6f, 3f));
          ember.noGravity = true;
          ember.fadeIn = 0.6f;
        }
      }
    }

    public override bool? CanDamage() => !Exploding || ExplosionTimer < ExplosionDamageWindow ? null : false;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
      if (!Exploding)
        return null;

      if (ExplosionTimer >= ExplosionDamageWindow)
        return false;

      float radius = CurrentRadius;
      Vector2 closest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
      return Vector2.DistanceSquared(closest, Projectile.Center) <= radius * radius;
    }

    public override bool PreDraw(ref Color lightColor)
    {
      if (Exploding)
      {
        DrawExplosion();
        return false;
      }

      Texture2D texture = TextureAssets.Projectile[Type].Value;
      Vector2 origin = texture.Size() * 0.5f;

      Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null,
        new Color(255, 120, 40, 0) * 0.5f, Projectile.rotation, origin, 1.2f, SpriteEffects.None, 0);
      Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null,
        Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
      return false;
    }

    private void DrawExplosion()
    {
      Texture2D glow = TextureAssets.Extra[98].Value;
      Vector2 origin = glow.Size() * 0.5f;
      Vector2 position = Projectile.Center - Main.screenPosition;

      float progress = ExplosionTimer / ExplosionDuration;
      float opacity = 1f - progress;
      float radius = CurrentRadius;
      float baseScale = radius * 2f / glow.Width;

      Main.EntitySpriteDraw(glow, position, null, new Color(255, 40, 8, 0) * (0.55f * opacity),
        0f, origin, baseScale * 1.25f, SpriteEffects.None, 0);
      Main.EntitySpriteDraw(glow, position, null, new Color(255, 110, 25, 0) * (0.7f * opacity),
        0f, origin, baseScale, SpriteEffects.None, 0);
      Main.EntitySpriteDraw(glow, position, null, new Color(255, 215, 150, 0) * (0.8f * opacity),
        0f, origin, baseScale * 0.5f, SpriteEffects.None, 0);

      Texture2D pixel = TextureAssets.MagicPixel.Value;
      Rectangle source = new Rectangle(0, 0, 1, 1);
      int segments = 48;
      float ringWidth = MathHelper.TwoPi * radius / segments + 2f;

      for (int i = 0; i < segments; i++)
      {
        float angle = MathHelper.TwoPi * i / segments;
        Vector2 point = position + angle.ToRotationVector2() * radius;
        Main.EntitySpriteDraw(pixel, point, source, new Color(255, 170, 70, 0) * (0.85f * opacity),
          angle + MathHelper.PiOver2, new Vector2(0.5f, 0.5f),
          new Vector2(ringWidth, 5f * opacity + 1f), SpriteEffects.None, 0);
      }
    }
  }
}