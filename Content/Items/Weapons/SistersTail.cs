using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

using DivineRoot.Content.Projectiles;

namespace DivineRoot.Content.Items.Weapons
{
    public class SistersTail : ModItem
    {
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance
                .SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToWhip(
                ModContent.ProjectileType<SistersTailProjectile>(),
                75,
                4f,
                10f
            );

            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.buyPrice(gold: 5);
        }

        public override bool MeleePrefix()
        {
            return true;
        }

        public override bool Shoot(
            Player player,
            EntitySource_ItemUse_WithAmmo source,
            Vector2 position,
            Vector2 velocity,
            int type,
            int damage,
            float knockback)
        {
            SistersTailPlayer modPlayer =
                player.GetModPlayer<SistersTailPlayer>();

            modPlayer.StrikeCount++;

            if (modPlayer.StrikeCount < 5)
                return true;

            modPlayer.StrikeCount = 0;

            Projectile.NewProjectile(
                source,
                position,
                velocity,
                ModContent.ProjectileType<SistersTailSplashProjectile>(),
                (int)(damage * 1.5f),
                knockback,
                player.whoAmI
            );

            return false;
        }
    }
}