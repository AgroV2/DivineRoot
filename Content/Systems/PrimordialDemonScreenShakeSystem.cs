using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace DivineRoot.Content.Systems
{
    public class PrimordialDemonScreenShakeSystem : ModSystem
    {
        private static float shakeStrength;

        public static void AddShake(Vector2 source, float strength)
        {
            if (Main.dedServ || Main.gameMenu || !Main.LocalPlayer.active)
                return;

            float distanceFade = 1f - MathHelper.Clamp((Vector2.Distance(Main.LocalPlayer.Center, source) - 500f) / 1600f, 0f, 1f);
            shakeStrength = MathHelper.Max(shakeStrength, MathHelper.Clamp(strength, 0f, 32f) * distanceFade);
        }

        public override void ModifyScreenPosition()
        {
            if (Main.gameMenu || Main.LocalPlayer.dead)
            {
                shakeStrength = 0f;
                return;
            }

            if (shakeStrength > 0.05f)
            {
                Main.screenPosition += Main.rand.NextVector2CircularEdge(shakeStrength, shakeStrength * 0.75f);
                shakeStrength *= 0.90f;
            }
        }

        public override void OnWorldUnload() => shakeStrength = 0f;
        public override void Unload() => shakeStrength = 0f;
    }
}
