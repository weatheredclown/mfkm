using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using static System.Formats.Asn1.AsnWriter;
using mfkm;
using System.Reflection.Emit;
using static mfkm.Card;
using System.Windows.Documents;
namespace mfkmapp
{
    public partial class MainWindow : Window
    {
        private readonly Game game;
        string? savedPrompt;
        bool savedCanPass;
        List<string>? saveChoices;
        TaskCompletionSource<int>? savedPromise = null;

        public MainWindow()
        {
            InitializeComponent();
            Player.cst = new WindowsSelectionTask(this);
            game = new Game(chattyPrint: false, numhumans: 2, numplayers: 2, useRoles: false);
            MenuUtilsHelpers.game = game;
            Task.Factory.StartNew(() =>
            {
                int turns = 0;
                do { Game.GamePrint($"\n### Round {++turns}"); } while (game.PlayRound());
                List<string> scores = game.players.Select(p=>$"{p.Name}: {p.GamePoints}").ToList();
                Game.GamePrint($"Game Over after {turns} turns! {string.Join(", ", scores)}", false);
            });
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (savedPromise == null)
            {
                return;
            }
            // Update the player's hand
            var listItems = new List<dynamic>();
            //PlayerHandList.Items.Clear();
            if (savedCanPass)
            {
                //PlayerHandList.Items.Add(new { RowText = "[pass]", RowColor = "Green" });
                listItems.Add(new { RowText = "[pass]", RowColor = "White" });
            }
            if (saveChoices != null)
            {
                for (int i = 0; i < saveChoices.Count; i++)
                {
                    string choice = saveChoices[i];
                    string rc = "White";
                    if (choice.Contains("(m)"))
                    {
                        rc = "LightGreen";
                        choice = choice.Remove(choice.Length - 4);
                    }
                    else if (choice.Contains("(f)"))
                    {
                        rc = "Magenta";
                        choice = choice.Remove(choice.Length - 4);
                    }
                    listItems.Add(new { RowText = choice.ToString(), RowColor = rc });
                }
            }
            PlayerHandList.ItemsSource = listItems;
            // Update the game status
            // MenuUtilsHelpers.curPlayer = i;
            string cur = MenuUtilsHelpers.curPlayer == 0 ? "One" : "Two";
            GameStatusLabel.Content = $"Ready Player {cur}\n" +
                $"Current Game Points: {MenuUtilsHelpers.game?.players[MenuUtilsHelpers.curPlayer].GamePoints:00.00}\n" +
                $"Completed Sets: {MenuUtilsHelpers.game?.players[MenuUtilsHelpers.curPlayer].CompleteSets}";

            HFLabel.Content = $"HF: {MenuUtilsHelpers.game.players[MenuUtilsHelpers.curPlayer].Bleachers.Where(c => c.Value == 5 && c.Color == CardColorEnum.Friend).Count()}";
            MFLabel.Content = $"MF: {MenuUtilsHelpers.game.players[MenuUtilsHelpers.curPlayer].Bleachers.Where(c => c.Value == 3 && c.Color == CardColorEnum.Friend).Count()}";
            EFLabel.Content = $"EF: {MenuUtilsHelpers.game.players[MenuUtilsHelpers.curPlayer].Bleachers.Where(c => c.Value == 1 && c.Color == CardColorEnum.Friend).Count()}";

            HMLabel.Content = $"HM: {MenuUtilsHelpers.game.players[MenuUtilsHelpers.curPlayer].Bleachers.Where(c => c.Value == 5 && c.Color == CardColorEnum.Monster).Count()}";
            MMLabel.Content = $"MM: {MenuUtilsHelpers.game.players[MenuUtilsHelpers.curPlayer].Bleachers.Where(c => c.Value == 3 && c.Color == CardColorEnum.Monster).Count()}";
            EMLabel.Content = $"EM: {MenuUtilsHelpers.game.players[MenuUtilsHelpers.curPlayer].Bleachers.Where(c => c.Value == 1 && c.Color == CardColorEnum.Monster).Count()}";

            FriendPointsLabel.Content = $"Friend Points: {MenuUtilsHelpers.game.players[MenuUtilsHelpers.curPlayer].PinkPoints}";
            MonsterPointsLabel.Content = $"Monster Points: {MenuUtilsHelpers.game.players[MenuUtilsHelpers.curPlayer].GreenPoints}";

            DrawPileLabel.Content = $"Draw Pile: {MenuUtilsHelpers.game.deck.CardsLeft()} cards remaining";

            //$"{savedPrompt}";
            /*
            InfoLabel.Content = 
                $"Player 1: {game.players[0].GamePoints} points\n" +
                $"Green: {game.players[0].GreenPoints}\nPink: {game.players[0].PinkPoints}\n\n" +
                $"Player 2: {game.players[1].GamePoints} points\n" +
                $"Green: {game.players[1].GreenPoints}\nPink: {game.players[1].PinkPoints}\n" +
                $"\nCards:{game.deck.CardsLeft()}";
            */
        }

        public class WindowsSelectionTask(MainWindow mw) : Player.ISelectionTask
        {
            readonly MainWindow mw = mw;
            void Player.ISelectionTask.RequestSelection(string prompt, List<string> choices, bool canPass, TaskCompletionSource<int> promise)
            {
                mw.saveChoices = choices;
                mw.savedCanPass = canPass;
                mw.savedPrompt = prompt;
                mw.savedPromise = promise;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    mw.UpdateUI();
                });
            }
        }

        private void PlayCard_Click(object sender, RoutedEventArgs e)
        {
            if (savedPromise == null)
            {
                MessageBox.Show("Not Ready!");
            }

            // Play the selected card
            if (PlayerHandList.SelectedItem != null)
            {
                int selectedIndex = PlayerHandList.SelectedIndex;
                if (savedCanPass) { selectedIndex--; }
                savedPromise?.SetResult(selectedIndex);
                savedPromise = null;
                UpdateUI();
            }
            else
            {
                MessageBox.Show("Select a card to play!");
            }
        }
    }
}
