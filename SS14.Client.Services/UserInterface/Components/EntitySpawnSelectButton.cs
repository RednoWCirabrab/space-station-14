﻿using SS14.Client.Interfaces.Resource;
using SS14.Client.Graphics.Sprite;
using SS14.Shared.GameObjects;
using System;
using System.Drawing;
using System.Linq;
using Font = SFML.Graphics.Font;
using SFML.Window;
using SS14.Shared.Maths;
using SS14.Client.Graphics;

namespace SS14.Client.Services.UserInterface.Components
{
    public class EntitySpawnSelectButton : GuiComponent
    {
        #region Delegates

        public delegate void EntitySpawnSelectPress(
            EntitySpawnSelectButton sender, EntityTemplate template, string templateName);

        #endregion

        private readonly IResourceManager _resourceManager;
        private readonly EntityTemplate associatedTemplate;
        private readonly string associatedTemplateName;
        private readonly Font font;

        private readonly TextSprite name;
		private readonly CluwneSprite objectSprite;

        public int fixed_width = -1;
        public Boolean selected = false;

        public EntitySpawnSelectButton(EntityTemplate entityTemplate, string templateName,
                                       IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;

            var spriteNameParam = entityTemplate.GetBaseSpriteParamaters().FirstOrDefault();
            string SpriteName = "";
            if (spriteNameParam != null)
            {
                SpriteName = spriteNameParam.GetValue<string>();
            }
            string ObjectName = entityTemplate.Name;

            associatedTemplate = entityTemplate;
            associatedTemplateName = templateName;

            objectSprite = _resourceManager.GetSprite(SpriteName);

            font = _resourceManager.GetFont("CALIBRI");
            name = new TextSprite("Label" + SpriteName, "Name", font);
            name.Color = Color.Black;
            name.Text = ObjectName;
        }

        public event EntitySpawnSelectPress Clicked;

		public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.X, (int) e.Y)))
            {
                if (Clicked != null) Clicked(this, associatedTemplate, associatedTemplateName);
                return true;
            }
            return false;
        }

		public override bool MouseUp(MouseButtonEventArgs e)
        {
            return false;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            objectSprite.Position = new Vector2(Position.X + 5, Position.Y + 5);
            name.Position = new Vector2(objectSprite.Position.X + objectSprite.Width + 5, objectSprite.Position.Y);
            ClientArea = new Rectangle(Position,
                                       new Size(
                                           fixed_width != -1
                                               ? fixed_width
                                               : ((int) objectSprite.Width + (int) name.Width + 15),
                                           ((int) objectSprite.Height > (int) name.Height
                                                ? (int) objectSprite.Height
                                                : ((int) name.Height + 5)) + 10));
        }

        public override void Render()
        {
           CluwneLib.drawRectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height,
                                                       selected ? Color.ForestGreen : Color.FloralWhite);
           CluwneLib.drawRectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height,
                                                 Color.Black);
            objectSprite.Draw();
            name.Draw();
        }
    }
}