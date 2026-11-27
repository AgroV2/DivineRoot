using System;
using DivineRoot.Content.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace DivineRoot.Content.NPCs.PrimordialDemon
{
  public static class PrimordialClawAttack
  {
    public const int WarningDuration = 60;
    public const int BeamDuration = 26;
    public const int DamageWindow = 20;

    public const int FrameCount = 4;
    public const float ClawLength = 114f;
    public const float HalfLength = ClawLength * 0.5f;
    public const float DamageWidth = 40f;

    public const int StartDelay = 45;
    public const int ClawCount = 5;
    public const int ClawInterval = 42;
    public const int TotalDuration = StartDelay + (ClawCount - 1) * ClawInterval
      + WarningDuration + BeamDuration + 35;

    public static bool TryGetClaw(int tick, out int index)
    {
      index = -1;
      int local = tick - StartDelay;
      if (local < 0 || local % ClawInterval != 0)
        return false;
      index = local / ClawInterval;
      return index < ClawCount;
    }
  }

  public class PrimordialDemonClaw : ModProjectile
  {
    private const int TotalLife = PrimordialClawAttack.WarningDuration + PrimordialClawAttack.BeamDuration;

    private static Asset<Texture2D> prepareTexture;

    private bool strikeEffectsStarted;

    public override string Texture => "DivineRoot/Content/NPCs/PrimordialDemon/claw_sprite_purple_sheet";

    private float Age => TotalLife - Projectile.timeLeft;
    private float ClawAge => Age - PrimordialClawAttack.WarningDuration;
    private bool Striking => ClawAge >= 0f;
    private float Angle => Projectile.ai[0];
    private Vector2 Direction => Vector2.UnitY.RotatedBy(Angle);
    private Vector2 LineStart => Projectile.Center - Direction * PrimordialClawAttack.HalfLength;
    private Vector2 LineEnd => Projectile.Center + Direction * PrimordialClawAttack.HalfLength;

    private float WidthEnvelope => MathHelper.Clamp(ClawAge / 3f, 0f, 1f)
      * MathHelper.Clamp((PrimordialClawAttack.BeamDuration - ClawAge) / 6f, 0f, 1f);

    public override void Load()
    {
      if (!Main.dedServ)
        prepareTexture = ModContent.Request<Texture2D>("DivineRoot/Content/NPCs/PrimordialDemon/ClawPrepare");
    }

    public override void Unload()
    {
      prepareTexture = null;
    }

    public override void SetStaticDefaults()
    {
      Main.projFrames[Type] = PrimordialClawAttack.FrameCount;
      ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)PrimordialClawAttack.ClawLength + 192;
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

      if (!Striking)
      {
        if (!Main.dedServ)
          AddWarningLight();
        return;
      }

      Projectile.frame = Math.Min(PrimordialClawAttack.FrameCount - 1,
        (int)(ClawAge * PrimordialClawAttack.FrameCount / PrimordialClawAttack.BeamDuration));

      if (Main.dedServ)
        return;

      if (!strikeEffectsStarted)
      {
        strikeEffectsStarted = true;
        StartStrikeEffects();
      }

      AddStrikeLight();
      SpawnSparks();
    }

    public override bool? CanDamage() => Striking && ClawAge < PrimordialClawAttack.DamageWindow ? null : false;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
      if (!Striking || ClawAge >= PrimordialClawAttack.DamageWindow)
        return false;

      float collisionPoint = 0f;
      return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
        LineStart, LineEnd, PrimordialClawAttack.DamageWidth * WidthEnvelope, ref collisionPoint);
    }

    private void StartStrikeEffects()
    {
      SoundEngine.PlaySound(SoundID.Item71 with
      {
        Volume = 0.7f,
        Pitch = 0.15f,
        PitchVariance = 0.12f,
        MaxInstances = 4
      }, Projectile.Center);

      PrimordialDemonScreenShakeSystem.AddShake(Projectile.Center, 16f);

      Vector2 direction = Direction;
      Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);

      for (int i = 0; i < 24; i++)
      {
        Vector2 position = LineStart + direction * (PrimordialClawAttack.ClawLength * i / 23f);
        Vector2 velocity = normal * Main.rand.NextFloat(-4.5f, 4.5f)
          + direction * Main.rand.NextFloat(-1.8f, 1.8f);
        Dust shade = Dust.NewDustPerfect(position, i % 3 == 0 ? DustID.PurpleTorch : DustID.Shadowflame,
          velocity, 20, default, Main.rand.NextFloat(1.3f, 2.1f));
        shade.noGravity = i % 4 != 0;
      }

      for (int end = 0; end < 2; end++)
      {
        Vector2 origin = end == 0 ? LineStart : LineEnd;
        Vector2 outward = direction * (end == 0 ? -1f : 1f);

        for (int i = 0; i < 14; i++)
        {
          Vector2 velocity = outward.RotatedBy(Main.rand.NextFloat(-1.2f, 1.2f))
            * Main.rand.NextFloat(1.2f, 6.5f);
          Dust burst = Dust.NewDustPerfect(origin + Main.rand.NextVector2Circular(14f, 14f),
            i % 3 == 0 ? DustID.PurpleTorch : DustID.Shadowflame, velocity, 0,
            default, Main.rand.NextFloat(1.6f, 2.6f));
          burst.noGravity = true;
          burst.fadeIn = 0.5f;
        }
      }
    }

    private void AddWarningLight()
    {
      float progress = MathHelper.Clamp(Age / PrimordialClawAttack.WarningDuration, 0f, 1f);
      Lighting.AddLight(Projectile.Center, 0.4f * progress, 0.12f * progress, 0.55f * progress);
    }

    private void AddStrikeLight()
    {
      float strength = WidthEnvelope;
      Vector2 direction = Direction;
      for (int i = 0; i <= 4; i++)
      {
        Vector2 point = LineStart + direction * (PrimordialClawAttack.ClawLength * i / 4f);
        Lighting.AddLight(point, 0.85f * strength, 0.22f * strength, 1.15f * strength);
      }
    }

    private void SpawnSparks()
    {
      if (ClawAge >= PrimordialClawAttack.DamageWindow + 3f)
        return;

      Vector2 direction = Direction;
      Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
      for (int i = 0; i < 2; i++)
      {
        Vector2 position = LineStart
          + direction * Main.rand.NextFloat(0f, PrimordialClawAttack.ClawLength)
          + normal * Main.rand.NextFloat(-20f, 20f) * WidthEnvelope;

        Vector2 velocity = normal * Main.rand.NextFloat(-2.4f, 2.4f)
          + direction * Main.rand.NextFloat(-1f, 1f);
        Dust shade = Dust.NewDustPerfect(position, i == 0 ? DustID.PurpleTorch : DustID.Shadowflame,
          velocity, 30, default, Main.rand.NextFloat(0.9f, 1.5f));
        shade.noGravity = i != 1;
      }
    }

    public override bool PreDraw(ref Color lightColor)
    {
      if (!Striking)
      {
        DrawWarning();
        return false;
      }

      Texture2D texture = TextureAssets.Projectile[Type].Value;
      float envelope = WidthEnvelope;
      if (envelope <= 0f)
        return false;

      int frameHeight = texture.Height / PrimordialClawAttack.FrameCount;
      Rectangle source = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
      Vector2 origin = source.Size() * 0.5f;
      Vector2 position = Projectile.Center - Main.screenPosition;

      float pulse = 1f + (float)Math.Sin(ClawAge * 0.9f) * 0.035f;
      float opacity = MathHelper.Clamp((PrimordialClawAttack.BeamDuration - ClawAge) / 4f, 0f, 1f);

      Main.EntitySpriteDraw(texture, position, source, new Color(150, 60, 255, 0) * (0.35f * opacity),
        Angle, origin, pulse * 1.08f, SpriteEffects.None, 0);
      Main.EntitySpriteDraw(texture, position, source, Color.White * opacity,
        Angle, origin, pulse, SpriteEffects.None, 0);
      return false;
    }

    private void DrawWarning()
    {
      if (prepareTexture == null)
        return;

      float progress = MathHelper.Clamp(Age / PrimordialClawAttack.WarningDuration, 0f, 1f);
      float fadeIn = MathHelper.Clamp(Age / 6f, 0f, 1f);
      float pulse = 1f + (float)Math.Sin(Age * 0.35f) * 0.06f;
      float opacity = fadeIn * (0.3f + MathHelper.SmoothStep(0f, 1f, progress) * 0.5f);

      Texture2D texture = prepareTexture.Value;
      Vector2 origin = texture.Size() * 0.5f;
      Vector2 position = Projectile.Center - Main.screenPosition;

      Main.EntitySpriteDraw(texture, position, null, new Color(190, 120, 255) * (opacity * 0.4f),
        Angle, origin, pulse * 1.06f, SpriteEffects.None, 0);
      Main.EntitySpriteDraw(texture, position, null, Color.White * opacity,
        Angle, origin, pulse, SpriteEffects.None, 0);
    }
  }
}