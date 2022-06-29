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
using Optional.Collections;
using Optional.Unsafe;

namespace DomanMahjongStatus
{
    public class UIReaderMahjongGame
    {
        private readonly IntPtr addonPtr;

        private unsafe AtkUnitBase* BasePtr => (AtkUnitBase*)addonPtr;
        private unsafe AtkResNode* RootNode => BasePtr->RootNode;
        private unsafe Option<UnmanagedPtr<AtkResNode>> RootPtr => MaybePtr(RootNode);

        public UIReaderMahjongGame(IntPtr addonPtr)
        {
            this.addonPtr = addonPtr;
        }


        public MahjongStatus ReadMahjongStatus()
        {
            (Round round, int hand) = ReadRoundHand().ValueOrFailure("round/hand");
            int honbaCount = ReadHonbaCount().ValueOrFailure("honba");
            int riichiCount = ReadRiichiCount().ValueOrFailure("riichi");
            PlayerStatus playerStatus = ReadPlayerStatus(0).ValueOrFailure("player status");
            PlayerStatus leftStatus = ReadPlayerStatus(1).ValueOrFailure("left opponent status");
            PlayerStatus middleStatus = ReadPlayerStatus(2).ValueOrFailure("middle opponent status");
            PlayerStatus rightStatus = ReadPlayerStatus(3).ValueOrFailure("right opponent status");

            return new MahjongStatus(
                playerStatus, leftStatus, middleStatus, rightStatus,
                round, hand,
                riichiCount, honbaCount);
        }

        #region GameStateReaders
        private Option<(Round, int)> ReadRoundHand()
        {
            unsafe
            {
                return RootPtr.FlatMap(root => root.GetChild(16, 19))
                    .FlatMap(imgNode => imgNode.GetImageTextureResource())
                    .Map(texRes => texRes.Ptr->IconID)
                    .FlatMap(Conversions.MaybeRoundHand);
            }
        }

        private Option<int> ReadHonbaCount()
        {
            unsafe
            {
                return RootPtr.FlatMap(root => root.GetChild(21, 23))
                    .FlatMap(GUINodeExtra.GetNodeText)
                    .Map(honbaText => honbaText.Trim('×').Trim(' '))
                    .FlatMap(GUINodeExtra.TryParseInt);
            }
        }

        private Option<int> ReadRiichiCount()
        {
            unsafe
            {
                return RootPtr.FlatMap(root => root.GetChild(21, 22))
                    .FlatMap(GUINodeExtra.GetNodeText)
                    .Map(riichiText => riichiText.Trim('×').Trim(' '))
                    .FlatMap(GUINodeExtra.TryParseInt);
            }
        }

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
                Option<UnmanagedPtr<AtkResNode>> paneNode = ReadPlayerPaneComponent(which);

                Option<string> maybeName = paneNode
                    .FlatMap(node => isPlayer ? node.GetChild(4, 5) : node.GetChild(4, 5, 6))
                    .FlatMap(node => node.GetNodeText());

                Option<int> maybeScore = paneNode
                    .FlatMap(node => isPlayer ? node.GetChild(10, 12, 2) : node.GetChild(11, 13, 2))
                    .FlatMap(node => node.GetNodeText())
                    .FlatMap(text => text.TryParseInt());

                Option<Seat> maybeSeat = paneNode
                    .FlatMap(node => isPlayer ? node.GetChild(7, 9) : node.GetChild(8, 10))
                    .FlatMap(node => node.GetNodeText())
                    .FlatMap(text => text.TryParseEnum<Seat>());

                return from name in maybeName
                       from score in maybeScore
                       from seat in maybeSeat
                       select new PlayerStatus(seat, name, score);
            }
        }

        private unsafe Option<UnmanagedPtr<AtkResNode>> ReadPlayerPaneComponent(RelativeSeat which)
            => which.Some()
                .FlatMap(_ => _ switch
                    {
                        RelativeSeat.Player => RootPtr.FlatMap(node => node.GetChild(36, 37, 38)),
                        RelativeSeat.Left => RootPtr.FlatMap(node => node.GetChild(36, 43, 44)),
                        RelativeSeat.Across => RootPtr.FlatMap(node => node.GetChild(36, 41, 42)),
                        RelativeSeat.Right => RootPtr.FlatMap(node => node.GetChild(36, 39, 40)),
                        _ => Option.None<UnmanagedPtr<AtkResNode>>(),
                    });
        #endregion GameStateReaders

        public bool IsCurrentPlayer(RelativeSeat which)
            => ReadPlayerPaneComponent(which)
                .FlatMap(node => node.GetChild(which == RelativeSeat.Player ? 14 : 15))
                .Map(node => node.GetChildren())
                .ValueOrDefault()
                .Any(childPtr => childPtr.Deref().IsVisible);

        public Option<Mahjong.RelativeSeat> ReadCurrentPlayer()
            => AllSeats().FirstOrNone(IsCurrentPlayer);

        #region ScoreScreen
        public Option<UnmanagedPtr<AtkResNode>> GetScoreScreenNode(bool onlyIfVisible = false)
                => RootPtr.FlatMap(root => root.GetChild(46, 54))
                .Filter(node =>
                !onlyIfVisible 
                || node.GetChildren().Any(nodePtr => nodePtr.Deref().IsVisible));
        public bool ScoreScreenVisible() => GetScoreScreenNode(true).HasValue;

        // score screen is finished loading when Next button is enabled
        public bool ScoreScreenFinished()
            => GetScoreScreenNode(true)
            .FlatMap(node => node.GetChild(97))
            .FlatMap(node => node.GetAsButtonComponent())
            .Map(btn => btn.Deref().IsEnabled).ValueOr(false);

        public Option<string> GetHanFuText()
            => GetScoreScreenNode(true)
                .FlatMap(node => node.GetChild(95, 2))
                .FlatMap(node => node.GetNodeText());

        public Option<string> GetHandScoreText()
            => GetScoreScreenNode(true)
            .FlatMap(node => node.GetChild(95, 3))
            .Filter(node => node.Deref().IsVisible)
            .FlatMap(node => node.GetNodeText());
        // TODO: also check whether 46->54->95->4 is anything

        public Option<string> GetWinType()
            => GetScoreScreenNode(true)
            .FlatMap(node => node.GetChild(56))
            .FlatMap(node => node.GetNodeText());

        public Option<string> GetWinSeatRound()
            => GetScoreScreenNode(true)
            .FlatMap(node => node.GetChild(57))
            .FlatMap(node => node.GetNodeText());

        public Option<string> GetWinnerName()
            => GetScoreScreenNode(true)
            .FlatMap(node => node.GetChild(58))
            .FlatMap(node => node.GetNodeText());

        public Option<UnmanagedPtr<AtkResNode>> GetWinnerHandNode()
            => GetScoreScreenNode(true)
            .FlatMap(node => node.GetChild(60));
        public Option<UnmanagedPtr<AtkResNode>> GetWinningTileNode()
            => GetScoreScreenNode(true)
            .FlatMap(node => node.GetChild(79));

        public Option<UnmanagedPtr<AtkResNode>> GetDoraIndicatorNode()
            => GetScoreScreenNode(true)
            .FlatMap(node => node.GetChild(80, 83))
            .Filter(node => node.Deref().IsVisible);
        public Option<UnmanagedPtr<AtkResNode>> GetUraDoraIndicatorNode()
            => GetScoreScreenNode(true)
            .FlatMap(node => node.GetChild(80, 84))
            .Filter(node => node.Deref().IsVisible);

        public Option<int> GetRiichiStickCount()
            => GetScoreScreenNode(true)
            .FlatMap(node => node.GetChild(86, 88))
            .FlatMap(node => node.GetNodeText())
            .FlatMap(s => s.TryParseInt());
        public Option<int> GetRiichiStickBonus()
            => GetScoreScreenNode(true)
            .FlatMap(node => node.GetChild(86, 89))
            .Filter(node => node.Deref().IsVisible)
            .FlatMap(node => node.GetChild(2))
            .FlatMap(node => node.GetNodeText())
            .FlatMap(s => s.TrimStart('+').TryParseInt());
        public Option<int> GetRepeatStickCount()
            => GetScoreScreenNode(true)
            .FlatMap(node => node.GetChild(86, 91))
            .FlatMap(node => node.GetNodeText())
            .FlatMap(s => s.TryParseInt());
        public Option<int> GetRepeatStickBonus()
            => GetScoreScreenNode(true)
            .FlatMap(node => node.GetChild(86, 92))
            .Filter(node => node.Deref().IsVisible)
            .FlatMap(node => node.GetChild(2))
            .FlatMap(node => node.GetNodeText())
            .FlatMap(s => s.TrimStart('+').TryParseInt());

        public List<(string, string)> GetWinningYakuList()
            => GetScoreScreenNode(true)
            .FlatMap(node => node.GetChild(93))
            .FlatMap(node => node.GetAsListComponent())
            .Map(list => list.GetChildren().Map(node =>
                {
                    var yaku = node.GetChild(2).FlatMap(textNode => textNode.GetNodeText()).ValueOr("<missing>");
                    var value = node.GetChild(3).FlatMap(textNode => textNode.GetNodeText()).ValueOr("<missing>");

                    return (yaku, value);
                }).ToList()).ValueOr(new List<(string yaku, string value)>());

        #endregion ScoreScreen

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
