using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Plugin;
using System;

namespace DomanMahjongStatus
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "Doman Mahjong Status";
        private const string logCommandName = "/mjstat";

        private ChatGui ChatGui { get; init; }
        private CommandManager CommandManager { get; init; }

        public Plugin(CommandManager commandManager, ChatGui chatGui)
        {
            this.CommandManager = commandManager;
            this.ChatGui = chatGui;

            _ = CommandManager.AddHandler(logCommandName, new CommandInfo(OnStatCommand)
            {
                HelpMessage = "Log mahjong stats parsed from UIState",
            });
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
                _ = CommandManager.RemoveHandler(logCommandName);
            }
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
    }
}
