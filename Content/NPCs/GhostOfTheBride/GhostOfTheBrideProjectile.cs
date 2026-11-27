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

namespace DivineRoot.Content.NPCs.GhostOfTheBride;

public class GhostOfTheBrideProjectile : ModProjectile {
										 
	public override string Texture => "DivineRoot/Content/NPCs/GhostOfTheBride/GhostOfTheBrideProjectile";

	public override void SetDefaults() {
            this.Projectile.width = 98;
            this.Projectile.height = 94;
            this.Projectile.hostile = true;
            this.Projectile.friendly = false;
            this.Projectile.tileCollide = false;
            this.Projectile.ignoreWater = true;
            this.Projectile.penetrate = -1;
            this.Projectile.timeLeft = 180;
			this.AIType = -1;
			this.Projectile.alpha = 67;
	}

	private float Speed = 3.4f;

	private float RotationSpeed = MathF.PI / 60;

	public override bool PreAI() {
		ProjAI();
		return false;
	}

	public override void SetStaticDefaults() {

		this.Projectile.light = 0.4f;
		// пока похуй
		//if (!Main.dedServ)
		//{
		//	this.NPC.GlowTexture = ModContent.Request<Texture2D>(Texture + "_Glow");
		//}
	}

	private void SpawnDust()
	{
		if (Main.dedServ)
			return;

		for (int i = 0; i < 8; i++)
		{
			Vector2 velocity = Main.rand.NextVector2CircularEdge(8f, 8f) * Main.rand.NextFloat(0.3f, 1f);
			Dust shade = Dust.NewDustPerfect(this.Projectile.Center + Main.rand.NextVector2Circular(50f, 70f),
			  DustID.Frost,
			  velocity, 0, default, Main.rand.NextFloat(0.5f, 1.8f));
			shade.noGravity = true;
			shade.fadeIn = 0.6f;
		}

	}
	private void ProjAI() {
		int frames = 10;
		float spd = this.Speed / frames;
		this.Projectile.alpha = (int)(((float)this.Projectile.timeLeft / 180f) * 128f);
		ref Vector2 vel = ref this.Projectile.velocity;
		float velBound = this.Speed * 4;
		if (vel.Length() < velBound)
		vel += vel * spd;
		this.Projectile.rotation -= this.RotationSpeed; 
		if (this.RotationSpeed < MathF.PI / 1.5f)
			this.RotationSpeed += (RotationSpeed * 4.3f) / frames;
		float lightVal = 1.1f;
		SpawnDust();
		Lighting.AddLight(this.Projectile.Center, lightVal, lightVal, lightVal);
	}
}
