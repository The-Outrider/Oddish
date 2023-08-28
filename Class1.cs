using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Plugins;
using CoreAPI = Hearthstone_Deck_Tracker.API.Core;
using Hearthstone_Deck_Tracker.Utility.Logging;
using System.IO;
using System.Collections;
using System.Text.Encodings.Web;
using System.Web.Script.Serialization;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.LogReader;

namespace Oddish
{
    public struct MatchObject
    {
        public string MatchId { get; set; }
        public PlayerObject Player { get; set; }
        public PlayerObject Opponent { get; set; }
        public List<TurnObject> Turns { get; set; }

    }
    public struct PlayerObject
    {
        public string Name { get; set; }
        public string Class { get; set; }

    }
    public struct TurnObject
    {
        public int TurnNumber { get; set; }
        public List<string> Hand { get; set; }
        public List<string> PlayerBoard { get; set; }
        public List<string> OpponentBoard { get; set; }
    }
 
     

    public class Class1
    {
        public static string FileName { get; set; }
        public static MatchObject match = new MatchObject();
        public static PlayerObject player = new PlayerObject();
        public static PlayerObject opponent = new PlayerObject();
        public static string json { get; set; }
        
        public static int playerPosition { get; set; }
        public static int opponentPosition { get; set; }

        public static List<Hearthstone_Deck_Tracker.Hearthstone.Entities.Entity> currentHand { get; set; }

        public static TurnObject turn = new TurnObject();

        internal static void TurnStart(ActivePlayer activePlayer)
        {
            Log.Info("GameEvents.OnTurnStart");
            Hearthstone_Deck_Tracker.Hearthstone.GameV2 game = CoreAPI.Game;
            var currentTurn = CoreAPI.Game.GetTurnNumber();

 

            if (activePlayer == ActivePlayer.Player)
            {
                // Check if player is in first or second position
                if (currentTurn == 1)
                {
                    //Get First player game tag
                    GameTagHelper gameTagHelper = new GameTagHelper();
                    var firstPlayer = gameTagHelper.GetType().GetField("FIRST_PLAYER");
                    var hasCoin = CoreAPI.Game.Player.HasCoin;
                    playerPosition = hasCoin ? 2 : 1;
                    opponentPosition = playerPosition == 1 ? 2 : 1;
                }

                // Player Turn
                if (player.Name == null)
                {
                    player.Name = CoreAPI.Game.Player.Name;
                    player.Class = CoreAPI.Game.Player.Class;
                    match.Player = player;
                }
 

                // Current Working theory about unknowns ... is ControlledBy may be switching game to game.
                currentHand = CoreAPI.Game.Entities.Values.Where(x => x.IsInHand && x.IsControlledBy(playerPosition)).ToList();
                var playerBoard = CoreAPI.Game.Entities.Values.Where(x => x.IsInPlay && x.IsPlayableCard && x.IsControlledBy(playerPosition)).ToList();
                var opponentBoard = CoreAPI.Game.Entities.Values.Where(x => x.IsInPlay && x.IsPlayableCard && x.IsControlledBy(opponentPosition)).ToList();
                // Enums.Hearthstone.GAME_TAG

                // Create new turn object
                turn = new TurnObject();
                turn.TurnNumber = currentTurn;
                turn.Hand = new List<string>();
                foreach(var cardObj in currentHand)
                {
                    turn.Hand.Add(cardObj.Card.Name);
                }
                turn.PlayerBoard = new List<string>();
                foreach(var cardObj in playerBoard)
                {
                    turn.PlayerBoard.Add(cardObj.Card.Name);
                }
                turn.OpponentBoard = new List<string>();
                foreach(var cardObj in opponentBoard)
                {
                    turn.OpponentBoard.Add(cardObj.Card.Name);
                }
                // Add turn object to match object
                if(match.Turns == null)
                {
                    match.Turns = new List<TurnObject>();
                } 

                match.Turns.Add(turn);

                writeMatchToFile();
            }
            else
            {
                // Opponent Turn
                if(opponent.Name == null)
                {
                    opponent.Name = CoreAPI.Game.Opponent.Name;
                    opponent.Class = CoreAPI.Game.Opponent.Class;
                    match.Opponent = opponent;
                }

            }
        }

        internal static void GameStart()
        {
            // If file does not exist, create it
            System.Guid gameId = CoreAPI.Game.CurrentGameStats.GameId;

            // convert the game id to a string
            string gameIdString = gameId.ToString();
            // Set the filename
            FileName = @"C:\Users\Starshot\Documents\HearthstoneDeckTracker\Replay\" + gameIdString + ".json";
            
            Log.Info(FileName);
            Log.Info("Starting to try and write the file");
            try
            {

                Log.Info("Checking");
                if (System.IO.File.Exists(FileName))
                {
                    Log.Info("File Exists, doing Nothing");
                }
                else
                {
                    Log.Info("File does not exist");
                    // Create the json file in the replay directory
                    var file = System.IO.File.Create(FileName);
                    file.Close();
                    
                }

            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            } 
            finally
            {
                Log.Info("File Created, lets begin");
                // Set the game and player information to the match object
                match.MatchId = gameIdString;
            }
          
        }

        internal static void PlayerDraw(Card card)
        {
            // Check if turn is empty
            if(turn.Hand == null)
            {
                turn.Hand.Add(card.Name);
                writeMatchToFile();

            }


        }

        internal static void GameEnd()
        {
            //Reset all values
            match = new MatchObject();
            player = new PlayerObject();
            opponent = new PlayerObject();
            turn = new TurnObject();

            json = "";
        }

        internal static void writeMatchToFile()
        {
            // Serialize the match object
            var serializer = new JavaScriptSerializer();
            json = serializer.Serialize(match);
            // Write to file
            System.IO.File.WriteAllText(FileName, json);
        }
    }

    public class Class1Plugin: IPlugin
    {
        public void OnLoad()
        {
            // Triggered upon startup and when the user ticks the plugin on
            GameEvents.OnGameStart.Add(Class1.GameStart);
            GameEvents.OnTurnStart.Add(Class1.TurnStart);
            GameEvents.OnGameEnd.Add(Class1.GameEnd);
            GameEvents.OnPlayerDraw.Add(Class1.PlayerDraw);
        }

        public void OnUnload()
        {
            // Triggered when the user unticks the plugin, however, HDT does not completely unload the plugin.
            // see https://git.io/vxEcH
        }

        public void OnButtonPress()
        {
            // Triggered when the user clicks your button in the plugin list
        }

        public void OnUpdate()
        {
            // called every ~100ms
        }

        public string Name => "Oddish";

        public string Description => "A ML Suggestion Engine for Hearthstone";

        public string ButtonText => "Settings";

        public string Author => "David Rynearson";

        public Version Version => new Version(0, 0, 1);

        public MenuItem MenuItem => null;
    }
}
