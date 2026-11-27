using Terraria;
using Terraria.ModLoader;

namespace DivineRoot.Content.Items.Weapons
{
    public sealed class SistersTailPlayer : ModPlayer
    {
        public int StrikeCount;

        public override void Initialize()
        {
            StrikeCount = 0;
        }

        public override void OnRespawn()
        {
            StrikeCount = 0;
        }
    }
}