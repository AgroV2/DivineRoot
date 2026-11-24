using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using DivineRoot.Content.Buffs;

namespace DivineRoot.Content.Projectiles;

public sealed class SpectralArmProjectile : ModProjectile
{
    private const float range = 25 * 16f;
    private const float bladeLength = 102f;
    private const float swingArc = 1.05f;
    private const float opacity = 0.87f;
    private const int windup = 12;
    private const int strike = 12;
    private const int recovery = 17;

    private static readonly Vector2 upperPivot = new(3, 41);
    private static readonly Rectangle forearmFrame = new(4, 0, 49, 36);
    private static readonly Vector2 bladePivot = new(76, 15);
    private static readonly Color spiritColor = new(183, 240, 255);

    private static readonly Vector2 glowPadding = new(10f);
    private static Texture2D upperGlow;
    private static Texture2D forearmGlow;
    private static Texture2D bladeGlow;
    private static Texture2D upperTexture;
    private static Texture2D forearmTexture;

    private Vector2 shoulderOffset;
    private Vector2 shoulder;
    private Vector2 previousShoulder;
    private Vector2 elbow;
    private Vector2 wrist;
    private Vector2 previousWrist;
    private Vector2 aimOffset;
    private float swordAngle;
    private float previousSwordAngle;
    private float swordLength;
    private float previousSwordLength;
    private float facing;
    private bool hasAim;
    private bool wasStriking;

    private int targetIndex => (int)Projectile.ai[0] - 1;
    private int timer => (int)Projectile.ai[1];
    private bool striking => targetIndex >= 0 && timer >= windup && timer < windup + strike;
    private Player owner => Main.player[Projectile.owner];

    public override void Load()
    {
        if (Main.dedServ)
            return;

        upperGlow = Mod.Assets.Request<Texture2D>("Content/Projectiles/SpectralArmUpperGlow", AssetRequestMode.ImmediateLoad).Value;
        forearmGlow = Mod.Assets.Request<Texture2D>("Content/Projectiles/SpectralArmForearmGlow", AssetRequestMode.ImmediateLoad).Value;
        bladeGlow = Mod.Assets.Request<Texture2D>("Content/Projectiles/SpectralArmProjectileGlow", AssetRequestMode.ImmediateLoad).Value;

        upperTexture = Mod.Assets.Request<Texture2D>("Content/Projectiles/SpectralArmUpper", AssetRequestMode.ImmediateLoad).Value;
        forearmTexture = Mod.Assets.Request<Texture2D>("Content/Projectiles/SpectralArmForearm", AssetRequestMode.ImmediateLoad).Value;
    }

    public override void Unload()
    {
        upperGlow = null;
        forearmGlow = null;
        bladeGlow = null;
        upperTexture = null;
        forearmTexture = null;
    }


    public override void SetStaticDefaults()
    {
        Main.projPet[Type] = true;
        ProjectileID.Sets.MinionSacrificable[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.width = 16;
        Projectile.height = 16;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.minion = true;
        Projectile.minionSlots = 1f;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.hide = true;
        Projectile.timeLeft = 2;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = strike;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool MinionContactDamage() => true;
    public override bool? CanCutTiles() => false;
    public override bool? CanDamage() => striking && hasAim ? null : false;

    public override void AI()
    {
        if (!owner.active || owner.dead || !owner.HasBuff(ModContent.BuffType<SpectralArmBuff>()))
        {
            hasAim = false;
            return;
        }

        Projectile.timeLeft = 2;
        previousShoulder = shoulder;
        previousWrist = wrist;
        previousSwordAngle = swordAngle;
        previousSwordLength = swordLength;
        wasStriking = striking && hasAim && Vector2.DistanceSquared(shoulder, owner.Center) < 64f * 64f;

        updateattack();
        updatepose();
        Projectile.Center = shoulder;
        emitparticles();
    }

    private void emitparticles()
    {
        if (Main.dedServ || !Main.rand.NextBool(striking ? 3 : 8))
            return;

        Vector2 tip = wrist + swordAngle.ToRotationVector2() * swordLength;
        Vector2 position = Vector2.Lerp(wrist, tip, Main.rand.NextFloat());
        Vector2 velocity = Main.rand.NextVector2Circular(0.65f, 0.65f);
        Dust particle = Dust.NewDustPerfect(position, DustID.IceTorch, velocity, 0, default, 0.75f);
        particle.noGravity = true;
    }

    private void updateattack()
    {
        bool isOwner = Main.myPlayer == Projectile.owner;
        if (targetIndex >= 0)
        {
            Projectile.ai[1]++;
            if (timer >= windup + strike + recovery)
            {
                Projectile.ai[0] = 0;
                Projectile.ai[1] = 0;
                hasAim = false;
                Projectile.netUpdate |= isOwner;
                return;
            }

            NPC target = Main.npc[targetIndex];
            if (timer < windup + strike && target.CanBeChasedBy(Projectile)
                && Vector2.DistanceSquared(owner.Center, target.Center) <= range * range)
            {
                aimOffset = target.Center - owner.Center;
                hasAim = true;
            }
            else if (timer < windup && isOwner)
            {
                Projectile.ai[1] = windup + strike;
                Projectile.netUpdate = true;
            }

            return;
        }

        hasAim = false;
        if (!isOwner)
            return;

        float closestDistance = range * range;
        int closest = -1;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!npc.CanBeChasedBy(Projectile))
                continue;

            float distance = Vector2.DistanceSquared(owner.Center, npc.Center);
            if (distance > closestDistance)
                continue;

            closestDistance = distance;
            closest = npc.whoAmI;
        }

        if (closest < 0)
            return;

        Projectile.ai[0] = closest + 1;
        Projectile.ai[2] = Projectile.ai[2] > 0f ? -1f : 1f;
        aimOffset = Main.npc[closest].Center - owner.Center;
        hasAim = true;
        Projectile.netUpdate = true;
    }

    private void updatepose()
    {
        facing = facing == 0f ? owner.direction : MathHelper.Lerp(facing, owner.direction, 0.22f);
        float gravity = owner.gravDir;
        float sway = MathF.Sin(Main.GameUpdateCount * 0.075f + Projectile.identity);
        shoulderOffset = new Vector2(-8f * facing, -4f * gravity);
        shoulder = owner.RotatedRelativePoint(owner.Center + shoulderOffset, addGfxOffY: false);
        Vector2 restDirection = new Vector2(-0.76f * facing, -0.66f * gravity).SafeNormalize(-Vector2.UnitY);
        Vector2 aimDirection = aimOffset.SafeNormalize(restDirection);
        float extension = timer < windup
            ? MathHelper.SmoothStep(0f, 1f, timer / (float)windup)
            : MathHelper.SmoothStep(1f, 0f, Math.Max(timer - windup - strike + 1, 0) / (float)recovery);

        if (!hasAim)
            extension = 0f;

        elbow = shoulder + (restDirection + aimDirection * (0.16f * extension)).SafeNormalize(restDirection) * 52f;
        Vector2 restWrist = elbow + new Vector2(-27f * facing, (-26f + sway * 3f) * gravity);
        Vector2 toTarget = owner.Center + aimOffset - elbow;
        Vector2 reachDirection = toTarget.SafeNormalize(aimDirection);
        float reach = Math.Clamp(toTarget.Length() - bladeLength * 0.59f, 8f, range);
        wrist = Vector2.Lerp(restWrist, elbow + reachDirection * reach, extension);

        float offset = timer < windup ? -swingArc : MathHelper.SmoothStep(-swingArc, swingArc,
            (timer - windup) / (float)(strike - 1));
        float restAngle = new Vector2(0.89f * facing, (-0.46f + sway * 0.04f) * gravity).ToRotation();
        float swingAngle = reachDirection.ToRotation() + offset * Projectile.ai[2] * gravity;
        swordAngle = restAngle + MathHelper.WrapAngle(swingAngle - restAngle) * extension;
        swordLength = MathHelper.Lerp(58f, bladeLength, extension);
    }

    public override bool? CanHitNPC(NPC target) =>
        Vector2.DistanceSquared(owner.Center, target.Center) <= range * range ? null : false;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 targetPosition = new(targetHitbox.X, targetHitbox.Y);
        Vector2 targetSize = new(targetHitbox.Width, targetHitbox.Height);
        int samples = wasStriking ? 4 : 0;
        for (int sample = 0; sample <= samples; sample++)
        {
            float progress = wasStriking ? sample / (float)samples : 1f;
            Vector2 start = Vector2.Lerp(previousWrist, wrist, progress);
            float angle = previousSwordAngle + MathHelper.WrapAngle(swordAngle - previousSwordAngle) * progress;
            Vector2 tip = start + angle.ToRotationVector2() * MathHelper.Lerp(previousSwordLength, swordLength, progress);
            float collisionPoint = 0f;
            if (Collision.CheckAABBvLineCollision(targetPosition, targetSize, start, tip, 14f, ref collisionPoint))
                return true;
        }

        return false;
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => behindProjectiles.Add(index);

    public override bool PreDraw(ref Color lightColor)
    {
        if (facing == 0f)
            return false;

        Texture2D blade = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
        Vector2 visualShoulder = owner.RotatedRelativePoint(owner.Center + shoulderOffset, addGfxOffY: true);
        Vector2 screenShoulder = visualShoulder - Main.screenPosition;
        Vector2 pixelShoulder = new(MathF.Round(screenShoulder.X), MathF.Round(screenShoulder.Y));
        Vector2 drawOffset = pixelShoulder + Main.screenPosition - shoulder;
        Vector2 drawWrist = wrist + drawOffset;

        if (striking && wasStriking)
            drawblade(blade, previousWrist - previousShoulder + shoulder + drawOffset, previousSwordAngle, previousSwordLength, spiritColor * 0.3f);

        Vector2 elbowOffset = elbow + drawOffset;
        Vector2 upperDirection = (elbow - shoulder).SafeNormalize(Vector2.Zero);
        Vector2 forearmDirection = (wrist - elbow).SafeNormalize(Vector2.Zero);
        Vector2 drawShoulder = shoulder + drawOffset;
        Vector2 upperEnd = elbowOffset + upperDirection * 3f;
        Vector2 forearmStart = elbowOffset - forearmDirection * 2f;
        Vector2 upperAxis = new(20f, 3f);
        Vector2 upperSegment = upperEnd - drawShoulder;
        Vector2 upperSource = upperAxis - upperPivot;
        Vector2 upperScale = new(upperSegment.Length() / upperSource.Length());
        Vector2 forearmSegment = drawWrist - forearmStart;
        Vector2 forearmScale = new(forearmSegment.Length() / forearmFrame.Width, 1.15f);
        float upperAngle = upperSegment.ToRotation() - upperSource.ToRotation();
        float forearmAngle = forearmSegment.ToRotation();
        Vector2 forearmOrigin = new(0f, 19f);
        drawglow(upperGlow, drawShoulder, upperAngle, upperPivot, upperScale);
        drawglow(forearmGlow, forearmStart, forearmAngle, forearmOrigin, forearmScale);
        drawglow(bladeGlow, drawWrist, swordAngle + MathHelper.Pi, bladePivot, new Vector2(swordLength / 75f, 1f));
        Main.EntitySpriteDraw(upperTexture, drawShoulder - Main.screenPosition, null, Color.White * opacity,
            upperAngle, upperPivot, upperScale, SpriteEffects.None);
        Main.EntitySpriteDraw(forearmTexture, forearmStart - Main.screenPosition, forearmFrame, Color.White * opacity,
            forearmAngle, forearmOrigin, forearmScale, SpriteEffects.None);
        drawblade(blade, drawWrist, swordAngle, swordLength, spiritColor);
        drawshimmer(blade, drawWrist, swordAngle, swordLength);
        return false;
    }

    private static void drawglow(Texture2D texture, Vector2 position, float angle, Vector2 pivot, Vector2 scale)
    {
        Color glow = new Color(106, 226, 255, 0) * (1.25f * opacity);
        Main.EntitySpriteDraw(texture, position - Main.screenPosition, null, glow, angle,
            pivot + glowPadding, scale, SpriteEffects.None);
    }

    private static void drawblade(Texture2D texture, Vector2 start, float angle, float length, Color color)
    {
        Main.EntitySpriteDraw(texture, start - Main.screenPosition, null, color * opacity, angle + MathHelper.Pi,
            bladePivot, new Vector2(length / 75f, 1f), SpriteEffects.None);
    }

    private static void drawshimmer(Texture2D texture, Vector2 start, float angle, float length)
    {
        const int strip = 2;
        float phase = Main.GlobalTimeWrappedHourly * 4f;
        Vector2 scale = new(length / 75f, 1f);
        for (int x = 0; x < 76; x += strip)
        {
            float wave = 0.5f + 0.5f * MathF.Sin(x * 0.19f + phase);
            float glint = wave * wave * wave;
            Color tint = Color.Lerp(new Color(85, 173, 255, 0), new Color(235, 208, 255, 0), glint)
                * ((0.12f + glint * 0.48f) * opacity);
            Rectangle section = new(x, 0, strip, texture.Height);
            Main.EntitySpriteDraw(texture, start - Main.screenPosition, section, tint, angle + MathHelper.Pi,
                new Vector2(bladePivot.X - x, bladePivot.Y), scale, SpriteEffects.None);
        }
    }
}