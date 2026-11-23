using System;
using DivineRoot.Content.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace DivineRoot.Content.NPCs.PrimordialDemon
{
  public static class PrimordialCrossAttack
  {
    public const int WarningDuration = 60;
    public const int BeamDuration = 26;
    public const int DamageWindow = 20;

    public const float LineLength = 15f * 16f;
    public const float HalfLength = LineLength * 0.5f;
    public const float DamageWidth = 52f;

    public const int StartDelay = 45;
    public const int CrossCount = 5;
    public const int CrossInterval = 42;
    public const int TotalDuration = StartDelay + (CrossCount - 1) * CrossInterval
      + WarningDuration + BeamDuration + 35;

    public static bool TryGetCross(int tick, out int index)
    {
      index = -1;
      int local = tick - StartDelay;
      if (local < 0 || local % CrossInterval != 0)
        return false;
      index = local / CrossInterval;
      return index < CrossCount;
    }
  }

  public class PrimordialDemonCrossLaser : ModProjectile
  {
    private const float TextureScale = 1.1f;
    private const float WidthScale = 2.2f;
    private const int TotalLife = PrimordialCrossAttack.WarningDuration + PrimordialCrossAttack.BeamDuration;

    private bool fireEffectsStarted;

    public override string Texture => "DivineRoot/Content/NPCs/PrimordialDemon/DemonLaser";

    private float Age => TotalLife - Projectile.timeLeft;
    private float BeamAge => Age - PrimordialCrossAttack.WarningDuration;
    private bool Firing => BeamAge >= 0f;
    private float Angle => Projectile.ai[0];
    private Vector2 Direction => Angle.ToRotationVector2();
    private Vector2 LineStart => Projectile.Center - Direction * PrimordialCrossAttack.HalfLength;
    private Vector2 LineEnd => Projectile.Center + Direction * PrimordialCrossAttack.HalfLength;

    private float WidthEnvelope => MathHelper.Clamp(BeamAge / 3f, 0f, 1f)
      * MathHelper.Clamp((PrimordialCrossAttack.BeamDuration - BeamAge) / 6f, 0f, 1f);

    public override void SetStaticDefaults()
    {
      Main.projFrames[Type] = 2;
      ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)PrimordialCrossAttack.LineLength + 192;
    }

    public override void SetDefaults()
    {
      Projectile.width = 24;
      Projectile.height = 24;
      Projectile.hostile = true;
      Projectile.friendly = false;
      Projectile.penetrate = -1;
      Projectile.timeLeft = TotalLife;
      Projectile.tileCollide = false;
      Projectile.ignoreWater = true;
      Projectile.aiStyle = -1;
      Projectile.netImportant = true;
      CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public override bool ShouldUpdatePosition() => false;

    public override void AI()
    {
      Projectile.rotation = Angle;
      Projectile.velocity = Vector2.Zero;

      if (!Firing)
      {
        if (!Main.dedServ)
          AddWarningLight();
        return;
      }

      Projectile.frame = ((int)BeamAge / 3) % 2;

      if (Main.dedServ)
        return;

      if (!fireEffectsStarted)
      {
        fireEffectsStarted = true;
        StartFireEffects();
      }

      AddBeamLight();
      SpawnSparks();
      SpawnEndCaps();
    }

    public override bool? CanDamage() => Firing && BeamAge < PrimordialCrossAttack.DamageWindow ? null : false;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
      if (!Firing || BeamAge >= PrimordialCrossAttack.DamageWindow)
        return false;

      float collisionPoint = 0f;
      return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
        LineStart, LineEnd, PrimordialCrossAttack.DamageWidth * WidthEnvelope, ref collisionPoint);
    }

    private void StartFireEffects()
    {
      SoundEngine.PlaySound(SoundID.DD2_BetsyFireballShot with
      {
        Volume = 0.55f,
        Pitch = -0.1f,
        PitchVariance = 0.12f,
        MaxInstances = 4
      }, Projectile.Center);

      PrimordialDemonScreenShakeSystem.AddShake(Projectile.Center, 16f);

      Vector2 direction = Direction;
      Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);

      for (int i = 0; i < 30; i++)
      {
        Vector2 position = LineStart + direction * (PrimordialCrossAttack.LineLength * i / 29f);
        Vector2 velocity = normal * Main.rand.NextFloat(-4.5f, 4.5f)
          + direction * Main.rand.NextFloat(-1.8f, 1.8f);
        Dust ember = Dust.NewDustPerfect(position, i % 3 == 0 ? DustID.YellowTorch : DustID.Torch,
          velocity, 20, default, Main.rand.NextFloat(1.3f, 2.1f));
        ember.noGravity = i % 4 != 0;
      }

      for (int end = 0; end < 2; end++)
      {
        Vector2 origin = end == 0 ? LineStart : LineEnd;
        Vector2 outward = direction * (end == 0 ? -1f : 1f);

        for (int i = 0; i < 20; i++)
        {
          Vector2 velocity = outward.RotatedBy(Main.rand.NextFloat(-1.2f, 1.2f))
            * Main.rand.NextFloat(1.2f, 6.5f);
          Dust burst = Dust.NewDustPerfect(origin + Main.rand.NextVector2Circular(14f, 14f),
            i % 3 == 0 ? DustID.YellowTorch : DustID.Torch, velocity, 0,
            default, Main.rand.NextFloat(1.6f, 2.6f));
          burst.noGravity = true;
          burst.fadeIn = 0.5f;
        }
      }
    }

    private void AddWarningLight()
    {
      float progress = MathHelper.Clamp(Age / PrimordialCrossAttack.WarningDuration, 0f, 1f);
      Vector2 direction = Direction;
      for (int i = 0; i <= 8; i++)
      {
        Vector2 point = LineStart + direction * (PrimordialCrossAttack.LineLength * i / 8f);
        Lighting.AddLight(point, 0.45f * progress, 0.14f * progress, 0.02f * progress);
      }
    }

    private void AddBeamLight()
    {
      float strength = WidthEnvelope;
      Vector2 direction = Direction;
      for (int i = 0; i <= 10; i++)
      {
        Vector2 point = LineStart + direction * (PrimordialCrossAttack.LineLength * i / 10f);
        Lighting.AddLight(point, 1.15f * strength, 0.3f * strength, 0.04f * strength);
      }
    }

    private void SpawnSparks()
    {
      if (BeamAge >= PrimordialCrossAttack.DamageWindow + 3f)
        return;

      Vector2 direction = Direction;
      Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
      for (int i = 0; i < 3; i++)
      {
        Vector2 position = LineStart
          + direction * Main.rand.NextFloat(0f, PrimordialCrossAttack.LineLength)
          + normal * Main.rand.NextFloat(-26f, 26f) * WidthEnvelope;

        Vector2 velocity = normal * Main.rand.NextFloat(-2.4f, 2.4f)
          + direction * Main.rand.NextFloat(-1f, 1f);
        Dust ember = Dust.NewDustPerfect(position, i == 0 ? DustID.YellowTorch : DustID.Torch,
          velocity, 30, default, Main.rand.NextFloat(0.9f, 1.5f));
        ember.noGravity = i != 1;
      }
    }

    private void SpawnEndCaps()
    {
      float envelope = WidthEnvelope;
      if (envelope <= 0.05f)
        return;

      Vector2 direction = Direction;
      float capRadius = 15f * WidthScale * envelope;

      for (int end = 0; end < 2; end++)
      {
        Vector2 origin = end == 0 ? LineStart : LineEnd;
        Vector2 outward = direction * (end == 0 ? -1f : 1f);

        Lighting.AddLight(origin, 1.4f * envelope, 0.45f * envelope, 0.07f * envelope);

        for (int i = 0; i < 5; i++)
        {
          Vector2 offset = Main.rand.NextVector2Circular(capRadius, capRadius);
          Vector2 velocity = outward * Main.rand.NextFloat(0.3f, 2.4f)
            + Main.rand.NextVector2Circular(1.5f, 1.5f);
          Dust ember = Dust.NewDustPerfect(origin + offset,
            i % 2 == 0 ? DustID.Torch : DustID.YellowTorch, velocity, 0,
            default, Main.rand.NextFloat(1.3f, 2.3f) * (0.6f + envelope * 0.4f));
          ember.noGravity = true;
          ember.fadeIn = 0.45f;
        }
      }
    }

    public override bool PreDraw(ref Color lightColor)
    {
      if (!Firing)
      {
        DrawWarning();
        return false;
      }

      Texture2D texture = TextureAssets.Projectile[Type].Value;
      float envelope = WidthEnvelope;
      if (envelope <= 0f)
        return false;

      float pulse = 1f + (float)Math.Sin(BeamAge * 0.9f) * 0.035f;
      float bodyWidth = WidthScale * envelope * pulse;
      float opacity = MathHelper.Clamp((PrimordialCrossAttack.BeamDuration - BeamAge) / 4f, 0f, 1f);

      DrawBeamLayer(texture, bodyWidth * 2.5f, new Color(255, 38, 5, 0) * (0.24f * opacity));
      DrawBeamLayer(texture, bodyWidth * 1.6f, new Color(255, 105, 15, 0) * (0.3f * opacity));
      DrawBeamLayer(texture, bodyWidth, Color.White * opacity);
      return false;
    }

    private void DrawWarning()
    {
      float progress = MathHelper.Clamp(Age / PrimordialCrossAttack.WarningDuration, 0f, 1f);
      float fadeIn = MathHelper.Clamp(Age / 6f, 0f, 1f);
      float pulse = 1f + (float)Math.Sin(Age * 0.35f) * 0.12f;
      float opacity = fadeIn * (0.3f + MathHelper.SmoothStep(0f, 1f, progress) * 0.5f);

      Texture2D pixel = TextureAssets.MagicPixel.Value;
      Rectangle source = new Rectangle(0, 0, 1, 1);
      Vector2 origin = new Vector2(0f, 0.5f);
      Vector2 position = LineStart - Main.screenPosition;
      float width = MathHelper.Lerp(16f, 32f, progress) * pulse;

      Main.EntitySpriteDraw(pixel, position, source, new Color(255, 110, 20, 0) * (opacity * 0.35f),
        Angle, origin, new Vector2(PrimordialCrossAttack.LineLength, width), SpriteEffects.None, 0);
      Main.EntitySpriteDraw(pixel, position, source, new Color(255, 150, 45, 0) * (opacity * 0.55f),
        Angle, origin, new Vector2(PrimordialCrossAttack.LineLength, width * 0.45f), SpriteEffects.None, 0);
      Main.EntitySpriteDraw(pixel, position, source, new Color(255, 205, 120) * (opacity * 0.85f),
        Angle, origin, new Vector2(PrimordialCrossAttack.LineLength, 2.4f), SpriteEffects.None, 0);
    }

    private void DrawBeamLayer(Texture2D texture, float width, Color color)
    {
      int frameHeight = texture.Height / 2;
      int sourceWidth = texture.Width - 2;
      float stride = (sourceWidth - 2) * TextureScale;
      Vector2 direction = Direction;
      Vector2 start = LineStart;

      for (float distance = 0f; distance < PrimordialCrossAttack.LineLength; distance += stride)
      {
        Vector2 position = start + direction * distance;
        int tileWidth = Math.Min(sourceWidth,
          (int)Math.Ceiling((PrimordialCrossAttack.LineLength - distance) / TextureScale));
        if (tileWidth <= 0)
          break;

        Rectangle source = new Rectangle(1, Projectile.frame * frameHeight, tileWidth, frameHeight);
        Vector2 origin = new Vector2(0f, frameHeight * 0.5f);

        Main.EntitySpriteDraw(texture, position - Main.screenPosition, source, color,
          Projectile.rotation, origin, new Vector2(TextureScale, width), SpriteEffects.None, 0);
      }
    }
  }
}