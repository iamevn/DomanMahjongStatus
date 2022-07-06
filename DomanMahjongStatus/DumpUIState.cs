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
    }
}