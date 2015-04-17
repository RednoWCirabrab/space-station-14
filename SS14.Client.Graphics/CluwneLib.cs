﻿using System;
using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics.Event;
using SS14.Client.Graphics.Render;
using SS14.Client.Graphics.Timing;
using SystemColor = System.Drawing.Color;
using SFMLColor = SFML.Graphics.Color;
using SS14.Client.Graphics.Shader;
using SS14.Shared.Maths;
using System.Drawing;
using SFML.Window;
using System.Collections.Generic;
using System.Collections;

namespace SS14.Client.Graphics
{
    public class CluwneDebug {
        public int RenderingDelay=0;
        public bool TextBorders=false;
        public uint Fontsize=0;
    };
    public class CluwneLib
    {
        public static Viewport CurrentClippingViewport;
        private static Clock _timer;
        private static RenderTarget[] _currentTarget;
        private static System.Threading.Mutex SFML_Threadlock;
        public static event FrameEventHandler Idle;
        private SystemColor DEFAULTCOLOR;
        public static CluwneDebug Debug;

        #region Accessors
        public static bool IsInitialized { get; set; }
        public static bool IsRunning { get; set; }
        public static CluwneWindow Screen {  get;  set; }
        public static TimingData FrameStats { get; set; }
        public static FXShader CurrentShader { get; set; }
        public static BlendingModes BlendingMode { get; set; }
        public Styles Style { get; set; }
        #endregion

        #region CluwneEngine
        /// <summary>
        /// Start engine rendering.
        /// </summary>
        /// Shamelessly taken from Gorgon.
        public static void Go()
        {
            SFML_Threadlock = new System.Threading.Mutex();

            if (!IsInitialized)
            {
                Initialize();
            }

            Idle += (delegate(object sender, FrameEventArgs e) {
               
                System.Threading.Thread.Sleep(10); // maybe pickup vsync here?
               
            });

            if ((Screen != null) && (_currentTarget == null))
                throw new InvalidOperationException("The render target is invalid.");

            if (IsRunning)
                return;

            _timer.Restart();
            FrameStats.Reset();

            if (_currentTarget != null)
            {
                for (int i = 0; i < _currentTarget.Length; i++)
                {
                    if (_currentTarget[0] != null)
                    {
                       //update targets and viewport
                    }
                }

            }

            IsRunning = true;
        }

        public static void Initialize()
        {
            if (IsInitialized)
                Terminate();

            Debug = new CluwneDebug();

            IsInitialized = true;

            _currentTarget = new RenderTarget[5];

            _timer = new Clock();
            FrameStats = new TimingData(_timer);
        }

        public static void RequestGC(Action action)
        {
          action.Invoke();         
        }

        public static void SetMode(int displayWidth, int displayHeight)
        {
            Screen = new CluwneWindow(new VideoMode((uint)displayWidth, (uint)displayHeight), "Space station 14");
        }

        public static void SetMode(int width, int height, bool fullscreen, bool p4, bool p5, int refreshRate)
        {
            Styles stylesTemp = new Styles();

            if(fullscreen)
                stylesTemp = Styles.Fullscreen;
            else stylesTemp = Styles.Default;

            Screen = new CluwneWindow(new VideoMode((uint)width, (uint)height),"Space Station 14",stylesTemp);
        }

        public static void Clear(SystemColor color)
        {
            CurrentRenderTarget.Clear(color.ToSFMLColor());
        }

        public static void Terminate()
        {
            
        }

        public static void RunIdle(object sender, FrameEventArgs e)
        {
            Idle(sender, e);
        }

        public static void Stop()
        {
            Console.WriteLine("CluwneLib: Stop() requested");
            IsRunning=false;
        }

        #endregion

        #region RenderTarget Stuff

        public static RenderTarget CurrentRenderTarget
        {
            get
            {
                if (_currentTarget[0] == null)
                    _currentTarget[0] = Screen;

                return _currentTarget[0];
            }
            set
            {
                if (value == null)
                    value = Screen;

                setAdditionalRenderTarget(0, value);
            }
        }

        public static void setAdditionalRenderTarget(int index, RenderTarget _target)
        {
           _currentTarget[index] = _target;
        }

        public static RenderTarget getAdditionalRenderTarget(int index)
        {
            return _currentTarget[index];
        }

        #endregion


        #region Drawing Methods

        #region Rectangle

        /// <summary>
        /// Draws a Rectangle to the current RenderTarget
        /// </summary>
        /// <param name="posX">Pos X of rectangle </param>
        /// <param name="posY"> Pos Y of rectangle </param>
        /// <param name="WidthX"> Width X of rectangle </param>
        /// <param name="HeightY"> Height Y of rectangle </param>
        /// <param name="Color"> Fill Color </param>
        public static void drawRectangle(int posX, int posY, int WidthX, int HeightY, SystemColor Color)
        {
            RectangleShape rectangle = new RectangleShape();
            rectangle.Position = new SFML.System.Vector2f(posX, posY);
            rectangle.Size = new SFML.System.Vector2f(WidthX, HeightY);
            rectangle.FillColor = Color.ToSFMLColor();

            CurrentRenderTarget.Draw(rectangle);
            if (CluwneLib.Debug.RenderingDelay > 0)
            {
                CluwneLib.Screen.Display();
                System.Threading.Thread.Sleep(CluwneLib.Debug.RenderingDelay);
            }
        }

        /// <summary>
        /// Draws a Hollow Rectangle to the Current RenderTarget
        /// </summary>
        /// <param name="posX"> Pos X of rectangle </param>
        /// <param name="posY"> Pos Y of rectangle </param>
        /// <param name="widthX"> Width X of rectangle </param>
        /// <param name="heightY"> Height Y of rectangle </param>
        /// <param name="OutlineThickness"> Outline Thickness of rectangle </param>
        /// <param name="OutlineColor"> Outline Color </param>
        public static void drawHollowRectangle(int posX, int posY, int widthX, int heightY, float OutlineThickness, SystemColor OutlineColor)
        {
            RectangleShape HollowRect = new RectangleShape();
            HollowRect.FillColor = SystemColor.Transparent.ToSFMLColor();
            HollowRect.Position = new Vector2f(posX, posY);
            HollowRect.Size = new Vector2f(widthX, heightY);
            HollowRect.OutlineThickness = OutlineThickness;
            HollowRect.OutlineColor = OutlineColor.ToSFMLColor();

            CurrentRenderTarget.Draw(HollowRect);
            if (CluwneLib.Debug.RenderingDelay > 0)
            {
                CluwneLib.Screen.Display();
                System.Threading.Thread.Sleep(CluwneLib.Debug.RenderingDelay);
            }


        }
        #endregion

        #region Circle
        /// <summary>
        /// Draws a Filled Circle to the CurrentRenderTarget
        /// </summary>
        /// <param name="posX"> Pos X of Circle</param>
        /// <param name="posY"> Pos Y of Circle </param>
        /// <param name="radius"> Radius of Circle </param>
        /// <param name="color"> Fill Color </param>
        public static void drawCircle(int posX, int posY, int radius, SystemColor color)
        {
            CircleShape Circle = new CircleShape();
            Circle.Position = new Vector2(posX, posY);
            Circle.Radius = radius;
            Circle.FillColor = color.ToSFMLColor();

            CurrentRenderTarget.Draw(Circle);


        }
        /// <summary>
        /// Draws a Hollow Circle to the CurrentRenderTarget
        /// </summary>
        /// <param name="posX"> Pos X of Circle </param>
        /// <param name="posY"> Pos Y of Circle </param>
        /// <param name="radius"> Radius of Circle </param>
        /// <param name="OutlineThickness"> Thickness of Circle Outline </param>
        /// <param name="OutlineColor"> Circle outline Color </param>
        public static void drawHollowCircle(int posX, int posY, int radius,float OutlineThickness ,SystemColor OutlineColor)
        {
            CircleShape Circle = new CircleShape();
            Circle.Position = new Vector2(posX, posY);
            Circle.Radius = radius;
            Circle.FillColor = SystemColor.Transparent.ToSFMLColor();
            Circle.OutlineThickness = OutlineThickness;
            Circle.OutlineColor = OutlineColor.ToSFMLColor();

            CurrentRenderTarget.Draw(Circle);
        }

        /// <summary>
        /// Draws a Filled Circle to the CurrentRenderTarget
        /// </summary>
        /// <param name="posX"> Pos X of Circle </param>
        /// <param name="posY"> Pos Y of Circle </param>
        /// <param name="radius"> Radius of Cirle </param>
        /// <param name="color"> Fill Color </param>
        /// <param name="vector2"></param>
        public static void drawCircle(float posX, float posY, int radius, SystemColor color, Vector2 vector2)
        {
            CircleShape Circle = new CircleShape();
            Circle.Position = new Vector2(posX, posY);
            Circle.Radius = radius;
            Circle.FillColor = SystemColor.Transparent.ToSFMLColor();

            CurrentRenderTarget.Draw(Circle);
        }
        #endregion

        #region Point
        /// <summary>
        /// Draws a Filled Point to the CurrentRenderTarget
        /// </summary>
        /// <param name="posX"> Pos X of Point </param>
        /// <param name="posY"> Pos Y of Point </param>
        /// <param name="color"> Fill Color </param>
        public static void drawPoint(int posX, int posY, SystemColor color)
        {
            RectangleShape Point = new RectangleShape();
            Point.Position = new Vector2(posX, posY);
            Point.Size = new Vector2(1, 1);
            Point.FillColor = color.ToSFMLColor();

            CurrentRenderTarget.Draw(Point);
        }

        /// <summary>
        /// Draws a hollow Point to the CurrentRenderTarget
        /// </summary>
        /// <param name="posX"> Pos X of Point </param>
        /// <param name="posY"> Pos Y of Point </param>
        /// <param name="OutlineColor"> Outline Color </param>
        public static void drawHollowPoint(int posX, int posY, SystemColor OutlineColor)
        {
            RectangleShape hollowPoint = new RectangleShape();
            hollowPoint.Position = new Vector2(posX, posY);
            hollowPoint.Size = new Vector2(1, 1);
            hollowPoint.FillColor = SystemColor.Transparent.ToSFMLColor();
            hollowPoint.OutlineThickness = .6f;
            hollowPoint.OutlineColor = OutlineColor.ToSFMLColor();

            CurrentRenderTarget.Draw(hollowPoint);
        }

        #endregion

        #region Line
        /// <summary>
        /// Draws a Line to the CurrentRenderTarget
        /// </summary>
        /// <param name="posX"> Pos X of Line </param>
        /// <param name="posY"> Pos Y of Line </param>
        /// <param name="rotate"> Line Rotation </param>
        /// <param name="thickness"> Line Thickness </param>
        /// <param name="Color"> Line Color </param>
        public static void drawLine(int posX, int posY, int rotate,float thickness, SystemColor Color)
        {
            RectangleShape line = new RectangleShape();
            line.Position = new Vector2(posX,posY);
            line.Rotation = rotate;
            line.OutlineThickness = thickness;
            line.FillColor = Color.ToSFMLColor();

            CurrentRenderTarget.Draw(line);
        }

        #endregion
        
       public static SFMLColor ColorFromARGB(byte A, SystemColor Color)
       {
           return new SFMLColor(Color.R, Color.G, Color.B, A);
       }

        #endregion


       
    }


    public static class Conversions
    {
        public static SFMLColor ToSFMLColor(this SystemColor SystemColor)
        {
            return new SFMLColor(SystemColor.R,SystemColor.G,SystemColor.B,SystemColor.A);
        }

        public static SystemColor ToSystemColor(this SFMLColor SFMLColor)
        {
            SystemColor temp = SystemColor.FromArgb(SFMLColor.A, SFMLColor.R, SFMLColor.G, SFMLColor.B);
            return temp;
        }

      

       
        public static Vector2 ToVector2(this Point point)
        {
            return new Vector2(point.X, point.Y);
        }
    }
}
