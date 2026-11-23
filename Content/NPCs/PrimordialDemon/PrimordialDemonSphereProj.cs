using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace DivineRoot.Content.NPCs.PrimordialDemon
{
  public class PrimordialDemonSphereProj : ModProjectile
  {
    private const int Lifetime = 240;
    private const float Scale = 1f;

    public override string Texture => "DivineRoot/Content/NPCs/PrimordialDemon/DemonSphereProj";

    public override void SetStaticDefaults()
    {
      Main.projFrames[Type] = 1;
      ProjectileID.Sets.TrailCacheLength[Type] = 8;
      ProjectileID.Sets.TrailingMode[Type] = 2;
    }

    public override void SetDefaults()
    {
      Projectile.width = 20;
      Projectile.height = 20;
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

    public override void AI()
    {
      Projectile.rotation += 0.06f;

      if (Main.dedServ)
        return;

      float fade = MathHelper.Clamp(Projectile.timeLeft / 30f, 0f, 1f);
      Lighting.AddLight(Projectile.Center, 0.95f * fade, 0.32f * fade, 0.045f * fade);

      if (Main.rand.NextBool(2))
      {
        Vector2 offset = Main.rand.NextVector2Circular(7f, 7f);
        Dust ember = Dust.NewDustPerfect(Projectile.Center + offset,
          Main.rand.NextBool(3) ? DustID.YellowTorch : DustID.Torch,
          -Projectile.velocity * 0.25f + Main.rand.NextVector2Circular(0.7f, 0.7f),
          0, default, Main.rand.NextFloat(1f, 1.8f) * fade);
        ember.noGravity = true;
      }

      if (Main.rand.NextBool(4))
      {
        Dust trail = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
          DustID.Torch, Vector2.Zero, 0, default, Main.rand.NextFloat(1.2f, 2.1f) * fade);
        trail.noGravity = true;
        trail.fadeIn = 0.45f;
        trail.velocity *= 0.2f;
      }
    }

    public override void OnKill(int timeLeft)
    {
      if (Main.dedServ)
        return;

      SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.45f, Pitch = -0.2f }, Projectile.Center);

      for (int i = 0; i < 14; i++)
      {
        Vector2 velocity = Main.rand.NextVector2Circular(3.5f, 3.5f);
        Dust ember = Dust.NewDustPerfect(Projectile.Center,
          i % 2 == 0 ? DustID.YellowTorch : DustID.Torch,
          velocity, 0, default, Main.rand.NextFloat(1.2f, 2f));
        ember.noGravity = true;
        ember.fadeIn = 0.4f;
      }
    }

    public override bool PreDraw(ref Color lightColor)
    {
      Texture2D texture = TextureAssets.Projectile[Type].Value;
      Vector2 origin = texture.Size() * 0.5f;
      float fade = MathHelper.Clamp(Projectile.timeLeft / 30f, 0f, 1f);
      float pulse = 1f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f) * 0.08f;

      for (int i = 0; i < Projectile.oldPos.Length; i++)
      {
        if (Projectile.oldPos[i] == Vector2.Zero)
          continue;
        float trailFade = (1f - i / (float)Projectile.oldPos.Length) * 0.35f * fade;
        Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
        Main.EntitySpriteDraw(texture, trailPos, null, new Color(255, 90, 20, 0) * trailFade,
          Projectile.rotation, origin, Scale * (1f - i * 0.05f), SpriteEffects.None, 0);
      }

      Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null,
        new Color(255, 170, 70, 0) * (0.55f * fade), Projectile.rotation, origin,
        Scale * 1.35f * pulse, SpriteEffects.None, 0);
      Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null,
        Color.White * fade, Projectile.rotation, origin, Scale, SpriteEffects.None, 0);

      return false;
    }
  }
}