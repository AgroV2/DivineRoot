using Terraria;
using Terraria.ModLoader;

using DivineRoot.Content.Projectiles;

namespace DivineRoot.Content.Buffs;

public sealed class SpectralArmBuff : ModBuff
{
    public override void SetStaticDefaults()
    {
        Main.buffNoSave[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        if (player.ownedProjectileCounts[ModContent.ProjectileType<SpectralArmProjectile>()] > 0)
        {
            player.buffTime[buffIndex] = 18000;
            return;
        }

        player.DelBuff(buffIndex);
        buffIndex--;
    }
}
