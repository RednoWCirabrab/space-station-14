﻿using SS14.Client.Interfaces.Resource;
using System;
using Color = System.Drawing.Color;
using System.Drawing;
using SS14.Client.Graphics.Sprite;
using SFML.Window;
using SS14.Client.Graphics;


namespace SS14.Client.Services.UserInterface.Components
{
    internal class Textbox : GuiComponent
    {
        #region Delegates

        public delegate void TextSubmitHandler(string text, Textbox sender);

        #endregion

        private readonly IResourceManager _resourceManager;
        public bool ClearFocusOnSubmit = true;
        public bool ClearOnSubmit = true;

        public TextSprite Label;
        public int MaxCharacters = 255;
        public int Width;
        private float _caretHeight = 12;
        private int _caretIndex;
        private float _caretPos;
        private float _caretWidth = 2;

        private Rectangle _clientAreaLeft;
        private Rectangle _clientAreaMain;
        private Rectangle _clientAreaRight;

        private int _displayIndex;

        private string _displayText = "";
        private string _text = "";
		private CluwneSprite _textboxLeft;
		private CluwneSprite _textboxMain;
		private CluwneSprite _textboxRight;

        public Color drawColor = Color.White;
        public Color textColor = Color.Black;

        private float blinkCount;

        public Textbox(int width, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            _textboxLeft = _resourceManager.GetSprite("text_left");
            _textboxMain = _resourceManager.GetSprite("text_middle");
            _textboxRight = _resourceManager.GetSprite("text_right");

            Width = width;

            Label = new TextSprite("Textbox", "", _resourceManager.GetFont("CALIBRI")) {Color =  Color.Black};

            Update(0);
        }

        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                SetVisibleText();
            }
        }

        public event TextSubmitHandler OnSubmit;

        public override void Update(float frameTime)
        {
            _clientAreaLeft = new Rectangle(Position, new Size((int) _textboxLeft.Width, (int) _textboxLeft.Height));
            _clientAreaMain = new Rectangle(new Point(_clientAreaLeft.Right, Position.Y),
                                            new Size(Width, (int) _textboxMain.Height));
            _clientAreaRight = new Rectangle(new Point(_clientAreaMain.Right, Position.Y),
                                             new Size((int) _textboxRight.Width, (int) _textboxRight.Height));
            ClientArea = new Rectangle(Position,
                                       new Size(_clientAreaLeft.Width + _clientAreaMain.Width + _clientAreaRight.Width,
                                                Math.Max(Math.Max(_clientAreaLeft.Height, _clientAreaRight.Height),
                                                         _clientAreaMain.Height)));
            Label.Position = new Point(_clientAreaLeft.Right,
                                       Position.Y + (int) (ClientArea.Height/2f) - (int) (Label.Height/2f));

            if (Focus)
            {
                blinkCount += 1*frameTime;
                if (blinkCount > 0.50f) blinkCount = 0;
            }
        }

        public override void Render()
        {
            if (drawColor != Color.White)
            {
                _textboxLeft.Color = drawColor.ToSFMLColor();
                _textboxMain.Color = drawColor.ToSFMLColor();
                _textboxRight.Color = drawColor.ToSFMLColor();
            }

            _textboxLeft.Draw(_clientAreaLeft);
            _textboxMain.Draw(_clientAreaMain);
            _textboxRight.Draw(_clientAreaRight);

            if (Focus && blinkCount <= 0.25f)
                //Draw Textbox

         // CluwneLib.CurrentRenderTarget.Draw(_caretPos - _caretWidth, Label.Position.Y + (Label.Height/2f) - (_caretHeight/2f),_caretWidth, _caretHeight, new Color(255,255,250));

            if (drawColor != Color.White)
            {
                _textboxLeft.Color = Color.White.ToSFMLColor();
                _textboxMain.Color = Color.White.ToSFMLColor();
                _textboxRight.Color = Color.White.ToSFMLColor();
            }

            Label.Color = textColor;
            Label.Text = _displayText;
            Label.Draw();
        }

        public override void Dispose()
        {
            Label = null;
            _textboxLeft = null;
            _textboxMain = null;
            _textboxRight = null;
            OnSubmit = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

		public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.X, (int) e.Y)))
            {
                return true;
            }

            return false;
        }

		public override bool MouseUp(MouseButtonEventArgs e)
        {
            return false;
        }

		public override bool KeyDown(KeyEventArgs e)
        {
            if (!Focus) return false;

            if (e.Control && e.Code == Keyboard.Key.V)
            {
             
                    string ret = System.Windows.Forms.Clipboard.GetText();
                    Text = Text.Insert(_caretIndex, ret);
                    if (_caretIndex < _text.Length) _caretIndex += ret.Length;
                    SetVisibleText();
                    return true;
            }

            if (e.Control && e.Code == Keyboard.Key.C)
            {
               
                System.Windows.Forms.Clipboard.SetText(Text);
                return true;
            }

            if (e.Code == Keyboard.Key.Left)
            {
                if (_caretIndex > 0) _caretIndex--;
                SetVisibleText();
                return true;
            }
            else if (e.Code == Keyboard.Key.Right)
            {
                if (_caretIndex < _text.Length) _caretIndex++;
                SetVisibleText();
                return true;
            }

            if (e.Code == Keyboard.Key.Return && Text.Length >= 1)
            {
                Submit();
                return true;
            }

            if (e.Code == Keyboard.Key.BackSpace && Text.Length >= 1)
            {
                if (_caretIndex == 0) return true;

                Text = Text.Remove(_caretIndex - 1, 1);
                if (_caretIndex > 0 && _caretIndex < Text.Length) _caretIndex--;
                SetVisibleText();
                return true;
            }

            if (e.Code == Keyboard.Key.Delete && Text.Length >= 1)
            {
                if (_caretIndex >= Text.Length) return true;
                Text = Text.Remove(_caretIndex, 1);
                SetVisibleText();
                return true;
            }

           
            return false;
        }

        private void SetVisibleText()
        {
            _displayText = "";

            if (Label.MeasureLine(_text) >= _clientAreaMain.Width) //Text wider than box.
            {
                if (_caretIndex < _displayIndex)
                    //Caret outside to the left. Move display text to the left by setting its index to the caret.
                    _displayIndex = _caretIndex;

                int glyphCount = 0;

                while (_displayIndex + (glyphCount + 1) < _text.Length &&
                       Label.MeasureLine(Text.Substring(_displayIndex + 1, glyphCount + 1)) < _clientAreaMain.Width)
                    glyphCount++; //How many glyphs we could/would draw with the current index.

                if (_caretIndex > _displayIndex + glyphCount) //Caret outside?
                {
                    if (_text.Substring(_displayIndex + 1).Length != glyphCount) //Still stuff outside the screen?
                    {
                        _displayIndex++;
                        //Increase display index by one since the carret is one outside to the right. But only if there's still letters to the right.

                        glyphCount = 0; //Update glyphcount with new index.

                        while (_displayIndex + (glyphCount + 1) < _text.Length &&
                               Label.MeasureLine(Text.Substring(_displayIndex + 1, glyphCount + 1)) <
                               _clientAreaMain.Width)
                            glyphCount++;
                    }
                }
                _displayText = Text.Substring(_displayIndex + 1, glyphCount);

                _caretPos = Label.Position.X +
                            Label.MeasureLine(Text.Substring(_displayIndex, _caretIndex - _displayIndex));
            }
            else //Text fits completely inside box.
            {
                _displayIndex = 0;
                _displayText = Text;

                if (Text.Length <= _caretIndex - 1)
                    _caretIndex = Text.Length;
                _caretPos = Label.Position.X +
                            Label.MeasureLine(Text.Substring(_displayIndex, _caretIndex - _displayIndex));
            }
        }

        private void Submit()
        {
            if (OnSubmit != null) OnSubmit(Text, this);
            if (ClearOnSubmit)
            {
                Text = string.Empty;
                _displayText = string.Empty;
            }
            if (ClearFocusOnSubmit) Focus = false;
        }
    }
}