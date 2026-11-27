using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Luminance.Core.Graphics;
using ReLogic.Content;
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
      StartDelay + slashCount * SlashInterval + BurstDelay
      + slashCount * BurstStagger + Recovery;
  }

  public class PrimordialDemonScytheCut : ModProjectile
  {
    public const int BurstDuration = 24;
    public const int DamageWindow = 18;

    private const float DamageWidth = 44f;
    private const int TrailPoints = 36;
    private const string ShaderName =
      "DivineRoot.DemonScytheSlashShader";

    private static ManagedShader slashShader;
    private static Asset<Texture2D> noiseTexture;
    private static int shaderFailures;

    private bool burstStarted;

    public override string Texture =>
      "DivineRoot/Content/NPCs/PrimordialDemon/DemonScytheCut";

    private float Angle => Projectile.ai[0];
    private float Length => Math.Max(16f, Projectile.ai[1]);
    private float Fuse => Math.Max(1f, Projectile.ai[2]);

    private Vector2 Direction => Angle.ToRotationVector2();

    private Vector2 LineStart =>
      Projectile.Center - Direction * (Length * 0.5f);

    private Vector2 LineEnd =>
      Projectile.Center + Direction * (Length * 0.5f);

    private float Age =>
      Fuse + BurstDuration - Projectile.timeLeft;

    private float BurstAge => Age - Fuse;

    private bool Bursting => BurstAge >= 0f;

    private float BurstFade =>
      MathHelper.Clamp(BurstAge / 4f, 0f, 1f)
      * MathHelper.Clamp((BurstDuration - BurstAge) / 7f, 0f, 1f);

    public override void Load()
    {
      if (Main.dedServ)
        return;

      noiseTexture = Main.Assets.Request<Texture2D>(
        "Images/Misc/Perlin",
        AssetRequestMode.ImmediateLoad);
    }

    public override void Unload()
    {
      slashShader = null;
      noiseTexture = null;
      shaderFailures = 0;
    }

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

        Lighting.AddLight(
          Projectile.Center,
          0.45f * charge,
          0.08f * charge,
          0.015f * charge);

        if (Main.rand.NextBool(charge > 0.75f ? 2 : 5))
        {
          Vector2 point = Vector2.Lerp(
            LineStart,
            LineEnd,
            Main.rand.NextFloat());

          Dust ember = Dust.NewDustPerfect(
            point,
            Main.rand.NextBool(3)
              ? DustID.YellowTorch
              : DustID.Torch,
            Main.rand.NextVector2Circular(0.8f, 0.8f),
            0,
            default,
            Main.rand.NextFloat(0.8f, 1.5f) * (0.5f + charge));

          ember.noGravity = true;
        }

        return;
      }

      if (!burstStarted)
      {
        burstStarted = true;
        StartBurst();
      }

      Lighting.AddLight(
        Projectile.Center,
        1.2f * BurstFade,
        0.28f * BurstFade,
        0.025f * BurstFade);

      if (Main.rand.NextBool(2))
      {
        Vector2 point = Vector2.Lerp(
          LineStart,
          LineEnd,
          Main.rand.NextFloat());

        Dust ember = Dust.NewDustPerfect(
          point,
          Main.rand.NextBool(2)
            ? DustID.YellowTorch
            : DustID.Torch,
          Main.rand.NextVector2Circular(2.4f, 2.4f),
          0,
          default,
          Main.rand.NextFloat(1.2f, 2.1f) * BurstFade);

        ember.noGravity = true;
      }
    }

    private void StartBurst()
    {
      SoundEngine.PlaySound(
        SoundID.Item74 with
        {
          Volume = 0.6f,
          Pitch = 0.1f,
          MaxInstances = 5
        },
        Projectile.Center);

      for (int i = 0; i < 36; i++)
      {
        Vector2 point = Vector2.Lerp(
          LineStart,
          LineEnd,
          i / 35f);

        Vector2 velocity =
          Direction.RotatedBy(MathHelper.PiOver2)
          * Main.rand.NextFloat(-5f, 5f)
          + Direction * Main.rand.NextFloat(-1.5f, 1.5f);

        Dust ember = Dust.NewDustPerfect(
          point,
          i % 3 == 0
            ? DustID.YellowTorch
            : DustID.Torch,
          velocity,
          0,
          default,
          Main.rand.NextFloat(1.4f, 2.5f));

        ember.noGravity = true;
        ember.fadeIn = 0.5f;
      }
    }

    public override bool? CanDamage() =>
      Bursting && BurstAge < DamageWindow;

    public override bool? Colliding(
      Rectangle projHitbox,
      Rectangle targetHitbox)
    {
      if (!Bursting || BurstAge >= DamageWindow)
        return false;

      float collisionPoint = 0f;

      return Collision.CheckAABBvLineCollision(
        targetHitbox.TopLeft(),
        targetHitbox.Size(),
        LineStart,
        LineEnd,
        DamageWidth * BurstFade,
        ref collisionPoint);
    }

    public override bool PreDraw(ref Color lightColor)
    {
      if (Main.dedServ)
        return false;

      if (!BurstyVisual())
        return false;

      DrawSlashTrail();
      DrawSlashCore();

      return false;
    }

  private bool BurstyVisual()
{
  if (Bursting)
    return BurstFade > 0.01f;

  return Age >= 0f && Age < Fuse;
}


    private void DrawSlashTrail()
    {
      Vector2[] points = new Vector2[TrailPoints];

      Vector2 normal = Direction.RotatedBy(MathHelper.PiOver2);
      float charge = MathHelper.Clamp(Age / Fuse, 0f, 1f);

      float visualWidth = Bursting
        ? 18f + 34f * BurstFade
        : 4f + 13f * charge;

      for (int i = 0; i < TrailPoints; i++)
      {
        float t = i / (float)(TrailPoints - 1);
        Vector2 point = Vector2.Lerp(LineStart, LineEnd, t);

        if (!Bursting)
        {
          float wave = MathF.Sin(
            t * MathHelper.TwoPi * 3f
            + Age * 0.18f) * 2.5f * charge;

          point += normal * wave;
        }
        else
        {
          float wave = MathF.Sin(
            t * MathHelper.TwoPi * 5f
            - BurstAge * 0.35f) * 4f * BurstFade;

          point += normal * wave;
        }

        points[i] = point;
      }

      ManagedShader shader = GetShader();

      Color lava = Bursting
        ? new Color(255, 55, 8)
        : new Color(170, 35, 8);

      Color orange = new Color(255, 145, 35);
      Color white = new Color(255, 235, 180);

      PrimitiveSettings settings = new PrimitiveSettings(
        WidthFunction: t =>
        {
          float taper = 0.72f + 0.28f * MathF.Sin(t * MathHelper.Pi);
          return visualWidth * taper;
        },
        ColorFunction: t =>
        {
          float edgeFade = 1f - MathF.Abs(t - 0.5f) * 1.35f;
          edgeFade = MathHelper.Clamp(edgeFade, 0.2f, 1f);

          Color color = Color.Lerp(lava, orange, edgeFade);

          if (Bursting)
            color = Color.Lerp(color, white, BurstFade * 0.55f);

          color *= Bursting
            ? BurstFade
            : 0.3f + charge * 0.7f;

          color.A = 0;
          return color;
        },
        Shader: shader);

      PrimitiveRenderer.RenderTrail(
        points,
        settings,
        80);
    }

    private void DrawSlashCore()
    {
      if (!BurstyVisual())
        return;

      Texture2D flare = TextureAssets.Extra[98].Value;
      Vector2 origin = flare.Size() * 0.5f;

      float charge = MathHelper.Clamp(Age / Fuse, 0f, 1f);
      float opacity = Bursting
        ? BurstFade
        : charge * 0.45f;

      Vector2 center = Projectile.Center - Main.screenPosition;

      Main.EntitySpriteDraw(
        flare,
        center,
        null,
        new Color(255, 70, 10, 0) * opacity,
        Angle,
        origin,
        new Vector2(Length / flare.Width, 0.55f + opacity * 0.8f),
        SpriteEffects.None,
        0f);

      Main.EntitySpriteDraw(
        flare,
        center,
        null,
        new Color(255, 190, 70, 0) * opacity,
        Angle,
        origin,
        new Vector2(Length / flare.Width, 0.18f + opacity * 0.25f),
        SpriteEffects.None,
        0f);
    }

    private static ManagedShader GetShader()
    {
      if (slashShader != null || shaderFailures >= 120)
        return slashShader;

      try
      {
        slashShader = ShaderManager.GetShader(ShaderName);

        slashShader.TrySetParameter(
          "edgeColor",
          new Vector3(1f, 0.22f, 0.03f));

        slashShader.TrySetParameter(
          "coreColor",
          new Vector3(1f, 0.8f, 0.3f));

        slashShader.TrySetParameter(
          "edgeIntensity",
          1.8f);

        slashShader.SetTexture(
          noiseTexture,
          1,
          SamplerState.LinearWrap);
      }
      catch
      {
        shaderFailures++;
        slashShader = null;
      }

      return slashShader;
    }
  }
}