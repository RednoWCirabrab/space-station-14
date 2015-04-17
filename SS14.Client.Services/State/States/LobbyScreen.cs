﻿using Lidgren.Network;
using SS14.Client.Interfaces.State;
using SS14.Client.Services.UserInterface.Components;
using SS14.Client.Graphics.Event;
using SS14.Shared;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.Drawing;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Graphics;
using SFML.Graphics;
using SFML.System;
using Color = System.Drawing.Color;

namespace SS14.Client.Services.State.States
{
    public class LobbyScreen : State, IState
    {
        private const double PlayerListRefreshDelaySec = 3; //Time in seconds before refreshing the playerlist.

        private readonly List<String> _playerListStrings = new List<string>();
        private string _gameType;

        private ScrollableContainer _jobButtonContainer;
        private Chatbox _lobbyChat;
        private TextSprite _lobbyText;
        private DateTime _playerListTime;
        private string _serverMapName;
        private int _serverMaxPlayers;
        private string _serverName;
        private int _serverPort;
        private string _welcomeString;

        public LobbyScreen(IDictionary<Type, object> managers)
            : base(managers)
        {
        }

        #region IState Members

        public void Startup()
        {
            UserInterfaceManager.DisposeAllComponents();

            NetworkManager.MessageArrived += NetworkManagerMessageArrived;

            _lobbyChat = new Chatbox(ResourceManager, UserInterfaceManager, KeyBindingManager);
            _lobbyChat.TextSubmitted += LobbyChatTextSubmitted;

            _lobbyChat.Update(0);

            UserInterfaceManager.AddComponent(_lobbyChat);

            _lobbyText = new TextSprite("lobbyText", "", ResourceManager.GetFont("CALIBRI"))
                             {
                                 Color = Color.Black,
                                 ShadowColor = Color.Transparent,
                                 Shadowed = true,
                                 //TODO CluwneSprite ShadowOffset
                                 // ShadowOffset = new Vector2(1, 1)
                             };

            NetOutgoingMessage message = NetworkManager.CreateMessage();
            message.Write((byte) NetMessage.WelcomeMessage); //Request Welcome msg.
            NetworkManager.SendMessage(message, NetDeliveryMethod.ReliableOrdered);

            NetworkManager.SendClientName(ConfigurationManager.GetPlayerName()); //Send name.

            NetOutgoingMessage playerListMsg = NetworkManager.CreateMessage();
            playerListMsg.Write((byte) NetMessage.PlayerList); //Request Playerlist.
            NetworkManager.SendMessage(playerListMsg, NetDeliveryMethod.ReliableOrdered);

            _playerListTime = DateTime.Now.AddSeconds(PlayerListRefreshDelaySec);

            NetOutgoingMessage jobListMsg = NetworkManager.CreateMessage();
            jobListMsg.Write((byte) NetMessage.JobList); //Request Joblist.
            NetworkManager.SendMessage(jobListMsg, NetDeliveryMethod.ReliableOrdered);

            var joinButton = new Button("Join Game", ResourceManager) {mouseOverColor = System.Drawing.Color.LightSteelBlue};
            joinButton.Position = new Point(605 - joinButton.ClientArea.Width - 5,
                                            200 - joinButton.ClientArea.Height - 5);
            joinButton.Clicked += JoinButtonClicked;

            UserInterfaceManager.AddComponent(joinButton);

            _jobButtonContainer = new ScrollableContainer("LobbyJobCont", new Size(375, 400), ResourceManager)
                                      {
                                          Position = new Point(630, 10)
                                      };

            UserInterfaceManager.AddComponent(_jobButtonContainer);

            CluwneLib.CurrentRenderTarget.Clear();
        }

        public void Render(FrameEventArgs e)
        {
            //public Vertex(Vector2f position, Color color, Vector2f texCoords);
            RectangleShape test = new RectangleShape(new Vector2f(200, 200));

            CluwneLib.CurrentRenderTarget.Clear();
            CluwneLib.CurrentRenderTarget.Draw(test);
           // CluwneLib.CurrentRenderTarget.Draw(625 , 5 , CluwneLib.CurrentRenderTarget.Size.X - 625 - 5 , CluwneLib.CurrentRenderTarget.Size.Y- 5 - 6, Color.SlateGray);

            // CluwneLib.CurrentRenderTarget.FilledRectangle(5, 220, 600, _lobbyChat.Position.Y - 250 - 5, Color.SlateGray);

            _lobbyText.Position = new Vector2(10, 10);
            _lobbyText.Text = "Server: " + _serverName;
            _lobbyText.Draw();
            _lobbyText.Position = new Vector2(10, 30);
            _lobbyText.Text = "Server-Port: " + _serverPort;
            _lobbyText.Draw();
            _lobbyText.Position = new Vector2(10, 50);
            _lobbyText.Text = "Max Players: " + _serverMaxPlayers;
            _lobbyText.Draw();
            _lobbyText.Position = new Vector2(10, 70);
            _lobbyText.Text = "Gamemode: " + _gameType;
            _lobbyText.Draw();
            _lobbyText.Position = new Vector2(10, 110);
            _lobbyText.Text = "MOTD: \n" + _welcomeString;
            _lobbyText.Draw();

            int pos = 225;
            foreach (string plrStr in _playerListStrings)
            {
                _lobbyText.Position = new Vector2(10, pos);
                _lobbyText.Text = plrStr;
                _lobbyText.Draw();
                pos += 20;
            }

            UserInterfaceManager.Render();
        }

        public void FormResize()
        {
            UserInterfaceManager.ResizeComponents();
        }

        public void Shutdown()
        {
            UserInterfaceManager.DisposeAllComponents();
            NetworkManager.MessageArrived -= NetworkManagerMessageArrived;
            //TODO RenderTargetCache.DestroyAll(); 
        }

        public void Update(FrameEventArgs e)
        {
            UserInterfaceManager.Update(e.FrameDeltaTime);
            if (_playerListTime.CompareTo(DateTime.Now) < 0)
            {
                NetOutgoingMessage playerListMsg = NetworkManager.CreateMessage();
                playerListMsg.Write((byte) NetMessage.PlayerList); // Request Playerlist.
                NetworkManager.SendMessage(playerListMsg, NetDeliveryMethod.ReliableOrdered);

                _playerListTime = DateTime.Now.AddSeconds(PlayerListRefreshDelaySec);
            }
        }

        #endregion

        private void JoinButtonClicked(Button sender)
        {
            PlayerManager.SendVerb("joingame", 0);
        }

        private void LobbyChatTextSubmitted(Chatbox chatbox, string text)
        {
            SendLobbyChat(text);
        }

        private void NetworkManagerMessageArrived(object sender, IncomingNetworkMessageArgs args)
        {
            NetIncomingMessage message = args.Message;
            switch (message.MessageType)
            {
                case NetIncomingMessageType.StatusChanged:
                    var statMsg = (NetConnectionStatus) message.ReadByte();
                    if (statMsg == NetConnectionStatus.Disconnected)
                    {
                        string disconnectMessage = message.ReadString();
                        UserInterfaceManager.AddComponent(new DisconnectedScreenBlocker(StateManager,
                                                                                        UserInterfaceManager,
                                                                                        ResourceManager,
                                                                                        disconnectMessage));
                    }
                    break;
                case NetIncomingMessageType.Data:
                    var messageType = (NetMessage) message.ReadByte();
                    switch (messageType)
                    {
                        case NetMessage.LobbyChat:
                            string text = message.ReadString();
                            AddChat(text);
                            break;
                        case NetMessage.PlayerCount:
                            //TODO var newCount = message.ReadByte();
                            break;
                        case NetMessage.PlayerList:
                            HandlePlayerList(message);
                            break;
                        case NetMessage.WelcomeMessage:
                            HandleWelcomeMessage(message);
                            break;
                        case NetMessage.ChatMessage:
                            HandleChatMessage(message);
                            break;
                        case NetMessage.JobList:
                            HandleJobList(message);
                            break;
                        case NetMessage.JobSelected:
                            HandleJobSelected(message);
                            break;
                        case NetMessage.JoinGame:
                            HandleJoinGame();
                            break;
                    }
                    break;
            }
        }

        private void HandleJobSelected(NetIncomingMessage msg)
        {
            string jobName = msg.ReadString();
            foreach (GuiComponent comp in _jobButtonContainer.components)
                ((JobSelectButton) comp).Selected = ((JobDefinition) comp.UserData).Name == jobName;
        }

        private void HandleJobList(NetIncomingMessage msg)
        {
            int byteNum = msg.ReadInt32();
            byte[] compressedXml = msg.ReadBytes(byteNum);

            string jobListXml = ZipString.UnZipStr(compressedXml);

            JobHandler.Singleton.LoadDefinitionsFromString(jobListXml);
            int pos = 5;
            _jobButtonContainer.components.Clear(); //Properly dispose old buttons !!!!!!!
            foreach (JobDefinition definition in JobHandler.Singleton.JobSettings.JobDefinitions)
            {
                var current = new JobSelectButton(definition.Name, definition.JobIcon, definition.Description,
                                                  ResourceManager)
                                  {
                                      Available = definition.Available,
                                      Position = new Point(5, pos)
                                  };

                current.Clicked += CurrentClicked;
                current.UserData = definition;
                _jobButtonContainer.components.Add(current);
                pos += current.ClientArea.Height + 20;
            }
        }

        private void CurrentClicked(JobSelectButton sender)
        {
            NetOutgoingMessage playerJobSpawnMsg = NetworkManager.CreateMessage();
            var picked = (JobDefinition) sender.UserData;
            playerJobSpawnMsg.Write((byte) NetMessage.RequestJob); //Request job.
            playerJobSpawnMsg.Write(picked.Name);
            NetworkManager.SendMessage(playerJobSpawnMsg, NetDeliveryMethod.ReliableOrdered);
        }

        private void HandlePlayerList(NetIncomingMessage msg)
        {
            byte playerCount = msg.ReadByte();
            _playerListStrings.Clear();
            for (int i = 0; i < playerCount; i++)
            {
                string currName = msg.ReadString();
                var currStatus = (SessionStatus) msg.ReadByte();
                float currRoundtrip = msg.ReadFloat();
                _playerListStrings.Add(currName + "\t\tStatus: " + currStatus + "\t\tLatency: " +
                                       Math.Truncate(currRoundtrip*1000) + " ms");
            }
        }

        private void HandleJoinGame()
        {
            StateManager.RequestStateChange<GameScreen>();
        }

        private void AddChat(string text)
        {
            _lobbyChat.AddLine(text, ChatChannel.Lobby);
        }

        public void SendLobbyChat(string text)
        {
            NetOutgoingMessage message = NetworkManager.CreateMessage();
            message.Write((byte) NetMessage.ChatMessage);
            message.Write((byte) ChatChannel.Lobby);
            message.Write(text);

            NetworkManager.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        private void HandleWelcomeMessage(NetIncomingMessage msg)
        {
            _serverName = msg.ReadString();
            _serverPort = msg.ReadInt32();
            _welcomeString = msg.ReadString();
            _serverMaxPlayers = msg.ReadInt32();
            _serverMapName = msg.ReadString();
            _gameType = msg.ReadString();
        }

        private void HandleChatMessage(NetIncomingMessage msg)
        {
            var channel = (ChatChannel) msg.ReadByte();
            string text = msg.ReadString();
            string message = "[" + channel + "] " + text;
            _lobbyChat.AddLine(message, ChatChannel.Lobby);
        }

        #region Input
        public void KeyDown ( KeyEventArgs e )
        {
            UserInterfaceManager.KeyDown(e);
        }

        public void KeyUp ( KeyEventArgs e )
        {
        }

        public void MouseUp ( MouseButtonEventArgs e )
        {
            UserInterfaceManager.MouseUp(e);
        }

        public void MouseDown ( MouseButtonEventArgs e )
        {
            UserInterfaceManager.MouseDown(e);
        }

        public void MouseMoved ( MouseMoveEventArgs e )
        {

        }
        public void MousePressed ( MouseButtonEventArgs e )
        {
            UserInterfaceManager.MouseDown(e);
        }
        public void MouseMove ( MouseMoveEventArgs e )
        {
            UserInterfaceManager.MouseMove(e);
        }

        public void MouseWheelMove ( MouseWheelEventArgs e )
        {
            UserInterfaceManager.MouseWheelMove(e);
        }

        #endregion
    }
}