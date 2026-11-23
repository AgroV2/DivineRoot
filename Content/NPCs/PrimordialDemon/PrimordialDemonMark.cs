using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DivineRoot.Content.Buffs
{
  public class DemonDebuff : ModBuff
  {
    public const int DamagePerSecond = 10;
    public const int DurationTicks = 60 * 10;
    public const float SpeedPenalty = 0.15f;

    public override string Texture => "DivineRoot/Content/Buffs/DemonDebuff";

    public override void SetStaticDefaults()
    {
      Main.debuff[Type] = true;
      Main.pvpBuff[Type] = true;
      Main.buffNoSave[Type] = true;
      BuffID.Sets.LongerExpertDebuff[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
      player.GetModPlayer<DemonMarkPlayer>().marked = true;
    }
  }

  public class DemonMarkPlayer : ModPlayer
  {
    public bool marked;
    private int damageTimer;

    public override void ResetEffects()
    {
      marked = false;
    }

    public override void PostUpdateEquips()
    {
      if (!marked)
      {
        damageTimer = 0;
        return;
      }

      Player.moveSpeed -= DemonDebuff.SpeedPenalty;
      Player.maxRunSpeed *= 1f - DemonDebuff.SpeedPenalty;
      Player.accRunSpeed *= 1f - DemonDebuff.SpeedPenalty;
    }

    public override void UpdateBadLifeRegen()
    {
      if (!marked)
        return;

      if (Player.lifeRegen > 0)
        Player.lifeRegen = 0;

      Player.lifeRegenTime = 0;
      Player.lifeRegen -= DemonDebuff.DamagePerSecond * 2;
    }

    public override void UpdateVisibleAccessories()
    {
    }

    public override void PostUpdate()
    {
      if (!marked || Main.dedServ)
        return;

      if (Main.rand.NextBool(2))
      {
        Vector2 offset = Main.rand.NextVector2Circular(Player.width * 0.7f, Player.height * 0.55f);
        Dust ember = Dust.NewDustPerfect(Player.Center + offset,
          Main.rand.NextBool(3) ? DustID.YellowTorch : DustID.Torch,
          new Vector2(0f, -Main.rand.NextFloat(0.6f, 1.8f)), 0, default,
          Main.rand.NextFloat(1f, 1.8f));
        ember.noGravity = true;
        ember.fadeIn = 0.3f;
      }

      Lighting.AddLight(Player.Center, 0.7f, 0.2f, 0.03f);
    }
  }
}