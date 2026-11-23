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
  public class PrimordialDemonLaser : ModProjectile
  {
    public const float BeamLength = 2400f;
    public const int Lifetime = PrimordialEmberAttack.BeamDuration;
    private const float TextureScale = 1.1f;
    private const float WidthScale = 2.2f;
    private const float DamageWidth = 60f;
    private bool visualEffectsStarted;

    private float Age => Projectile.ai[2];
    private Vector2 Direction => Projectile.velocity.SafeNormalize(Vector2.UnitX);
    private float WidthEnvelope => MathHelper.Clamp(Age / 3f, 0f, 1f)
      * MathHelper.Clamp((Lifetime - Age) / 6f, 0f, 1f);

    public override string Texture => "DivineRoot/Content/NPCs/PrimordialDemon/DemonLaser";

    public override void SetStaticDefaults()
    {
      Main.projFrames[Type] = 2;
      ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)BeamLength + 192;
    }

    public override void SetDefaults()
    {
      Projectile.width = 24;
      Projectile.height = 24;
      Projectile.hostile = true;
      Projectile.friendly = false;
      Projectile.penetrate = -1;
      Projectile.timeLeft = Lifetime;
      Projectile.tileCollide = false;
      Projectile.ignoreWater = true;
      Projectile.aiStyle = -1;
      Projectile.netImportant = true;
      CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public override bool ShouldUpdatePosition() => false;

    public override void AI()
    {
      int ownerIndex = (int)Projectile.ai[0];
      if (ownerIndex < 0 || ownerIndex >= Main.maxNPCs)
      {
        Projectile.Kill();
        return;
      }

      NPC owner = Main.npc[ownerIndex];
      if (!owner.active || owner.life <= 0 || owner.ModNPC is not PrimordialDemon demon)
      {
        Projectile.Kill();
        return;
      }

      if (!demon.EmberAttackActive && (Main.netMode != NetmodeID.MultiplayerClient || Age >= 3f))
      {
        Projectile.Kill();
        return;
      }

      Projectile.velocity = Direction;
      Projectile.rotation = Projectile.velocity.ToRotation();
      Projectile.ai[2]++;
      Projectile.frame = ((int)Age / 3) % 2;

      if (Main.dedServ)
        return;

      if (!visualEffectsStarted && Age <= 4f)
      {
        visualEffectsStarted = true;
        StartVisualEffects();
      }

      AddBeamLight();
      SpawnSparks();
      SpawnEndCaps();
    }

    public override bool? CanDamage() => Age >= 2f && Age < 22f ? null : false;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
      if (Age < 2f || Age >= 22f)
        return false;

      float collisionPoint = 0f;
      return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
        Projectile.Center, Projectile.Center + Direction * BeamLength,
        DamageWidth * WidthEnvelope, ref collisionPoint);
    }

    private void StartVisualEffects()
    {
      SoundEngine.PlaySound(SoundID.DD2_BetsyFireballShot with
      {
        Volume = 0.8f,
        Pitch = -0.35f,
        PitchVariance = 0.08f,
        MaxInstances = 3
      }, Projectile.Center);
      SoundEngine.PlaySound(SoundID.Item74 with
      {
        Volume = 0.42f,
        Pitch = -0.55f,
        PitchVariance = 0.08f,
        MaxInstances = 3
      }, Projectile.Center);

      Vector2 beamDirection = Direction;
      float nearestDistance = MathHelper.Clamp(Vector2.Dot(Main.LocalPlayer.Center - Projectile.Center, beamDirection), 0f, BeamLength);
      Vector2 shakeOrigin = Projectile.Center + beamDirection * nearestDistance;
      PrimordialDemonScreenShakeSystem.AddShake(shakeOrigin, 28f);

      Vector2 direction = Direction;
      for (int i = 0; i < 30; i++)
      {
        Vector2 velocity = direction.RotatedBy(Main.rand.NextFloat(-0.9f, 0.9f))
          * Main.rand.NextFloat(3f, 12f);
        Dust ember = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(14f, 14f),
          i % 3 == 0 ? DustID.YellowTorch : DustID.Torch, velocity, 20,
          default, Main.rand.NextFloat(1.4f, 2.4f));
        ember.noGravity = i % 4 != 0;
      }

      Vector2 tip = Projectile.Center + direction * BeamLength;
      if (NearScreen(tip, 200f))
      {
        for (int i = 0; i < 24; i++)
        {
          Vector2 velocity = direction.RotatedBy(Main.rand.NextFloat(-1.3f, 1.3f))
            * Main.rand.NextFloat(1.5f, 7f);
          Dust burst = Dust.NewDustPerfect(tip + Main.rand.NextVector2Circular(18f, 18f),
            i % 3 == 0 ? DustID.YellowTorch : DustID.Torch, velocity, 0,
            default, Main.rand.NextFloat(1.7f, 2.8f));
          burst.noGravity = true;
          burst.fadeIn = 0.5f;
        }
      }
    }

    private void AddBeamLight()
    {
      Vector2 direction = Direction;
      float strength = WidthEnvelope;
      for (int i = 0; i <= 20; i++)
      {
        Vector2 point = Projectile.Center + direction * (BeamLength * i / 20f);
        if (NearScreen(point, 240f))
          Lighting.AddLight(point, 1.05f * strength, 0.28f * strength, 0.035f * strength);
      }
    }

    private void SpawnSparks()
    {
      if (Age >= 23f)
        return;

      Vector2 direction = Direction;
      Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
      for (int i = 0; i < 3; i++)
      {
        Vector2 position = Projectile.Center + direction * Main.rand.NextFloat(0f, BeamLength)
          + normal * Main.rand.NextFloat(-34f, 34f) * WidthEnvelope;
        if (!NearScreen(position, 100f))
          continue;

        Vector2 velocity = direction * Main.rand.NextFloat(1f, 4f)
          + normal * Main.rand.NextFloat(-2.6f, 2.6f);
        Dust ember = Dust.NewDustPerfect(position, i == 0 ? DustID.YellowTorch : DustID.Torch,
          velocity, 30, default, Main.rand.NextFloat(0.9f, 1.5f));
        ember.noGravity = i != 2;
        if (i == 2)
          ember.velocity.Y += 0.8f;
      }
    }

    private void SpawnEndCaps()
    {
      if (Age >= 24f)
        return;

      float envelope = WidthEnvelope;
      if (envelope <= 0.05f)
        return;

      Vector2 direction = Direction;
      float capRadius = 16f * WidthScale * envelope;

      for (int end = 0; end < 2; end++)
      {
        Vector2 origin = end == 0
          ? Projectile.Center
          : Projectile.Center + direction * BeamLength;
        Vector2 outward = direction * (end == 0 ? -1f : 1f);

        if (!NearScreen(origin, 160f))
          continue;

        Lighting.AddLight(origin, 1.4f * envelope, 0.45f * envelope, 0.07f * envelope);

        for (int i = 0; i < 6; i++)
        {
          Vector2 offset = Main.rand.NextVector2Circular(capRadius, capRadius);
          Vector2 velocity = outward * Main.rand.NextFloat(0.3f, 2.8f)
            + Main.rand.NextVector2Circular(1.6f, 1.6f);
          Dust ember = Dust.NewDustPerfect(origin + offset,
            i % 2 == 0 ? DustID.Torch : DustID.YellowTorch, velocity, 0,
            default, Main.rand.NextFloat(1.4f, 2.4f) * (0.6f + envelope * 0.4f));
          ember.noGravity = true;
          ember.fadeIn = 0.45f;
        }
      }
    }

    private static bool NearScreen(Vector2 position, float padding)
    {
      return position.X >= Main.screenPosition.X - padding
        && position.X <= Main.screenPosition.X + Main.screenWidth + padding
        && position.Y >= Main.screenPosition.Y - padding
        && position.Y <= Main.screenPosition.Y + Main.screenHeight + padding;
    }

    public override bool PreDraw(ref Color lightColor)
    {
      Texture2D texture = TextureAssets.Projectile[Type].Value;
      float envelope = WidthEnvelope;
      if (envelope <= 0f)
        return false;

      float pulse = 1f + (float)Math.Sin(Age * 0.9f) * 0.035f;
      float bodyWidth = WidthScale * envelope * pulse;
      float opacity = MathHelper.Clamp((Lifetime - Age) / 4f, 0f, 1f);

      DrawBeamLayer(texture, bodyWidth * 2.5f, new Color(255, 38, 5, 0) * (0.24f * opacity));
      DrawBeamLayer(texture, bodyWidth * 1.6f, new Color(255, 105, 15, 0) * (0.3f * opacity));
      DrawBeamLayer(texture, bodyWidth, Color.White * opacity);
      return false;
    }

    private void DrawBeamLayer(Texture2D texture, float width, Color color)
    {
      int frameHeight = texture.Height / 2;
      int sourceWidth = texture.Width - 2;
      float stride = (sourceWidth - 2) * TextureScale;
      Vector2 direction = Direction;

      for (float distance = 0f; distance < BeamLength; distance += stride)
      {
        Vector2 position = Projectile.Center + direction * distance;
        if (!NearScreen(position, 180f))
          continue;

        int tileWidth = Math.Min(sourceWidth, (int)Math.Ceiling((BeamLength - distance) / TextureScale));
        if (tileWidth <= 0)
          break;

        Rectangle source = new Rectangle(1, Projectile.frame * frameHeight, tileWidth, frameHeight);
        Vector2 origin = new Vector2(0f, frameHeight * 0.5f);
        Vector2 screenPosition = position - Main.screenPosition;

        Main.EntitySpriteDraw(texture, screenPosition, source, color,
          Projectile.rotation, origin, new Vector2(TextureScale, width),
          SpriteEffects.None, 0f);
      }
    }
  }
}