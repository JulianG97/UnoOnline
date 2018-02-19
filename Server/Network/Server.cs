﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Server
{
    public class Server
    {
        private TcpListener listener;
        private Thread listenerThread;
        private List<Game> games;
        private int nextGameID = 1;

        public bool isRunning
        {
            get;
            private set;
        }

        public void Start()
        {
            this.games = new List<Game>();
            this.listener = new TcpListener(IPAddress.Any, 3000);
            this.listenerThread = new Thread(this.StartListening);
            listenerThread.Start();
            this.isRunning = true;

            Console.WriteLine("The server has been successfully started!");
        }

        public void StartListening()
        {
            this.listener.Start();

            while (this.isRunning == true)
            {
                if (!this.listener.Pending())
                {
                    Thread.Sleep(100);
                    continue;
                }

                TcpClient client = this.listener.AcceptTcpClient();
                Thread session = new Thread(new ParameterizedThreadStart(HandleNewSession));
                session.Start(client);
            }
        }

        public void HandleNewSession(object data)
        {
            TcpClient client = (TcpClient)data;
            NetworkManager networkManager = new NetworkManager(client);
            networkManager.OnConnectionsLost += this.ConnectionLost;
            networkManager.OnDataReceived += this.DataReceived;
            networkManager.Start();
        }

        public void Stop()
        {
            // Stop all connected network managers
            this.listener.Stop();
            this.isRunning = false;
            Console.WriteLine("The server has been successfully stopped!");
        }

        private void ConnectionLost(object sender, EventArgs args)
        {
            // Cards of the player moves to the bottom of the discard pile
            // Player gets removed from the game
            // Game continues if there are still at least two players
            // Else the game ends and the player who is still connected wins the game
        }

        private void DataReceived(object sender, OnDataReceivedEventArgs args)
        {
            if (args.Protocol != null)
            {
                if (args.Protocol.Type.SequenceEqual(ProtocolTypes.CreateGame))
                {
                    int integer;

                    bool isInteger = int.TryParse(Encoding.ASCII.GetString(args.Protocol.Content), out integer);

                    if (isInteger == true)
                    {
                        this.CreateNewGame(integer, args.NetworkManager);
                    }
                }
                else if (args.Protocol.Type.SequenceEqual(ProtocolTypes.JoinGame))
                {
                    this.JoinGame(args.Protocol.Content, args.NetworkManager);
                }
                else if (args.Protocol.Type.SequenceEqual(ProtocolTypes.RequestRooms))
                {
                    this.SendRoomList(args.NetworkManager);
                }
                else
                {
                    // Redirect to game class
                }
            }
        }

        private void CreateNewGame(int amountOfPlayers, NetworkManager networkManager)
        {
            Game game = new Game(this.nextGameID, amountOfPlayers);
            this.nextGameID++;
            game.Players = new List<Player>();
            game.Players.Add(new Player(1, networkManager));
            this.games.Add(game);

            if (amountOfPlayers < 2 || amountOfPlayers > 4)
            {
                networkManager.Send(ProtocolManager.Invalid());
            }
            else
            {
                networkManager.Send(ProtocolManager.OK());
            }
        }

        private void JoinGame(byte[] joinSettings, NetworkManager networkManager)
        {
            bool gameFound = false;

            foreach (Game game in games)
            {
                if (game.GameID == joinSettings[0])
                {
                    if (game.JoinedPlayers < game.PlayersNeeded)
                    {
                        game.Players.Add(new Player(game.JoinedPlayers + 1, networkManager));
                        gameFound = true;
                        networkManager.Send(ProtocolManager.OK());
                    }
                    else
                    {
                        networkManager.Send(ProtocolManager.Invalid());
                    }

                    break;
                }
            }

            if (gameFound == false)
            {
                networkManager.Send(ProtocolManager.Invalid());
            }
        }

        private void SendRoomList(NetworkManager networkManager)
        {
            networkManager.Send(ProtocolManager.RoomList(games));
            networkManager.Stop();
        }
    }
}