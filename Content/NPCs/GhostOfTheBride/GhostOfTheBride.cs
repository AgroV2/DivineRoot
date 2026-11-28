using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.ModLoader.Utilities;

namespace DivineRoot.Content.NPCs.GhostOfTheBride;

public class GhostOfTheBride : ModNPC
{

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 5;
		NPC.netAlways = true;
	}

	private SoundStyle throwSound = new ("DivineRoot/Content/Audio/ice_throw_sfx");
	public override string Texture => "DivineRoot/Content/NPCs/GhostOfTheBride/GhostOfTheBride";


	public override void SetDefaults()
	{
		NPC.width = 150;
		NPC.height = 138;

		NPC.HitSound = SoundID.NPCHit54;
		NPC.DeathSound = SoundID.NPCDeath6;

		NPC.damage = 50;
		NPC.defense = 100;
		NPC.lifeMax = 1400;

		NPC.value = 1000f;
		NPC.knockBackResist = 0f;

		NPC.noGravity = true;
		NPC.noTileCollide = true;
		NPC.aiStyle = NPCAIStyleID.HoveringFighter;
		NPC.target = -1;

		NPC.TargetClosest(false);
		NPC.alpha = 69;
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
	{
		if (spawnInfo.Player.ZoneSnow && spawnInfo.Player.ZoneRain)
		{
			return SpawnCondition.Overworld.Chance * 0.10f;
		}

		return 0f;
	}

	private int ScytheTimer;

	private const int SCYTHE_TIME = 60 * 3;

	public override bool PreAI()
	{
		NPC.VanillaAI();
		BrideAI();
		return false;
	}
	public override void FindFrame(int frameHeight)
	{
		NPC.frameCounter++;

		const int framesPerSecond = 3;
		if (NPC.frameCounter >= 60 / framesPerSecond)
		{
			NPC.frameCounter = 0;
			NPC.frame.Y += frameHeight;

			if (NPC.frame.Y >= frameHeight * Main.npcFrameCount[NPC.type])
				NPC.frame.Y = 0;
		}
	}
	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.Write(ScytheTimer);
	}
	public override void ReceiveExtraAI(BinaryReader reader)
	{
		ScytheTimer = reader.ReadInt32();
	}
	private void FaceTarget(Vector2 target)
	{
		NPC.direction = target.X < NPC.Center.X ? -1 : 1;
	}

	private void SpawnDustDeath()
	{
		if (Main.dedServ)
			return;

		for (int i = 0; i < 24; i++)
		{
			Vector2 velocity = Main.rand.NextVector2CircularEdge(8f, 8f) * Main.rand.NextFloat(0.3f, 1f);
			Dust shade = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(50f, 70f),
			  DustID.Cloud,
			  velocity, 0, default, Main.rand.NextFloat(1.5f, 3.8f));
			shade.noGravity = true;
			shade.fadeIn = 0.6f;
		}

	}
	private void SpawnDust()
	{
		if (Main.dedServ)
			return;

		for (int i = 0; i < 2; i++)
		{
			Vector2 velocity = Main.rand.NextVector2CircularEdge(2f, 2f) * Main.rand.NextFloat(0.0f, 0.4f);
			Dust dust = Dust.NewDustPerfect(NPC.position + new Vector2(Random.Shared.Next(0, NPC.width), Random.Shared.Next(0, NPC.height)),
			  DustID.Frost,
			  velocity, 0, default, Main.rand.NextFloat(0.5f, 1.3f));
			dust.noGravity = true;
			dust.fadeIn = 0.6f;
		}

	}

	public override bool PreKill()
	{
		SpawnDustDeath();
		return true;
	}

	public override void OnHitPlayer(Player target,
		Player.HurtInfo info
	)
	{
		target.AddBuff(BuffID.Frostburn, 60 * 10, false);
	}
	public void BrideAI()
	{
		FindFrame(NPC.height);
		SpawnDust();
		NPC.spriteDirection = NPC.direction;
		const float lightVal = 1.1f;
		Lighting.AddLight(NPC.Center, lightVal, lightVal, lightVal);
		if (!NPC.HasValidTarget)
		{
			ScytheTimer = 0;
			return;
		}
		ScytheTimer++;
		Vector2 targetPos = Main.player[NPC.target].position;
		FaceTarget(targetPos);
		Vector2 dir = Vector2.Normalize(targetPos - NPC.Center);
		if (ScytheTimer == SCYTHE_TIME)
		{
			int bounds = NPC.width / 2;
			Vector2 offset = -dir * bounds;
			Vector2 scythePos = new (
					offset.X + NPC.Center.X,
					offset.Y + NPC.Center.Y
					);
			SoundEngine.PlaySound(throwSound, NPC.position);
			Projectile.NewProjectile(
					NPC.GetSource_FromAI(),
					scythePos,
					dir,
					ModContent.ProjectileType<GhostOfTheBrideProjectile>(),
					1,
					0,
					-1,
					0,
					0f,
					0f);
			ScytheTimer = 0;
		}
	}
}

