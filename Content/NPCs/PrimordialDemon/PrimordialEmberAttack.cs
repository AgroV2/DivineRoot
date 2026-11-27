using System;

namespace DivineRoot.Content.NPCs.PrimordialDemon
{
    internal static class PrimordialEmberAttack
    {
        public const int OrbCount = 4;
        public const int SwingFrames = 9;
        public const int TicksPerSwingFrame = 4;
        public const int SummonDuration = 2 * SwingFrames * TicksPerSwingFrame;
        public const int OrbBirthStart = 42;
        public const int OrbBirthStagger = 5;
        public const int OrbBirthDuration = 14;
        public const int SpreadDuration = 42;
        public const int AimDuration = 42;
        public const int LockDuration = 24;
        public const int BeamDuration = 26;
        public const int ShotRestDuration = 12;
        public const int FireTime = AimDuration + LockDuration;
        public const int ShotDuration = FireTime + BeamDuration + ShotRestDuration;
        public const int VolleyStart = SummonDuration + SpreadDuration;
        public const int RecoveryDuration = 36;
        public const float ScatterDistance = 230f;
        public const int RecoveryStart = VolleyStart + OrbCount * ShotDuration;
        public const int TotalDuration = RecoveryStart + RecoveryDuration;

        private static readonly int[] FireOrder = { 0, 2, 1, 3 };

        public static bool TryGetShot(int attackTick, out int orbIndex, out int shotTick)
        {
            int volleyTick = attackTick - VolleyStart;
            if (volleyTick < 0 || volleyTick >= OrbCount * ShotDuration)
            {
                orbIndex = -1;
                shotTick = -1;
                return false;
            }

            orbIndex = FireOrder[volleyTick / ShotDuration];
            shotTick = volleyTick % ShotDuration;
            return true;
        }

        public static float GetCharge(int shotTick) => Math.Clamp(shotTick / (float)FireTime, 0f, 1f);
    }
}
