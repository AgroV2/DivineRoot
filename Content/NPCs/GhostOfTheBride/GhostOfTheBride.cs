using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

using static Terraria.Audio.ActiveSound;
using ReLogic.Content;
using DivineRoot.Content.Buffs;
using DivineRoot.Content.Systems;
using Terraria;
using Terraria.ID;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.Utilities;

using static Terraria.ModLoader.ModContent;
namespace DivineRoot.Content.NPCs.GhostOfTheBride;

public class GhostOfTheBride : ModNPC
{

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[this.Type] = 5;
	}

	private SoundStyle throwSound = new SoundStyle("DivineRoot/Content/Audio/ice_throw_sfx");
	public override string Texture => "DivineRoot/Content/NPCs/GhostOfTheBride/GhostOfTheBride";


	public override void SetDefaults()
	{
		this.NPC.width = 150;
		this.NPC.height = 138;

		NPC.HitSound = SoundID.NPCHit54;
		NPC.DeathSound = SoundID.NPCDeath6;
		// TODO: change this PLEASE
		this.NPC.damage = 1;
		this.NPC.defense = 1;
		this.NPC.lifeMax = 5000;

		this.NPC.value = 1000f;
		this.NPC.knockBackResist = 0f;

		this.NPC.noGravity = true;
		this.NPC.noTileCollide = true;
		this.NPC.aiStyle = -1;
		this.NPC.target = -1;

		this.NPC.TargetClosest(false);
		AnimationType = NPCID.Reaper;
		this.NPC.alpha = 69;
		this.State = (int)BrideState.IDLE;
	}

	public override float SpawnChance(NPCSpawnInfo spawnInfo)
	{
		if (spawnInfo.Player.ZoneSnow && spawnInfo.Player.ZoneRain)
		{
			return SpawnCondition.Overworld.Chance * 0.25f;
		}

		return 0f;
	}

	private enum BrideState
	{
		IDLE = 0,
		AGGRO = 1,
	}

	private ref float State => ref this.NPC.ai[0];
	private ref float DistToTarget => ref this.NPC.ai[1];
	private ref float ScytheTimer => ref this.NPC.ai[2];

	private const float MAX_PLAYER_DIST = 1500f;
	private const int SCYTHE_TIME = 60 * 3;

	public override bool PreAI()
	{
		BrideAI();
		return false;
	}

	private void FaceTarget(Vector2 target)
	{
		this.NPC.direction = target.X < this.NPC.Center.X ? -1 : 1;
	}

	// TODO: удалить когда агро запушит подключенный люминанс
	public static Point FindGroundVertical(Point p)
	{
		// The tile is solid. Check up to verify that this tile is not inside of solid ground.
		if (WorldGen.SolidTile(p))
		{
			while (WorldGen.SolidTile(p.X, p.Y - 1) && p.Y >= 1)
				p.Y--;
		}

		// The tile is not solid. Check down to verify that this tile is not above ground in the middle of the air.
		else
		{
			while (!WorldGen.SolidTile(p.X, p.Y + 1) && p.Y < Main.maxTilesY)
				p.Y++;
		}

		return p;
	}

	private void SpawnDustDeath()
	{
		if (Main.dedServ)
			return;

		for (int i = 0; i < 24; i++)
		{
			Vector2 velocity = Main.rand.NextVector2CircularEdge(8f, 8f) * Main.rand.NextFloat(0.3f, 1f);
			Dust shade = Dust.NewDustPerfect(this.NPC.Center + Main.rand.NextVector2Circular(50f, 70f),
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
			Dust dust = Dust.NewDustPerfect(this.NPC.position + new Vector2(Random.Shared.Next(0, this.NPC.width), Random.Shared.Next(0, this.NPC.height)),
			  DustID.Frost,
			  velocity, 0, default, Main.rand.NextFloat(0.5f, 1.3f));
			dust.noGravity = true;
			dust.fadeIn = 0.6f;
		}

	}

	public override bool PreKill() {
		SpawnDustDeath();
		return true;
	}
	public void BrideAI()
	{
		SpawnDust();
		this.NPC.spriteDirection = this.NPC.direction;
		float lightVal = 1.1f;
		Lighting.AddLight(NPC.Center, lightVal, lightVal, lightVal);
		switch ((int)this.State)
		{
			case (int)BrideState.IDLE:
				this.ScytheTimer = 0;
				this.NPC.velocity *= 0.75f;
				if (this.NPC.velocity.Length() < 0.0001f) this.NPC.velocity *= 0;
				if (this.NPC.HasValidTarget)
				{
					this.State = (float)BrideState.AGGRO;
					break;
				}

				int player = this.NPC.FindClosestPlayer(out float dist);
				if (dist > MAX_PLAYER_DIST) break;

				this.DistToTarget = dist;
				this.NPC.target = player;
				this.State = (float)BrideState.AGGRO;
				break;

			case (int)BrideState.AGGRO:
				this.ScytheTimer++;
				if (!this.NPC.HasValidTarget)
				{
					this.State = (float)BrideState.IDLE;
					break;
				}
				Vector2 targetPos = Main.player[this.NPC.target].position;
				this.DistToTarget = Vector2.Distance(targetPos, this.NPC.Center);
				if (this.DistToTarget > MAX_PLAYER_DIST)
				{
					this.State = (float)BrideState.IDLE;
					this.NPC.target = -1;
					break;
				}
				FaceTarget(targetPos);
				const int projectileDamage = 1;
				Vector2 dir = Vector2.Normalize(targetPos - this.NPC.Center);
				if (this.ScytheTimer == SCYTHE_TIME)
				{
					int bounds = this.NPC.width / 2;
					Vector2 offset = -dir * bounds;
					Vector2 scythePos = new Vector2(
							offset.X + this.NPC.Center.X,
							offset.Y + this.NPC.Center.Y
							);
					SoundEngine.PlaySound(throwSound, this.NPC.position);
					Projectile.NewProjectile(
							this.NPC.GetSource_FromAI(),
							scythePos,
							dir,
							ModContent.ProjectileType<GhostOfTheBrideProjectile>(),
							1,
							0,
							-1,
							0,
							0f,
							0f);
					this.ScytheTimer = 0;
				}
				const float speed = 2f;
				Vector2 MIN_VEL = new(-speed, -speed);
				Vector2 MAX_VEL = new(speed, speed);

				// ЭТО ТАКААААЯ ПОЕБОТА ЭТО ПРОСТО ПИЗДЕЦ
				Point groundP = FindGroundVertical(new((int)this.NPC.Center.X / 16, (int)this.NPC.Center.Y / 16));
				Vector2 ground = new(groundP.X * 16, groundP.Y * 16);

				Vector2 distToGround = ground - this.NPC.Center;
				distToGround.X = 0;
				Vector2 dirToGround = Vector2.Normalize(ground - this.NPC.Center);

				Vector2 targetVel = dir * speed;
				ref Vector2 vel = ref this.NPC.velocity;
				// чтобы моб двигался плавно как рипер прям короче
				vel += targetVel / 60f;
				float minDistToGround = this.NPC.height / 2 + 32;
				if (distToGround.Y < minDistToGround)
				{
					vel.Y -= speed / 60;
				}
				vel = Vector2.Clamp(vel, MIN_VEL, MAX_VEL);
				break;
		}
	}
}

