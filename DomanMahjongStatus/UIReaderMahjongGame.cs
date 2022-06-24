using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using static DomanMahjongStatus.Mahjong;
using static DomanMahjongStatus.GUINodeExtra;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Logging;

namespace DomanMahjongStatus
{
    public partial class UIReaderMahjongGame
    {
        private readonly IntPtr addonPtr;

        public unsafe AtkUnitBase* BasePtr => (AtkUnitBase*)addonPtr;
        public unsafe AtkResNode* RootNode => BasePtr->RootNode;

        public UIReaderMahjongGame(IntPtr addonPtr)
        {
            this.addonPtr = addonPtr;
        }
        public MahjongStatus ReadMahjongStatus()
        {
            MahjongStatus gameState = MahjongStatus.DummyStatus();
            // round/hand number
            unsafe
            {
                // var topRight = GetImmediateChildWithId(RootNode, 16);
                // var imgNode = GetImmediateChildWithId(topRight, 19);
                // var imgNode = GUINodeExtra.GetChildWithId(RootNode, 19); // also works
                var imgNode = GetChildNested(RootNode, 16, 19);
                var texRes = GetImageTextureResource(imgNode);
                if (texRes != null)
                {
                    (Round round, int hand) = Conversions.RoundHand(texRes->IconID);
                    gameState.SetRoundHand(round, hand);
                }
            }
            // riichi/honba count
            unsafe
            {
                var indicatorsNode = GetImmediateChildWithId(RootNode, 21);
                var honbaNode = GetImmediateChildWithId(indicatorsNode, 23);
                var riichiNode = GetImmediateChildWithId(indicatorsNode, 22);
                var honbaText = GUINodeUtils.GetNodeText(honbaNode);
                var riichiText = GUINodeUtils.GetNodeText(riichiNode);

                if (int.TryParse(honbaText?.Trim('×')?.Trim(' '), out int honbaCount))
                    gameState.HonbaCount = honbaCount;
                if (int.TryParse(riichiText?.Trim('×')?.Trim(' '), out int riichiCount))
                    gameState.RiichiCount = riichiCount;
            }
            // player states
            unsafe
            {
                const int PLAYER_COUNT = 4;
                var playerPanes = new AtkResNode*[PLAYER_COUNT]
                {
                    GetChildNested(RootNode, 36, 37, 38),
                    GetChildNested(RootNode, 36, 43, 44),
                    GetChildNested(RootNode, 36, 41, 42),
                    GetChildNested(RootNode, 36, 39, 40),
                };

                var players = new PlayerStatus[PLAYER_COUNT]
                {
                    gameState.Player,
                    gameState.LeftOpponent,
                    gameState.MiddleOpponent,
                    gameState.RightOpponent,
                };

                for (int i = 0; i < PLAYER_COUNT; i++)
                {
                    AtkResNode* pane = playerPanes[i];
                    PlayerStatus player = players[i];
                    bool isPlayer = i == 0;

                    if (pane != null)
                    {
                        var nameContainer = GetChildWithId(pane->GetComponent(), 4);
                        var nameNode = isPlayer ? GetImmediateChildWithId(nameContainer, 5) : GetChildNested(nameContainer, 5, 6);
                        var name = GUINodeUtils.GetNodeText(nameNode);
                        if (name != null)
                            player.name = name;

                        var scoreContainer = GetChildWithId(pane->GetComponent(), isPlayer ? 10 : 11);
                        var pointsComponent = GetChildWithId(scoreContainer, isPlayer ? 12 : 13);
                        var topTextNode = GetChildWithId(pointsComponent->GetComponent(), 2);
                        var scoreText = GUINodeUtils.GetNodeText(topTextNode);
                        if (int.TryParse(scoreText, out int score))
                            player.score = score;

                        var seatContainer = GetChildWithId(pane->GetComponent(), isPlayer ? 7 : 8);
                        var seatTextNode = GetChildWithId(seatContainer, isPlayer ? 9 : 10);
                        var seatText = GUINodeUtils.GetNodeText(seatTextNode);
                        if (Enum.TryParse(seatText, false, out Seat seat))
                            player.seat = seat;
                    }
                }
            }

            return gameState;
        }

        public class UIReaderError : Exception
        {
            public UIReaderError() : base() { }
            public UIReaderError(string message) : base(message) { }
            public UIReaderError(string message, Exception innerException) : base(message, innerException) { }
        }

        public static class Conversions
        {
            public static (Round, int) RoundHand(int texId)
            {
                return texId switch
                {
                    121451 => (Round.East, 1),
                    121452 => (Round.East, 2),
                    121453 => (Round.East, 3),
                    121454 => (Round.East, 4),
                    121455 => (Round.South, 1),
                    121456 => (Round.South, 2),
                    121457 => (Round.South, 3),
                    121458 => (Round.South, 4),
                    _ => throw new UIReaderError($"Unknown texture ID for game/round indicator: {texId}"),
                };
            }
        }
    }
}
