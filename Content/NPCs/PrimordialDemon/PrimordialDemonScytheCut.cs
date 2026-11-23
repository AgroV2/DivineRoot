using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DivineRoot.Content.Systems;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace DivineRoot.Content.NPCs.PrimordialDemon
{
  public static class PrimordialSlashAttack
  {
    public const int SlashInterval = 30;
    public const int MinSlashes = 5;
    public const int MaxSlashes = 7;
    public const float DashRadius = 15f * 16f;
    public const int StartDelay = 30;
    public const int BurstDelay = 90;
    public const int BurstStagger = 22;
    public const int Recovery = 70;

    public static int TotalDuration(int slashCount) =>
      StartDelay + slashCount * SlashInterval + BurstDelay + slashCount * BurstStagger + Recovery;
  }

  public class PrimordialDemonScytheCut : ModProjectile
  {
    public const int BurstDuration = 24;
    public const int DamageWindow = 18;
    private const float DamageWidth = 44f;

    private bool burstStarted;

    public override string Texture => "DivineRoot/Content/NPCs/PrimordialDemon/DemonScytheCut";

    private float Angle => Projectile.ai[0];
    private float Length => Projectile.ai[1];
    private float Fuse => Projectile.ai[2];
    private Vector2 Direction => Angle.ToRotationVector2();
    private Vector2 LineStart => Projectile.Center - Direction * (Length * 0.5f);
    private Vector2 LineEnd => Projectile.Center + Direction * (Length * 0.5f);

    private float Age => Fuse + BurstDuration - Projectile.timeLeft;
    private float BurstAge => Age - Fuse;
    private bool Bursting => BurstAge >= 0f;

    private float WidthEnvelope => MathHelper.Clamp(BurstAge / 3f, 0f, 1f)
      * MathHelper.Clamp((BurstDuration - BurstAge) / 6f, 0f, 1f);

    public override void SetDefaults()
    {
      Projectile.width = 20;
      Projectile.height = 20;
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

    public override void OnSpawn(IEntitySource source)
    {
      Projectile.timeLeft = (int)Fuse + BurstDuration;
    }

    public override bool ShouldUpdatePosition() => false;

    public override void AI()
    {
      Projectile.rotation = Angle;
      Projectile.velocity = Vector2.Zero;

      if (Main.dedServ)
        return;

      if (!Bursting)
      {
        float charge = MathHelper.Clamp(Age / Fuse, 0f, 1f);
        if (Main.rand.NextBool(charge > 0.75f ? 2 : 5))
        {
          Vector2 point = LineStart + Direction * Main.rand.NextFloat(Length);
          Dust ember = Dust.NewDustPerfect(point, DustID.Torch,
            Main.rand.NextVector2Circular(0.8f, 0.8f), 0, default,
            Main.rand.NextFloat(0.8f, 1.5f) * (0.6f + charge));
          ember.noGravity = true;
        }

        for (int i = 0; i <= 6; i++)
          Lighting.AddLight(LineStart + Direction * (Length * i / 6f),
            0.5f * charge, 0.12f * charge, 0.02f * charge);
        return;
      }

      if (!burstStarted)
      {
        burstStarted = true;
        StartBurst();
      }

      float envelope = WidthEnvelope;
      for (int i = 0; i <= 8; i++)
        Lighting.AddLight(LineStart + Direction * (Length * i / 8f),
          1.2f * envelope, 0.35f * envelope, 0.05f * envelope);

      for (int i = 0; i < 2; i++)
      {
        Vector2 point = LineStart + Direction * Main.rand.NextFloat(Length)
          + Direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-20f, 20f) * envelope;
        Dust ember = Dust.NewDustPerfect(point, i == 0 ? DustID.YellowTorch : DustID.Torch,
          Main.rand.NextVector2Circular(2.5f, 2.5f), 0, default, Main.rand.NextFloat(1f, 1.8f));
        ember.noGravity = true;
      }
    }

    private void StartBurst()
    {
      SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.55f, Pitch = 0.15f, MaxInstances = 5 }, Projectile.Center);
      PrimordialDemonScreenShakeSystem.AddShake(Projectile.Center, 12f);

      Vector2 normal = Direction.RotatedBy(MathHelper.PiOver2);
      for (int i = 0; i < 34; i++)
      {
        Vector2 point = LineStart + Direction * (Length * i / 33f);
        Vector2 velocity = normal * Main.rand.NextFloat(-5f, 5f) + Direction * Main.rand.NextFloat(-1.5f, 1.5f);
        Dust ember = Dust.NewDustPerfect(point, i % 3 == 0 ? DustID.YellowTorch : DustID.Torch,
          velocity, 0, default, Main.rand.NextFloat(1.4f, 2.4f));
        ember.noGravity = true;
        ember.fadeIn = 0.5f;
      }
    }

    public override bool? CanDamage() => Bursting && BurstAge < DamageWindow ? null : false;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
      if (!Bursting || BurstAge >= DamageWindow)
        return false;

      float collisionPoint = 0f;
      return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
        LineStart, LineEnd, DamageWidth * WidthEnvelope, ref collisionPoint);
    }

    public override bool PreDraw(ref Color lightColor)
    {
      Texture2D texture = TextureAssets.Projectile[Type].Value;
      Vector2 origin = new Vector2(0f, texture.Height * 0.5f);
      Vector2 position = LineStart - Main.screenPosition;
      float stretch = Length / texture.Width;

      if (!Bursting)
      {
        float charge = MathHelper.Clamp(Age / Fuse, 0f, 1f);
        float fadeIn = MathHelper.Clamp(Age / 8f, 0f, 1f);
        float pulse = 1f + (float)Math.Sin(Age * 0.3f) * 0.15f;
        float opacity = fadeIn * (0.35f + charge * 0.5f);

        Main.EntitySpriteDraw(texture, position, null, new Color(255, 95, 20, 0) * (opacity * 0.5f),
          Angle, origin, new Vector2(stretch, 1.6f * pulse), SpriteEffects.None, 0);
        Main.EntitySpriteDraw(texture, position, null, new Color(255, 180, 90, 0) * opacity,
          Angle, origin, new Vector2(stretch, 0.75f), SpriteEffects.None, 0);
        return false;
      }

      float envelope = WidthEnvelope;
      if (envelope <= 0f)
        return false;

      float burstPulse = 1f + (float)Math.Sin(BurstAge * 0.8f) * 0.06f;

      Main.EntitySpriteDraw(texture, position, null, new Color(255, 45, 8, 0) * (0.45f * envelope),
        Angle, origin, new Vector2(stretch, 3.4f * envelope * burstPulse), SpriteEffects.None, 0);
      Main.EntitySpriteDraw(texture, position, null, new Color(255, 120, 25, 0) * (0.6f * envelope),
        Angle, origin, new Vector2(stretch, 2f * envelope), SpriteEffects.None, 0);
      Main.EntitySpriteDraw(texture, position, null, Color.White * envelope,
        Angle, origin, new Vector2(stretch, 1f * envelope), SpriteEffects.None, 0);
      return false;
    }
  }
}