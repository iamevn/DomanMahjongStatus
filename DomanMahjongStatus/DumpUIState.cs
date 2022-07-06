using System;
using System.IO;
using System.Runtime.InteropServices;
using Dalamud.Game.Gui;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace DomanMahjongStatus
{
    public static class DebugUIState
    {
        private const string BaseFolder = @"c:\Users\Evan\Desktop\XIV_UIState_dumps";
        private const string FileBase = "UIState";

        private const int UIStateSize = 0x168D8;
        private static IntPtr UIStatePtr
        {
            get
            {
                unsafe
                {
                    return (IntPtr)UIState.Instance();
                }
            }
        }

        private static byte[] GetUIStateBytes()
        {
            IntPtr uistate = UIStatePtr;
            byte[] barr = new byte[UIStateSize];
            Marshal.Copy(uistate, barr, 0, UIStateSize);
            return barr;
        }

        private static string GetFilePath(int n)
            => Path.Combine(BaseFolder, $"{FileBase}_{n:D2}");

        private static FileStream GetNewFileHandle()
        {
            int n = 0;
            string path = GetFilePath(n);
            while (File.Exists(path))
            {
                n += 1;
                path = GetFilePath(n);
            }

            Directory.CreateDirectory(BaseFolder);
            return File.Create(path);
        }

        public static string Dump()
        {
            using FileStream file = GetNewFileHandle();
            file.Write(GetUIStateBytes(), 0, UIStateSize);
            return file.Name;
        }


        private const int RankInfoOffset = 0x14F78;
        private static IntPtr RankInfoPtr { get => UIStatePtr + RankInfoOffset; }
        private static short MatchCount { get => Marshal.ReadInt16(RankInfoPtr, 0); }
        private static short CurrentRating { get => Marshal.ReadInt16(RankInfoPtr, 2); }
        private static short MaxRating { get => Marshal.ReadInt16(RankInfoPtr, 4); }
        private static short RankPoints { get => Marshal.ReadInt16(RankInfoPtr, 6); }
        private static byte UnknownByte1 { get => Marshal.ReadByte(RankInfoPtr, 8); }
        private static byte UnknownByte2 { get => Marshal.ReadByte(RankInfoPtr, 9); }

        public static void LogChat(ChatGui chat)
        {
            byte[] barr = new byte[10];
            Marshal.Copy(RankInfoPtr, barr, 0, 10);
            string bytes = BitConverter.ToString(barr);

            chat.Print($"read {bytes} from 0x{RankInfoPtr:X} (UIState* + 0x{RankInfoOffset:X})");
            chat.Print($"Rank: ??? (0x{UnknownByte1:X2} 0x{UnknownByte2:X2}) - {RankPoints} points");
            chat.Print($"Current Rating: {CurrentRating}");
            chat.Print($"Highest Rating: {MaxRating}");
            chat.Print($"MatchesPlayed: {MatchCount}");
        }

        public static void OverwriteRankInfo(int offset, byte value)
        {
            if (offset >= 0 && offset <= 9)
                Marshal.WriteByte(RankInfoPtr + offset, value);
        }
    }
}