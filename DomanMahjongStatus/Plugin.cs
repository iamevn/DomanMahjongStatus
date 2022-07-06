using Dalamud;
using Dalamud.Configuration;
using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Threading.Tasks;
using Optional;
using Optional.Linq;
using Optional.Collections;
using Optional.Unsafe;

namespace DomanMahjongStatus
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "Doman Mahjong Status";

        private const string commandName = "/majstat";
        private const string dumpCommandName = "/dumpUIState";
        private const string logCommandName = "/majlog";
        private const string writeCommandName = "/majwrite";
        private IntPtr addonPtr = IntPtr.Zero;

        private IntPtr AddonPtr
        {
            get
            {
                var firstChoice = GameGui.GetAddonByName("EmjL", 1);
                if (firstChoice == IntPtr.Zero)
                    return GameGui.GetAddonByName("Emj", 1);
                else
                    return firstChoice;
            }
        }

#pragma warning disable IDE0052
        private DalamudPluginInterface PluginInterface { get; init; }
        private ChatGui ChatGui { get; init; }
        private GameGui GameGui { get; init; }
        private Framework Framework { get; init; }
        private CommandManager CommandManager { get; init; }
#pragma warning restore IDE0052

        public Plugin(DalamudPluginInterface pluginInterface, Framework framework, CommandManager commandManager, ChatGui chatGui, GameGui gameGui)
        {
            this.PluginInterface = pluginInterface;
            this.Framework = framework;
            this.CommandManager = commandManager;
            this.ChatGui = chatGui;
            this.GameGui = gameGui;

            _ = CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Write current mahjong status to chat log"
            });

            _ = CommandManager.AddHandler(dumpCommandName, new CommandInfo(OnDumpCommand)
            {
                HelpMessage = "Dump UIState bytes",
            });

            _ = CommandManager.AddHandler(logCommandName, new CommandInfo(OnLogCommand)
            {
                HelpMessage = "Log mahjong stats parsed from UIState",
            });

            _ = CommandManager.AddHandler(writeCommandName, new CommandInfo(OnWriteCommand)
            {
                HelpMessage = "Overwrite a byte in the mahjong stat section of UIState",
            });

            // Framework.Update += PollMahjong;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Framework.Update -= PollMahjong;
                _ = CommandManager.RemoveHandler(commandName);
                _ = CommandManager.RemoveHandler(dumpCommandName);
                _ = CommandManager.RemoveHandler(logCommandName);
                _ = CommandManager.RemoveHandler(writeCommandName);
            }
        }

        private void OnDumpCommand(string command, string args)
        {
            ChatGui.Print("Dumping UIState...");
            string dest = DebugUIState.Dump();
            ChatGui.Print($"Dumped to {dest}");
        }

        private void OnLogCommand(string command, string args)
        {
            ChatGui.Print("Reading UIState...");
            DebugUIState.LogChat(ChatGui);

            ChatGui.Print("Scanning...");
            SigScanner scanner = new SigScanner();
            IntPtr found = scanner.ScanModule("2D 00 9E 06 CB 06 3C 00 09 01");
            if (found == IntPtr.Zero)
            {
                ChatGui.Print("found nothing");
            }
            else
            {
                ChatGui.Print($"found at 0x{found:X}");
            }
        }

        private static bool TryParseHex(string s, out int result)
        {
            char[] _trim_hex = new char[] { '0', 'x' };
            return int.TryParse(s.TrimStart(_trim_hex), System.Globalization.NumberStyles.HexNumber, null, out result);
        }
        private static bool TryParseHex(string s, out byte result)
        {
            char[] _trim_hex = new char[] { '0', 'x' };
            return byte.TryParse(s.TrimStart(_trim_hex), System.Globalization.NumberStyles.HexNumber, null, out result);
        }

        private void OnWriteCommand(string command, string args)
        {
            string[] splitArgs = args.Split(' ');
            if (splitArgs.Length != 2)
            {
                ChatGui.PrintError($"error parsing {command} {args}. expected 2 args, got {splitArgs.Length}");
                return;
            }

            int offset;
            byte value;
            bool success;

            if (splitArgs[0].StartsWith("0x"))
                success = TryParseHex(splitArgs[0], out offset);
            else
                success = int.TryParse(splitArgs[0], out offset);
            if (!success)
            {
                ChatGui.PrintError($"Unable to parse \"{splitArgs[0]}\" as an int");
                return;
            }
            if (offset < 0 || offset >= 10)
            {
                ChatGui.PrintError($"Offset {offset} out of range (0-9 inclusive)");
                return;
            }

            if (splitArgs[1].StartsWith("0x"))
                success = TryParseHex(splitArgs[1], out value);
            else
                success = byte.TryParse(splitArgs[1], out value);
            if (!success)
            {
                ChatGui.PrintError($"Unable to parse \"{splitArgs[1]}\" as a byte");
                return;
            }

            ChatGui.Print($"OverwriteRankInfo({offset}, 0x{value:X2});");
            DebugUIState.OverwriteRankInfo(offset, value);
        }

        private void OnCommand(string command, string args)
        {
            PluginLog.Log("hello again from {Name}", Name);
            if (AddonPtr != IntPtr.Zero)
            {
                PluginLog.Log("mahjong addon at: {addonPtr}", AddonPtr.ToString("X"));
                ChatGui.Print(" yup, looks like you're playing mahjong!");
                try
                {
                    var reader = new UIReaderMahjongGame(AddonPtr);
                    MahjongStatus gameState = reader.ReadMahjongStatus();
                    ChatGui.Print(gameState.ToString());

                    reader.ReadCurrentPlayer()
                        .MatchSome(seat => ChatGui.Print($" current turn: {seat}"));

                    if (reader.ScoreScreenFinished())
                    {
                        PluginLog.Log("score screen visible");
                        Option<string> name = reader.GetWinnerName();
                        Option<string> winType = reader.GetWinType();

                        Option<string> hanFuText = reader.GetHanFuText();
                        Option<string> score = reader.GetHandScoreText();
                        var yakuList = reader.GetWinningYakuList();
                        var yakuText = yakuList.Map(tup =>
                        {
                            (string yaku, string value) = tup;
                            return $"{yaku} ({value})";
                        });
                        name.MatchSome(name => winType.MatchSome(winType => hanFuText.MatchSome(hanFu => score.MatchSome(score =>
                            ChatGui.Print($"{name} {winType} with [{string.Join(", ", yakuText)}] for {hanFu} totaling {score}")))));
                    }
                }
                catch (UIReaderMahjongGame.UIReaderError err)
                {
                    ChatGui.PrintError($"error parsing mahjong ui: {err.Message}");
                }
            }
            else
            {
                ChatGui.Print(" why aren't you playing mahjong?");
            }
            PluginLog.Log("end of command");
        }

        private void PollMahjong(Framework framework)
        {
            try
            {
                var maybeAddon = GameGui.GetAddonByName("EmjL", 1);
                if (addonPtr == IntPtr.Zero)
                    return;
                addonPtr = maybeAddon;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Updater has crashed for {Name}", Name);
                ChatGui.PrintError($"{Name} has encountered a critical error");
            }
        }
    }
}
