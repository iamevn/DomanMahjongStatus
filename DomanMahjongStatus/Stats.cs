using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace DomanMahjongStatus
{
    static class Stats
    {
        public class Rank
        {
            public enum RankCategory { Unranked, Kyu, Dan }

            public RankCategory Category { get; init; }
            public int Level { get; init; }

            public Rank(byte b) => (this.Category, this.Level) = b switch
            {
                0 => (RankCategory.Unranked, 0),
                <= 9 => (RankCategory.Kyu, 10 - b),
                _ => (RankCategory.Dan, b - 9),
            };

            public override string ToString() => Category == RankCategory.Unranked ? Category.ToString() : $"{Level} {Category}";

            public static implicit operator int(Rank r) => (r.Category, r.Level) switch
            {
                (RankCategory.Unranked, _) => 0,
                (RankCategory.Kyu, int n) => 10 - n,
                (RankCategory.Dan, int n) => n + 9,
            };
        }

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
        public static byte RankLevelRaw => Marshal.ReadByte(RankInfoPtr, 8);
        public static byte Unknown => Marshal.ReadByte(RankInfoPtr, 9);
        public static Rank RankLevel => new Rank(RankLevelRaw);

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
