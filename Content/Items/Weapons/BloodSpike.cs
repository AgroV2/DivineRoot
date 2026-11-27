using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

using DivineRoot.Content.Projectiles;

namespace DivineRoot.Content.Items.Weapons
{
    public class BloodSpike : ModItem
    {
        private const float SurfaceSearchRadius = 200f;
        private const int RayCount = 72;

        public override void SetDefaults()
        {
            Item.damage = 45;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 12;

            Item.width = 28;
            Item.height = 28;

            Item.useTime = 18;
            Item.useAnimation = 18;
            Item.useStyle = ItemUseStyleID.Shoot;

            Item.noMelee = true;
            Item.knockBack = 3f;

            Item.value = Item.sellPrice(gold: 2);
            Item.rare = ItemRarityID.Pink;

            Item.shoot = ModContent.ProjectileType<BloodSpikeProjectile>();
            Item.shootSpeed = 1f;
            Item.autoReuse = true;
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
            Vector2 target = Main.MouseWorld;

            List<Vector2> surfacePoints =
                FindSurfacePointsAroundTarget(
                    target,
                    SurfaceSearchRadius
                );

            if (surfacePoints.Count == 0)
                return false;

            int spikeCount = Math.Min(
                Main.rand.Next(3, 6),
                surfacePoints.Count
            );

            for (int i = surfacePoints.Count - 1; i > 0; i--)
            {
                int j = Main.rand.Next(i + 1);

                Vector2 temporary = surfacePoints[i];
                surfacePoints[i] = surfacePoints[j];
                surfacePoints[j] = temporary;
            }

            for (int i = 0; i < spikeCount; i++)
            {
                int projectileIndex = Projectile.NewProjectile(
                    source,
                    surfacePoints[i],
                    Vector2.Zero,
                    type,
                    damage,
                    knockback,
                    player.whoAmI
                );

                if (projectileIndex < 0 ||
                    projectileIndex >= Main.maxProjectiles)
                {
                    continue;
                }

                Projectile projectile =
                    Main.projectile[projectileIndex];

                projectile.ai[0] = target.X;
                projectile.ai[1] = target.Y;
                projectile.netUpdate = true;
            }

            return false;
        }

        private static List<Vector2> FindSurfacePointsAroundTarget(
            Vector2 target,
            float radius)
        {
            List<Vector2> points =
                new List<Vector2>();

            for (int ray = 0; ray < RayCount; ray++)
            {
                float angle =
                    MathHelper.TwoPi * ray / RayCount +
                    Main.rand.NextFloat(-0.04f, 0.04f);

                Vector2 direction = new Vector2(
                    MathF.Cos(angle),
                    MathF.Sin(angle)
                );

                for (
                    float distance = 16f;
                    distance < radius;
                    distance += 8f)
                {
                    Vector2 checkPosition =
                        target + direction * distance;

                    Point tilePosition =
                        checkPosition.ToTileCoordinates();

                    if (tilePosition.X < 0 ||
                        tilePosition.X >= Main.maxTilesX ||
                        tilePosition.Y < 0 ||
                        tilePosition.Y >= Main.maxTilesY)
                    {
                        break;
                    }

                    Tile tile =
                        Main.tile[
                            tilePosition.X,
                            tilePosition.Y
                        ];

                    if (!tile.HasTile ||
                        !Main.tileSolid[tile.TileType])
                    {
                        continue;
                    }

                    Vector2 spawnPosition =
                        target + direction * (distance - 10f);

                    Point spawnTile =
                        spawnPosition.ToTileCoordinates();

                    if (spawnTile.X < 0 ||
                        spawnTile.X >= Main.maxTilesX ||
                        spawnTile.Y < 0 ||
                        spawnTile.Y >= Main.maxTilesY)
                    {
                        break;
                    }

                    Tile spawnTileData =
                        Main.tile[
                            spawnTile.X,
                            spawnTile.Y
                        ];

                    if (!spawnTileData.HasTile ||
                        !Main.tileSolid[spawnTileData.TileType])
                    {
                        points.Add(spawnPosition);
                    }

                    break;
                }
            }

            return points;
        }
    }
}