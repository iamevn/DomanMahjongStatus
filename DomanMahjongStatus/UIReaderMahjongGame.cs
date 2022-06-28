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
using Optional;
using Optional.Linq;
using Optional.Unsafe;

namespace DomanMahjongStatus
{
    public class UIReaderMahjongGame
    {
        private readonly IntPtr addonPtr;

        private unsafe AtkUnitBase* BasePtr => (AtkUnitBase*)addonPtr;
        private unsafe AtkResNode* RootNode => BasePtr->RootNode;

        public UIReaderMahjongGame(IntPtr addonPtr)
        {
            this.addonPtr = addonPtr;
        }


        public MahjongStatus ReadMahjongStatus()
        {
            MahjongStatus gameState = MahjongStatus.DummyStatus();
            // round/hand number
            ReadRoundHand().Map(gameState.SetRoundHand);
            // riichi/honba count
            ReadHonbaCount().MatchSome(honbaCount => gameState.HonbaCount = honbaCount);
            ReadRiichiCount().MatchSome(riichiCount => gameState.RiichiCount = riichiCount);
            // player states
            ReadPlayerStatus(0).MatchSome(status => gameState.Player = status);
            ReadPlayerStatus(1).MatchSome(status => gameState.LeftOpponent = status);
            ReadPlayerStatus(2).MatchSome(status => gameState.MiddleOpponent = status);
            ReadPlayerStatus(3).MatchSome(status => gameState.RightOpponent = status);

            return gameState;
        }

        private Option<(Round, int)> ReadRoundHand()
        {
            unsafe
            {
                AtkResNode* imgNode = GetChildNested(RootNode, 16, 19);
                AtkTextureResource* texRes = GetImageTextureResource(imgNode);
                return MaybeDeref(texRes).FlatMap(texRes => Conversions.MaybeRoundHand(texRes.IconID));
            }
        }

        private Option<int> ReadHonbaCount()
        {
            unsafe
            {
                AtkResNode* honbaNode = GetChildNested(RootNode, 21, 23);
                string honbaText = GUINodeUtils.GetNodeText(honbaNode);
                if (int.TryParse(honbaText?.Trim('×')?.Trim(' '), out int honbaCount))
                    return Option.Some(honbaCount);
                else
                    return Option.None<int>();
            }
        }

        private Option<int> ReadRiichiCount()
        {
            unsafe
            {
                AtkResNode* riichiNode = GetChildNested(RootNode, 21, 22);
                string riichiText = GUINodeUtils.GetNodeText(riichiNode);
                if (int.TryParse(riichiText?.Trim('×')?.Trim(' '), out int riichiCount))
                    return Option.Some(riichiCount);
                else
                    return Option.None<int>();
            }
        }

        // which: 0=player 1=left 2=across 3=right
        private Option<PlayerStatus> ReadPlayerStatus(int which) =>
            (which switch
            {
                0 => RelativeSeat.Player.Some(),
                1 => RelativeSeat.Left.Some(),
                2 => RelativeSeat.Across.Some(),
                3 => RelativeSeat.Right.Some(),
                _ => Option.None<RelativeSeat>(),
            }).FlatMap(ReadPlayerStatus);

        private Option<PlayerStatus> ReadPlayerStatus(RelativeSeat which)
        {
            bool isPlayer = which == RelativeSeat.Player;
            unsafe
            {
                Option<UnmanagedPtr<AtkResNode>> paneNode = which.Some().FlatMap(n => n switch
                {
                    RelativeSeat.Player => MaybePtr(GetChildNested(RootNode, 36, 37, 38)),
                    RelativeSeat.Left => MaybePtr(GetChildNested(RootNode, 36, 43, 44)),
                    RelativeSeat.Across => MaybePtr(GetChildNested(RootNode, 36, 41, 42)),
                    RelativeSeat.Right => MaybePtr(GetChildNested(RootNode, 36, 39, 40)),
                    _ => Option.None<UnmanagedPtr<AtkResNode>>(),
                });
                Option<UnmanagedPtr<AtkComponentBase>> pc = paneNode.FlatMap(res => MaybePtr(res.Ptr->GetComponent()));
                Option<string> maybeName = paneNode
                    .FlatMap(node => isPlayer ? node.ChildChain(4, 5) : node.ChildChain(4, 5, 6))
                    .FlatMap(node => node.GetNodeText());

                Option<int> maybeScore = paneNode
                    .FlatMap(node => isPlayer ? node.ChildChain(10, 12, 2) : node.ChildChain(11, 13, 2))
                    .FlatMap(node => node.GetNodeText())
                    .FlatMap(text => text.TryParseInt());

                Option<Seat> maybeSeat = paneNode
                    .FlatMap(node => isPlayer ? node.ChildChain(7, 9) : node.ChildChain(8, 10))
                    .FlatMap(node => node.GetNodeText())
                    .FlatMap(text => text.TryParseEnum<Seat>());

                return from name in maybeName
                       from score in maybeScore
                       from seat in maybeSeat
                       select new PlayerStatus(seat, name, score);
            }
        }

        private Option<GameType> ReadGameType()
        {
            // TODO: actually read game type
            return Option.None<GameType>();
        }

        public class UIReaderError : Exception
        {
            public UIReaderError() : base() { }
            public UIReaderError(string message) : base(message) { }
            public UIReaderError(string message, Exception innerException) : base(message, innerException) { }
        }

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
                _ => throw new UIReaderMahjongGame.UIReaderError($"Unknown texture ID for game/round indicator: {texId}"),
            };
        }
        public static Option<(Round, int)> MaybeRoundHand(int texId)
        {
            return texId switch
            {
                121451 => Option.Some((Round.East, 1)),
                121452 => Option.Some((Round.East, 2)),
                121453 => Option.Some((Round.East, 3)),
                121454 => Option.Some((Round.East, 4)),
                121455 => Option.Some((Round.South, 1)),
                121456 => Option.Some((Round.South, 2)),
                121457 => Option.Some((Round.South, 3)),
                121458 => Option.Some((Round.South, 4)),
                _ => Option.None<(Round, int)>(),
            };
        }
    }
}
