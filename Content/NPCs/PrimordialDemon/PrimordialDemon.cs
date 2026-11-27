using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using DivineRoot.Content.Buffs;
using DivineRoot.Content.Systems;
using Terraria;
using Terraria.ID;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.DataStructures;

namespace DivineRoot.Content.NPCs.PrimordialDemon
{
  public class PrimordialDemon : ModNPC
  {
    private const int HoverState = 0;
    private const int ChangeSideState = 1;
    private const int EmberVolleyState = 2;
    private const int ClawBarrageState = 3;
    private const int SphereSummonState = 4;

    private const int SlashRingState = 10;
    private const int MarkDashState = 11;
    private const int ScytheThrowState = 12;
    private const int TeleportState = 13;

    private const float BaseScale = 1.5f;
    private const float SizeMultiplier = 1.2f;
    private const float OrbCircleHeight = 270f;
    private const float OrbBirthRadius = 132f;
    private const float OrbCircleRadius = 112f;
    private const float OrbAngularSpeed = 0.016f;

    private const float HorizontalOffset = 450f;
    private const float FlyoverHeight = 530f;
    private const float HoverMaxSpeed = 14f;
    private const float FlyoverMaxSpeed = 22f;
    private const float HoverAcceleration = 0.055f;
    private const float FlyoverAcceleration = 0.085f;

    private const int FlyoverDuration = 150;
    private const int MinTimeBeforeSideChange = 360;
    private const int MaxTimeBeforeSideChange = 600;

    private const float PhaseTwoThresholdExpert = 0.6f;
    private const float PhaseTwoThresholdClassic = 0.4f;

    private const float SpinTriggerPadding = 2f * 16f;
    private const int SpinCooldownTicks = 120;
    private const int SpinDurationTicks = 12;
    private const float SpinKnockback = 26f;

    private const int MarkDashWindup = 50;
    private const int MarkDashTravel = 34;
    private const int MarkDashPause = 16;

    private const int TeleportWindup = 26;
    private const int TeleportRecovery = 24;
    private const float TeleportDistance = 5f * 16f;
    private const int TeleportSpinGrace = 45;

    private const int ScytheWindup = 34;
    private const int ScytheWait = 130;

    private static Asset<Texture2D> wingTexture;
    private static Asset<Texture2D> swingTexture;
    private static Asset<Texture2D> bodyTexturePhaseTwo;
    private static Asset<Texture2D> swingTexturePhaseTwo;

    private float wingRotation;
    private bool wingVisualsInitialized;
    private Vector2 orbAnchor;
    private bool orbAnchorInitialized;
    private Vector2 lockedShotOrigin;
    private float lockedShotAngle;
    private int lockedOrbIndex = -1;
    private int attackDirection = -1;
    private int lastVisualAttackTick = -1;
    private int attackCycle;
    private int phase = 1;

    private int slashCount;
    private int slashesMade;
    private Vector2 lastSlashPoint;
    private bool hasLastSlashPoint;
    private float slashRingAngle;

    private int dashCount;
    private int dashesMade;
    private Vector2 dashVelocity;
    private bool dashHitRegistered;
    private float glowIntensity;

    private int spinCooldown;
    private int spinTimer;
    private float spinRotation;

    private ref float State => ref NPC.ai[0];
    private ref float Side => ref NPC.ai[1];
    private ref float SideChangeTimer => ref NPC.ai[2];
    private ref float FlyoverTimer => ref NPC.ai[3];

    public bool EmberAttackActive => (int)State == EmberVolleyState;
    public bool ClawAttackActive => (int)State == ClawBarrageState;
    public bool SphereAttackActive => (int)State == SphereSummonState;
    public bool TeleportActive => (int)State == TeleportState;
    public bool PhaseTwoAttackActive => (int)State >= SlashRingState;
    public bool AnyAttackActive => EmberAttackActive || ClawAttackActive
      || SphereAttackActive || PhaseTwoAttackActive;
    public int Phase => phase;

    private int AttackTick => (int)FlyoverTimer;
    private float LifeRatio => NPC.lifeMax <= 0 ? 0f : NPC.life / (float)NPC.lifeMax;
    private static float PhaseTwoThreshold =>
      Main.expertMode || Main.masterMode ? PhaseTwoThresholdExpert : PhaseTwoThresholdClassic;

    public override string Texture => "DivineRoot/Content/NPCs/PrimordialDemon/PrimordialDemonSheet";

    public override void Load()
    {
      if (!Main.dedServ)
      {
        wingTexture = ModContent.Request<Texture2D>("DivineRoot/Content/NPCs/PrimordialDemon/PrimordialDemonWing");
        swingTexture = ModContent.Request<Texture2D>("DivineRoot/Content/NPCs/PrimordialDemon/PrimordialDemonSwingSheet");
        bodyTexturePhaseTwo = ModContent.Request<Texture2D>("DivineRoot/Content/NPCs/PrimordialDemon/PrimordialDemonSheetScythe");
        swingTexturePhaseTwo = ModContent.Request<Texture2D>("DivineRoot/Content/NPCs/PrimordialDemon/PrimordialDemonSwingSheetScythe");
      }
    }

    public override void Unload()
    {
      wingTexture = null;
      swingTexture = null;
      bodyTexturePhaseTwo = null;
      swingTexturePhaseTwo = null;
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
      bool second = phase >= 2;
      Texture2D body = second && bodyTexturePhaseTwo != null
        ? bodyTexturePhaseTwo.Value : TextureAssets.Npc[Type].Value;

      Vector2 origin = NPC.frame.Size() * 0.5f;
      Vector2 drawPosition = NPC.Center - screenPos + new Vector2(
        0f, NPC.gfxOffY + NPC.height * 0.5f - origin.Y * NPC.scale);
      Color color = NPC.GetAlpha(drawColor);
      SpriteEffects bodyEffects = NPC.spriteDirection == 1
        ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
      bool summoning = EmberAttackActive && AttackTick < PrimordialEmberAttack.SummonDuration;

      if (wingTexture != null)
      {
        Texture2D wings = wingTexture.Value;
        int wingFrameHeight = wings.Height / Main.npcFrameCount[Type];
        int frameIndex = NPC.frame.Y / NPC.frame.Height;
        Rectangle wingFrame = new Rectangle(0, frameIndex * wingFrameHeight, wings.Width, wingFrameHeight);
        Vector2 wingOrigin = wingFrame.Size() * 0.5f;
        Vector2 wingOffset = new Vector2(NPC.spriteDirection == 1 ? -136f : -76f, -10f) * (NPC.scale / BaseScale);
        spriteBatch.Draw(wings, drawPosition + wingOffset, wingFrame, color,
          wingRotation, wingOrigin, NPC.scale, SpriteEffects.None, 0f);
      }

      Texture2D swingSheet = second && swingTexturePhaseTwo != null
        ? swingTexturePhaseTwo.Value : swingTexture?.Value;

      if (summoning && swingSheet != null)
      {
        int frameHeight = swingSheet.Height / PrimordialEmberAttack.SwingFrames;
        int frameIndex = AttackTick / PrimordialEmberAttack.TicksPerSwingFrame % PrimordialEmberAttack.SwingFrames;
        Rectangle swingFrame = new Rectangle(0, frameIndex * frameHeight, swingSheet.Width, frameHeight);
        Vector2 feet = NPC.Bottom - screenPos + new Vector2(0f, NPC.gfxOffY);
        spriteBatch.Draw(swingSheet, feet, swingFrame, color, NPC.rotation,
          new Vector2(swingSheet.Width * 0.5f, frameHeight), NPC.scale, bodyEffects, 0f);
      }
      else
      {
        spriteBatch.Draw(body, drawPosition, NPC.frame, color,
          NPC.rotation, origin, NPC.scale, bodyEffects, 0f);

        if (glowIntensity > 0.02f)
        {
          Color glow = new Color(255, 90, 30, 0) * (glowIntensity * 0.75f);
          for (int i = 0; i < 4; i++)
          {
            Vector2 offset = (MathHelper.PiOver2 * i).ToRotationVector2() * (3f + glowIntensity * 5f);
            spriteBatch.Draw(body, drawPosition + offset, NPC.frame, glow,
              NPC.rotation, origin, NPC.scale, bodyEffects, 0f);
          }
        }
      }

      if (spinTimer > 0)
        DrawSpin(spriteBatch, screenPos);

      if (EmberAttackActive)
        DrawEmberAttack(spriteBatch, screenPos);

      return false;
    }

    private void DrawSpin(SpriteBatch spriteBatch, Vector2 screenPos)
    {
      Texture2D pixel = TextureAssets.MagicPixel.Value;
      float progress = 1f - spinTimer / (float)SpinDurationTicks;
      float radius = (NPC.width * 0.5f + SpinTriggerPadding) * (0.6f + progress * 0.6f);
      Vector2 center = NPC.Center - screenPos;

      for (int i = 0; i < 26; i++)
      {
        float angle = spinRotation + MathHelper.TwoPi * i / 26f;
        Vector2 point = center + angle.ToRotationVector2() * radius;
        spriteBatch.Draw(pixel, point, new Rectangle(0, 0, 1, 1),
          new Color(255, 110, 30, 0) * (0.7f * (1f - progress)), angle,
          new Vector2(0.5f, 0.5f), new Vector2(26f, 7f), SpriteEffects.None, 0f);
      }
    }

    public override void SetStaticDefaults()
    {
      Main.npcFrameCount[Type] = 6;
      NPCID.Sets.MustAlwaysDraw[Type] = true;
    }

    public override void SetDefaults()
    {
      NPC.scale = BaseScale * SizeMultiplier;
      NPC.width = (int)(117 * NPC.scale + 0.5f);
      NPC.height = (int)(167 * NPC.scale + 0.5f);

      NPC.aiStyle = -1;
      NPC.damage = 0;
      NPC.defense = 0;
      NPC.lifeMax = 14_000_000;
      NPC.knockBackResist = 0f;

      NPC.noGravity = true;
      NPC.noTileCollide = true;
      NPC.friendly = false;
      NPC.boss = true;

      NPC.value = 0f;
      NPC.npcSlots = 1f;
      NPC.HitSound = SoundID.NPCHit1;
      NPC.DeathSound = SoundID.NPCDeath1;
    }

    public override void FindFrame(int frameHeight)
    {
      NPC.frameCounter++;
      if (NPC.frameCounter >= 8)
      {
        NPC.frameCounter = 0;
        NPC.frame.Y += frameHeight;
        if (NPC.frame.Y >= frameHeight * Main.npcFrameCount[Type])
          NPC.frame.Y = 0;
      }
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo) => 0f;

    public override void OnKill()
    {
      WorldProgressSystem.DownedPrimordialDemon = true;

      if (Main.netMode == NetmodeID.Server)
        NetMessage.SendData(MessageID.WorldData);
    }

    public override void PostAI()
    {
      float targetTilt = MathHelper.Clamp(NPC.velocity.X / HoverMaxSpeed, -1f, 1f)
        * (AnyAttackActive ? 0.18f : 1.28f);
      NPC.rotation = MathHelper.Lerp(NPC.rotation, targetTilt, 0.3f);

      float targetWingTilt = NPC.rotation * 0.3f;
      if (!wingVisualsInitialized)
      {
        wingRotation = targetWingTilt;
        wingVisualsInitialized = true;
      }
      else
      {
        wingRotation = MathHelper.Lerp(wingRotation, targetWingTilt, 0.08f);
      }
    }

    public override void AI()
    {
      NPC.rotation = 0f;

      UpdatePhase();
      glowIntensity = MathHelper.Lerp(glowIntensity, 0f, 0.07f);

      NPC.TargetClosest(false);
      Player player = Main.player[NPC.target];

      if (!player.active || player.dead)
      {
        if (AnyAttackActive && Main.netMode != NetmodeID.MultiplayerClient)
        {
          State = HoverState;
          FlyoverTimer = 0f;
          SideChangeTimer = 120f;
          lockedOrbIndex = -1;
          NPC.netUpdate = true;
        }
        NPC.velocity *= 0.96f;
        NPC.velocity.Y -= 0.08f;
        return;
      }

      InitializeMovement(player);
      UpdateOrbAnchor();

      if (TeleportActive)
      {
        spinTimer = 0;
        if (spinCooldown < TeleportSpinGrace)
          spinCooldown = TeleportSpinGrace;
      }
      else if (phase >= 2)
      {
        UpdateSpinGuard(player);
      }

      if (spinTimer > 0)
      {
        spinTimer--;
        spinRotation += 0.45f;
        NPC.velocity *= 0.97f;
        return;
      }

      if (EmberAttackActive)
      {
        DoEmberAttack(player);
        return;
      }

      if (ClawAttackActive)
      {
        lastVisualAttackTick = -1;
        DoClawAttack(player);
        return;
      }

      if (SphereAttackActive)
      {
        lastVisualAttackTick = -1;
        DoSphereAttack(player);
        return;
      }

      if (PhaseTwoAttackActive)
      {
        lastVisualAttackTick = -1;
        switch ((int)State)
        {
          case SlashRingState: DoSlashRing(player); return;
          case MarkDashState: DoMarkDash(player); return;
          case ScytheThrowState: DoScytheThrow(player); return;
          case TeleportState: DoTeleport(player); return;
        }
      }

      lastVisualAttackTick = -1;

      Vector2 destination;
      float maxSpeed;
      float acceleration;

      if ((int)State == ChangeSideState)
      {
        float progress = MathHelper.Clamp(FlyoverTimer / FlyoverDuration, 0f, 1f);
        float easedProgress = MathHelper.SmoothStep(0f, 1f, progress);
        float angle = MathHelper.Pi * easedProgress;

        Vector2 flyoverOffset = new Vector2(
          Side * HorizontalOffset * (float)Math.Cos(angle),
          -FlyoverHeight * (float)Math.Sin(angle));

        destination = player.Center + flyoverOffset;
        maxSpeed = FlyoverMaxSpeed;
        acceleration = FlyoverAcceleration;
        FlyoverTimer++;

        if (Main.netMode != NetmodeID.MultiplayerClient && FlyoverTimer >= FlyoverDuration)
        {
          Side *= -1f;
          State = HoverState;
          FlyoverTimer = 0f;
          SideChangeTimer = phase >= 2
            ? Main.rand.Next(MinTimeBeforeSideChange / 3, MaxTimeBeforeSideChange / 3)
            : Main.rand.Next(MinTimeBeforeSideChange, MaxTimeBeforeSideChange + 1);
          NPC.netUpdate = true;
        }
      }
      else
      {
        destination = player.Center + new Vector2(Side * HorizontalOffset, 0f);
        maxSpeed = HoverMaxSpeed;
        acceleration = HoverAcceleration;
        SideChangeTimer--;

        if (Main.netMode != NetmodeID.MultiplayerClient && SideChangeTimer <= 0f)
        {
          State = phase == 1 ? PickPhaseOneAttack() : PickPhaseTwoAttack();
          attackCycle++;
          FlyoverTimer = 0f;
          attackDirection = player.Center.X >= NPC.Center.X ? 1 : -1;
          lockedOrbIndex = -1;
          ResetPhaseTwoAttackState();
          NPC.netUpdate = true;
        }
      }

      MoveSmoothly(destination, maxSpeed, acceleration);

      int lookDirection = player.Center.X >= NPC.Center.X ? 1 : -1;
      NPC.direction = lookDirection;
      NPC.spriteDirection = lookDirection;
    }

    private void ResetPhaseTwoAttackState()
    {
      slashesMade = 0;
      hasLastSlashPoint = false;
      slashCount = Main.rand.Next(PrimordialSlashAttack.MinSlashes, PrimordialSlashAttack.MaxSlashes + 1);
      slashRingAngle = Main.rand.NextFloat(MathHelper.TwoPi);

      dashesMade = 0;
      dashCount = Main.rand.Next(3, 5);
      dashHitRegistered = false;
      dashVelocity = Vector2.Zero;
    }

    private void UpdatePhase()
    {
      if (phase >= 2 || LifeRatio > PhaseTwoThreshold)
        return;

      phase = 2;

      if (Main.netMode != NetmodeID.MultiplayerClient)
      {
        State = ChangeSideState;
        FlyoverTimer = 0f;
        SideChangeTimer = 0f;
        lockedOrbIndex = -1;
        NPC.netUpdate = true;
      }

      glowIntensity = 1f;

      if (!Main.dedServ)
      {
        SoundEngine.PlaySound(SoundID.Roar with { Pitch = -0.5f, Volume = 1f }, NPC.Center);
        PrimordialDemonScreenShakeSystem.AddShake(NPC.Center, 34f);

        for (int i = 0; i < 70; i++)
        {
          Vector2 velocity = Main.rand.NextVector2CircularEdge(9f, 9f) * Main.rand.NextFloat(0.4f, 1f);
          Dust ember = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(60f, 80f),
            i % 3 == 0 ? DustID.YellowTorch : DustID.Torch, velocity, 0,
            default, Main.rand.NextFloat(1.6f, 3f));
          ember.noGravity = true;
          ember.fadeIn = 0.6f;
        }
      }
    }

    private int PickPhaseOneAttack()
    {
      return (attackCycle % 3) switch
      {
        0 => EmberVolleyState,
        1 => ClawBarrageState,
        _ => SphereSummonState
      };
    }

    private int PickPhaseTwoAttack() => (attackCycle % 4) switch
    {
      0 => SlashRingState,
      1 => MarkDashState,
      2 => ScytheThrowState,
      _ => TeleportState
    };

    private void UpdateSpinGuard(Player player)
    {
      if (spinCooldown > 0)
      {
        spinCooldown--;
        return;
      }

      if (spinTimer > 0)
        return;

      Rectangle inflated = NPC.Hitbox;
      inflated.Inflate((int)SpinTriggerPadding, (int)SpinTriggerPadding);

      if (!inflated.Intersects(player.Hitbox))
        return;

      spinTimer = SpinDurationTicks;
      spinCooldown = SpinCooldownTicks;
      spinRotation = 0f;
      glowIntensity = 1f;

      Vector2 away = (NPC.Center - player.Center).SafeNormalize(Vector2.UnitX);
      NPC.velocity = away * 26f;

      if (player.whoAmI == Main.myPlayer && !player.dead)
      {
        int damage = Main.masterMode ? 220 : Main.expertMode ? 170 : 110;
        player.velocity = -away * SpinKnockback;
        player.Hurt(PlayerDeathReason.ByNPC(NPC.whoAmI), damage, -Math.Sign(away.X));
      }

      if (!Main.dedServ)
      {
        SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.9f, Pitch = -0.3f }, NPC.Center);
        PrimordialDemonScreenShakeSystem.AddShake(NPC.Center, 30f);

        for (int i = 0; i < 50; i++)
        {
          float angle = MathHelper.TwoPi * i / 50f;
          Dust ember = Dust.NewDustPerfect(NPC.Center + angle.ToRotationVector2() * 60f,
            i % 3 == 0 ? DustID.YellowTorch : DustID.Torch,
            angle.ToRotationVector2() * 7f, 0, default, Main.rand.NextFloat(1.6f, 2.8f));
          ember.noGravity = true;
        }
      }

      NPC.netUpdate = true;
    }

    private void DoSlashRing(Player player)
    {
      FlyoverTimer++;

      int tick = AttackTick - PrimordialSlashAttack.StartDelay;
      bool slashing = slashesMade < slashCount;

      if (slashing)
      {
        float targetAngle = slashRingAngle + MathHelper.TwoPi * slashesMade / slashCount;
        Vector2 target = player.Center + targetAngle.ToRotationVector2() * PrimordialSlashAttack.DashRadius;
        MoveSmoothly(target, 34f, 0.3f);

        if (!Main.dedServ && Main.rand.NextBool(2))
        {
          Dust trail = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(40f, 50f),
            DustID.Torch, -NPC.velocity * 0.15f, 0, default, Main.rand.NextFloat(1.2f, 2f));
          trail.noGravity = true;
        }

        if (tick >= 0 && tick % PrimordialSlashAttack.SlashInterval == 0)
        {
          if (hasLastSlashPoint && Main.netMode != NetmodeID.MultiplayerClient)
          {
            Vector2 start = lastSlashPoint;
            Vector2 end = NPC.Center;
            Vector2 mid = (start + end) * 0.5f;
            float angle = (end - start).ToRotation();
            float length = Vector2.Distance(start, end);
            int damage = Main.masterMode ? 72 : Main.expertMode ? 60 : 42;
            float fuse = PrimordialSlashAttack.BurstDelay
              + (slashCount - slashesMade) * PrimordialSlashAttack.SlashInterval
              + slashesMade * PrimordialSlashAttack.BurstStagger;

            Projectile.NewProjectile(NPC.GetSource_FromAI(), mid, Vector2.Zero,
              ModContent.ProjectileType<PrimordialDemonScytheCut>(),
              damage, 0f, Main.myPlayer, angle, length, fuse);
          }

          lastSlashPoint = NPC.Center;
          hasLastSlashPoint = true;
          slashesMade++;

          if (!Main.dedServ)
            SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.5f, Pitch = 0.3f }, NPC.Center);

          NPC.netUpdate = true;
        }
      }
      else
      {
        MoveSmoothly(player.Center + new Vector2(Side * HorizontalOffset, -120f), 12f, 0.05f);
      }

      int look = player.Center.X >= NPC.Center.X ? 1 : -1;
      NPC.direction = look;
      NPC.spriteDirection = look;

      if (Main.netMode != NetmodeID.MultiplayerClient
        && AttackTick >= PrimordialSlashAttack.TotalDuration(slashCount))
      {
        State = ChangeSideState;
        FlyoverTimer = 0f;
        NPC.netUpdate = true;
      }
    }

    private void DoMarkDash(Player player)
    {
      FlyoverTimer++;
      int tick = AttackTick;

      if (tick < MarkDashWindup)
      {
        glowIntensity = MathHelper.Lerp(glowIntensity, 1f, 0.12f);
        MoveSmoothly(player.Center + new Vector2(0f, -200f), 10f, 0.06f);

        if (tick == 1 && !Main.dedServ)
        {
          SoundEngine.PlaySound(SoundID.Roar with { Pitch = -0.2f }, NPC.Center);
          PrimordialDemonScreenShakeSystem.AddShake(NPC.Center, 18f);
        }

        if (!Main.dedServ && Main.rand.NextBool(2))
        {
          Vector2 offset = Main.rand.NextVector2CircularEdge(90f, 100f);
          Dust ember = Dust.NewDustPerfect(NPC.Center + offset, DustID.Torch,
            -offset * 0.05f, 0, default, Main.rand.NextFloat(1.2f, 2.2f));
          ember.noGravity = true;
        }

        int look = player.Center.X >= NPC.Center.X ? 1 : -1;
        NPC.direction = look;
        NPC.spriteDirection = look;
        return;
      }

      int cycle = MarkDashTravel + MarkDashPause;
      int local = (tick - MarkDashWindup) % cycle;
      int index = (tick - MarkDashWindup) / cycle;

      if (index >= dashCount)
      {
        MoveSmoothly(player.Center + new Vector2(Side * HorizontalOffset, -80f), 12f, 0.06f);

        if (Main.netMode != NetmodeID.MultiplayerClient && local >= MarkDashPause)
        {
          State = ChangeSideState;
          FlyoverTimer = 0f;
          NPC.netUpdate = true;
        }
        return;
      }

      if (local == 0)
      {
        dashVelocity = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitY) * 31f;
        dashHitRegistered = false;
        glowIntensity = 1f;

        if (!Main.dedServ)
          SoundEngine.PlaySound(SoundID.ForceRoarPitched with { Pitch = 0.2f, Volume = 0.7f }, NPC.Center);

        NPC.netUpdate = true;
      }

      if (local < MarkDashTravel)
      {
        NPC.velocity = dashVelocity;

        if (!dashHitRegistered && NPC.Hitbox.Intersects(player.Hitbox)
          && player.whoAmI == Main.myPlayer && !player.dead)
        {
          dashHitRegistered = true;
          int damage = Main.masterMode ? 150 : Main.expertMode ? 115 : 80;
          player.AddBuff(ModContent.BuffType<DemonDebuff>(), DemonDebuff.DurationTicks);
          player.Hurt(PlayerDeathReason.ByNPC(NPC.whoAmI), damage,
            Math.Sign(dashVelocity.X == 0f ? 1f : dashVelocity.X));
        }

        if (!Main.dedServ && Main.rand.NextBool())
        {
          Dust trail = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(50f, 60f),
            DustID.Torch, -dashVelocity * 0.08f, 0, default, Main.rand.NextFloat(1.4f, 2.3f));
          trail.noGravity = true;
        }
      }
      else
      {
        NPC.velocity *= 0.9f;
      }

      NPC.direction = NPC.spriteDirection = dashVelocity.X >= 0f ? 1 : -1;
    }

    private void DoScytheThrow(Player player)
    {
      FlyoverTimer++;
      int tick = AttackTick;

      MoveSmoothly(player.Center + new Vector2(Side * HorizontalOffset, -110f), 9f, 0.05f);

      if (tick < ScytheWindup)
      {
        glowIntensity = MathHelper.Lerp(glowIntensity, 0.8f, 0.1f);
      }
      else if (tick == ScytheWindup && Main.netMode != NetmodeID.MultiplayerClient)
      {
        Vector2 direction = (player.Center + player.velocity * 10f - NPC.Center).SafeNormalize(Vector2.UnitX);
        int damage = Main.masterMode ? 95 : Main.expertMode ? 78 : 55;

        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center,
          direction * PrimordialDemonScythe.ThrowSpeed,
          ModContent.ProjectileType<PrimordialDemonScythe>(),
          damage, 0f, Main.myPlayer, NPC.Center.X, NPC.Center.Y);

        if (!Main.dedServ)
          SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.8f }, NPC.Center);

        NPC.netUpdate = true;
      }

      int look = player.Center.X >= NPC.Center.X ? 1 : -1;
      NPC.direction = look;
      NPC.spriteDirection = look;

      if (Main.netMode != NetmodeID.MultiplayerClient && tick >= ScytheWindup + ScytheWait)
      {
        State = ChangeSideState;
        FlyoverTimer = 0f;
        NPC.netUpdate = true;
      }
    }

    private void DoTeleport(Player player)
    {
      FlyoverTimer++;
      int tick = AttackTick;

      if (tick < TeleportWindup)
      {
        NPC.velocity *= 0.9f;
        glowIntensity = MathHelper.Lerp(glowIntensity, 0.9f, 0.12f);

        if (tick == 1 && !Main.dedServ)
          SoundEngine.PlaySound(SoundID.Roar with { Pitch = 0.15f, Volume = 0.8f }, NPC.Center);

        if (!Main.dedServ && Main.rand.NextBool(2))
        {
          Vector2 offset = Main.rand.NextVector2CircularEdge(80f, 90f);
          Dust shade = Dust.NewDustPerfect(NPC.Center + offset, DustID.Shadowflame,
            -offset * 0.07f, 0, default, Main.rand.NextFloat(1.3f, 2.2f));
          shade.noGravity = true;
        }
        return;
      }

      if (tick == TeleportWindup)
      {
        SpawnTeleportDust();

        float facing = player.direction >= 0 ? -1f : 1f;
        Vector2 destination = player.Center + new Vector2(facing * TeleportDistance, -40f);

        NPC.Center = destination;
        NPC.velocity = Vector2.Zero;
        Side = facing;
        orbAnchor = NPC.Center;

        SpawnTeleportDust();

        if (!Main.dedServ)
          SoundEngine.PlaySound(SoundID.Item8 with { Pitch = -0.5f, Volume = 0.9f }, NPC.Center);

        NPC.netUpdate = true;
      }

      NPC.velocity *= 0.92f;
      int look = player.Center.X >= NPC.Center.X ? 1 : -1;
      NPC.direction = look;
      NPC.spriteDirection = look;

      if (Main.netMode != NetmodeID.MultiplayerClient && tick >= TeleportWindup + TeleportRecovery)
      {
        State = ChangeSideState;
        FlyoverTimer = 0f;
        NPC.netUpdate = true;
      }
    }

    private void SpawnTeleportDust()
    {
      if (Main.dedServ)
        return;

      for (int i = 0; i < 45; i++)
      {
        Vector2 velocity = Main.rand.NextVector2CircularEdge(8f, 8f) * Main.rand.NextFloat(0.3f, 1f);
        Dust shade = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(50f, 70f),
          i % 2 == 0 ? DustID.Shadowflame : DustID.PurpleTorch,
          velocity, 0, default, Main.rand.NextFloat(1.5f, 2.8f));
        shade.noGravity = true;
        shade.fadeIn = 0.6f;
      }

      Lighting.AddLight(NPC.Center, 0.8f, 0.2f, 1.1f);
    }

    private void InitializeMovement(Player player)
    {
      if (Side != 0f)
        return;

      Side = NPC.Center.X <= player.Center.X ? -1f : 1f;

      if (Main.netMode != NetmodeID.MultiplayerClient)
      {
        SideChangeTimer = 120f;
        NPC.netUpdate = true;
      }
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
      writer.Write(attackDirection);
      writer.Write(lockedOrbIndex);
      writer.Write(lockedShotOrigin.X);
      writer.Write(lockedShotOrigin.Y);
      writer.Write(lockedShotAngle);
      writer.Write(orbAnchor.X);
      writer.Write(orbAnchor.Y);
      writer.Write(attackCycle);
      writer.Write(phase);
      writer.Write(slashCount);
      writer.Write(slashesMade);
      writer.Write(dashCount);
      writer.Write(dashesMade);
      writer.Write(spinTimer);
      writer.Write(dashVelocity.X);
      writer.Write(dashVelocity.Y);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
      attackDirection = reader.ReadInt32();
      lockedOrbIndex = reader.ReadInt32();
      lockedShotOrigin = new Vector2(reader.ReadSingle(), reader.ReadSingle());
      lockedShotAngle = reader.ReadSingle();
      orbAnchor = new Vector2(reader.ReadSingle(), reader.ReadSingle());
      attackCycle = reader.ReadInt32();
      phase = reader.ReadInt32();
      slashCount = reader.ReadInt32();
      slashesMade = reader.ReadInt32();
      dashCount = reader.ReadInt32();
      dashesMade = reader.ReadInt32();
      spinTimer = reader.ReadInt32();
      dashVelocity = new Vector2(reader.ReadSingle(), reader.ReadSingle());
      orbAnchorInitialized = true;
    }

    private void UpdateOrbAnchor()
    {
      if (!orbAnchorInitialized || Vector2.DistanceSquared(orbAnchor, NPC.Center) > 1600f * 1600f)
      {
        orbAnchor = NPC.Center;
        orbAnchorInitialized = true;
      }
      else
        orbAnchor = Vector2.Lerp(orbAnchor, NPC.Center, 0.11f);
    }

    private void DoEmberAttack(Player player)
    {
      FlyoverTimer++;
      NPC.direction = attackDirection;
      NPC.spriteDirection = attackDirection;
      MoveSmoothly(player.Center + new Vector2(Side * HorizontalOffset, -30f), 5f, 0.04f);

      if (PrimordialEmberAttack.TryGetShot(AttackTick, out int orbIndex, out int shotTick))
      {
        if (shotTick == 0)
          lockedOrbIndex = -1;

        if (shotTick == PrimordialEmberAttack.AimDuration && Main.netMode != NetmodeID.MultiplayerClient)
        {
          lockedShotOrigin = GetDynamicOrbPosition(orbIndex);
          lockedShotAngle = (player.Center + player.velocity * 6f - lockedShotOrigin).ToRotation();
          lockedOrbIndex = orbIndex;
          NPC.netUpdate = true;
        }

        if (shotTick == PrimordialEmberAttack.FireTime && Main.netMode != NetmodeID.MultiplayerClient)
        {
          int damage = Main.masterMode ? 52 : Main.expertMode ? 45 : 30;
          Projectile.NewProjectile(NPC.GetSource_FromAI(), lockedShotOrigin,
            lockedShotAngle.ToRotationVector2(), ModContent.ProjectileType<PrimordialDemonLaser>(),
            damage, 0f, Main.myPlayer, NPC.whoAmI, orbIndex);
          NPC.netUpdate = true;
        }
      }

      if (!Main.dedServ)
        UpdateEmberEffects();

      if (Main.netMode != NetmodeID.MultiplayerClient && AttackTick >= PrimordialEmberAttack.TotalDuration)
      {
        State = ChangeSideState;
        FlyoverTimer = 0f;
        lockedOrbIndex = -1;
        NPC.netUpdate = true;
      }
    }

    private void DoClawAttack(Player player)
    {
      FlyoverTimer++;
      NPC.direction = attackDirection;
      NPC.spriteDirection = attackDirection;
      MoveSmoothly(player.Center + new Vector2(Side * HorizontalOffset, -140f), 7f, 0.045f);

      if (Main.netMode != NetmodeID.MultiplayerClient
        && PrimordialClawAttack.TryGetClaw(AttackTick, out int index))
      {
        Vector2 center = player.Center + player.velocity * 12f
          + Main.rand.NextVector2Circular(48f, 48f);
        float angle = Main.rand.NextBool() ? -MathHelper.PiOver4 : MathHelper.PiOver4;
        int damage = Main.masterMode ? 58 : Main.expertMode ? 48 : 34;

        Projectile.NewProjectile(NPC.GetSource_FromAI(), center, Vector2.Zero,
          ModContent.ProjectileType<PrimordialDemonClaw>(),
          damage, 0f, Main.myPlayer, angle);

        NPC.netUpdate = true;
      }

      if (Main.netMode != NetmodeID.MultiplayerClient
        && AttackTick >= PrimordialClawAttack.TotalDuration)
      {
        State = ChangeSideState;
        FlyoverTimer = 0f;
        NPC.netUpdate = true;
      }
    }

    private void DoSphereAttack(Player player)
    {
      FlyoverTimer++;
      NPC.direction = attackDirection;
      NPC.spriteDirection = attackDirection;
      MoveSmoothly(player.Center + new Vector2(Side * HorizontalOffset, -90f), 6f, 0.04f);

      if (Main.netMode != NetmodeID.MultiplayerClient
        && PrimordialSphereAttack.TryGetSphere(AttackTick, out int sphereIndex))
      {
        float angle = -MathHelper.PiOver2
          + MathHelper.TwoPi * sphereIndex / PrimordialSphereAttack.SphereCount;
        Vector2 direction = angle.ToRotationVector2();
        Vector2 spawn = NPC.Center + direction * PrimordialSphereAttack.SpawnRadius;

        int index = NPC.NewNPC(NPC.GetSource_FromAI(), (int)spawn.X, (int)spawn.Y,
          ModContent.NPCType<PrimordialDemonSphere>(), 0, 0f, NPC.whoAmI);

        if (index < Main.maxNPCs)
        {
          Main.npc[index].velocity = direction * 5f;
          if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.SyncNPC, number: index);
        }

        SoundEngine.PlaySound(SoundID.Item73 with { Pitch = -0.3f, Volume = 0.7f }, NPC.Center);
        NPC.netUpdate = true;
      }

      if (Main.netMode != NetmodeID.MultiplayerClient
        && AttackTick >= PrimordialSphereAttack.TotalDuration)
      {
        State = ChangeSideState;
        FlyoverTimer = 0f;
        NPC.netUpdate = true;
      }
    }

    public Vector2 GetEmberOrbPosition(int index)
    {
      if (lockedOrbIndex == index && PrimordialEmberAttack.TryGetShot(AttackTick, out int currentOrb, out int shotTick)
        && currentOrb == index && shotTick >= PrimordialEmberAttack.AimDuration)
      {
        float release = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(
          (shotTick - PrimordialEmberAttack.FireTime - PrimordialEmberAttack.BeamDuration)
          / (float)PrimordialEmberAttack.ShotRestDuration, 0f, 1f));
        return Vector2.Lerp(lockedShotOrigin, GetDynamicOrbPosition(index), release);
      }

      return GetDynamicOrbPosition(index);
    }

    private Vector2 GetDynamicOrbPosition(int index)
    {
      index = Math.Clamp(index, 0, PrimordialEmberAttack.OrbCount - 1);
      float spread = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(
        (AttackTick - PrimordialEmberAttack.SummonDuration) / (float)PrimordialEmberAttack.SpreadDuration, 0f, 1f));
      float orbitAge = Math.Max(0, AttackTick - PrimordialEmberAttack.SummonDuration);
      float angle = GetOrbBirthAngle(index) + orbitAge * OrbAngularSpeed * spread;
      float radius = MathHelper.Lerp(OrbBirthRadius, OrbCircleRadius, spread);
      Vector2 offset = new Vector2(0f, -OrbCircleHeight) + angle.ToRotationVector2() * radius;

      float scatterProgress = MathHelper.Clamp(
        (AttackTick - PrimordialEmberAttack.RecoveryStart) / (float)PrimordialEmberAttack.RecoveryDuration, 0f, 1f);
      if (scatterProgress > 0f)
      {
        float scatter = scatterProgress * scatterProgress;
        float frozenAge = PrimordialEmberAttack.RecoveryStart - PrimordialEmberAttack.SummonDuration;
        float frozenAngle = GetOrbBirthAngle(index) + frozenAge * OrbAngularSpeed;
        float scatterAngle = frozenAngle + scatter * 0.45f;
        float distance = PrimordialEmberAttack.ScatterDistance * (0.85f + index * 0.1f) * scatter;
        offset = new Vector2(0f, -OrbCircleHeight) + frozenAngle.ToRotationVector2() * OrbCircleRadius
          + scatterAngle.ToRotationVector2() * distance;
      }

      return orbAnchor + offset * (NPC.scale / BaseScale);
    }

    private static float GetOrbBirthAngle(int index) => -MathHelper.PiOver4
      + index * MathHelper.TwoPi / PrimordialEmberAttack.OrbCount;

    private float GetOrbVisibility(int index)
    {
      float birthTick = PrimordialEmberAttack.OrbBirthStart + index * PrimordialEmberAttack.OrbBirthStagger;
      float appear = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(
        (AttackTick - birthTick) / PrimordialEmberAttack.OrbBirthDuration, 0f, 1f));
      float disappear = 1f - MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(
        (AttackTick - PrimordialEmberAttack.RecoveryStart) / (float)PrimordialEmberAttack.RecoveryDuration, 0f, 1f));
      return appear * disappear;
    }

    private Vector2 GetSummoningGlowPosition(int index)
    {
      Vector2 offset = new Vector2(0f, -OrbCircleHeight) + GetOrbBirthAngle(index).ToRotationVector2() * OrbBirthRadius;
      return orbAnchor + offset * (NPC.scale / BaseScale);
    }

    private float GetSummoningGlowVisibility(int index)
    {
      float stagger = index * PrimordialEmberAttack.OrbBirthStagger;
      float gather = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((AttackTick - 4f - stagger) / 26f, 0f, 1f));
      float release = 1f - MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(
        (AttackTick - PrimordialEmberAttack.OrbBirthStart - stagger) / 42f, 0f, 1f));
      return gather * release;
    }

    private void UpdateEmberEffects()
    {
      int tick = AttackTick;
      if (tick == lastVisualAttackTick)
        return;

      int secondSwing = PrimordialEmberAttack.SummonDuration / 2;
      if ((lastVisualAttackTick < 0 && tick < secondSwing)
        || (lastVisualAttackTick < secondSwing && tick >= secondSwing && tick < PrimordialEmberAttack.SummonDuration))
        SoundEngine.PlaySound(SoundID.Item20 with { Pitch = -0.45f, PitchVariance = 0.12f, Volume = 0.65f }, NPC.Center);
      lastVisualAttackTick = tick;

      for (int i = 0; i < PrimordialEmberAttack.OrbCount; i++)
      {
        float summoningGlow = GetSummoningGlowVisibility(i);
        if (summoningGlow <= 0.05f)
          continue;
        Vector2 glowCenter = GetSummoningGlowPosition(i);
        Lighting.AddLight(glowCenter, new Vector3(1.25f, 0.32f, 0.04f) * summoningGlow);
        if ((tick + i) % 5 == 0)
        {
          Vector2 offset = Main.rand.NextVector2CircularEdge(60f, 50f) * (NPC.scale / BaseScale);
          Dust gatheringEmber = Dust.NewDustPerfect(glowCenter + offset, DustID.Torch,
            -offset * 0.055f + NPC.velocity * 0.12f, 35, default, Main.rand.NextFloat(0.7f, 1.15f));
          gatheringEmber.noGravity = true;
        }
      }

      for (int i = 0; i < PrimordialEmberAttack.OrbCount; i++)
      {
        float visibility = GetOrbVisibility(i);
        if (visibility <= 0.05f)
          continue;
        Vector2 position = GetEmberOrbPosition(i);
        Lighting.AddLight(position, new Vector3(1.1f, 0.24f, 0.025f) * visibility);
        if (Main.rand.NextBool(7))
        {
          Vector2 offset = Main.rand.NextVector2CircularEdge(22f, 22f);
          Dust ember = Dust.NewDustPerfect(position + offset, DustID.Torch,
            offset * 0.035f + new Vector2(0f, -0.8f), 20, default, Main.rand.NextFloat(0.8f, 1.3f));
          ember.noGravity = true;
        }
      }
    }

    private Vector2 GetWarningDirection(int orbIndex, int shotTick)
    {
      if (shotTick >= PrimordialEmberAttack.AimDuration && lockedOrbIndex == orbIndex)
        return lockedShotAngle.ToRotationVector2();

      Player target = Main.player[NPC.target];
      float progress = MathHelper.Clamp(shotTick / (float)PrimordialEmberAttack.AimDuration, 0f, 1f);
      float radius = 140f * (1f - MathHelper.SmoothStep(0f, 1f, progress));
      float angle = progress * MathHelper.TwoPi * 1.15f + orbIndex * 1.7f;
      Vector2 aimPoint = target.Center + target.velocity * 6f + angle.ToRotationVector2() * radius;
      return (aimPoint - GetEmberOrbPosition(orbIndex)).SafeNormalize(new Vector2(attackDirection, 0f));
    }

    private void DrawEmberAttack(SpriteBatch spriteBatch, Vector2 screenPos)
    {
      for (int i = 0; i < PrimordialEmberAttack.OrbCount; i++)
        PrimordialEmberVisuals.DrawSummoningGlow(spriteBatch, GetSummoningGlowPosition(i), screenPos,
          AttackTick * 0.08f + i * 1.7f, GetSummoningGlowVisibility(i), NPC.scale / BaseScale);

      bool hasShot = PrimordialEmberAttack.TryGetShot(AttackTick, out int firingOrb, out int shotTick);
      if (hasShot && shotTick < PrimordialEmberAttack.FireTime)
      {
        Vector2 position = GetEmberOrbPosition(firingOrb) - screenPos;
        float rotation = GetWarningDirection(firingOrb, shotTick).ToRotation();
        float charge = PrimordialEmberAttack.GetCharge(shotTick);
        float opacity = MathHelper.Clamp(shotTick / 8f, 0f, 1f);
        bool locked = shotTick >= PrimordialEmberAttack.AimDuration;
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        Rectangle source = new Rectangle(0, 0, 1, 1);
        Vector2 origin = new Vector2(0f, 0.5f);
        spriteBatch.Draw(pixel, position, source, new Color(200, 15, 8, 0) * (opacity * 0.1f), rotation,
          origin, new Vector2(2400f, 26f + charge * 12f), SpriteEffects.None, 0f);
        spriteBatch.Draw(pixel, position, source, new Color(255, 35, 12, 0) * (opacity * 0.3f), rotation,
          origin, new Vector2(2400f, 9f), SpriteEffects.None, 0f);
        spriteBatch.Draw(pixel, position, source, new Color(255, locked ? 92 : 36, 28) * (opacity * (locked ? 0.92f : 0.64f)), rotation,
          origin, new Vector2(2400f, locked ? 3.6f : 2.2f), SpriteEffects.None, 0f);
      }

      for (int i = 0; i < PrimordialEmberAttack.OrbCount; i++)
      {
        float charge = hasShot && firingOrb == i && shotTick < PrimordialEmberAttack.FireTime
          ? PrimordialEmberAttack.GetCharge(shotTick) : 0f;
        float recoil = hasShot && firingOrb == i && shotTick >= PrimordialEmberAttack.FireTime
          ? MathHelper.Clamp(1f - (shotTick - PrimordialEmberAttack.FireTime) / 16f, 0f, 1f) : 0f;
        float orbPhase = AttackTick * 0.12f + i * 2.17f + NPC.whoAmI * 0.41f;
        PrimordialEmberVisuals.Draw(spriteBatch, GetEmberOrbPosition(i), screenPos,
          orbPhase, GetOrbVisibility(i), charge, recoil, AttackTick * 0.02f + i * 1.37f);
      }
    }

    private void MoveSmoothly(Vector2 destination, float maxSpeed, float acceleration)
    {
      Vector2 offset = destination - NPC.Center;
      float distance = offset.Length();
      Vector2 desiredVelocity = Vector2.Zero;

      if (distance > 0.01f)
      {
        float speed = MathHelper.Min(maxSpeed, distance * 0.08f);
        desiredVelocity = offset * (speed / distance);
      }

      NPC.velocity = Vector2.Lerp(NPC.velocity, desiredVelocity, acceleration);
    }
  }
}