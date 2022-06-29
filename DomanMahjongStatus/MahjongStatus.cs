using System;
using static DomanMahjongStatus.Mahjong;
namespace DomanMahjongStatus
{
    public class PlayerStatus
    {
        public Seat seat;
        public string name;
        public int score;

        public PlayerStatus(Seat seat, string name, int score = 25000)
        {
            this.seat = seat;
            this.name = name;
            this.score = score;
        }

        public void AdvanceSeat()
        {
            seat = seat.Next();
        }

        public override string ToString()
        {
            if (seat == Seat.East)
                return $"{name}: {score} (dealer)";
            else
                return $"{name}: {score}";
        }
    }
    public class MahjongStatus
    {


        public PlayerStatus Player;
        public PlayerStatus LeftOpponent;
        public PlayerStatus MiddleOpponent;
        public PlayerStatus RightOpponent;
        public Round Round;
        public int hand;
        public int Hand
        {
            get { return hand; }
            set { hand = (value < 1) ? 1 : (value > 4) ? 4 : value; }
        }
        public int HonbaCount;
        public int RiichiCount;

        public MahjongStatus(
            string playerName,
            string leftName, string middleName, string rightName,
            Seat playerSeat)
        {
            this.Player = new PlayerStatus(playerSeat, playerName);
            this.LeftOpponent = new PlayerStatus(playerSeat.Next(), leftName);
            this.MiddleOpponent = new PlayerStatus(playerSeat.Next(2), middleName);
            this.RightOpponent = new PlayerStatus(playerSeat.Next(3), rightName);
            this.Round = Round.East;
            this.HonbaCount = 0;
            this.RiichiCount = 0;
            this.hand = 1;
        }

        public MahjongStatus(
            PlayerStatus player,
            PlayerStatus leftOpponent,
            PlayerStatus middleOpponent,
            PlayerStatus rightOpponent,
            Round round = Round.East,
            int hand = 1,
            int riichiCount = 0,
            int honbaCount = 0)
        {
            this.Player = player;
            this.LeftOpponent = leftOpponent;
            this.MiddleOpponent = middleOpponent;
            this.RightOpponent = rightOpponent;
            this.Round = round;
            this.Hand = hand;
            this.RiichiCount = riichiCount;
            this.HonbaCount = honbaCount;
        }

        public bool AdvanceHand(bool clearPot = false, bool clearHonba = false)
        {
            if (Round == Round.South && hand == 4)
                return false;

            Player.AdvanceSeat();
            LeftOpponent.AdvanceSeat();
            MiddleOpponent.AdvanceSeat();
            RightOpponent.AdvanceSeat();

            if (clearPot)
                RiichiCount = 0;
            if (clearHonba)
                HonbaCount = 0;

            if (hand == 4)
            {
                hand = 1;
                Round = Round.South;
            }
            else
            {
                hand += 1;
            }

            return true;
        }

        public bool SetRoundHand((Round round, int hand) roundHand) => SetRoundHand(roundHand.round, roundHand.hand);

        public bool SetRoundHand(Round round, int hand)
        {
            if (hand < 1 || hand > 4)
                return false;
            Round = round;
            this.hand = hand;
            return true;
        }

        public void AdjustScores(int playerScoreChange, int leftScoreChange, int middleScoreChange, int rightScoreChange)
        {
            Player.score += playerScoreChange;
            LeftOpponent.score += leftScoreChange;
            MiddleOpponent.score += middleScoreChange;
            RightOpponent.score += rightScoreChange;
        }

        public void SetScores(int playerScore, int leftScore, int middleScore, int rightScore)
        {
            Player.score = playerScore;
            LeftOpponent.score = leftScore;
            MiddleOpponent.score = middleScore;
            RightOpponent.score = rightScore;
        }

        public override string ToString()
        {
            string roundHand = $"{Round} {hand}" + (HonbaCount > 0 ? $" (honba {HonbaCount})" : "");
            return $"[{roundHand}] {Player}, {LeftOpponent}, {MiddleOpponent}, {RightOpponent}";
        }
    }
}