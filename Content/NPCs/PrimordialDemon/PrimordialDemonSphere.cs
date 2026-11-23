using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace DivineRoot.Content.NPCs.PrimordialDemon
{
  public static class PrimordialSphereAttack
  {
    public const int SummonStart = 40;
    public const int SphereCount = 5;
    public const int SphereInterval = 16;
    public const float SpawnRadius = 170f;
    public const int TotalDuration = SummonStart + SphereCount * SphereInterval + 100;

    public static bool TryGetSphere(int tick, out int index)
    {
      index = -1;
      int local = tick - SummonStart;
      if (local < 0 || local % SphereInterval != 0)
        return false;
      index = local / SphereInterval;
      return index < SphereCount;
    }
  }

  public class PrimordialDemonSphere : ModNPC
  {
    public const int SphereLife = 2000;
    private const int LifetimeTicks = 60 * 7;
    private const int FrameCount = 4;
    private const int FrameWidth = 31;
    private const int FrameHeight = 41;
    private const float Scale = 1.6f;

    private const float HomingSpeed = 8.5f;
    private const float HomingAcceleration = 0.028f;

    private const float BurstSpeed = 2.8f;
    private const int MinBurstCount = 2;
    private const int MaxBurstCount = 4;

    private ref float LifeTimer => ref NPC.ai[0];
    private ref float OwnerIndex => ref NPC.ai[1];
    private ref float TimedOut => ref NPC.ai[2];

    public override string Texture => "DivineRoot/Content/NPCs/PrimordialDemon/DemonSphere";

    public override void SetStaticDefaults()
    {
      Main.npcFrameCount[Type] = FrameCount;
      NPCID.Sets.MustAlwaysDraw[Type] = true;
      NPCID.Sets.NPCBestiaryDrawOffset[Type] = new NPCID.Sets.NPCBestiaryDrawModifiers { Hide = true };
    }

    public override void SetDefaults()
    {
      NPC.scale = Scale;
      NPC.width = (int)(FrameWidth * Scale);
      NPC.height = (int)(FrameHeight * Scale);

      NPC.aiStyle = -1;
      NPC.damage = 40;
      NPC.defense = 0;
      NPC.lifeMax = SphereLife;
      NPC.knockBackResist = 0f;

      NPC.noGravity = true;
      NPC.noTileCollide = true;
      NPC.friendly = false;
      NPC.lavaImmune = true;

      NPC.value = 0f;
      NPC.npcSlots = 0f;
      NPC.HitSound = SoundID.NPCHit52;
      NPC.DeathSound = SoundID.NPCDeath14;
    }

    public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
    {
      NPC.lifeMax = SphereLife;
      NPC.life = SphereLife;
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo) => 0f;

    public override void FindFrame(int frameHeight)
    {
      NPC.frameCounter++;
      if (NPC.frameCounter >= 6)
      {
        NPC.frameCounter = 0;
        NPC.frame.Y += frameHeight;
        if (NPC.frame.Y >= frameHeight * FrameCount)
          NPC.frame.Y = 0;
      }
    }

    public override void AI()
    {
      int owner = (int)OwnerIndex;
      if (owner >= 0 && owner < Main.maxNPCs)
      {
        NPC boss = Main.npc[owner];
        if (!boss.active || boss.life <= 0 || boss.ModNPC is not PrimordialDemon)
        {
          Despawn();
          return;
        }
      }

      NPC.TargetClosest(false);
      Player player = Main.player[NPC.target];

      if (!player.active || player.dead)
      {
        NPC.velocity *= 0.97f;
      }
      else
      {
        Vector2 desired = (player.Center - NPC.Center).SafeNormalize(Vector2.UnitY) * HomingSpeed;
        NPC.velocity = Vector2.Lerp(NPC.velocity, desired, HomingAcceleration);
      }

      NPC.rotation += NPC.velocity.X * 0.012f;

      LifeTimer++;
      float fuse = MathHelper.Clamp(LifeTimer / LifetimeTicks, 0f, 1f);

      Lighting.AddLight(NPC.Center, 1.1f + fuse * 0.5f, 0.35f, 0.05f);

      if (!Main.dedServ)
      {
        if (Main.rand.NextBool(fuse > 0.8f ? 1 : 2))
        {
          Vector2 offset = Main.rand.NextVector2Circular(16f, 16f) * Scale;
          Dust ember = Dust.NewDustPerfect(NPC.Center + offset,
            Main.rand.NextBool(3) ? DustID.YellowTorch : DustID.Torch,
            -NPC.velocity * 0.12f + Main.rand.NextVector2Circular(1.2f, 1.2f), 0,
            default, Main.rand.NextFloat(1.1f, 1.9f) * (0.8f + fuse * 0.5f));
          ember.noGravity = true;
        }
      }

      if (Main.netMode != NetmodeID.MultiplayerClient && LifeTimer >= LifetimeTicks)
      {
        TimedOut = 1f;
        NPC.life = 0;
        NPC.HitEffect();
        NPC.checkDead();
        NPC.netUpdate = true;
      }
    }

    private void Despawn()
    {
      NPC.active = false;
      if (Main.netMode == NetmodeID.Server)
        NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
    }

    public override void HitEffect(NPC.HitInfo hit)
    {
      if (Main.dedServ)
        return;

      int count = NPC.life <= 0 ? 34 : 4;
      for (int i = 0; i < count; i++)
      {
        Dust ember = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(18f, 18f),
          i % 3 == 0 ? DustID.YellowTorch : DustID.Torch,
          Main.rand.NextVector2Circular(6f, 6f), 0, default,
          Main.rand.NextFloat(1.2f, 2.4f));
        ember.noGravity = true;
        ember.fadeIn = 0.4f;
      }
    }

    public override void OnKill()
    {
      SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.6f, Pitch = -0.3f }, NPC.Center);

      if (TimedOut >= 1f)
        SpawnBurstProjectiles();
    }

    private void SpawnBurstProjectiles()
    {
      if (Main.netMode == NetmodeID.MultiplayerClient)
        return;

      int count = Main.rand.Next(MinBurstCount, MaxBurstCount + 1);
      float baseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
      int damage = Main.masterMode ? 44 : Main.expertMode ? 36 : 26;

      for (int i = 0; i < count; i++)
      {
        float angle = baseAngle + MathHelper.TwoPi * i / count
          + Main.rand.NextFloat(-0.35f, 0.35f);
        Vector2 velocity = angle.ToRotationVector2() * BurstSpeed * Main.rand.NextFloat(0.85f, 1.2f);

        Projectile.NewProjectile(NPC.GetSource_Death(), NPC.Center, velocity,
          ModContent.ProjectileType<PrimordialDemonSphereProj>(), damage, 0f, Main.myPlayer);
      }
    }
  }
}