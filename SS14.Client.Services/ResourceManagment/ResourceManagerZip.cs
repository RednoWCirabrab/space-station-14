﻿using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.Configuration;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.GameObjects;
using Vector2 = SS14.Shared.Maths.Vector2;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using SS14.Client.Graphics.Shader;
using SS14.Client.Graphics.Sprite;
using TextureCache = SS14.Client.Graphics.TextureCache;
using Image = SFML.Graphics.Image;
using Font = SFML.Graphics.Font;
using Color = SFML.Graphics.Color;
using SFML.Graphics;
using SS14.Client.Graphics.Collection;

namespace SS14.Client.Services.Resources
{
    public class ResourceManager : IResourceManager
    {
        private const int zipBufferSize = 4096;
        private readonly IConfigurationManager _configurationManager;
        private readonly Dictionary<string, Font> _fonts = new Dictionary<string, Font>();
        private readonly Dictionary<string, ParticleSettings> _particles = new Dictionary<string, ParticleSettings>();
        private readonly Dictionary<string, Image> _images = new Dictionary<string, Image>();
        private readonly Dictionary<string, FXShader> _shaders = new Dictionary<string, FXShader>();
        private readonly Dictionary<string, SpriteInfo> _spriteInfos = new Dictionary<string, SpriteInfo>();
        private readonly Dictionary<string, CluwneSprite> _sprites = new Dictionary<string, CluwneSprite>();
        private readonly Dictionary<string, AnimationCollection> _animationCollections = new Dictionary<string, AnimationCollection>(); 
        private readonly Dictionary<string, AnimatedSprite> _animatedSprites = new Dictionary<string, AnimatedSprite>(); 
        private readonly List<string> supportedImageExtensions = new List<string> {".png"};

        public int done = 0;

        public ResourceManager(IConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;
        }

        #region Resource Loading & Disposal

        /// <summary>
        ///  <para>Loads the embedded base files.</para>
        /// </summary>
        public void LoadBaseResources()
        {
            Assembly _assembly = Assembly.GetExecutingAssembly(); ;
            Stream _stream;

            _stream = _assembly.GetManifestResourceStream("SS14.Client.Services._EmbeddedBaseResources.bluehigh.ttf");
            if (_stream != null)
                _fonts.Add("base_font", new Font( _stream));
            _stream = null;

            _stream = _assembly.GetManifestResourceStream("SS14.Client.Services._EmbeddedBaseResources.noSprite.png");
            if (_stream != null)
            {
                Image nospriteimage = new Image( _stream);
                _images.Add("nosprite", nospriteimage);
                _sprites.Add("nosprite", new CluwneSprite("nosprite", nospriteimage));  
            }
            _stream = null;
        }

        /// <summary>
        ///  <para>Loads the local resources as specified by the config</para>
        /// </summary>
        public void LoadLocalResources()
        {
            LoadResourceZip();
            LoadAnimatedSprites();
        }

        /// <summary>
        ///  <para>Loads all Resources from given Zip into the respective Resource Lists and Caches</para>
        /// </summary>
        public void LoadResourceZip(string path = null, string pw = null)
        {
            string zipPath = path ?? _configurationManager.GetResourcePath();
            string password = pw ?? _configurationManager.GetResourcePassword();


            if (!File.Exists(zipPath)) throw new FileNotFoundException("Specified Zip does not exist: " + zipPath);

            FileStream zipFileStream = File.OpenRead(zipPath);
            var zipFile = new ZipFile(zipFileStream);

            if (!string.IsNullOrWhiteSpace(password)) zipFile.Password = password;

            var directories = from ZipEntry a in zipFile
                              where a.IsDirectory
                              orderby a.Name.ToLowerInvariant() == "textures" descending 
                              select a;

            Dictionary<string, List<ZipEntry>> sorted = new Dictionary<string, List<ZipEntry>>();

            foreach (ZipEntry dir in directories)
            {
                if (sorted.ContainsKey(dir.Name.ToLowerInvariant())) continue; //Duplicate folder? shouldnt happen.

                List<ZipEntry> folderContents = (from ZipEntry entry in zipFile
                                                 where entry.Name.ToLowerInvariant().Contains(dir.Name.ToLowerInvariant())
                                                 where entry.IsFile
                                                 select entry).ToList();

                sorted.Add(dir.Name.ToLowerInvariant(), folderContents);
            }

            sorted = sorted.OrderByDescending(x => x.Key == "textures/").ToDictionary(x => x.Key, x => x.Value); //Textures first.

            foreach (KeyValuePair<string, List<ZipEntry>> current in sorted)
            {
                switch (current.Key)
                {
                    case("textures/"):
                        foreach (ZipEntry texture in current.Value)
                        {
                            if(supportedImageExtensions.Contains(Path.GetExtension(texture.Name).ToLowerInvariant()))
                            {
                                Image loadedImg = LoadImageFrom(zipFile, texture);
                                if (loadedImg == null) continue;
                                else _images.Add(texture.Name, loadedImg);
                            }
                        }
                        break;

                    case("tai/"): // Tai? 
                        foreach (ZipEntry tai in current.Value)
                        {
                            if (Path.GetExtension(tai.Name).ToLowerInvariant() == ".tai")
                            {
                                IEnumerable<CluwneSprite> loadedSprites = LoadSpritesFrom(zipFile, tai);
                                foreach (CluwneSprite currentSprite in loadedSprites.Where(currentSprite => !_sprites.ContainsKey(currentSprite.Name)))
                                    _sprites.Add(currentSprite.Name, currentSprite);                               
                            }
                        }
                        break;

                    case("fonts/"):
                        foreach (ZipEntry font in current.Value)
                        {
                            if (Path.GetExtension(font.Name).ToLowerInvariant() == ".ttf")
                            {
                                Font loadedFont = LoadFontFrom(zipFile, font);
                                if (loadedFont == null) continue;
                                string ResourceName = Path.GetFileNameWithoutExtension(font.Name).ToLowerInvariant();
                                _fonts.Add(ResourceName, loadedFont);
                            }
                        }
                        break;

                    case("particlesystems/"):
                        foreach (ZipEntry particles in current.Value)
                        {
                            if (Path.GetExtension(particles.Name).ToLowerInvariant() == ".xml")
                            {
                                ParticleSettings particleSettings = LoadParticlesFrom(zipFile, particles);
                                if (particleSettings == null) continue;
                                else _particles.Add(Path.GetFileNameWithoutExtension(particles.Name), particleSettings);
                            }
                        }
                        break;

                    case("shaders/"):
                        foreach (ZipEntry shader in current.Value)
                        {
                            //FIXME Throws Exception
                            //if (Path.GetExtension(shader.Name).ToLowerInvariant() == ".fx")
                            //{
                            //    FXShader loadedShader = LoadShaderFrom(zipFile, shader);
                            //    if (loadedShader == null) continue;
                            //    else _shaders.Add(shader.Name, loadedShader);
                            //}
                        }
                        break;

                    case("animations/"):
                        foreach (ZipEntry animation in current.Value)
                        {
                            if (Path.GetExtension(animation.Name).ToLowerInvariant() == ".xml")
                            {
                                AnimationCollection animationCollection = LoadAnimationCollectionFrom(zipFile, animation);
                                if (animationCollection == null) continue;
                                else _animationCollections.Add(animationCollection.Name, animationCollection);
                            }
                        }
                        break;

                }
            }

            sorted = null;

            zipFile.Close();
            zipFileStream.Close();
            zipFileStream.Dispose();

            GC.Collect();
        }

        /// <summary>
        ///  <para>Clears all Resource lists</para>
        /// </summary>
        public void ClearLists()
        {
            _images.Clear();
            _shaders.Clear();
            _fonts.Clear();
            _spriteInfos.Clear();
            _sprites.Clear();
        }

        /// <summary>
        ///  <para>Loads Image from given Zip-File and Entry.</para>
        /// </summary>
        private Image LoadImageFrom(ZipFile zipFile, ZipEntry imageEntry)
        {
            string ResourceName = Path.GetFileNameWithoutExtension(imageEntry.Name).ToLowerInvariant();

            if (TextureCache.Textures.Contains(ResourceName))
                return null; // ImageCache.Images[ResourceName];

            var byteBuffer = new byte[zipBufferSize];

            try
            {
                Stream zipStream = zipFile.GetInputStream(imageEntry);
                var memStream = new MemoryStream();

                StreamUtils.Copy(zipStream, memStream, byteBuffer);
                memStream.Position = 0;

                Image loadedImg = new Image(memStream);
                TextureCache.Add(ResourceName, loadedImg);

                memStream.Close();
                zipStream.Close();
                memStream.Dispose();
                zipStream.Dispose();
                return loadedImg;

            }
            catch(Exception I)
            {
                System.Console.WriteLine("Failed to load " + imageEntry.Name + ": " + I.ToString());
            }

            return null;
          
        }

        /// <summary>
        ///  <para>Loads Shader from given Zip-File and Entry.</para>
        /// </summary>
        private FXShader LoadShaderFrom(ZipFile zipFile, ZipEntry shaderEntry)
        {
            string ResourceName = Path.GetFileNameWithoutExtension(shaderEntry.Name).ToLowerInvariant();

          

            var byteBuffer = new byte[zipBufferSize];

            Stream zipStream = zipFile.GetInputStream(shaderEntry);
            //Will throw exception is missing or wrong password. Handle this.

            var memStream = new MemoryStream();

            StreamUtils.Copy(zipStream, memStream, byteBuffer);
            memStream.Position = 0;

            FXShader loadedShader = new FXShader(ResourceName,ResourceName);
            loadedShader.memStream = memStream;

            memStream.Close();
            zipStream.Close();
            memStream.Dispose();
            zipStream.Dispose();

            return loadedShader;
        }

        /// <summary>
        ///  <para>Loads Font from given Zip-File and Entry.</para>
        /// </summary>
        private Font LoadFontFrom(ZipFile zipFile, ZipEntry fontEntry)
        {
            var byteBuffer = new byte[zipBufferSize];

            Stream zipStream = zipFile.GetInputStream(fontEntry);
            //Will throw exception is missing or wrong password. Handle this.

            var memStream = new MemoryStream();

            StreamUtils.Copy(zipStream, memStream, byteBuffer);
            memStream.Position = 0;

            Font loadedFont = new Font(memStream);


            // memStream.Close();
            zipStream.Close();
            // memStream.Dispose();
            zipStream.Dispose();

            return loadedFont;
        }

        /// <summary>
        /// Loads particle settings from given zipfile and entry.
        /// </summary>
        /// <param name="zipFile"></param>
        /// <param name="entry"></param>
        /// <returns></returns>
        private ParticleSettings LoadParticlesFrom(ZipFile zipFile, ZipEntry entry)
        {
            string ResourceName = Path.GetFileNameWithoutExtension(entry.Name).ToLowerInvariant();

            var byteBuffer = new byte[zipBufferSize];

            Stream zipStream = zipFile.GetInputStream(entry);
            //Will throw exception is missing or wrong password. Handle this.

            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(ParticleSettings));

            var particleSettings = (ParticleSettings)serializer.Deserialize(zipStream);
            zipStream.Close();
            zipStream.Dispose();

            return particleSettings;
        }

        /// <summary>
        /// Loads animation collection from given zipfile and entry.
        /// </summary>
        /// <param name="zipFile"></param>
        /// <param name="entry"></param>
        /// <returns></returns>
        private AnimationCollection LoadAnimationCollectionFrom(ZipFile zipFile, ZipEntry entry)
        {
            string ResourceName = Path.GetFileNameWithoutExtension(entry.Name).ToLowerInvariant();


            var byteBuffer = new byte[zipBufferSize];

            Stream zipStream = zipFile.GetInputStream(entry);
            //Will throw exception is missing or wrong password. Handle this.

            System.Xml.Serialization.XmlSerializer serializer =
                new System.Xml.Serialization.XmlSerializer(typeof(AnimationCollection));

            var animationCollection = (AnimationCollection)serializer.Deserialize(zipStream);
            zipStream.Close();
            zipStream.Dispose();

            return animationCollection;
        }

        /// <summary>
        ///  <para>Loads TAI from given Zip-File and Entry and creates & loads Sprites from it.</para>
        /// </summary>
        private IEnumerable<CluwneSprite> LoadSpritesFrom(ZipFile zipFile, ZipEntry taiEntry)
        {
            string ResourceName = Path.GetFileNameWithoutExtension(taiEntry.Name).ToLowerInvariant();

            var loadedSprites = new List<CluwneSprite>();

            var byteBuffer = new byte[zipBufferSize];

            Stream zipStream = zipFile.GetInputStream(taiEntry);
            //Will throw exception is missing or wrong password. Handle this.

            var memStream = new MemoryStream();

            StreamUtils.Copy(zipStream, memStream, byteBuffer);
            memStream.Position = 0;

            var taiReader = new StreamReader(memStream, true);
            string loadedTAI = taiReader.ReadToEnd();

            memStream.Close();
            zipStream.Close();
            taiReader.Close();
            memStream.Dispose();
            zipStream.Dispose();
            taiReader.Dispose();

            string[] splitContents = Regex.Split(loadedTAI, "\r\n"); //Split by newlines.

            foreach (string line in splitContents)
            {
                if (String.IsNullOrWhiteSpace(line)) continue;

                string[] splitLine = line.Split(',');
                string[] fullPath = Regex.Split(splitLine[0], "\t");

                string PlatformPathname = SS14.Shared.Utility.PlatformTools.SanePath(fullPath[0]);

                string originalName = Path.GetFileNameWithoutExtension(PlatformPathname).ToLowerInvariant();
                //The name of the original picture without extension, before it became part of the atlas. 
                //This will be the name we can find this under in our Resource lists.

                string[] splitResourceName = fullPath[2].Split('.');

                string imageName = splitResourceName[0].ToLowerInvariant();

                if (!TextureCache.Textures.Contains(splitResourceName[0]))
                    continue; //Image for this sprite does not exist. Possibly set to defered later.

                Texture atlasTex = TextureCache.Textures[splitResourceName[0]];
                //Grab the image for the sprite from the cache.

                var info = new SpriteInfo();
                info.Name = originalName;

                float offsetX = 0;
                float offsetY = 0;
                float sizeX = 0;
                float sizeY = 0;

                if (splitLine.Length > 8) //Separated with ','. This causes some problems and happens on some EU PCs.
                {
                    offsetX = float.Parse(splitLine[3] + "." + splitLine[4], CultureInfo.InvariantCulture);
                    offsetY = float.Parse(splitLine[5] + "." + splitLine[6], CultureInfo.InvariantCulture);
                    sizeX = float.Parse(splitLine[8] + "." + splitLine[9], CultureInfo.InvariantCulture);
                    sizeY = float.Parse(splitLine[10] + "." + splitLine[11], CultureInfo.InvariantCulture);
                }
                else
                {
                    offsetX = float.Parse(splitLine[3], CultureInfo.InvariantCulture);
                    offsetY = float.Parse(splitLine[4], CultureInfo.InvariantCulture);
                    sizeX = float.Parse(splitLine[6], CultureInfo.InvariantCulture);
                    sizeY = float.Parse(splitLine[7], CultureInfo.InvariantCulture);
                }

                info.Offsets = new Vector2((float) Math.Round(offsetX*atlasTex.Size.X, 1),
                    (float) Math.Round(offsetY*atlasTex.Size.Y, 1));
                info.Size = new Vector2((float) Math.Round(sizeX*atlasTex.Size.X, 1),
                    (float) Math.Round(sizeY*atlasTex.Size.Y, 1));

                if (!_spriteInfos.ContainsKey(originalName)) _spriteInfos.Add(originalName, info);

                loadedSprites.Add(new CluwneSprite(originalName, atlasTex,
                    new IntRect((int)info.Offsets.X, (int)info.Offsets.Y, (int)info.Size.X, (int)info.Size.Y)));

            }

            return loadedSprites;
        }

        public void LoadAnimatedSprites()
        {
            foreach(var col in _animationCollections)
            {
                _animatedSprites.Add(col.Key, new AnimatedSprite(col.Key, col.Value, this));
            }
        }

        #endregion

        #region Resource Retrieval

        /// <summary>
        ///  <para>Retrieves the Image with the given key from the Resource list and returns it as a Sprite.</para>
        ///  <para>If a sprite has been created before using this method, it will return that Sprite. Returns error Sprite if not found.</para>
        /// </summary>
        public CluwneSprite GetSpriteFromImage(string key)
        {
            key = key.ToLowerInvariant();
            if (_images.ContainsKey(key))
            {
                if (_sprites.ContainsKey(key))
                {
                    return _sprites[key];
                }
                else
                {
                    var newSprite = new CluwneSprite(key, _images[key]);
                    _sprites.Add(key, newSprite);
                    return newSprite;
                }
            }
            return GetNoSprite();
        }

        /// <summary>
        ///  Retrieves the Sprite with the given key from the Resource List. Returns error Sprite if not found.
        /// </summary>
        public CluwneSprite GetSprite(string key)
        {
            key = key.ToLowerInvariant();
            if (_sprites.ContainsKey(key))
            {
                _sprites[key].Color = Color.White;
                return _sprites[key];
            }
            else return GetSpriteFromImage(key);
        }

        public List<CluwneSprite> GetSprites()
        {
            return _sprites.Values.ToList();
        } 

        public List<string> GetSpriteKeys()
        {
            return _sprites.Keys.ToList();
        } 

        public object GetAnimatedSprite(string key)
        {
            key = key.ToLowerInvariant();
            if (_animationCollections.ContainsKey(key))
            {
                return new AnimatedSprite(key, _animationCollections[key], this);
            }
            return null;
        }

        public CluwneSprite GetNoSprite()
        {
            return _sprites["nosprite"];
        }

        /// <summary>
        /// Checks if a sprite with the given key is in the Resource List.
        /// </summary>
        /// <param name="key">key to check</param>
        /// <returns></returns>
        public bool SpriteExists(string key)
        {
            key = key.ToLowerInvariant();
            return _sprites.ContainsKey(key);
        }

        /// <summary>
        /// Checks if an Image with the given key is in the Resource List.
        /// </summary>
        /// <param name="key">key to check</param>
        /// <returns></returns>
        public bool ImageExists(string key)
        {
            key = key.ToLowerInvariant();
            return _images.ContainsKey(key);
        }

        /// <summary>
        ///  Retrieves the SpriteInfo with the given key from the Resource List. Returns null if not found.
        /// </summary>
        public SpriteInfo? GetSpriteInfo(string key)
        {
            key = key.ToLowerInvariant();
            if (_spriteInfos.ContainsKey(key)) return _spriteInfos[key];
            else return null;
        }

        /// <summary>
        ///  Retrieves the Shader with the given key from the Resource List. Returns null if not found.
        /// </summary>
        public FXShader GetShader(string key)
        {
            key = key.ToLowerInvariant();
            if (_shaders.ContainsKey(key)) return _shaders[key];
            else return null;
        }

        /// <summary>
        ///  Retrieves the ParticleSettings with the given key from the Resource List. Returns null if not found.
        /// </summary>
        public ParticleSettings GetParticles(string key)
        {
            key = key.ToLowerInvariant();
            if (_particles.ContainsKey(key)) return _particles[key];
            else return null;
        }

        /// <summary>
        ///  Retrieves the Image with the given key from the Resource List. Returns error Image if not found.
        /// </summary>
        public Image GetImage(string key)
        {
            //key = key.ToLowerInvariant(); FUCK THIS LINE OF CODE ESPECIALLY BROKENASFUCK
            if (_images.ContainsKey(key)) return _images[key];
            else return _images["nosprite"];
        }

        /// <summary>
        ///  Retrieves the Font with the given key from the Resource List. Returns base_font if not found.
        /// </summary>
        public Font GetFont(string key)
        {
            key = key.ToLowerInvariant();
            if (_fonts.ContainsKey(key)) return _fonts[key];
            else return _fonts["base_font"];
        }

        #endregion
    }
}
