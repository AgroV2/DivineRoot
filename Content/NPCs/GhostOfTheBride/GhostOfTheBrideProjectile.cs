using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DivineRoot.Content.NPCs.GhostOfTheBride;

public class GhostOfTheBrideProjectile : ModProjectile
{
	public override string Texture => "DivineRoot/Content/NPCs/GhostOfTheBride/GhostOfTheBrideProjectile";

	public override void SetDefaults()
	{
		Projectile.width = 98;
		Projectile.height = 94;
		Projectile.hostile = true;
		Projectile.friendly = false;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 180;
		AIType = -1;
		Projectile.alpha = 67;
	}

	private readonly float Speed = 3.4f;

	private float RotationSpeed = MathF.PI / 60;

	public override bool PreAI()
	{
		ProjAI();
		return false;
	}

	public override void SetStaticDefaults()
	{
		Projectile.light = 0.4f;
	}

	private void SpawnDust()
	{
		if (Main.dedServ)
			return;

		for (int i = 0; i < 8; i++)
		{
			Vector2 velocity = Main.rand.NextVector2CircularEdge(8f, 8f) * Main.rand.NextFloat(0.3f, 1f);
			Dust shade = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(50f, 70f),
			  DustID.Frost,
			  velocity, 0, default, Main.rand.NextFloat(0.5f, 1.8f));
			shade.noGravity = true;
			shade.fadeIn = 0.6f;
		}

	}
	public override void OnHitPlayer(Player target,
		Player.HurtInfo info
	)
	{
		target.AddBuff(BuffID.Frostburn, 60 * 10, false);
	}
	private void ProjAI()
	{
		const int frames = 10;
		float spd = Speed / frames;
		Projectile.alpha = (int)(Projectile.timeLeft / 180f * 128f);
		ref Vector2 vel = ref Projectile.velocity;
		float velBound = Speed * 4;
		if (vel.Length() < velBound)
			vel += vel * spd;
		Projectile.rotation -= RotationSpeed;
		if (RotationSpeed < MathF.PI / 1.5f)
			RotationSpeed += RotationSpeed * 4.3f / frames;
		const float lightVal = 1.1f;
		SpawnDust();
		Lighting.AddLight(Projectile.Center, lightVal, lightVal, lightVal);
	}
}
