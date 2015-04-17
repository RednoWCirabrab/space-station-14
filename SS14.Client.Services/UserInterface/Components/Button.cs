﻿using SS14.Client.Interfaces.Resource;
using System;
using System.Drawing;
using SS14.Client.Graphics.Sprite;
using SFML.Window;

namespace SS14.Client.Services.UserInterface.Components
{
    internal class Button : GuiComponent
    {
        #region Delegates

        public delegate void ButtonPressHandler(Button sender);

        #endregion

        private readonly IResourceManager _resourceManager;

		private CluwneSprite _buttonLeft;
		private CluwneSprite _buttonMain;
		private CluwneSprite _buttonRight;

        private Rectangle _clientAreaLeft;
        private Rectangle _clientAreaMain;
        private Rectangle _clientAreaRight;

        private Color drawColor = Color.White;
        public Color mouseOverColor = Color.White;

        public Button(string buttonText, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            _buttonLeft = _resourceManager.GetSprite("button_left");
            _buttonMain = _resourceManager.GetSprite("button_middle");
            _buttonRight = _resourceManager.GetSprite("button_right");

            Label = new TextSprite("ButtonLabel" + buttonText, buttonText, _resourceManager.GetFont("CALIBRI"))
                        {
                            Color = Color.Black
                        };

            Update(0);
        }

        public TextSprite Label { get; private set; }

        public event ButtonPressHandler Clicked;

        public override sealed void Update(float frameTime)
        {
            _clientAreaLeft = new Rectangle(Position, new Size((int) _buttonLeft.Width, (int) _buttonLeft.Height));
            _clientAreaMain = new Rectangle(new Point(_clientAreaLeft.Right, Position.Y),
                                            new Size((int) Label.Width, (int) _buttonMain.Height));
            _clientAreaRight = new Rectangle(new Point(_clientAreaMain.Right, Position.Y),
                                             new Size((int) _buttonRight.Width, (int) _buttonRight.Height));
            ClientArea = new Rectangle(Position,
                                       new Size(_clientAreaLeft.Width + _clientAreaMain.Width + _clientAreaRight.Width,
                                                Math.Max(Math.Max(_clientAreaLeft.Height, _clientAreaRight.Height),
                                                         _clientAreaMain.Height)));
            Label.Position = new Point(_clientAreaLeft.Right,
                                       Position.Y + (int) (ClientArea.Height/2f) - (int) (Label.Height/2f));
        }

        public override void Render()
        {
            _buttonLeft.Color = new SFML.Graphics.Color(drawColor.R, drawColor.G,drawColor.B);
            _buttonMain.Color = new SFML.Graphics.Color(drawColor.R, drawColor.G, drawColor.B);
            _buttonRight.Color = new SFML.Graphics.Color(drawColor.R, drawColor.G,drawColor.B);

            _buttonLeft.Draw(_clientAreaLeft);
            _buttonMain.Draw(_clientAreaMain);
            _buttonRight.Draw(_clientAreaRight);

            _buttonLeft.Color = new SFML.Graphics.Color(Color.White.R, Color.White.G, Color.White.B);
            _buttonMain.Color = new SFML.Graphics.Color(Color.White.R, Color.White.G, Color.White.B);
            _buttonRight.Color = new SFML.Graphics.Color(Color.White.R, Color.White.G, Color.White.B);

            Label.Draw();
        }

        public override void Dispose()
        {
            Label = null;
            _buttonLeft = null;
            _buttonMain = null;
            _buttonRight = null;
            Clicked = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

		public override void MouseMove(MouseMoveEventArgs e)
        {
            if (mouseOverColor != Color.White)
                if (ClientArea.Contains(new Point((int) e.X, (int) e.Y)))
                    drawColor = mouseOverColor;
                else
                    drawColor = Color.White;
        }

		public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.X, (int) e.Y)))
            {
                if (Clicked != null) Clicked(this);
                return true;
            }
            return false;
        }

		public override bool MouseUp(MouseButtonEventArgs e)
        {
            return false;
        }
    }
}