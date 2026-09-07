using DivineRoot.Content.Configs;
using Terraria;
using Terraria.ModLoader;

namespace DivineRoot.Content.Systems
{
    public class MusicSystem : ModSceneEffect
    {
        private const string MusicPath = "DivineRoot/Content/Music/";

        public override SceneEffectPriority Priority
        {
            get
            {
                if (GetBossMusic() >= 0)
                    return SceneEffectPriority.BossHigh;

                if (Main.LocalPlayer.ZoneCrimson &&
                    !Main.LocalPlayer.ZoneUnderworldHeight)
                {
                    return SceneEffectPriority.BiomeHigh;
                }

                return SceneEffectPriority.Environment;
            }
        }

        public override bool IsSceneEffectActive(Player player)
        {
            if (Main.dedServ ||
                !ModContent.GetInstance<CustomMusicConfig>().CustomMusic)
            {
                return false;
            }

            return GetSceneMusic(player) >= 0;
        }

        public override int Music
        {
            get
            {
                if (Main.dedServ ||
                    !ModContent.GetInstance<CustomMusicConfig>().CustomMusic)
                {
                    return -1;
                }

                return GetSceneMusic(Main.LocalPlayer);
            }
        }

        private int GetSceneMusic(Player player)
        {
            int bossMusic = GetBossMusic();

            if (bossMusic >= 0)
                return bossMusic;

            if (AnyBossAlive())
                return -1;

            if (player.ZoneUnderworldHeight)
                return GetMusic("HellDivineRoot");

            if (player.ZoneCrimson)
                return GetMusic("CrimsonDivineRoot");

            if (player.ZoneSnow)
                return GetMusic("WinterDivineRoot");

            if (Main.dayTime && player.townNPCs > 2f)
                return GetMusic("CityDayDivineRoot");

            return -1;
        }

        private int GetBossMusic()
        {
            if (IsModNpcAlive("PrimordialDemon"))
                return GetMusic("PrimordialDemonDivineRoot");

            if (IsModNpcAlive("CouncilOfSisters"))
                return GetMusic("SistersDivineRoot");

            return -1;
        }

        private bool IsModNpcAlive(string internalName)
        {
            return Mod.TryFind<ModNPC>(internalName, out var modNpc) &&
                   NPC.AnyNPCs(modNpc.Type);
        }

        private static int GetMusic(string name)
        {
            return MusicLoader.GetMusicSlot(MusicPath + name);
        }

        private static bool AnyBossAlive()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (npc.active && npc.boss)
                    return true;
            }

            return false;
        }
    }
}