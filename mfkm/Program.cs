// See https://aka.ms/new-console-template for more information
using System.Collections.ObjectModel;
using System.Data;
using System.Text;
using static mfkm.Card;

namespace mfkm
{
    public static class MenuUtils
    {
        public static int GetPlayerChoiceWithArrowKeys(string prompt, List<string> options, bool canPass = true)
        {
            int selectedIndex = 0;
            bool selectionMade = false;

            ConsoleKeyInfo keyInfo;

            while (!selectionMade)
            {
                Console.Clear();
                if (MenuUtilsHelpers.game != null)
                {
                    //Console.SetCursorPosition(60, 0);
                    string label = MenuUtilsHelpers.curPlayer + 1 == 1 ? "One" : "Two";
                    Game.GamePrint($"Ready Player {label}");
                    Game.GamePrint($"Current Game Points: {MenuUtilsHelpers.game.players[MenuUtilsHelpers.curPlayer].GamePoints:00.00}");
                    Game.GamePrint($"Completed Sets: {MenuUtilsHelpers.game.players[MenuUtilsHelpers.curPlayer].CompleteSets}");

                    Game.SetColor(ConsoleColor.Magenta);
                    Game.GamePrintN($"\nHF: ");
                    Game.ResetColor();
                    Game.GamePrintN($"{MenuUtilsHelpers.game.players[MenuUtilsHelpers.curPlayer].Bleachers.Where(c => c.Value == 5 && c.Color == CardColorEnum.Friend).Count()}");

                    Game.SetColor(ConsoleColor.Magenta);
                    Game.GamePrintN($"    MF: ");
                    Game.ResetColor();
                    Game.GamePrintN($"{MenuUtilsHelpers.game.players[MenuUtilsHelpers.curPlayer].Bleachers.Where(c => c.Value == 3 && c.Color == CardColorEnum.Friend).Count()}");

                    Game.SetColor(ConsoleColor.Magenta);
                    Game.GamePrintN($"    EF: ");
                    Game.ResetColor();
                    Game.GamePrint($"{MenuUtilsHelpers.game.players[MenuUtilsHelpers.curPlayer].Bleachers.Where(c => c.Value == 1 && c.Color == CardColorEnum.Friend).Count()}");

                    Game.SetColor(ConsoleColor.Green);
                    Game.GamePrintN($"HM: ");
                    Game.ResetColor();
                    Game.GamePrintN($"{MenuUtilsHelpers.game.players[MenuUtilsHelpers.curPlayer].Bleachers.Where(c => c.Value == 5 && c.Color == CardColorEnum.Monster).Count()}");

                    Game.SetColor(ConsoleColor.Green);
                    Game.GamePrintN($"    MM: ");
                    Game.ResetColor();
                    Game.GamePrintN($"{MenuUtilsHelpers.game.players[MenuUtilsHelpers.curPlayer].Bleachers.Where(c => c.Value == 3 && c.Color == CardColorEnum.Monster).Count()}");

                    Game.SetColor(ConsoleColor.Green);
                    Game.GamePrintN($"    EM: ");
                    Game.ResetColor();
                    Game.GamePrint($"{MenuUtilsHelpers.game.players[MenuUtilsHelpers.curPlayer].Bleachers.Where(c => c.Value == 1 && c.Color == CardColorEnum.Monster).Count()}");

                    Game.GamePrintN($"\nFriend Points:  ");
                    Game.SetColor(ConsoleColor.Magenta);
                    Game.GamePrint($"{MenuUtilsHelpers.game.players[MenuUtilsHelpers.curPlayer].PinkPoints}");
                    Game.ResetColor();

                    Game.GamePrintN($"Monster Points: ");
                    Game.SetColor(ConsoleColor.Green);
                    Game.GamePrint($"{MenuUtilsHelpers.game.players[MenuUtilsHelpers.curPlayer].GreenPoints}");
                    Game.ResetColor();
                }
                if (!prompt.StartsWith('\n'))
                {
                    Console.WriteLine("");
                }
                Console.WriteLine(prompt);

                for (int i = 0; i < options.Count; i++)
                {
                    string item = options[i];
                    if (i == selectedIndex)
                    {
                        Console.BackgroundColor = ConsoleColor.DarkBlue;
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    else
                    {
                        var color = CardColorEnum.None;
                        if (item.EndsWith(" (f)"))
                        {
                            item = item.Remove(item.Length - 4);
                            color = CardColorEnum.Friend;
                        }
                        else if (item.EndsWith(" (m)"))
                        {
                            item = item.Remove(item.Length - 4);
                            color = CardColorEnum.Monster;
                        }
                        switch (color)
                        {
                            case CardColorEnum.None:
                                Game.ResetColor(); break;
                            case CardColorEnum.Friend:
                                Game.SetColor(ConsoleColor.Magenta); break;
                            case CardColorEnum.Monster:
                                Game.SetColor(ConsoleColor.Green); break;
                        }
                    }

                    Console.WriteLine($"  {item}");

                    Console.ResetColor();
                }

                if (canPass)
                {
                    Console.WriteLine("\nPress 0 to pass.");
                }

                if (MenuUtilsHelpers.game != null)
                {
                    Console.WriteLine($"\n\nDraw Pile: {MenuUtilsHelpers.game.deck.CardsLeft()} cards remaining");
                }
                keyInfo = Console.ReadKey(true);

                switch (keyInfo.Key)
                {
                    case ConsoleKey.UpArrow:
                        selectedIndex = (selectedIndex - 1 + options.Count) % options.Count;
                        break;
                    case ConsoleKey.DownArrow:
                        selectedIndex = (selectedIndex + 1) % options.Count;
                        break;
                    case ConsoleKey.Enter:
                        selectionMade = true;
                        break;
                    case ConsoleKey.D0 when canPass:
                        return -1; // Pass option
                }
            }

            return selectedIndex;
        }
    }

    public class PlayContainer
    {
        public float[] totalscore;
        public float[] minscore;
        public float[] maxscore;
        public int totalturns = 0;
        public int maxturns = 0;
        public int minturns = 100;
        public float maxscorediff = 0.0f;
        public float scorediff = 0.0f;

        public PlayContainer(int numplayers = 2, int numgames = 1, int numhumans = 2, bool chattyPrint = false)
        {
            this.Numhumans = numhumans;
            this.ChattyPrint = chattyPrint;
            this.Numgames = numgames;
            this.Numplayers = numplayers;
            totalscore = new float[numplayers];
            minscore = new float[numplayers];
            maxscore = new float[numplayers];
            for (int k = 0; k < numplayers; k++)
            {
                totalscore[k] = 0;
                minscore[k] = 100;
                maxscore[k] = 0;
            }
        }
        public int Numplayers { get; }
        public int Numgames { get; }
        public int Numhumans { get; }
        public bool ChattyPrint { get; }

        internal void PlayGames()
        {
            for (int i = 0; i < Numgames; i++)
            {
                Game g = new(chattyPrint: ChattyPrint, numhumans: Numhumans, numplayers: Numplayers, useRoles: false);
                MenuUtilsHelpers.game = g;
                int turns = 0;
                do { Game.GamePrint($"\n### Round {++turns}"); } while (g.PlayRound());
                List<string> scores = [];
                for (int j = 0; j < Numplayers; j++)
                {
                    scores.Add($"P{j + 1}: {g.players[j].GamePoints}");
                }
                Game.ResetColor();
                Game.GamePrint($"Game Over after {turns} turns! {string.Join(", ", scores)}", false);
                if (GameHelpers.printVerbose)
                {
                    for (int j = 0; j < Numplayers; j++)
                    {
                        Console.WriteLine($"P{j + 1}:");
                        Console.WriteLine($"{g.players[j].Stats}");
                    }
                }
                if (turns > maxturns) { maxturns = turns; }
                if (turns < minturns) { minturns = turns; }
                for (int j = 0; j < Numplayers; j++)
                {
                    if (g.players[j].GamePoints > maxscore[j]) { maxscore[j] = g.players[j].GamePoints; }
                    if (g.players[j].GamePoints < minscore[j]) { minscore[j] = g.players[j].GamePoints; }
                    totalscore[j] += g.players[j].GamePoints;
                }
                totalturns += turns;
                float sd = Math.Abs(g.players[0].GamePoints - g.players[1].GamePoints);
                scorediff += sd;
                if (maxscorediff < sd) maxscorediff = sd;
            }
        }

        internal void PrintSummary()
        {
            Console.Write($"Summary: Avg turns {totalturns / Numgames}. ");
            Console.Write($"Summary: Avg scorediff {scorediff / Numgames}. ");
            for (int i = 0; i < Numplayers; i++)
            {
                Console.Write($"Avg p{i + 1} score {totalscore[i] / Numgames} ");
            }
            Console.WriteLine("");
            Console.Write($"Summary: Max turns {maxturns}. ");
            for (int i = 0; i < Numplayers; i++)
            {
                Console.Write($"Max p{i + 1} score {maxscore[i]} ");
            }
            Console.WriteLine("");
            Console.Write($"Summary: Min turns {minturns}. ");
            for (int i = 0; i < Numplayers; i++)
            {
                Console.Write($"Min p{i + 1} score {minscore[i]} ");
            }
            Console.WriteLine("");

            Console.WriteLine("Press 'Q' to quit...");
            ConsoleKeyInfo keyInfo;
            do
            {
                keyInfo = Console.ReadKey(true);
            } while (keyInfo.Key != ConsoleKey.Q);
        }
    }
    public class Card(Card.CardColorEnum color, int value, Card.CardTypeEnum cardType, Card.CardActionEnum cardAction, string description) : IComparable<Card>
    {
        public enum CardColorEnum { None, Friend, Monster }
        public enum CardTypeEnum { Point, Action, FriendMonster }
        public enum CardActionEnum { NoAction, PickFromDiscard, TakeTwoActions, DrawOneCard, StealEasyFriendMonster, StealCard, TradeCard, BuyWithBank, BuyWithDiscount }
        public int CompareTo(Card? other)
        {
            if (other == null) return 1;
            if (this.CardType == CardTypeEnum.FriendMonster && other.CardType != CardTypeEnum.FriendMonster)
            {
                return -1;
            }

            if (this.CardType != CardTypeEnum.FriendMonster && other.CardType == CardTypeEnum.FriendMonster)
            {
                return 1;
            }

            if (this.Color == CardColorEnum.Friend && other.Color != CardColorEnum.Friend)
            {
                return -1;
            }

            if (this.Color != CardColorEnum.Friend && other.Color == CardColorEnum.Friend)
            {
                return 1;
            }

            // Friend costs (high to low)
            if (this.CardType == CardTypeEnum.FriendMonster && this.Color == CardColorEnum.Friend &&
                other.CardType == CardTypeEnum.FriendMonster && other.Color == CardColorEnum.Friend)
            {
                return other.Value.CompareTo(this.Value); // High to low
            }

            // Monster costs (high to low)
            if (this.CardType == CardTypeEnum.FriendMonster && this.Color == CardColorEnum.Monster &&
                other.CardType == CardTypeEnum.FriendMonster && other.Color == CardColorEnum.Monster)
            {
                return other.Value.CompareTo(this.Value); // High to low
            }

            // Friend points (high to low)
            if (this.CardType == CardTypeEnum.Point && this.Color == CardColorEnum.Friend &&
                other.CardType == CardTypeEnum.Point && other.Color == CardColorEnum.Friend)
            {
                return other.Value.CompareTo(this.Value); // High to low
            }

            // Monster points (high to low)
            if (this.CardType == CardTypeEnum.Point && this.Color == CardColorEnum.Monster &&
                other.CardType == CardTypeEnum.Point && other.Color == CardColorEnum.Monster)
            {
                return other.Value.CompareTo(this.Value); // High to low
            }

            // Action cards (no specific order)
            if (this.CardType == CardTypeEnum.Action && other.CardType == CardTypeEnum.Action)
            {
                return 0; // Or any other logic to order action cards if needed
            }

            // Default order (you might want to refine this)
            return this.CardType.CompareTo(other.CardType);
        }
        public CardColorEnum Color { get; set; } = color;
        public int Value { get; set; } = value;
        public CardTypeEnum CardType { get; set; } = cardType;
        public CardActionEnum CardAction { get; set; } = cardAction;
        public string Description { get; set; } = description;
        public override string ToString()
        {
            string colorString = Color == CardColorEnum.None ? "" : $"{Color} ";
            string valueString = Value > 0 ? $"{Value} " : "";
            //string cardTypeString = CardType == CardTypeEnum.FriendMonster ? (Color == CardColorEnum.Friend ? "Friend" : "Monster") : $"{CardType}";
            string cardTypeString = $"{CardType}";
            switch (CardType)
            {
                case CardTypeEnum.Point:
                    string p = "pt" + ((Value > 1) ? "s" : "");
                    return $"{colorString}{p}: {valueString}- {Description}";
                case CardTypeEnum.Action:
                    return $"{cardTypeString}: {Description}";
                case CardTypeEnum.FriendMonster:
                    string costDescribe = "";
                    switch (Value)
                    {
                        case 1:
                            costDescribe = "Easy";
                            break;
                        case 3:
                            costDescribe = "Medium";
                            break;
                        case 5:
                            costDescribe = "Hard";
                            break;
                    }
                    return $"{Description} - {costDescribe} {colorString}- cost: {valueString}";
            }
            return $"{Description} - {colorString}{valueString}{cardTypeString}";
        }
        public bool HandleCard(Player player)
        {
            return CardType switch
            {
                CardTypeEnum.Point => HandlePointCard(player),
                CardTypeEnum.Action => HandleActionCard(player),
                CardTypeEnum.FriendMonster => HandleFriendMonsterCard(player),
                _ => false,
            };
        }
        private bool HandlePointCard(Player player)
        {
            int choice;
            if (CardAction != CardActionEnum.NoAction && CardAction != CardActionEnum.DrawOneCard) // disabled when actions not impl
            {
                List<string> choices = [];
                string prompt = $"Playing {this} as:";
                choices.Add("Points");
                choices.Add("Action");
                choice = player.GetPlayerChoice(prompt, choices, false); // 0 or 1 because of 0 based
            }
            else
            {
                choice = 0;
            }
            if (choice == 0) // Bank as points
            {
                player.BankedCards.Add(this);
                player.PlayCard(this);
                Game.GamePrint($"[{this}] banked.");
                player.Stats.cardsBanked++;
            }
            if (choice == 1 || this.CardAction == CardActionEnum.DrawOneCard) // Use as action
            {
                return HandleActionCard(player);
            }
            return true;
        }

        private bool HandleActionCard(Player player)
        {
            player.Deck.DiscardCard(this);
            player.PlayCard(this);
            player.Stats.actionsPlayed++;
            switch (CardAction)
            {
                case CardActionEnum.DrawOneCard:
                    if (player.Deck.HasDrawCards())
                    {
                        Card? bonusCard = player.Deck.DrawCard();
                        Game.GamePrint($"Drawing one card... [{bonusCard}]");
                        player.Stats.cardsDrawn++;
                        player.HandAdd(bonusCard); // Assuming player has a reference to the Game object
                    }
                    break;
                case CardActionEnum.TakeTwoActions:
                    {
                        Game.GamePrint("First bonus action:");
                        player.PlayACard();
                        Game.GamePrint("Second bonus action:");
                        player.PlayACard();
                    }
                    break;
                case CardActionEnum.PickFromDiscard:
                    int cardIndex = player.PrintCardsAndCollectSelection("Discard Pile: ", player.Deck.discardPile.AsReadOnly());
                    if (cardIndex == -1)
                    {
                        player.Stats.passes++;
                        Game.GamePrint("Passed on Discard");
                    }
                    else
                    {
                        player.HandAdd(player.Deck.discardPile[cardIndex]);
                        player.Deck.discardPile.RemoveAt(cardIndex);
                        Game.GamePrint("Picked");
                        player.Stats.discardsDrawn++;
                    }
                    break;
                case CardActionEnum.StealEasyFriendMonster:
                    {
                        List<string> choices = player.opponents.Select(x => x.Name).ToList();
                        int opp = player.GetPlayerChoice("Pick an opponent: ", choices, false);

                        var stealableEasy = player.opponents[opp].Bleachers.Where(c => c.Value == 1 && c.Color == Color).ToList();
                        int easy = player.PrintCardsAndCollectSelection($"{player.opponents[opp].Name}'s hand:", stealableEasy);
                        if (easy != -1)
                        {
                            Card stolen = stealableEasy[easy];
                            player.Bleachers.Add(stolen);
                            player.opponents[opp].Bleachers.Remove(stolen);
                            Game.GamePrint($"Stolen [{stolen}] from opponent {player.opponents[opp].Name}");
                            player.Stats.steals++;
                        }
                        else
                        {
                            player.Deck.discardPile.Remove(this);
                            player.HandAdd(this);
                            Game.GamePrint("Passed on stealing, card put back in hand");
                            return false;
                        }
                    }
                    break;
                // ... (handle other point card actions) ...
                case CardActionEnum.StealCard:
                    {
                        List<string> choices = player.opponents.Select(x => x.Name).ToList();
                        int opp2 = player.GetPlayerChoice("Pick an opponent: ", choices, false);

                        if (player.opponents[opp2].Hand.Count > 0)
                        {
                            // TODO: make this more vague
                            int crd = player.PrintCardsAndCollectSelection("Opponent's Hand:", player.opponents[opp2].Hand);
                            if (crd == -1)
                            {
                                Game.GamePrint("No card selected.");
                                player.HandAdd(this);
                                player.Deck.discardPile.Remove(this);
                                return false;
                            }
                            Card stolenCard = player.opponents[opp2].Hand[crd];
                            player.HandAdd(stolenCard);
                            player.opponents[opp2].HandRemove(stolenCard);
                            Game.GamePrint($"Stole {stolenCard} from {player.opponents[opp2].Name}");
                            player.Stats.steals++;
                        }
                    }
                    break;
                case CardActionEnum.TradeCard:
                    {
                        List<string> choices = player.opponents.Select(x => x.Name).ToList();
                        int opp2 = player.GetPlayerChoice("Pick an opponent: ", choices, false);
                        if (player.opponents[opp2].Hand.Count > 0 && player.Hand.Count > 0)
                        {
                            int crd = player.PrintCardsAndCollectSelection("Card to take:", player.opponents[opp2].Hand);
                            if (crd == -1)
                            {
                                Game.GamePrint("No card selected.");
                                player.HandAdd(this);
                                player.Deck.discardPile.Remove(this);
                                return false;
                            }
                            Card stolenCard = player.opponents[opp2].Hand[crd];
                            int tcrd = player.PrintCardsAndCollectSelection("Card to give:", player.Hand);
                            if (tcrd == -1)
                            {
                                Game.GamePrint("No card selected.");
                                player.HandAdd(this);
                                player.Deck.discardPile.Remove(this);
                                return false;
                            }
                            Card tradeCard = player.Hand[tcrd];
                            player.HandAdd(stolenCard);
                            player.HandRemove(tradeCard);
                            player.opponents[opp2].HandRemove(stolenCard);
                            player.opponents[opp2].HandAdd(tradeCard);
                            Game.GamePrint($"Traded {stolenCard} from {player.opponents[opp2].Name} for {tradeCard}");
                            player.Stats.trades++;
                        }
                    }
                    break;
                case CardActionEnum.BuyWithDiscount:
                    {
                        var buyable = player.Hand.Where(c => c.CardType == CardTypeEnum.FriendMonster).ToList();
                        int buyaction = player.PrintCardsAndCollectSelection("Card to buy:", buyable);
                        if (buyaction != -1)
                        {
                            Card toBuy = buyable[buyaction];
                            int ogValue = toBuy.Value;
                            toBuy.Value = Math.Max(0, ogValue - 2);
                            toBuy.HandleFriendMonsterCard(player);
                            toBuy.Value = ogValue;
                        }
                        else
                        {
                            player.Deck.discardPile.Remove(this);
                            player.HandAdd(this);
                            return false;
                        }
                    }
                    break;
            }
            return true;
        }

        private bool HandleFriendMonsterCard(Player player)
        {
            // Check if the player has enough banked points
            bool canUseDiscount = player.BankedPoints(Color) >= Value - player.Role.discount;
            int requiredPoints = Math.Max(0, Value - (canUseDiscount ? player.Role.discount : 0));

            if (player.UsablePoints(Color) >= requiredPoints)
            {
                // "Eat" the banked cards (remove them from BankedCards) 
                // ... (You'll need to implement logic to remove the correct amount of banked cards) ...
                // Find the optimal combination of banked cards to minimize waste
                var usableCards = player.BankedCards.Where(c => c.Color == Color).ToList();
                if (!canUseDiscount && player.Role.canBuyWithHandPoints)
                {
                    usableCards.AddRange(player.Hand.Where(c => c.CardType == CardTypeEnum.Point && Color == c.Color));
                }
                List<Card> optimalCards = FindOptimalCards(usableCards, requiredPoints);

                // Remove the optimal cards from the player's banked cards
                int spentCount = 0;
                foreach (Card cardToRemove in optimalCards)
                {
                    Game.GamePrint($"Spent {cardToRemove}");
                    if (player.BankedCards.Remove(cardToRemove))
                    {
                        player.Stats.spent++;
                    }
                    else
                    {
                        // if not found in the bank, it must have been a hand card
                        player.HandRemove(cardToRemove);
                        player.Stats.spentFromHand++;
                    }
                    spentCount += cardToRemove.Value;
                }
                //var sortedBankedCards = player.BankedCards.Where(c => c.Color == color).OrderByDescending(c => c.Value).ToList();

                // Add the Friend/Monster card to the FriendsMonsters list
                player.Bleachers.Add(this);
                player.PlayCard(this);
                Game.GamePrint($"({this}) purchased for {spentCount}.");
                player.Stats.bought++;
                return true;
            }
            else
            {
                Game.GamePrint($"Not enough banked points to purchase {this}.");
                return false;
                // You might want to handle this differently (e.g., give the player another turn)
            }
        }
        static private List<Card> FindOptimalCards(List<Card> bankedCards, int targetPoints)
        {
            // Sort banked cards in ascending order of value (important for DP)
            bankedCards = [.. bankedCards.OrderBy(c => c.Value)];
            List<Card> selectedCards = [];
            var exactIndex = bankedCards.FindIndex(c => c.Value == targetPoints);
            if (exactIndex != -1)
            {
                selectedCards.Add(bankedCards[exactIndex]);
                return selectedCards;
            }

            int total = 0;
            for (int i = 0; i < bankedCards.Count; i++)
            {
                Card card = bankedCards[i];
                total += card.Value;
                selectedCards.Add(card);
                if (card.Value >= targetPoints)
                {
                    // special case in case the card we just reached could have paid for all of it
                    selectedCards = [ card ];
                    return selectedCards;
                }
                else if (total >= targetPoints)
                {
                    return selectedCards;
                }
            }
            // if we got here, things have gone wrong.
            Game.GamePrint("ERROR! Not enough cards");
            return selectedCards;
        }
    }

    public class Deck
    {
        protected List<Card> drawPile;
        internal List<Card> discardPile;

        public Deck()
        {
            drawPile = [];
            discardPile = [];

            // Add point cards
            drawPile.AddRange([
    // Static constructors for point cards
    new Card(CardColorEnum.Monster, 1, CardTypeEnum.Point, CardActionEnum.NoAction, "No Action"),
    new Card(CardColorEnum.Monster, 2, CardTypeEnum.Point, CardActionEnum.DrawOneCard, "(plus draw again)"),
    new Card(CardColorEnum.Monster, 3, CardTypeEnum.Point, CardActionEnum.TakeTwoActions, "or take two actions"),
    new Card(CardColorEnum.Monster, 3, CardTypeEnum.Point, CardActionEnum.PickFromDiscard, "or pick from discard"),
    new Card(CardColorEnum.Monster, 5, CardTypeEnum.Point, CardActionEnum.StealEasyFriendMonster, "or Steal an easy monster"),
    new Card(CardColorEnum.Friend, 1, CardTypeEnum.Point, CardActionEnum.NoAction, "No Action"),
    new Card(CardColorEnum.Friend, 2, CardTypeEnum.Point, CardActionEnum.DrawOneCard, "(plus draw again)"),
    new Card(CardColorEnum.Friend, 3, CardTypeEnum.Point, CardActionEnum.PickFromDiscard, "or pick from discard"),
    new Card(CardColorEnum.Friend, 3, CardTypeEnum.Point, CardActionEnum.TakeTwoActions, "or take two actions"),
    new Card(CardColorEnum.Friend, 5, CardTypeEnum.Point, CardActionEnum.StealEasyFriendMonster, "or Steal an easy friend"),

    new Card(CardColorEnum.Monster, 1, CardTypeEnum.Point, CardActionEnum.NoAction, "No Action"),
    new Card(CardColorEnum.Monster, 2, CardTypeEnum.Point, CardActionEnum.DrawOneCard, "(plus draw again)"),
    new Card(CardColorEnum.Monster, 3, CardTypeEnum.Point, CardActionEnum.TakeTwoActions, "or take two actions"),
    new Card(CardColorEnum.Monster, 3, CardTypeEnum.Point, CardActionEnum.PickFromDiscard, "or pick from discard"),
    new Card(CardColorEnum.Monster, 5, CardTypeEnum.Point, CardActionEnum.StealEasyFriendMonster, "or Steal an easy monster"),
    new Card(CardColorEnum.Friend, 1, CardTypeEnum.Point, CardActionEnum.NoAction, "No Action"),
    new Card(CardColorEnum.Friend, 2, CardTypeEnum.Point, CardActionEnum.DrawOneCard, "(plus draw again)"),
    new Card(CardColorEnum.Friend, 3, CardTypeEnum.Point, CardActionEnum.PickFromDiscard, "or pick from discard"),
    new Card(CardColorEnum.Friend, 3, CardTypeEnum.Point, CardActionEnum.TakeTwoActions, "or take two actions"),
    new Card(CardColorEnum.Friend, 5, CardTypeEnum.Point, CardActionEnum.StealEasyFriendMonster, "or Steal an easy friend"),

    new Card(CardColorEnum.Monster, 1, CardTypeEnum.Point, CardActionEnum.NoAction, "No Action"),
    new Card(CardColorEnum.Monster, 2, CardTypeEnum.Point, CardActionEnum.DrawOneCard, "(plus draw again)"),
    new Card(CardColorEnum.Monster, 3, CardTypeEnum.Point, CardActionEnum.TakeTwoActions, "or take two actions"),
    new Card(CardColorEnum.Monster, 3, CardTypeEnum.Point, CardActionEnum.PickFromDiscard, "or pick from discard"),
    new Card(CardColorEnum.Monster, 5, CardTypeEnum.Point, CardActionEnum.StealEasyFriendMonster, "or Steal an easy monster"),
    new Card(CardColorEnum.Friend, 1, CardTypeEnum.Point, CardActionEnum.NoAction, "No Action"),
    new Card(CardColorEnum.Friend, 2, CardTypeEnum.Point, CardActionEnum.DrawOneCard, "(plus draw again)"),
    new Card(CardColorEnum.Friend, 3, CardTypeEnum.Point, CardActionEnum.PickFromDiscard, "or pick from discard"),
    new Card(CardColorEnum.Friend, 3, CardTypeEnum.Point, CardActionEnum.TakeTwoActions, "or take two actions"),
    new Card(CardColorEnum.Friend, 5, CardTypeEnum.Point, CardActionEnum.StealEasyFriendMonster, "or Steal an easy friend"),

    new Card(CardColorEnum.Monster, 1, CardTypeEnum.Point, CardActionEnum.NoAction, "No Action"),
    new Card(CardColorEnum.Monster, 2, CardTypeEnum.Point, CardActionEnum.DrawOneCard, "(plus draw again)"),
    new Card(CardColorEnum.Monster, 3, CardTypeEnum.Point, CardActionEnum.TakeTwoActions, "or take two actions"),
    new Card(CardColorEnum.Monster, 3, CardTypeEnum.Point, CardActionEnum.PickFromDiscard, "or pick from discard"),
    new Card(CardColorEnum.Monster, 5, CardTypeEnum.Point, CardActionEnum.StealEasyFriendMonster, "or Steal an easy monster"),
    new Card(CardColorEnum.Friend, 1, CardTypeEnum.Point, CardActionEnum.NoAction, "No Action"),
    new Card(CardColorEnum.Friend, 2, CardTypeEnum.Point, CardActionEnum.DrawOneCard, "(plus draw again)"),
    new Card(CardColorEnum.Friend, 3, CardTypeEnum.Point, CardActionEnum.PickFromDiscard, "or pick from discard"),
    new Card(CardColorEnum.Friend, 3, CardTypeEnum.Point, CardActionEnum.TakeTwoActions, "or take two actions"),
    new Card(CardColorEnum.Friend, 5, CardTypeEnum.Point, CardActionEnum.StealEasyFriendMonster, "or Steal an easy friend"),

    /*
    new Card(CardColorEnum.Monster, 1, CardTypeEnum.Point, CardActionEnum.NoAction, "No Action"),
    new Card(CardColorEnum.Monster, 2, CardTypeEnum.Point, CardActionEnum.DrawOneCard, "(plus draw again)"),
    new Card(CardColorEnum.Monster, 3, CardTypeEnum.Point, CardActionEnum.TakeTwoActions, "or take two actions"),
    new Card(CardColorEnum.Monster, 3, CardTypeEnum.Point, CardActionEnum.PickFromDiscard, "or pick from discard"),
    new Card(CardColorEnum.Monster, 5, CardTypeEnum.Point, CardActionEnum.StealEasyFriendMonster, "or Steal an easy monster"),
    new Card(CardColorEnum.Friend, 1, CardTypeEnum.Point, CardActionEnum.NoAction, "No Action"),
    new Card(CardColorEnum.Friend, 2, CardTypeEnum.Point, CardActionEnum.DrawOneCard, "(plus draw again)"),
    new Card(CardColorEnum.Friend, 3, CardTypeEnum.Point, CardActionEnum.PickFromDiscard, "or pick from discard"),
    new Card(CardColorEnum.Friend, 3, CardTypeEnum.Point, CardActionEnum.TakeTwoActions, "or take two actions"),
    new Card(CardColorEnum.Friend, 5, CardTypeEnum.Point, CardActionEnum.StealEasyFriendMonster, "or Steal an easy friend"),
                                                                                                                                                                                                                                                                                                                                                                                                                                                                              new Card(CardColorEnum.Monster, 1, CardTypeEnum.Point, CardActionEnum.NoAction, "No Action"),
    new Card(CardColorEnum.Monster, 2, CardTypeEnum.Point, CardActionEnum.DrawOneCard, "(plus draw again)"),
    new Card(CardColorEnum.Monster, 3, CardTypeEnum.Point, CardActionEnum.TakeTwoActions, "or take two actions"),
    new Card(CardColorEnum.Monster, 3, CardTypeEnum.Point, CardActionEnum.PickFromDiscard, "or pick from discard"),
    new Card(CardColorEnum.Monster, 5, CardTypeEnum.Point, CardActionEnum.StealEasyFriendMonster, "or Steal an easy monster"),
    new Card(CardColorEnum.Friend, 1, CardTypeEnum.Point, CardActionEnum.NoAction, "No Action"),
    new Card(CardColorEnum.Friend, 2, CardTypeEnum.Point, CardActionEnum.DrawOneCard, "(plus draw again)"),
    new Card(CardColorEnum.Friend, 3, CardTypeEnum.Point, CardActionEnum.PickFromDiscard, "or pick from discard"),
    new Card(CardColorEnum.Friend, 3, CardTypeEnum.Point, CardActionEnum.TakeTwoActions, "or take two actions"),
    new Card(CardColorEnum.Friend, 5, CardTypeEnum.Point, CardActionEnum.StealEasyFriendMonster, "or Steal an easy friend"),
*/
    // Static constructors for action cards
    
    new Card(CardColorEnum.None, 0, CardTypeEnum.Action, CardActionEnum.BuyWithDiscount, "Buy Action"),
    new Card(CardColorEnum.None, 0, CardTypeEnum.Action, CardActionEnum.BuyWithDiscount, "Buy Action"),
    new Card(CardColorEnum.None, 0, CardTypeEnum.Action, CardActionEnum.BuyWithDiscount, "Buy Action"),
    new Card(CardColorEnum.None, 0, CardTypeEnum.Action, CardActionEnum.BuyWithDiscount, "Buy Action"),
    new Card(CardColorEnum.None, 0, CardTypeEnum.Action, CardActionEnum.BuyWithDiscount, "Buy Action"),

    new Card(CardColorEnum.None, 0, CardTypeEnum.Action, CardActionEnum.StealCard, "Forced Steal"),
    new Card(CardColorEnum.None, 0, CardTypeEnum.Action, CardActionEnum.TradeCard, "Forced Trade"),
    new Card(CardColorEnum.None, 0, CardTypeEnum.Action, CardActionEnum.StealCard, "Forced Steal"),
    new Card(CardColorEnum.None, 0, CardTypeEnum.Action, CardActionEnum.TradeCard, "Forced Trade"),

    // ... (You'll need to add static constructors for your Friend/Monster cards here) ...
    // ... (previous code) ...

    // ... (previous code) ...

    // Static constructors for Friend/Monster cards
    new Card(CardColorEnum.Friend, 1, CardTypeEnum.FriendMonster, CardActionEnum.BuyWithBank, "Howman"),
    new Card(CardColorEnum.Friend, 3, CardTypeEnum.FriendMonster, CardActionEnum.BuyWithBank, "Kevin"),
    new Card(CardColorEnum.Friend, 5, CardTypeEnum.FriendMonster, CardActionEnum.BuyWithBank, "Kevin's Mom"),
    new Card(CardColorEnum.Monster, 1, CardTypeEnum.FriendMonster, CardActionEnum.BuyWithBank, "Gelatinous Cube"),
    new Card(CardColorEnum.Monster, 3, CardTypeEnum.FriendMonster, CardActionEnum.BuyWithBank, "Sasquatch"),
    new Card(CardColorEnum.Monster, 5, CardTypeEnum.FriendMonster, CardActionEnum.BuyWithBank, "Green Dragon"),

    new Card(CardColorEnum.Friend, 1, CardTypeEnum.FriendMonster, CardActionEnum.BuyWithBank, "Grace"),
    new Card(CardColorEnum.Friend, 3, CardTypeEnum.FriendMonster, CardActionEnum.BuyWithBank, "Roshni"),
    new Card(CardColorEnum.Friend, 5, CardTypeEnum.FriendMonster, CardActionEnum.BuyWithBank, "Kate"),
    new Card(CardColorEnum.Monster, 1, CardTypeEnum.FriendMonster, CardActionEnum.BuyWithBank, "Mimic"),
    new Card(CardColorEnum.Monster, 3, CardTypeEnum.FriendMonster, CardActionEnum.BuyWithBank, "Beholder"),
    new Card(CardColorEnum.Monster, 5, CardTypeEnum.FriendMonster, CardActionEnum.BuyWithBank, "Black Dragon"),

    new Card(CardColorEnum.Friend, 1, CardTypeEnum.FriendMonster, CardActionEnum.BuyWithBank, "Anthony"),
    new Card(CardColorEnum.Friend, 3, CardTypeEnum.FriendMonster, CardActionEnum.BuyWithBank, "Muhan"),
    new Card(CardColorEnum.Friend, 5, CardTypeEnum.FriendMonster, CardActionEnum.BuyWithBank, "Dez"),
    new Card(CardColorEnum.Monster, 1, CardTypeEnum.FriendMonster, CardActionEnum.BuyWithBank, "Kobold"),
    new Card(CardColorEnum.Monster, 3, CardTypeEnum.FriendMonster, CardActionEnum.BuyWithBank, "Mind Flayer"),
    new Card(CardColorEnum.Monster, 5, CardTypeEnum.FriendMonster, CardActionEnum.BuyWithBank, "Blue Dragon"),

    // ... add more Friend/Monster cards as needed ...
]);

            Shuffle();
        }

        public void Shuffle()
        {
            Random rng = new();
            int n = drawPile.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (drawPile[n], drawPile[k]) = (drawPile[k], drawPile[n]);
            }
        }

        public void DiscardCard(Card card)
        {
            discardPile.Add(card);
        }

        public Card DrawCard()
        {
            System.Diagnostics.Debug.Assert(drawPile.Count > 0, "Deck can't be empty");
            Card drawnCard = drawPile[0];
            drawPile.RemoveAt(0);
            return drawnCard;
        }

        public bool HasDrawCards()
        {
            return drawPile.Count != 0;
        }

        public int CardsLeft()
        {
            return drawPile.Count;
        }

        public bool HasDiscardCards()
        {
            return discardPile.Count > 0;
        }

        internal Card DrawCardFromDiscard()
        {
            System.Diagnostics.Debug.Assert(drawPile.Count > 0, "Draw pile can't be empty");
            int topCard = discardPile.Count - 1;
            Card drawnCard = discardPile[topCard];
            discardPile.RemoveAt(topCard);
            return drawnCard;
        }

        // ... (other methods for discarding, etc.) ...
    }
    public class Role(string name, int discount, float leftovermultiplier, bool canBuyWithHandPoints, bool canDrawFromDiscard, bool canBankInsteadOfDraw)
    {
        public int discount = discount;
        public bool canBankInsteadOfDraw = canBankInsteadOfDraw;
        public bool canBuyWithHandPoints = canBuyWithHandPoints;
        public float leftovermultiplier = leftovermultiplier;
        public string name = name;
        public bool canDrawFromDiscard = canDrawFromDiscard;

        public override string ToString()
        {
            return name;
        }
    }
    public static class MenuUtilsHelpers
    {
        public static Game? game;
        public static int curPlayer;
    }

    internal static class RoleHelpers
    {
        public static Role none = new("none", discount: 0, 0.25f, canBuyWithHandPoints: false, canDrawFromDiscard: false, canBankInsteadOfDraw: false);
        public static Role gravedigger = new("gravedigger", discount: 0, 1.0f, canBuyWithHandPoints: false, canDrawFromDiscard: false, canBankInsteadOfDraw: false);
        public static Role artificer = new("artificer", discount: 1, 0.25f, canBuyWithHandPoints: true, canDrawFromDiscard: false, canBankInsteadOfDraw: false);
        public static Role fateweaver = new("fateweaver", discount: 0, 0.25f, canBuyWithHandPoints: false, canDrawFromDiscard: true, canBankInsteadOfDraw: true);
    }
    public class Player(string name, Role role, Deck deck, bool autoPilot)
    {
        public string Name { get; set; } = name;
        public readonly List<Card> hand = [];
        public ReadOnlyCollection<Card> Hand => hand.AsReadOnly();
        public List<Card> BankedCards { get; set; } = [];
        public List<Card> Bleachers { get; set; } = [];
        public Role Role { get; set; } = role;
        public Deck Deck { get; set; } = deck;

        public int GreenPoints
        {
            get
            {
                return BankedCards.Where(c => c.Color == Card.CardColorEnum.Monster).Sum(c => c.Value);
            }
        }
        public int PinkPoints
        {
            get
            {
                return BankedCards.Where(c => c.Color == Card.CardColorEnum.Friend).Sum(c => c.Value);
            }
        }
        public float GamePoints
        {
            get
            {
                float sum = Bleachers.Sum(c => c.Value);
                sum += this.CompleteSets * 3;
                int bonusGreenOnes = BankedCards.Where(c => c.Value == 1 && c.Color == CardColorEnum.Monster).Count() +
                    Hand.Where(c => c.Value == 1 && c.Color == CardColorEnum.Monster).Count();
                int bonusPinkOnes = BankedCards.Where(c => c.Value == 1 && c.Color == CardColorEnum.Friend).Count() +
                    Hand.Where(c => c.Value == 1 && c.Color == CardColorEnum.Friend).Count();
                float cardBonus = this.Role.leftovermultiplier;
                if (BankedCards.Count > 0)
                {
                    sum += (BankedCards.Where(c => c.CardType == CardTypeEnum.Point).Count() * cardBonus);
                }
                if (Hand.Count > 0)
                {
                    sum += (Hand.Where(c => c.CardType == CardTypeEnum.Point).Count() * cardBonus);
                }
                int numBonusGreen = bonusGreenOnes / 3; // this will round them down
                int numBonusPink = bonusPinkOnes / 3; // this will round them down
                sum -= ((numBonusGreen + numBonusPink) * 3 * cardBonus); // this will remove the per-card bonus
                sum += numBonusGreen + numBonusPink;
                return sum;
            }
        }

        public bool AutoPilot { get; private set; } = autoPilot;
        public PlayerStats Stats { get; internal set; } = new PlayerStats();

        public class PlayerStats
        {
            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.AppendLine("Player Stats:");
                sb.AppendLine($"  Cards Banked    : {cardsBanked}");
                sb.AppendLine($"  Actions Played  : {actionsPlayed}");
                sb.AppendLine($"  Cards Drawn     : {cardsDrawn}");
                sb.AppendLine($"  Discards Drawn  : {discardsDrawn}");
                sb.AppendLine($"  Steals          : {steals}");
                sb.AppendLine($"  Trades          : {trades}");
                sb.AppendLine($"  Spent           : {spent}");
                sb.AppendLine($"  Spent From Hand : {spentFromHand}");
                sb.AppendLine($"  Bought          : {bought}");
                sb.AppendLine($"  Passes          : {passes}");
                sb.AppendLine($"  Choices         : {choices}");
                sb.AppendLine($"  Bank Not Draw   : {bankInsteadOfDraw}");
                return sb.ToString();
            }
            public int cardsBanked;
            public int actionsPlayed;
            internal int cardsDrawn;
            internal int discardsDrawn;
            internal int steals;
            internal int bankInsteadOfDraw;
            internal int trades;
            internal int spent;
            internal int spentFromHand;
            internal int bought;
            internal int passes;
            internal int choices;
            public PlayerStats()
            {
                bankInsteadOfDraw = 0;
                cardsBanked = 0;
                actionsPlayed = 0;
                cardsDrawn = 0;
                discardsDrawn = 0;
                steals = 0;
                trades = 0;
                spent = 0;
                spentFromHand = 0;
                bought = 0;
                passes = 0;
                choices = 0;
            }

        }

        public void PlayCard(Card card)
        {
            hand.Remove(card);
        }
        public void DealCard(Card card)
        {
            HandAdd(card);
        }
        public bool TakeTurn(Deck deck)
        {
            Game.GamePrintN($"\n-----------------------------------\n* Turn for ");
            this.Print();

            if (!deck.HasDrawCards())
            {
                Game.GamePrint($"\n{Name} cannot draw a card. The deck is empty!");
                return false;
            }

            // 1. Draw a card
            DrawCard(deck);

            PlayACard();

            return true; // Turn was successful
        }
        public override string ToString()
        {
            string handString = string.Join(", ", Hand.Select(card => card.ToString()));
            string bankedCardsString = string.Join(", ", BankedCards.Select(card => card.ToString()));
            string friendsMonstersString = string.Join(", ", Bleachers.Select(card => card.ToString()));
            return $"Player: {Name} [{Role}]\n" +
                   //$"  Hand: {handString}\n" +
                   //$"  Banked Cards: {bankedCardsString}\n" +
                   $"  Friends/Monsters: {friendsMonstersString}\n" +
                   $"  Victory Points: {GamePoints}\n" +
                   $"  Green/Pink Points: {GreenPoints}, {PinkPoints}";
        }
        private void Print()
        {
            string handString = string.Join(", ", Hand.Select(card => card.ToString()));
            string bankedCardsString = string.Join(", ", BankedCards.Select(card => card.ToString()));
            string friendsMonstersString = string.Join(", ", Bleachers.Select(card => card.ToString()));
            if (GameHelpers.printVerbose)
            {
                Game.GamePrint($"Player: {Name} [{Role}]");
                Game.GamePrint($"  Friends/Monsters: {friendsMonstersString}");
                Game.GamePrint($"  Victory Points: {GamePoints}");
                Game.GamePrintN($"  Green/Pink Points: ");
                Game.SetColor(ConsoleColor.Green);
                Game.GamePrintN($"{GreenPoints}");
                Game.ResetColor();
                Game.GamePrintN($", ");
                Game.SetColor(ConsoleColor.Magenta);
                Game.GamePrint($"{PinkPoints}");
                Game.ResetColor();
            }
        }
        public void DrawCard(Deck deck)
        {
            bool handEmpty = Hand.Count == 0;
            if (this.Role.canDrawFromDiscard)
            {
                // TODO: Let a human decide
                if (deck.HasDiscardCards())
                {
                    List<string> choices = [];
                    choices.Add("Draw Pile");
                    choices.Add("Discard Pile");
                    int choice = GetPlayerChoice("Draw from:", choices, false); // 0 or 1 because of 0 based
                    if (choice == 1)
                    {
                        Card drawnCard2 = deck.DrawCardFromDiscard();
                        Game.GamePrint($"\n{Name} draws from discard: {drawnCard2}");
                        HandAdd(drawnCard2);
                        this.Stats.discardsDrawn++;
                        return;
                    }
                }
            }

            if (this.Role.canBankInsteadOfDraw && Hand.Where(c => c.CardType == CardTypeEnum.Point).Any())
            {
                var h = Hand.Where(c => c.CardType == CardTypeEnum.Point).ToList();
                List<string> choices = [];
                choices.Add("Draw");
                choices.Add("Bank");
                int choice = GetPlayerChoice("Bank or Draw:", choices, false); // 0 or 1 because of 0 based
                if (choice == 1)
                {
                    int cardIndex = PrintCardsAndCollectSelection("Card to bank:", h);
                    if (cardIndex > 0)
                    {
                        Card chosenCard = h[cardIndex];
                        chosenCard.HandleCard(this);
                        Game.GamePrint($"Playing {chosenCard} instead of drawing");
                        Stats.bankInsteadOfDraw++;
                        return;
                    }
                }
            }
            Card drawnCard = deck.DrawCard();
            Game.SetColor(ConsoleColor.Cyan);
            Game.GamePrint($"\n{Name} draws: {drawnCard}");
            Game.ResetColor();
            HandAdd(drawnCard);
            Stats.cardsDrawn++;
            if (handEmpty && deck.HasDrawCards())
            {
                // draw an extra card with empty hand, if possible
                DrawCard(deck);
            }

        }
        public void PlayACard()
        {
            // 2. (For now) Print the hand
            bool done = false;
            while (!done)
            {
                int cardIndex = PrintCardsAndCollectSelection($"\n{Name}'s hand:", Hand);

                // 4. (You'll add logic here to handle the chosen card) ...
                if (cardIndex == -1)
                {
                    Game.GamePrint("Player passes.");
                    Stats.passes++;
                    done = true;
                }
                else
                {
                    Card chosenCard = Hand[cardIndex];
                    Game.GamePrint($"Player plays {chosenCard}");
                    done = chosenCard.HandleCard(this);
                }
            }
        }
        internal int PrintCardsAndCollectSelection(string prompt, List<Card> h)
        {
            return PrintCardsAndCollectSelection(prompt, h.AsReadOnly());
        }
        internal int PrintCardsAndCollectSelection(string prompt, ReadOnlyCollection<Card> h)
        {
            List<string> choices = [];
            for (int i = 0; i < h.Count; i++)
            {
                string cardName = h[i].ToString();
                switch (h[i].Color)
                {
                    case CardColorEnum.Friend:
                        cardName += " (f)"; break;
                    case CardColorEnum.Monster:
                        cardName += " (m)"; break;
                }
                choices.Add(cardName);
            }
            // 3. Get player's card choice
            int cardIndex = GetPlayerChoice(prompt, choices);
            return cardIndex;
        }

        private static int GetAIInput(int maxCount, bool canPass)
        {
            int choice;
            Random rng = new();
            if (maxCount == 0)
            {
                choice = 0;
            }
            else
            {
                choice = rng.Next(maxCount) + 1;
                if (rng.Next(10) == 0 && canPass)
                {
                    choice = 0;
                }
            }
            Game.GamePrint($"[AI choice] {choice}");
            return choice - 1;
        }

        internal int UsablePoints(Card.CardColorEnum color)
        {
            int bonus = 0;
            if (Role == RoleHelpers.artificer)
            {
                bonus = Hand.Where(c => c.CardType == CardTypeEnum.Point && c.Color == color).Sum(c => c.Value);
            }
            return bonus + BankedPoints(color);
        }
        internal int BankedPoints(Card.CardColorEnum color)
        {
            return BankedCards.Where(c => c.Color == color).Sum(c => c.Value);
        }
        internal void HandAdd(Card card)
        {
            System.Diagnostics.Debug.Assert(card != null, "Card can't be null");
            hand.Add(card);
            hand.Sort();
        }
        internal void HandRemove(Card card)
        {
            hand.Remove(card);
        }
        public interface ISelectionTask
        {
            void RequestSelection(string prompt, List<string> choices, bool canPass, TaskCompletionSource<int> promise);
        }

        public class MenuSelectionTask() : ISelectionTask
        {
            public void RequestSelection(string prompt, List<string> choices, bool canPass, TaskCompletionSource<int> promise)
            {
                promise.SetResult(MenuUtils.GetPlayerChoiceWithArrowKeys(prompt, choices, canPass));
            }
        }
        public class ConsoleSelectionTask() : ISelectionTask
        {
            static public int GetHumanInput(int maxCount, bool canPass = true)
            {
                if (maxCount == 0)
                {
                    Game.GamePrint("Nothing to pick from!");
                    return -1;
                }

                int choice;
                string passString = canPass ? " (0 to pass)" : "";
                string prompt = $"Make a selection{passString}: ";
                do
                {
                    Console.Write(prompt);
                    // change from infinite while to awaiting a task completion
                    // https://stackoverflow.com/questions/38996593/promise-equivalent-in-c-sharp
                    // the completion could come from another thread.
                    // the windows version should run the game thread on the side
                } while (!int.TryParse(Console.ReadLine(), out choice) || choice > maxCount || (choice == 0 && !canPass) || choice < 0);
                return choice - 1; // Pass (entered as 0) will be -1
            }
            public void RequestSelection(string prompt, List<string> choices, bool canPass, TaskCompletionSource<int> promise)
            {
                Game.GamePrint(prompt);
                if (canPass)
                {
                    // Game.gamePrint("  0. Pass");
                }
                int c = 0;
                foreach (string itemX in choices)
                {
                    string item = itemX;
                    var color = CardColorEnum.None;
                    if (item.EndsWith(" (f)"))
                    {
                        item = item.Remove(item.Length - 4);
                        color = CardColorEnum.Friend;
                    }
                    else if (item.EndsWith(" (m)"))
                    {
                        item = item.Remove(item.Length - 4);
                        color = CardColorEnum.Monster;
                    }
                    switch (color)
                    {
                        case CardColorEnum.None:
                            Game.ResetColor(); break;
                        case CardColorEnum.Friend:
                            Game.SetColor(ConsoleColor.Magenta); break;
                        case CardColorEnum.Monster:
                            Game.SetColor(ConsoleColor.Green); break;
                    }
                    Game.GamePrint($"  {++c}. {item}");
                    Game.ResetColor();
                }
                promise.SetResult(GetHumanInput(choices.Count, canPass));
                return;
            }
        }

        private static readonly MenuSelectionTask menuSelectionTask = new();
        public static ISelectionTask cst = menuSelectionTask; //  ConsoleSelectionTask();

        internal int GetPlayerChoice(string prompt, List<string> choices, bool canPass = true)
        {
            Stats.choices++;

            if (AutoPilot)
            {
                return GetAIInput(choices.Count, canPass);
            }

            TaskCompletionSource<int> tcs1 = new();
            Task.Factory.StartNew(() =>
            {
                cst.RequestSelection(prompt, choices, canPass, tcs1);
            });
            return tcs1.Task.Result;
        }

        internal List<Player> opponents = [];
        public int CompleteSets
        {
            get
            {
                int setBonus = 0;
                foreach (CardColorEnum cardColorEnum in Enum.GetValues<CardColorEnum>())
                {
                    int easy = Bleachers.Where(c => c.Value == 1 && c.Color == cardColorEnum).Count();
                    int med = Bleachers.Where(c => c.Value == 3 && c.Color == cardColorEnum).Count();
                    int hard = Bleachers.Where(c => c.Value == 5 && c.Color == cardColorEnum).Count();
                    setBonus += Math.Min(Math.Min(easy, med), hard);
                }
                return setBonus;
            }
        }
    }

    public class Game
    {
        public static void GamePrint(string output, bool verbose = true)
        {
            if (!GameHelpers.printVerbose && verbose)
            {
                return;
            }
            Console.WriteLine(output);
        }

        public Deck deck;
        public List<Player> players;

        public Game(bool chattyPrint, int numhumans, int numplayers, bool useRoles)
        {
            if (numhumans > 0)
            {
                chattyPrint = true;
            }
            players = [];
            GameHelpers.printVerbose = chattyPrint;
            deck = new Deck();
            int pcount = 0;
            for (int i = 0; i < numplayers; i++)
            {
                var role = RoleHelpers.none;
                if (useRoles)
                {
                    switch (i % 3)
                    {
                        case 0:
                            role = RoleHelpers.gravedigger; break;
                        case 1:
                            role = RoleHelpers.artificer; break;
                        case 2:
                            role = RoleHelpers.fateweaver; break;
                    }
                }
                players.Add(new Player($"Player {i + 1}", role, deck, (++pcount) > numhumans));
            }
            for (int i = 0; i < numplayers; i++)
            {
                for (int j = 0; j < numplayers; j++)
                {
                    if (i != j)
                    {
                        players[i].opponents.Add(players[j]);
                    }
                }
            }
            DealStartingHands();
        }

        private void DealStartingHands()
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < players.Count; j++)
                {
                    if (deck.HasDrawCards())
                    {
                        players[j].DealCard(deck.DrawCard());
                    }
                }
            }
        }

        public bool PlayRound()
        {
            GamePrint($"Draw Pile: {deck.CardsLeft()}");
            int i = 0;
            foreach (Player player in players)
            {
                MenuUtilsHelpers.curPlayer = i;
                i++;
                if (!player.TakeTurn(deck))
                {
                    return false;
                }
            }
            return true;
        }

        public override string ToString()
        {
            string retval = $"{deck}\n";
            foreach (Player player in players)
            {
                retval += player.ToString() + "\n";
            }
            return retval;
        }

        internal static void ResetColor()
        {
            if (GameHelpers.printVerbose)
            {
                Console.ResetColor();
            }
        }

        internal static void SetColor(ConsoleColor color)
        {
            if (GameHelpers.printVerbose)
            {
                Console.ForegroundColor = color;
            }
        }

        internal static void GamePrintN(string v)
        {
            if (GameHelpers.printVerbose)
            {
                Console.Write(v);
            }
        }
    }
}