﻿using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.State;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Services.State.States;
using System.Drawing;
using SFML.Window;
using SS14.Client.Graphics;

namespace SS14.Client.Services.UserInterface.Components
{
    public class DisconnectedScreenBlocker : GuiComponent
    {
        private readonly Button _mainMenuButton;
        private readonly Label _message;
        private readonly IResourceManager _resourceManager;
        private readonly IStateManager _stateManager;
        private readonly IUserInterfaceManager _userInterfaceManager;

        public DisconnectedScreenBlocker(IStateManager stateManager, IUserInterfaceManager userInterfaceManager,
                                         IResourceManager resourceManager, string message = "Connection closed.")
        {
            _stateManager = stateManager;
            _resourceManager = resourceManager;
            _userInterfaceManager = userInterfaceManager;
            _userInterfaceManager.DisposeAllComponents();

            _message = new Label(message, "CALIBRI", _resourceManager);
            _mainMenuButton = new Button("Main Menu", _resourceManager);
            _mainMenuButton.Clicked += MainMenuButtonClicked;
            _mainMenuButton.Label.Color = Color.WhiteSmoke;
            _message.Text.Color = Color.WhiteSmoke;
        }

        private void MainMenuButtonClicked(Button sender)
        {
            _stateManager.RequestStateChange<MainScreen>();
        }

        public override void Update(float frameTime)
        {
            _message.Position = new Point((int) (CluwneLib.CurrentRenderTarget.Size.X/2f - _message.ClientArea.Width/2f),
                                          (int) (CluwneLib.CurrentRenderTarget.Size.Y/2f - _message.ClientArea.Height/2f) -
                                          50);
            _message.Update(frameTime);
            _mainMenuButton.Position =
                new Point((int) (CluwneLib.CurrentRenderTarget.Size.X/2f - _message.ClientArea.Width/2f),
                          _message.ClientArea.Bottom + 20);
            _mainMenuButton.Update(frameTime);
        }

        public override void Render()
        {
            CluwneLib.drawRectangle(0, 0, (int)CluwneLib.CurrentRenderTarget.Size.X,  (int)CluwneLib.CurrentRenderTarget.Size.Y, Color.Black);
            _message.Render();
            _mainMenuButton.Render();
        }

        public override void Dispose()
        {
            _message.Dispose();
            _mainMenuButton.Dispose();
            base.Dispose();
        }

		public override bool MouseDown(MouseButtonEventArgs e)
        {
            _mainMenuButton.MouseDown(e);
            return true;
        }

		public override bool MouseUp(MouseButtonEventArgs e)
        {
            _mainMenuButton.MouseUp(e);
            return true;
        }

		public override void MouseMove(MouseMoveEventArgs e)
        {
            _mainMenuButton.MouseMove(e);
        }

		public override bool MouseWheelMove(MouseWheelEventArgs e)
        {
            return true;
        }

		public override bool KeyDown(KeyEventArgs e)
        {
            _mainMenuButton.KeyDown(e);
            return true;
        }
    }
}