using DivineRoot.Content.Configs;
using Terraria;
using Terraria.ModLoader;

namespace DivineRoot.Content.Systems
{
    public class MusicSystem : ModSceneEffect
    {
        private const string AudioPath = "Content/Audio/";

        public override void Load()
        {
            if (Main.dedServ)
                return;

            MusicLoader.AddMusic(Mod, AudioPath + "DemonPrimThemeDivRoot");
            MusicLoader.AddMusic(Mod, AudioPath + "HellThemeDivRoot");
            MusicLoader.AddMusic(Mod, AudioPath + "TownThemeDivRoot");
            MusicLoader.AddMusic(Mod, AudioPath + "SnowThemeDivRoot");
            MusicLoader.AddMusic(Mod, AudioPath + "CorruptionThemeDivRoot");
            MusicLoader.AddMusic(Mod, AudioPath + "CrimosnThemeDivRoot");
        }

        public override SceneEffectPriority Priority
        {
            get
            {
                if (GetBossMusic() >= 0)
                    return SceneEffectPriority.BossHigh;

                Player player = Main.LocalPlayer;

                if (player.ZoneUnderworldHeight)
                    return SceneEffectPriority.Environment;

                if (player.ZoneCrimson || player.ZoneCorrupt)
                    return SceneEffectPriority.BiomeHigh;

                if (player.ZoneSnow)
                    return SceneEffectPriority.BiomeMedium;

                return SceneEffectPriority.BiomeLow;
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
                return GetMusic("HellThemeDivRoot");

            if (player.ZoneCrimson)
                return GetMusic("CrimosnThemeDivRoot");

            if (player.ZoneCorrupt)
                return GetMusic("CorruptionThemeDivRoot");

            if (player.ZoneSnow)
                return GetMusic("SnowThemeDivRoot");

            if (Main.dayTime && player.townNPCs > 2f)
                return GetMusic("TownThemeDivRoot");

            return -1;
        }

        private int GetBossMusic()
        {
            if (IsModNpcAlive("PrimordialDemon"))
                return GetMusic("DemonPrimThemeDivRoot");

            if (IsModNpcAlive("CouncilOfSisters"))
                return MusicLoader.GetMusicSlot(Mod, "Content/Music/SistersDivineRoot");

            return -1;
        }

        private bool IsModNpcAlive(string internalName)
        {
            return Mod.TryFind<ModNPC>(internalName, out var modNpc) &&
                   NPC.AnyNPCs(modNpc.Type);
        }

        private int GetMusic(string name)
        {
            return MusicLoader.GetMusicSlot(Mod, AudioPath + name);
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
