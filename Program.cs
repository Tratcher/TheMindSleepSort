using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace TheMindSleepSort
{
    class Program
    {
        private static Random Random = new Random();

        static void Main(string[] args)
        {
            while (PlayGame())
            {

            }
        }

        public static bool PlayGame()
        {
            // Console.WriteLine("Starting game");
            var deck = CreateDeck();
            Shuffle(deck);
            var players = CreatePlayers();
            for (var round = 1; round < 7; round++) // How many rounds are there supposed to be?
            {
                // Console.WriteLine($"Starting around {round}");
                if (!PlayRound(round, deck, players))
                {
                    Console.WriteLine("Game over, you loose.");
                    return false;
                }
            }

            // Console.WriteLine("Done");
            return true;
        }

        private static List<int> CreateDeck()
        {
            var deck = new List<int>(99);
            for (var i = 0; i <= 99; i++)
            {
                deck.Add(i + 1);
            }
            return deck;
        }

        private static void Shuffle(List<int> deck)
        {
            for (var i = 0; i < deck.Count; i++)
            {
                var otherIndex = Random.Next(0, deck.Count);
                var temp = deck[otherIndex];
                deck[otherIndex] = deck[i];
                deck[i] = temp;
            }
        }

        private static List<List<int>> CreatePlayers()
        {
            var players = new List<List<int>>(4);
            for (var i = 0; i < 4; i++)
            {
                players.Add(new List<int>(12));
            }
            return players;
        }

        private static bool PlayRound(int round, List<int> deck, List<List<int>> players)
        {
            Deal(round, deck, players);

            var pile = new List<int>(round * players.Count);
            var playerTasks = new List<Task>(players.Count);

            // Play
            foreach (var player in players)
            {
                var task = StartPlayer(player, pile);
                playerTasks.Add(task);
            }

            Task.WaitAll(playerTasks.ToArray());

            // Verify
            var prior = 0;
            for (var i = 0; i < pile.Count; i++)
            {
                var current = pile[i];
                if (current <= prior)
                {
                    Console.WriteLine("Ordering issue: " + string.Join(',', pile));
                    return false;
                }
                prior = current;
            }

            if (pile.Count != round * players.Count)
            {
                Console.WriteLine($"Threading issue, wrong number of cards played: {pile.Count}/{pile.Capacity}");
                return false;
            }

            return true;
        }

        private static void Deal(int round, List<int> deck, List<List<int>> players)
        {
            foreach (var player in players)
            {
                for (var i = 0; i < round; i++)
                {
                    Debug.Assert(deck.Count > 0, "Empty deck"); // TODO: Do we ever need to reshuffle? (Before round 7 with 4 players)
                    player.Add(deck[deck.Count - 1]);
                    deck.RemoveAt(deck.Count - 1);
                    // Console.WriteLine($"Dealt {player[player.Count - 1]}");
                }
            }
        }

        private static Task StartPlayer(List<int> player, List<int> pile)
        {
            // Locked is stable at 0.0075, fails at 0.005
            // Unlocked is stable at 0.004 and fails at 0.003
            const double scaleFactor = 0.004; // Unlocked list corruption as high as 0.0006
            return Task.Run(() =>
            {
                player.Sort((x, y) => -1 * x.CompareTo(y)); // Decending
                var priorCard = 0;
                for (var i = player.Count - 1; i >= 0; i--)
                {
                    var currentCard = player[i];
                    player.RemoveAt(i);
                    Thread.Sleep(TimeSpan.FromSeconds((currentCard - priorCard) * scaleFactor));
                    // Console.WriteLine($"Playing card {currentCard}");
                    // lock (pile)
                    {
                        pile.Add(currentCard);
                    }
                    priorCard = currentCard;
                }
            });
        }
    }
}
