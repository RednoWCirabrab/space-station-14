﻿using SS14.Client.Graphics;
using SS14.Client.Graphics.Render;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Services.Helpers;
using SS14.Shared.IoC;
using System;
using System.Drawing;

namespace SS14.Client.Services.Player.PostProcessing
{
    public class BlurPostProcessingEffect : PostProcessingEffect
    {
        private readonly GaussianBlur _gaussianBlur = new GaussianBlur(IoCManager.Resolve<IResourceManager>());

        public BlurPostProcessingEffect(float duration)
            : base(duration)
        {
        }

        public override void ProcessImage(RenderImage image)
        {
            if (_duration < 3)
                _gaussianBlur.SetRadius(3);
            else if (_duration < 10)
                _gaussianBlur.SetRadius(5);
            else
                _gaussianBlur.SetRadius(7);

            _gaussianBlur.SetSize(new SizeF(image.Height, image.Height));
            _gaussianBlur.SetAmount(Math.Min(_duration/2, 3f));
            _gaussianBlur.PerformGaussianBlur(image);
        }
    }
}