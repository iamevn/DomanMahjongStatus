using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomanMahjongStatus
{
    public static class Mahjong
    {
        public enum GameType { QuickMatch, FullMatch }
        public static Round[] GetRounds(this GameType gameType)
        {
            return gameType switch
            {
                GameType.QuickMatch => new Round[] { Round.East },
                GameType.FullMatch => new Round[] { Round.East, Round.South },
                _ => Array.Empty<Round>(),
            };
        }
        public enum Round { East, South }

        public enum Seat { East, South, West, North }
        public static Seat Next(this Seat seat)
        {
            return seat switch
            {
                Seat.East => Seat.South,
                Seat.South => Seat.West,
                Seat.West => Seat.North,
                Seat.North => Seat.East,
                _ => Seat.East,
            };
        }
        public static Seat Prev(this Seat seat)
        {
            return seat switch
            {
                Seat.East => Seat.North,
                Seat.South => Seat.East,
                Seat.West => Seat.South,
                Seat.North => Seat.West,
                _ => Seat.East,
            };
        }
        public static Seat Next(this Seat seat, int count)
        {
            if (count == 0)
                return seat;
            else if (count < 0)
                return seat.Prev(-count);
            else
                return Next(seat.Next(), count - 1);
        }
        public static Seat Prev(this Seat seat, int count)
        {
            if (count == 0)
                return seat;
            else if (count < 0)
                return seat.Next(-count);
            else
                return Prev(seat.Prev(), count - 1);
        }

        public enum Call { Chi, Pon, Kan, Riichi, Ron, Tsumo }

        public class Tile
        {
            public enum Suit { }
        }
    }
}
