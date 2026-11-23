using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace DivineRoot.Content.NPCs.PrimordialDemon
{
    internal static class PrimordialEmberVisuals
    {
        private const int CoreSize = 64;
        private const int CoreFrames = 8;
        private const int GlowSize = 128;
        private static Texture2D coreTexture;
        private static Texture2D glowTexture;

        public static void DrawSummoningGlow(SpriteBatch spriteBatch, Vector2 worldPosition, Vector2 screenPos,
            float phase, float visibility, float bodyScale)
        {
            if (Main.dedServ || visibility <= 0.001f)
                return;

            EnsureTextures();
            float strength = MathHelper.Clamp(visibility, 0f, 1f);
            float pulse = 1f + MathF.Sin(phase * 1.4f) * 0.035f;
            float scale = (0.5f + MathF.Sqrt(strength) * 0.6f) * pulse * bodyScale * 1.7f;
            Vector2 position = worldPosition - screenPos;
            Vector2 origin = new Vector2(GlowSize * 0.5f);

            spriteBatch.Draw(glowTexture, position, null, new Color(255, 33, 7, 0) * (strength * 0.75f),
                0f, origin, new Vector2(1.15f, 1f) * scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(glowTexture, position, null, new Color(255, 104, 13, 0) * (strength * 0.9f),
                0f, origin, new Vector2(0.72f, 0.62f) * scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(glowTexture, position, null, new Color(255, 191, 57, 0) * (strength * 0.65f),
                0f, origin, new Vector2(0.3f, 0.28f) * scale, SpriteEffects.None, 0f);
        }

        public static void Draw(SpriteBatch spriteBatch, Vector2 worldPosition, Vector2 screenPos,
            float phase, float visibility, float charge, float recoil, float rotation)
        {
            if (Main.dedServ || visibility <= 0.001f)
                return;

            EnsureTextures();
            visibility = MathHelper.Clamp(visibility, 0f, 1f);
            charge = MathHelper.Clamp(charge, 0f, 1f);
            recoil = MathHelper.Clamp(recoil, 0f, 1f);

            Vector2 position = worldPosition - screenPos;
            float pulse = 1f + MathF.Sin(phase * 1.7f) * 0.045f + MathF.Sin(phase * 2.63f + 1.3f) * 0.025f;
            float growth = MathHelper.SmoothStep(0f, 1f, visibility);
            float scale = (0.74f + growth * 0.26f) * pulse * (1f + charge * 0.08f + recoil * 0.14f);
            float heat = visibility * (0.78f + charge * 0.22f);
            Vector2 glowOrigin = new Vector2(GlowSize * 0.5f);

            spriteBatch.Draw(glowTexture, position, null, new Color(255, 35, 8, 0) * (heat * 0.42f),
                0f, glowOrigin, scale * (0.94f + charge * 0.14f) * 1.45f, SpriteEffects.None, 0f);
            spriteBatch.Draw(glowTexture, position, null, new Color(255, 102, 9, 0) * (heat * 0.55f),
                0f, glowOrigin, scale * 0.67f, SpriteEffects.None, 0f);

            int frame = PositiveModulo((int)MathF.Floor(phase * 2.4f), CoreFrames);
            Rectangle coreFrame = new Rectangle(0, frame * CoreSize, CoreSize, CoreSize);
            spriteBatch.Draw(coreTexture, position, coreFrame, Color.White * visibility, rotation,
                new Vector2(CoreSize * 0.5f), scale, SpriteEffects.None, 0f);

            Vector2 hotSpot = position + new Vector2(-3f + MathF.Sin(phase * 0.8f), -3f).RotatedBy(rotation) * scale;
            spriteBatch.Draw(glowTexture, hotSpot, null, new Color(255, 174, 36, 0) * (heat * (0.40f + recoil * 0.22f)),
                0f, glowOrigin, scale * 0.22f, SpriteEffects.None, 0f);
        }

        private static void EnsureTextures()
        {
            if (coreTexture != null)
                return;

            Color[] corePixels = new Color[CoreSize * CoreSize * CoreFrames];
            for (int frame = 0; frame < CoreFrames; frame++)
            {
                float time = MathHelper.TwoPi * frame / CoreFrames;
                for (int y = 0; y < CoreSize; y++)
                {
                    for (int x = 0; x < CoreSize; x++)
                    {
                        float px = (x / 2 * 2 + 1) - CoreSize * 0.5f;
                        float py = (y / 2 * 2 + 1) - CoreSize * 0.5f;
                        float distance = MathF.Sqrt(px * px + py * py);
                        float angle = MathF.Atan2(py, px);
                        float outline = 22.5f + MathF.Sin(angle * 5f + time) * 1.7f
                            + MathF.Sin(angle * 9f - time) * 1.1f
                            + MathF.Sin(angle * 13f + time * 2f) * 0.65f;
                        if (distance > outline)
                            continue;

                        float rim = distance / outline;
                        float flow = MathF.Sin(px * 0.27f + MathF.Sin(py * 0.19f - time) * 2.2f + time)
                            + MathF.Sin(py * 0.35f - time * 2f)
                            + MathF.Sin((px - py) * 0.24f + time);
                        float heat = (1f - rim) * 0.68f + flow * 0.105f + 0.28f;
                        Color color;
                        if (rim > 0.88f)
                            color = flow > 0.3f ? new Color(181, 23, 5) : new Color(113, 9, 11);
                        else if (heat < 0.29f)
                            color = new Color(151, 14, 6);
                        else if (heat < 0.43f)
                            color = new Color(218, 37, 4);
                        else if (heat < 0.60f)
                            color = new Color(255, 87, 5);
                        else if (heat < 0.79f)
                            color = new Color(255, 139, 8);
                        else
                            color = new Color(255, 194, 39);

                        corePixels[x + (y + frame * CoreSize) * CoreSize] = color;
                    }
                }
            }

            Color[] glowPixels = new Color[GlowSize * GlowSize];
            for (int y = 0; y < GlowSize; y++)
            {
                for (int x = 0; x < GlowSize; x++)
                {
                    float dx = (x + 0.5f - GlowSize * 0.5f) / (GlowSize * 0.5f);
                    float dy = (y + 0.5f - GlowSize * 0.5f) / (GlowSize * 0.5f);
                    float radius = MathF.Sqrt(dx * dx + dy * dy);
                    float falloff = MathHelper.Clamp(1f - radius, 0f, 1f);
                    glowPixels[x + y * GlowSize] = Color.White * (falloff * falloff * falloff);
                }
            }

            coreTexture = new Texture2D(Main.instance.GraphicsDevice, CoreSize, CoreSize * CoreFrames);
            coreTexture.SetData(corePixels);
            glowTexture = new Texture2D(Main.instance.GraphicsDevice, GlowSize, GlowSize);
            glowTexture.SetData(glowPixels);
        }

        private static int PositiveModulo(int value, int divisor) => (value % divisor + divisor) % divisor;

        internal static void Unload()
        {
            Texture2D oldCore = coreTexture;
            Texture2D oldGlow = glowTexture;
            coreTexture = null;
            glowTexture = null;
            if (oldCore != null || oldGlow != null)
            {
                Main.QueueMainThreadAction(() =>
                {
                    oldCore?.Dispose();
                    oldGlow?.Dispose();
                });
            }
        }
    }

    public sealed class PrimordialEmberVisualResourceSystem : ModSystem
    {
        public override void Unload() => PrimordialEmberVisuals.Unload();
    }
}
