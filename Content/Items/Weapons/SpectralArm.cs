using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

using DivineRoot.Content.Buffs;
using DivineRoot.Content.Projectiles;

namespace DivineRoot.Content.Items.Weapons;

public sealed class SpectralArm : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 44;
        Item.height = 16;
        Item.damage = 26;
        Item.knockBack = 2f;
        Item.mana = 8;
        Item.DamageType = DamageClass.Summon;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useTime = 24;
        Item.useAnimation = 24;
        Item.UseSound = SoundID.Item44;
        Item.noMelee = true;
        Item.rare = ItemRarityID.Green;
        Item.buffType = ModContent.BuffType<SpectralArmBuff>();
        Item.shoot = ModContent.ProjectileType<SpectralArmProjectile>();
    }

    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        player.AddBuff(Item.buffType, 2);
        int index = Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, knockback, player.whoAmI);
        if (index < Main.maxProjectiles)
            Main.projectile[index].originalDamage = Item.damage;

        return false;
    }

}
