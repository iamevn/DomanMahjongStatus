using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace DomanMahjongStatus
{
    static class Stats
    {
        public static IntPtr UIStatePtr
        {
            get
            {
                unsafe
                {
                    return (IntPtr)UIState.Instance();
                }
            }
        }

        public const int UIStateSize = 0x168D8;
        public const int RankInfoOffset = 0x14F78;
        public const int RankInfoSize = 10;
        public static IntPtr RankInfoPtr => UIStatePtr + RankInfoOffset;

        public static short MatchCount => Marshal.ReadInt16(RankInfoPtr, 0);
        public static short CurrentRating => Marshal.ReadInt16(RankInfoPtr, 2);
        public static short MaxRating => Marshal.ReadInt16(RankInfoPtr, 4);
        public static short RankPoints => Marshal.ReadInt16(RankInfoPtr, 6);
        public static byte Unknown1 => Marshal.ReadByte(RankInfoPtr, 8);
        public static byte Unknown2 => Marshal.ReadByte(RankInfoPtr, 9);

        public static byte[] RankInfoBytes
        {
            get
            {
                byte[] barr = new byte[RankInfoSize];
                Marshal.Copy(RankInfoPtr, barr, 0, RankInfoSize);

                return barr;
            }
        }

        public static bool Initialized
        {
            get
            {
                int sum = 0;
                foreach (byte b in RankInfoBytes)
                    sum += b;
                return sum > 0;
            }
        }
    }
}
