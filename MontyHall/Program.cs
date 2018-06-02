using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MontyHall
{

    public static class RandomGenerator
    {
        static RandomGenerator()
        {
            Random = new Random(DateTime.Now.Millisecond);
        }

        public static Random Random { get; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("MontyHall!!");

            const int iterations = 10000000;
            List<GameResults> results = new List<GameResults>(iterations);
            Stopwatch watch = new Stopwatch();
            Console.WriteLine($"Starting {iterations} iterations");
            watch.Start();
            for (int i = 0; i < iterations; ++i)
            {
                GameResults gameResults = new GameResults();
                Player player = new Player();
                GameHost host = new GameHost();
                Game game = new Game();
                int chosenFirstDoorIndex = player.FirstChoice(game);
                game.SetFirstPlayerChoice(chosenFirstDoorIndex);
                int chosenHostDoorIndex = host.OpenGoatDoor(game);
                game.SetHostChoice(chosenHostDoorIndex);
                PlayerAction chosenAction = player.ChooseAction();

                bool win = game.ExecuteAction(chosenAction);
                gameResults.ChosenAction = chosenAction;
                gameResults.Win = win;
                gameResults.Game = game;

                results.Add(gameResults);
            }
            watch.Stop();

            Console.WriteLine($"{iterations} iterations in {watch.ElapsedMilliseconds}ms");

            long wonChanging = results.Count(x => x.Win && x.ChosenAction == PlayerAction.Change);
            long lostChanging = results.Count(x => !x.Win && x.ChosenAction == PlayerAction.Change);
            long wonStaying = results.Count(x => x.Win && x.ChosenAction == PlayerAction.Stay);
            long lostStaying = results.Count(x => !x.Win && x.ChosenAction == PlayerAction.Stay);
            Console.WriteLine($"{results.Count(x => x.ChosenAction == PlayerAction.Change)} DOOR CHANGES and {results.Count(x => x.ChosenAction == PlayerAction.Stay)} STAYS WITH ORIGINAL DOOR");
            Console.WriteLine($"{wonChanging} won by CHANGING original choice ({wonChanging / (Double)iterations:P} on total, {wonChanging / (Double)(wonChanging + lostChanging):P} on CHANGE)");
            Console.WriteLine($"{lostChanging} lost by CHANGING original choice ({lostChanging / (Double)iterations:P} on total, {lostChanging / (Double)(wonChanging + lostChanging):P} on CHANGE)");
            Console.WriteLine($"{wonStaying} won by STAYING on original choice ({wonStaying / (Double)iterations:P} on total, {wonStaying / (Double)(wonStaying + lostStaying):P} on STAY)");
            Console.WriteLine($"{lostStaying} lost by STAYING on original choice ({lostStaying / (Double)iterations:P} on total, {lostStaying / (Double)(wonStaying + lostStaying):P} on STAY)");
            Console.ReadLine();



        }
    }

    internal class GameResults
    {
        public PlayerAction ChosenAction { get; internal set; }
        public bool Win { get; internal set; }
        public Game Game { get; set; }
    }

    public enum PlayerAction
    {
        Change,
        Stay
    }

    internal class Game
    {
        public List<Door> Doors { get; }
        public Game()
        {
            int doorsNumber = 3;
            Doors = new List<Door>(doorsNumber);
            int indexOfcar = RandomGenerator.Random.Next(0, doorsNumber);
            for (int i = 0; i < doorsNumber; i++)
            {
                Price p = i == indexOfcar ? (Price)new Car() : (Price)new Goat();
                Door d = new Door(p);
                Doors.Add(d);
            }
        }

        public void SetFirstPlayerChoice(int chosenFirstDoor)
        {
            Doors[chosenFirstDoor].FirstChoice = true;
        }

        public void SetHostChoice(int chosenHostDoor)
        {
            Doors[chosenHostDoor].HostChoice = true;
        }

        public bool ExecuteAction(PlayerAction chosenAction)
        {
            Door definitiveDoor = null;
            if (chosenAction == PlayerAction.Stay)
                definitiveDoor = Doors[Doors.FindIndex(d => d.FirstChoice)];
            else
                definitiveDoor = Doors[Doors.FindIndex(d => !d.FirstChoice && !d.HostChoice)];

            definitiveDoor.DefinitiveChoice = true;
            return (definitiveDoor.Price is Car);
        }

        internal void Print()
        {
            StringBuilder sb = new StringBuilder();

            for (var index = 0; index < this.Doors.Count; index++)
            {
                sb.Append($"|  DOOR {index + 1}  |");
            }
            sb.AppendLine();
            for (var index = 0; index < this.Doors.Count; index++)
            {
                var door = this.Doors[index];
                if (door.Price is Goat)
                    sb.Append($"|   Goat   |");
                else
                    sb.Append($"|   CAR!   |");
            }
            sb.AppendLine();
            for (var index = 0; index < this.Doors.Count; index++)
            {
                var door = this.Doors[index];
                if (door.FirstChoice && door.DefinitiveChoice)
                    sb.Append($"|Player F D|");
                else if (door.FirstChoice && !door.DefinitiveChoice)
                    sb.Append($"|Player F  |");
                else if (!door.FirstChoice && door.DefinitiveChoice)
                    sb.Append($"|Player   D|");
                else
                    sb.Append($"|Player    |");
            }
            sb.AppendLine();
            for (var index = 0; index < this.Doors.Count; index++)
            {
                var door = this.Doors[index];
                if (door.HostChoice)
                    sb.Append($"|Host    H |");
                else
                    sb.Append($"|Host      |");
            }
            sb.AppendLine();
            Console.WriteLine(sb.ToString());
        }

        internal class Door
        {
            private bool _hostChoice;
            private bool _definitiveChoice;
            internal Price Price { get; }
            public Door(Price p)
            {
                Price = p;
            }

            internal bool FirstChoice { get; set; }

            internal bool HostChoice
            {
                get { return _hostChoice; }
                set
                {
                    if (value && this.FirstChoice)
                        throw new InvalidOperationException();
                    _hostChoice = value;
                }
            }

            public bool DefinitiveChoice
            {
                get { return _definitiveChoice; }
                internal set
                {
                    if (value && this.HostChoice)
                        throw new InvalidOperationException();
                    _definitiveChoice = value;
                }
            }
        }
    }

    internal class Goat : Price
    {
    }

    internal class Car : Price
    {

    }

    internal class Price
    {
    }



    internal class GameHost
    {

        public GameHost()
        {

        }
        internal int OpenGoatDoor(Game game)
        {
            var remainingDoors = game.Doors.Where(d => !d.FirstChoice && d.Price is Goat).ToList();
            if (remainingDoors.Count() == 1)
            {
                return game.Doors.IndexOf(remainingDoors.ElementAt(0));
            }
            else
            {
                var idx = RandomGenerator.Random.Next(0, remainingDoors.Count());
                return game.Doors.IndexOf(remainingDoors.ElementAt(idx));
            }
        }


    }

    internal class Player
    {

        public Player()
        {

        }
        internal PlayerAction ChooseAction(PlayerAction? forceAction = null)
        {
            if (forceAction.HasValue) return forceAction.Value;
            return (RandomGenerator.Random.Next(0, 2) == 0 ? PlayerAction.Stay : PlayerAction.Change);
        }


        internal int FirstChoice(Game game)
        {
            return RandomGenerator.Random.Next(0, game.Doors.Count);
        }
    }
}
