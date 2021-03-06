﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Client
{
    public class Game
    {
        private NetworkManager networkManager;
        private bool validAction;
        private bool serverResponseReceived;
        private string roomList;
        private int playerID;
        private int winnerID;
        private int playerIDWhoIsOnTurn;
        private int lobbyID;
        private Card lastCard;
        private List<Card> Deck;
        private List<string> numberOfCardsOfPlayers;
        private bool gameOver;

        public Game(NetworkManager networkManager)
        {
            this.networkManager = networkManager;
            this.networkManager.OnConnectionsLost += this.ConnectionLost;
            this.networkManager.OnDataReceived += this.DataReceived;
            this.validAction = false;
        }

        public void CreateGame()
        {
            int amountOfPlayers = 2;

            while (true)
            {
                Menu.DisplayGameHeader();

                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.White;

                Console.WriteLine("Please set the number of players with the UP ARROW and DOWN ARROW!");
                Console.WriteLine("Press ENTER to confirm and create a new game!");
                Console.WriteLine();
                Console.Write("Players: ");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(amountOfPlayers);

                ConsoleKeyInfo cki = Console.ReadKey(true);

                if (cki.Key == ConsoleKey.UpArrow)
                {
                    if (amountOfPlayers + 1 > 4)
                    {
                        amountOfPlayers = 2;
                    }
                    else
                    {
                        amountOfPlayers++;
                    }
                }
                else if (cki.Key == ConsoleKey.DownArrow)
                {
                    if (amountOfPlayers - 1 < 2)
                    {
                        amountOfPlayers = 4;
                    }
                    else
                    {
                        amountOfPlayers--;
                    }
                }
                else if (cki.Key == ConsoleKey.Enter)
                {
                    Console.Clear();

                    this.networkManager.Start();

                    if (this.networkManager.Connected == true)
                    {
                        this.networkManager.Send(ProtocolManager.CreateGame(amountOfPlayers.ToString()));

                        this.WaitForServerResponse();

                        if (validAction == true)
                        {
                            this.DisplayWaitingScreen();
                            this.Start();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine();
                            Console.WriteLine("The game couldn't be created!");
                            Console.WriteLine("Press any key to return to the main menu...");

                            Console.ReadKey(true);
                        }
                    }

                    break;
                }

                Console.Clear();
            }

                            Console.Clear();

                Menu.DisplayMainMenu();
        }

        public void JoinGame()
        {
            this.networkManager.Start();

            if (this.networkManager.Connected == true)
            {
                string[] roomArray = null;
                this.networkManager.Send(ProtocolManager.RequestRooms());

                this.WaitForServerResponse();

                int position = 0;

                while (true)
                {
                    Menu.DisplayGameHeader();
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.White;

                    Console.WriteLine("[UP/DOWN ARROW] Navigate [R] Refresh room list [E] Go back to main menu");
                    Console.WriteLine();

                    if (this.roomList == null || this.roomList == string.Empty)
                    {
                        Console.WriteLine("There aren't any rooms open to join!");
                        Console.WriteLine("Please create a new game...");
                    }
                    else
                    {
                        roomArray = this.roomList.Split('-');

                        for (int i = 0; i < roomArray.Length; i += 3)
                        {
                            if (i == position * 3)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                            }

                            Console.WriteLine("Room ID: {0}", roomArray[i]);
                            Console.WriteLine("Players: {0}/{1}", roomArray[i + 1], roomArray[i + 2]);
                            Console.WriteLine();

                            Console.ForegroundColor = ConsoleColor.White;
                        }
                    }

                    ConsoleKeyInfo cki = Console.ReadKey(true);

                    if (cki.Key == ConsoleKey.R)
                    {
                        this.networkManager.Send(ProtocolManager.RequestRooms());

                        this.WaitForServerResponse();
                    }
                    else if (cki.Key == ConsoleKey.E)
                    {
                        break;
                    }
                    else if (cki.Key == ConsoleKey.UpArrow)
                    {
                        if (roomArray != null)
                        {
                            if (position - 1 < 0)
                            {
                                position = (roomArray.Length / 3) - 1;
                            }
                            else
                            {
                                position--;
                            }
                        }
                    }
                    else if (cki.Key == ConsoleKey.DownArrow)
                    {
                        if (roomArray != null)
                        {
                            if (position + 1 > (roomArray.Length / 3) - 1)
                            {
                                position = 0;
                            }
                            else
                            {
                                position++;
                            }
                        }
                    }
                    else if (cki.Key == ConsoleKey.Enter)
                    {
                        if (roomArray != null)
                        {
                            this.networkManager.Send(ProtocolManager.JoinGame(roomArray[position * 3]));

                            this.WaitForServerResponse();

                            Console.Clear();

                            if (this.validAction == true)
                            {
                                DisplayWaitingScreen();
                                this.Start();
                            }
                            else if (this.validAction == false)
                            {
                                Menu.DisplayGameHeader();
                                Console.WriteLine();
                                Console.ForegroundColor = ConsoleColor.White;

                                Console.WriteLine("The room you tried to join isn't open or already full!");
                                Console.WriteLine("Press any key to continue!");

                                Console.ReadKey(true);

                                this.networkManager.Send(ProtocolManager.RequestRooms());

                                this.WaitForServerResponse();

                                roomArray = this.roomList.Split('-');
                            }
                        }

                        break;
                    }

                    Console.Clear();
                }

                this.networkManager.Stop();

                Console.Clear();

                Menu.DisplayMainMenu();
            }
        }

        private void DisplayWaitingScreen()
        {
            Menu.DisplayGameHeader();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("Please wait until enough players joined the game and the game starts!");

            for (int i = 0; i < 3; i++)
            {
                this.WaitForServerResponse();
            }
        }

        public void Start()
        {
            while (this.gameOver == false)
            {
                Console.Clear();
                this.ShowPlayerStats();
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("[U] Press U for UNO");
                Console.ResetColor();
                this.ShowPiles();

                if (this.playerIDWhoIsOnTurn == this.playerID)
                {
                    this.SetCard();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine();
                    Console.WriteLine("Player {0} is on turn! Please wait...", this.playerIDWhoIsOnTurn);
                }

                for (int i = 0; i < 2; i++)
                {
                    this.WaitForServerResponse();
                }
            }

            this.GameOver();
        }

        private void GameOver()
        {
            this.networkManager.Stop();

            Console.Clear();

            Menu.DisplayGameHeader();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.White;

            if (this.winnerID == this.playerID)
            {
                Console.WriteLine("Game Over! You won the game!");
            }
            else
            {
                Console.WriteLine("Game Over! Player {0} won the game!", this.winnerID);
            }
            Console.WriteLine();
            Console.WriteLine("Press any key to return to the main menu...");
            Console.ResetColor();
            Console.ReadKey(true);
        }

        private void ShowPiles()
        {
            Card drawPile = new Card(Color.White, Value.Uno);
            Card discardPile = this.lastCard;

            Console.SetCursorPosition(8, this.numberOfCardsOfPlayers.Count + 4);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("DRAW   DISCARD");
            Console.ResetColor();

            drawPile.Draw(7, this.numberOfCardsOfPlayers.Count + 5);
            discardPile.Draw(15, this.numberOfCardsOfPlayers.Count + 5);
        }

        private void SetCard()
        {
            int position = 0;
            int positionStart = 0;
            int positionEnd;
            int unoYesOrNo = 0;

            if (this.Deck.Count > 5)
            {
                positionEnd = 5;
            }
            else
            {
                positionEnd = this.Deck.Count;
            }

            while (this.gameOver == false)
            {
                DisplayCards(positionStart, positionEnd, position);

                if (Console.KeyAvailable == false)
                {
                    continue;
                }

                ConsoleKeyInfo cki = Console.ReadKey(true);

                if (cki.Key == ConsoleKey.RightArrow)
                {
                    if (position + 1 == this.Deck.Count)
                    { }
                    else if (position + 1 > positionEnd - 1)
                    {
                        position++;
                        positionStart++;
                        positionEnd++;
                    }
                    else
                    {
                        position++;
                    }
                }
                else if (cki.Key == ConsoleKey.LeftArrow)
                {
                    if (position - 1 < 0)
                    { }
                    else if (position - 1 < positionStart)
                    {
                        position--;
                        positionStart--;
                        positionEnd--;
                    }
                    else
                    {
                        position--;
                    }
                }
                else if (cki.Key == ConsoleKey.U)
                {
                    unoYesOrNo = 1;
                }
                else if (cki.Key == ConsoleKey.Enter)
                {
                    if ((this.Deck[position].Color == Color.White))
                    {
                        this.Deck[position] = this.ChooseColor(this.Deck[position]);
                    }

                    this.networkManager.Send(ProtocolManager.SetCard(this.lobbyID.ToString(), this.playerID.ToString(), ((char)this.Deck[position].Color).ToString(), ((char)this.Deck[position].Value).ToString(), unoYesOrNo.ToString()));

                    this.WaitForServerResponse();

                    if (this.validAction == true)
                    {
                        break;
                    }
                    else if (this.validAction == false)
                    {
                        // When the client receives invalid from server he's still on turn
                        // Set Card continues
                    }
                }
            }
        }

        public void DisplayCards(int positionStart, int positionEnd, int selectedCard)
        {
            if (positionStart > 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
            }

            Console.SetCursorPosition(1, numberOfCardsOfPlayers.Count + 17);
            Console.WriteLine(" /|_ ");
            Console.SetCursorPosition(1, numberOfCardsOfPlayers.Count + 18);
            Console.WriteLine("|  _|");
            Console.SetCursorPosition(1, numberOfCardsOfPlayers.Count + 19);
            Console.WriteLine(" \\|  ");

            Console.ResetColor();

            int positionX = 7;

            for (int i = positionStart; i < positionEnd; i++, positionX += 8)
            {
                if (i == selectedCard)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;

                    Console.SetCursorPosition(positionX + 1, numberOfCardsOfPlayers.Count + 11);
                    Console.WriteLine("  _  ");
                    Console.SetCursorPosition(positionX + 1, numberOfCardsOfPlayers.Count + 12);
                    Console.WriteLine("_| |_");
                    Console.SetCursorPosition(positionX + 1, numberOfCardsOfPlayers.Count + 13);
                    Console.WriteLine("\\   /");
                    Console.SetCursorPosition(positionX + 1, numberOfCardsOfPlayers.Count + 14);
                    Console.WriteLine(" \\_/ ");

                    Console.ResetColor();
                }
                else
                {
                    Console.SetCursorPosition(positionX + 1, numberOfCardsOfPlayers.Count + 11);
                    Console.WriteLine("     ");
                    Console.SetCursorPosition(positionX + 1, numberOfCardsOfPlayers.Count + 12);
                    Console.WriteLine("     ");
                    Console.SetCursorPosition(positionX + 1, numberOfCardsOfPlayers.Count + 13);
                    Console.WriteLine("     ");
                    Console.SetCursorPosition(positionX + 1, numberOfCardsOfPlayers.Count + 14);
                    Console.WriteLine("     ");
                }

                this.Deck[i].Draw(positionX, this.numberOfCardsOfPlayers.Count + 15);

                Console.ResetColor();
            }

            if (positionEnd < this.Deck.Count)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
            }

            Console.SetCursorPosition(positionX, numberOfCardsOfPlayers.Count + 17);
            Console.WriteLine(" _|\\ ");
            Console.SetCursorPosition(positionX, numberOfCardsOfPlayers.Count + 18);
            Console.WriteLine("|_  |");
            Console.SetCursorPosition(positionX, numberOfCardsOfPlayers.Count + 19);
            Console.WriteLine("  |/ ");

            Console.ResetColor();
        }

        private void ShowPlayerStats()
        {
            for (int i = 0, playerID = 1; i < numberOfCardsOfPlayers.Count; i++, playerID++)
            {
                Console.ForegroundColor = ConsoleColor.White;

                Console.Write("Player {0} | Turn: ", playerID);

                if (playerID == this.playerIDWhoIsOnTurn)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("true ");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("false");
                }

                Console.ForegroundColor = ConsoleColor.White;

                Console.Write(" | Cards: {0}", numberOfCardsOfPlayers[i]);

                if (playerID == this.playerID)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;

                    Console.Write(" (YOU)");
                }

                Console.ResetColor();

                Console.WriteLine();
            }
        }

        private Card ChooseColor(Card card)
        {
            Card newCard = null;

            string[] colors = new string[] { "Red", "Blue", "Green", "Yellow" };

            int position = 0;

            while (true)
            {
                Console.SetCursorPosition(8, this.numberOfCardsOfPlayers.Count + 22);

                for (int i = 0; i < colors.Length; i++)
                {
                    Console.ForegroundColor = (ConsoleColor)(Enum.Parse(typeof(ConsoleColor), colors[i]));

                    if (i == position)
                    {
                        Console.BackgroundColor = ConsoleColor.DarkGray;
                    }

                    Console.Write(colors[i]);

                    Console.ResetColor();

                    Console.Write(" ");
                }

                ConsoleKeyInfo cki = Console.ReadKey(true);

                if (cki.Key == ConsoleKey.RightArrow)
                {
                    if (position + 1 > colors.Length - 1)
                    {
                        position = 0;
                    }
                    else
                    {
                        position++;
                    }
                }
                else if (cki.Key == ConsoleKey.LeftArrow)
                {
                    if (position - 1 < 0)
                    {
                        position = colors.Length - 1;
                    }
                    else
                    {
                        position--;
                    }
                }
                else if (cki.Key == ConsoleKey.Enter)
                {
                    break;
                }
            }

            return newCard = new Card((Color)(Enum.Parse(typeof(Color), colors[position])), card.Value);
        }

        private void DataReceived(object sender, OnDataReceivedEventArgs args)
        {
            if (args.Protocol != null)
            {
                if (args.Protocol.Type.SequenceEqual(ProtocolTypes.OK))
                {
                    this.validAction = true;
                    this.serverResponseReceived = true;
                }
                else if (args.Protocol.Type.SequenceEqual(ProtocolTypes.Invalid))
                {
                    this.validAction = false;
                    this.serverResponseReceived = true;
                }
                else if (args.Protocol.Type.SequenceEqual(ProtocolTypes.GameStart))
                {
                    string[] gameStartArray = (Encoding.ASCII.GetString(args.Protocol.Content)).Split('-');

                    bool isInteger = int.TryParse(gameStartArray[0], out int lobbyID);
                    bool isInteger2 = int.TryParse(gameStartArray[1], out int playerID);

                    if (isInteger == true && isInteger2 == true)
                    {
                        this.lobbyID = lobbyID;
                        this.playerID = playerID;
                        this.serverResponseReceived = true;
                    }
                }
                else if (args.Protocol.Type.SequenceEqual(ProtocolTypes.GameOver))
                {
                    string[] gameOverArray = (Encoding.ASCII.GetString(args.Protocol.Content)).Split('-');

                    this.winnerID = int.Parse(gameOverArray[0]);
                    this.gameOver = true;
                }
                else if (args.Protocol.Type.SequenceEqual(ProtocolTypes.PlayerCards))
                {
                    this.Deck = new List<Card>();

                    string cards = Encoding.ASCII.GetString(args.Protocol.Content);
                    string[] cardArray = cards.Split('-');

                    char[] cardCharArray = new char[cardArray.Length];

                    for (int i = 0; i < cardArray.Length; i++)
                    {
                        cardCharArray[i] = Convert.ToChar(cardArray[i]);
                    }

                    for (int i = 0; i < cardArray.Length; i += 2)
                    {
                        Color color = (Color)cardCharArray[i];
                        Value value = (Value)cardCharArray[i + 1];

                        this.Deck.Add(new Card(color, value));
                    }

                    this.serverResponseReceived = true;
                }
                else if (args.Protocol.Type.SequenceEqual(ProtocolTypes.RoomList))
                {
                    this.roomList = Encoding.ASCII.GetString(args.Protocol.Content);
                    this.serverResponseReceived = true;
                }
                else if (args.Protocol.Type.SequenceEqual(ProtocolTypes.RoundInformation))
                {
                    string roundInformation = Encoding.ASCII.GetString(args.Protocol.Content);
                    string[] roundInformationArray = roundInformation.Split('-');

                    char[] roundInformationCharArray = new char[2];

                    for (int i = 0; i < 2; i++)
                    {
                        roundInformationCharArray[i] = Convert.ToChar(roundInformationArray[i]);
                    }

                    Color color = (Color)roundInformationCharArray[0];
                    Value value = (Value)roundInformationCharArray[1];

                    this.lastCard = new Card(color, value);

                    this.playerIDWhoIsOnTurn = Int32.Parse(roundInformationArray[2]);

                    this.numberOfCardsOfPlayers = new List<string>();

                    for (int i = 3; i < roundInformationArray.Length; i++)
                    {
                        this.numberOfCardsOfPlayers.Add(roundInformationArray[i]);
                    }

                    this.serverResponseReceived = true;
                }
            }
        }

        private void WaitForServerResponse()
        {
            while (this.serverResponseReceived == false && this.gameOver == false)
            { }

            this.serverResponseReceived = false;
        }

        private void ConnectionLost(object sender, EventArgs args)
        {
            // If connection to server is lost
        }
    }
}
