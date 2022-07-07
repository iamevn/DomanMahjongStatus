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

        private const string commandName = "/mj";
        private const string logCommandName = "/mjstat";
        private const string dumpCommandName = "/dumpUIState";

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

            _ = CommandManager.AddHandler(logCommandName, new CommandInfo(OnStatCommand)
            {
                HelpMessage = "Log mahjong stats parsed from UIState",
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
            }
        }

        private void OnDumpCommand(string command, string args)
        {
            ChatGui.Print("Dumping UIState...");
            string dest = DebugUIState.Dump();
            ChatGui.Print($"Dumped to {dest}");
        }

        private void OnStatCommand(string command, string args)
        {
            ChatGui.Print("Reading UIState...");

            string bytes = "[" + BitConverter.ToString(Stats.RankInfoBytes).Replace("-", ", ") + "]";
            ChatGui.Print($"read {bytes} from UIState+0x{Stats.RankInfoOffset:X}");

            if (Stats.Initialized)
            {
                ChatGui.Print($"Matches Played: {Stats.MatchCount}");
                ChatGui.Print($"Current Rating: {Stats.CurrentRating}");
                ChatGui.Print($"Highest Rating: {Stats.MaxRating}");
                ChatGui.Print($"Rank: ??? (0x{Stats.Unknown1:X2} 0x{Stats.Unknown2:X2}) - {Stats.RankPoints} points");
            }
            else
            {
                ChatGui.Print("Couldn't find stats, have you opened the Gold Saucer panel and have you played any mahjong?");
            }
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
