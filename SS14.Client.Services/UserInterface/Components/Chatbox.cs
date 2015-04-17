﻿using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SFML.Window;
using SS14.Client.Graphics;

namespace SS14.Client.Services.UserInterface.Components
{
    public class Chatbox : GuiComponent
    {
        #region Delegates

        public delegate void TextSubmitHandler(Chatbox chatbox, string text);

        #endregion

        private const int MaxHistory = 20;
        private const int MaxLines = 10;
        private const int MaxLinePixelLength = 450;
        private readonly Dictionary<ChatChannel, Color> _chatColors;
        private readonly StringBuilder _currentInputText = new StringBuilder();

        private readonly IList<Label> _entries = new List<Label>();
        private readonly IList<String> _inputHistory = new List<String>();
        private readonly IKeyBindingManager _keyBindingManager;
        private readonly IResourceManager _resourceManager;
        private readonly IUserInterfaceManager _userInterfaceManager;

        public Vector2 Size = new Vector2(475, 175);

        private bool _active;
        private bool _disposing;
        private int _inputIndex = -1;
        private string _inputTemp;
        private Label _textInputLabel;

        public Chatbox(IResourceManager resourceManager, IUserInterfaceManager userInterfaceManager,
                       IKeyBindingManager keyBindingManager)
        {
            _resourceManager = resourceManager;
            _userInterfaceManager = userInterfaceManager;
            _keyBindingManager = keyBindingManager;

            Position = new Point((int)CluwneLib.CurrentClippingViewport.Width - (int)Size.X - 10, 10);

            ClientArea = new Rectangle(Position.X, Position.Y, (int) Size.X, (int) Size.Y);

            _textInputLabel = new Label("", "CALIBRI", _resourceManager)
                                  {
                                      Text =
                                          {
                                              Size = new Size(ClientArea.Width - 10, 12),
                                              Color = Color.Green
                                          }
                                  };

            _chatColors = new Dictionary<ChatChannel, Color>
                              {
                                  {ChatChannel.Default, Color.Gray},
                                  {ChatChannel.Damage, Color.Red},
                                  {ChatChannel.Radio, Color.DarkGreen},
                                  {ChatChannel.Server, Color.Blue},
                                  {ChatChannel.Player, Color.Green},
                                  {ChatChannel.Lobby, Color.White},
                                  {ChatChannel.Ingame, Color.Green},
                                  {ChatChannel.OOC, Color.White},
                                  {ChatChannel.Emote, Color.Cyan},
                                  {ChatChannel.Visual, Color.Yellow},
                              };
        }

        private bool Active
        {
            get { return _active; }
            set
            {
                _active = value;
                _keyBindingManager.Enabled = !_active;
            }
        }

        public event TextSubmitHandler TextSubmitted;

        private IEnumerable<string> CheckInboundMessage(string message)
        {
            var lineList = new List<string>();

            if (_textInputLabel.Text.MeasureLine(message) < MaxLinePixelLength)
            {
                lineList.Add(message);
                return lineList;
            }

            Match match = Regex.Match(message, @"^\[.+\]\s.+\:\s", RegexOptions.Singleline);
            string header = match.ToString();
            message = message.Substring(match.Length);

            List<string> stringChunks = message.Split(new[] {' ', '-'}).ToList();
            int totalChunks = stringChunks.Count();
            int i = 0;

            lineList.Add(header);

            while (_textInputLabel.Text.MeasureLine(lineList[i]) < MaxLinePixelLength)
            {
                if (!stringChunks.Any()) break;

                if (_textInputLabel.Text.MeasureLine(lineList[i] + stringChunks.First()) < MaxLinePixelLength)
                {
                    lineList[i] += stringChunks.First() + " ";
                    stringChunks.RemoveAt(0);
                }
                else if ((i == 0 && totalChunks == stringChunks.Count()) ||
                         (lineList[i] == " " &&
                          _textInputLabel.Text.MeasureLine(stringChunks.First() + " ") > MaxLinePixelLength) ||
                         (_textInputLabel.Text.MeasureLine(lineList[i]) < MaxLinePixelLength &&
                          _textInputLabel.Text.MeasureLine(stringChunks.First() + " ") > MaxLinePixelLength))
                {
                    List<char> largeWordChars = stringChunks.First().ToList();
                    stringChunks.RemoveAt(0);

                    while (_textInputLabel.Text.MeasureLine(lineList[i] + largeWordChars.First() + "-") <
                           MaxLinePixelLength)
                    {
                        lineList[i] += largeWordChars.First();
                        largeWordChars.RemoveAt(0);
                    }

                    lineList[i] += "-";
                    lineList.Add(" " + new string(largeWordChars.ToArray()) + " ");
                    i++;
                }
                else
                {
                    lineList.Add(" ");
                    i++;
                }
            }

            return lineList;
        }

        public void AddLine(string message, ChatChannel channel)
        {
            if (_disposing) return;

            IEnumerable<string> messageSplit = CheckInboundMessage(message);

            foreach (Label label in messageSplit.Select(part => new Label(part, "CALIBRI", _resourceManager)
                                                                    {
                                                                        Text =
                                                                            {
                                                                                Size =
                                                                                    new Size(ClientArea.Width - 10, 12),
                                                                                Color = _chatColors[channel]
                                                                            }
                                                                    }))
            {
                _entries.Add(label);
            }

            DrawLines();
        }

        private void DrawLines()
        {
            CheckAndSetLine(_currentInputText.ToString());

            _textInputLabel.Position = new Point(ClientArea.X + 4, ClientArea.Y + ClientArea.Height - 23);
            _textInputLabel.Render();

            while (_entries.Count > MaxLines)
                _entries.RemoveAt(0);

            int start = Math.Max(0, _entries.Count - 12);

            for (int i = _entries.Count - 1; i >= start; i--)
            {
                _entries[i].Position = new Point(ClientArea.X + 2,
                                                 ClientArea.Y + ClientArea.Height - (14*(_entries.Count - i)) - 26);
                _entries[i].Render();
            }
        }

        private void CheckAndSetLine(string line)
        {
            // TODO: Refactor to use dynamic input box size.
            // Magic number is pixel width of chatbox input rectangle at
            // current resolution. Will need to modify once handling
            // different resolutions and scaling.
            if (_textInputLabel.Text.MeasureLine(line) > MaxLinePixelLength)
            {
                CheckAndSetLine(line.Substring(1));
            }
            else
            {
                _textInputLabel.Text.Text = line;
            }
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            if (e.Code == Keyboard.Key.T && !Active)
            {
                _userInterfaceManager.SetFocus(this);
                Active = true;
                return true;
            }

            if (!Active)
                return false;

            if (e.Code == Keyboard.Key.Return)
            {
                if (TextSubmitted != null && !String.IsNullOrWhiteSpace(_currentInputText.ToString()))
                {
                    TextSubmitted(this, _currentInputText.ToString());
                    _inputHistory.Insert(0, _currentInputText.ToString());

                    while (_inputHistory.Count() > MaxHistory)
                    {
                        _inputHistory.RemoveAt(MaxHistory);
                    }
                }

                _inputIndex = -1;
                _currentInputText.Clear();

                Active = false;
                return true;
            }

            if (e.Code == Keyboard.Key.Up)
            {
                if (_inputIndex == -1 && _inputHistory.Any())
                {
                    _inputTemp = _currentInputText.ToString();
                    _inputIndex++;
                }
                else if (_inputIndex + 1 < _inputHistory.Count())
                {
                    _inputIndex++;
                }


                if (_inputIndex != -1)
                {
                    _currentInputText.Clear();
                    _currentInputText.Append(_inputHistory[_inputIndex]);
                }

                return true;
            }

            if (e.Code == Keyboard.Key.Down)
            {
                if (_inputIndex == 0)
                {
                    _currentInputText.Clear();
                    _currentInputText.Append(_inputTemp);
                    _inputTemp = "";
                    _inputIndex--;
                }
                else if (_inputIndex != -1)
                {
                    _inputIndex--;
                    _currentInputText.Clear();
                    _currentInputText.Append(_inputHistory[_inputIndex]);
                }
            }

            if (e.Code == Keyboard.Key.BackSpace)
            {
                if (_currentInputText.Length > 0)
                    _currentInputText.Remove(_currentInputText.Length - 1, 1);
                return true;
            }

            

            return true;
        }

        public override void Dispose()
        {
            _disposing = true;
            TextSubmitted = null;
            _entries.Clear();
            _textInputLabel = null;
            _chatColors.Clear();
            base.Dispose();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            ClientArea = new Rectangle(Position.X, Position.Y, (int) Size.X, (int) Size.Y);
            // _textInputLabel.Text.Scale = new SFML.System.Vector2f (ClientArea.Width - 10, 12);
            _textInputLabel.Update(frameTime);
            foreach (Label l in _entries) l.Update(frameTime);
        }

        public override void Render()
        {
            if (_disposing || !IsVisible()) return;
            CluwneLib.BlendingMode = BlendingModes.Modulated;
            CluwneLib.drawRectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height, Color.FromArgb(100, Color.Black));
            CluwneLib.drawRectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height, Color.FromArgb(100, Color.LightGray));
            CluwneLib.BlendingMode = BlendingModes.None;
            DrawLines();
        }
    }
}