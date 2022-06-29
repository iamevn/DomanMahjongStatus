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

namespace DomanMahjongStatus
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "Doman Mahjong Status";

        private const string commandName = "/majstat";
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
            //pluginInterface.Create<Plugin>();
            this.PluginInterface = pluginInterface;
            this.Framework = framework;
            this.CommandManager = commandManager;
            this.ChatGui = chatGui;
            this.GameGui = gameGui;

            _ = CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Write current mahjong status to chat log"
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
                    MahjongStatus gameState = new UIReaderMahjongGame(AddonPtr).ReadMahjongStatus();
                    ChatGui.Print(gameState.ToString());

                    new UIReaderMahjongGame(AddonPtr).ReadCurrentPlayer()
                        .MatchSome(seat => ChatGui.Print($" current turn: {seat}"));
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
