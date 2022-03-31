using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TileMapGame.StateManagement
{
    /// <summary>
    /// The ScreenManager is a component which manages one or more GameScreen instance.
    /// It maintains a stack of screens, calls their Update and Draw methods when 
    /// appropriate, and automatically routes input to the topmost screen.
    /// </summary>
    public class ScreenManager : DrawableGameComponent
    {
        private readonly List<GameScreen> _screens = new List<GameScreen>();
        private readonly List<GameScreen> _tmpScreensList = new List<GameScreen>();

        private readonly ContentManager _content;
        private readonly InputState _input = new InputState();

        private GraphicsDeviceManager _graphics;
        private bool _isInitialized;
        private DisplayStrategy _displayStrategy;
        private GameResolution _gameResolution;
        private Texture2D _standard;
        private Texture2D _widescreen;
        private float _gameScale;
        private Vector2 _gameOffset;

        /// <summary>
        /// A SpriteBatch shared by all GameScreens
        /// </summary>
        public SpriteBatch SpriteBatch { get; private set; }

        /// <summary>
        /// A SpriteFont shared by all GameScreens
        /// </summary>
        public SpriteFont Font { get; private set; }

        /// <summary>
        /// A blank texture that can be used by the screens.
        /// </summary>
        public List<Texture2D> PowerUpTextures { get; private set; }

        /// <summary>
        /// Constructs a new ScreenManager
        /// </summary>
        /// <param name="game">The game this ScreenManager belongs to</param>
        public ScreenManager(Game game, GraphicsDeviceManager gdm) : base(game)
        {
            _graphics = gdm;
            _content = new ContentManager(game.Services, "Content");
            DisplayMode screen = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            _graphics.IsFullScreen = true;
            _graphics.PreferredBackBufferWidth = screen.Width;
            _graphics.PreferredBackBufferHeight = screen.Height;
            PowerUpTextures = new List<Texture2D>();
            _graphics.ApplyChanges();
        }

        /// <summary>
        /// Initializes the ScreenManager
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            // TODO: Add your initialization logic here
            _gameResolution = GameResolution.SixteenToNine;
            _displayStrategy = DisplayStrategy.ScaleToFit;
            DetermineScreenSize();

            _isInitialized = true;
        }

        /// <summary>
        /// Loads content for the ScreenManager and its screens
        /// </summary>
        protected override void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            Font = _content.Load<SpriteFont>("gameplayfont");
            //var temp = _content.Load<Texture2D>("PowerUpHealth");
            PowerUpTextures.Add(_content.Load<Texture2D>("PowerUpHealth"));
            //var temp2 = _content.Load<Texture2D>("PowerUpDamage");
            PowerUpTextures.Add(_content.Load<Texture2D>("PowerUpDamageFixed"));
            PowerUpTextures.Add(_content.Load<Texture2D>("PowerFireRateFixed"));
            // Tell each of the screens to load thier content 
            foreach (var screen in _screens)
            {
                screen.Activate();
            }
        }

        /// <summary>
        /// Unloads content for the ScreenManager's screens
        /// </summary>
        protected override void UnloadContent()
        {
            foreach (var screen in _screens)
            {
                screen.Unload();
            }
        }

        /// <summary>
        /// Updates all screens managed by the ScreenManager
        /// </summary>
        /// <param name="gameTime">An object representing time in the game</param>
        public override void Update(GameTime gameTime)
        {
            // Read in the keyboard and gamepad
            _input.Update();

            // Make a copy of the screen list, to avoid confusion if 
            // the process of updating a screen adds or removes others
            _tmpScreensList.Clear();
            _tmpScreensList.AddRange(_screens);

            bool otherScreenHasFocus = !Game.IsActive;
            bool coveredByOtherScreen = false;

            while (_tmpScreensList.Count > 0)
            {
                // Pop the topmost screen 
                var screen = _tmpScreensList[_tmpScreensList.Count - 1];
                _tmpScreensList.RemoveAt(_tmpScreensList.Count - 1);

                screen.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

                if (screen.ScreenState == ScreenState.TransitionOn || screen.ScreenState == ScreenState.Active)
                {
                    // if this is the first active screen, let it handle input 
                    if (!otherScreenHasFocus)
                    {
                        screen.HandleInput(gameTime, _input);
                        otherScreenHasFocus = true;
                    }

                    // if this is an active non-popup, all subsequent 
                    // screens are covered 
                    if (!screen.IsPopup) coveredByOtherScreen = true;
                }
            }
        }

        /// <summary>
        /// Draws the appropriate screens managed by the SceneManager
        /// </summary>
        /// <param name="gameTime">An object representing time in the game</param>
        public override void Draw(GameTime gameTime)
        {
            // Determine the necessary transform to scale and position game on-screen
            Matrix transform =
                Matrix.CreateScale(_gameScale) * // Scale the game to screen size 
                Matrix.CreateTranslation(_gameOffset.X, _gameOffset.Y, 0); // Translate game to letterbox position
            foreach (var screen in _screens)
            {
                if (screen.ScreenState == ScreenState.Hidden) continue;

                screen.Draw(gameTime);
            }
        }

        /// <summary>
        /// Adds a screen to the ScreenManager
        /// </summary>
        /// <param name="screen">The screen to add</param>
        public void AddScreen(GameScreen screen, PlayerIndex? controllingPlayer)
        {
            screen.ControllingPlayer = controllingPlayer;
            screen.ScreenManager = this;
            screen.IsExiting = false;

            // If we have a graphics device, tell the screen to load content
            if (_isInitialized) screen.Activate();

            _screens.Add(screen);
        }

        public void RemoveScreen(GameScreen screen)
        {
            // If we have a graphics device, tell the screen to unload its content 
            if (_isInitialized) screen.Unload();

            _screens.Remove(screen);
            _tmpScreensList.Remove(screen);
        }

        /// <summary>
        /// Exposes an array holding all the screens managed by the ScreenManager
        /// </summary>
        /// <returns>An array containing references to all current screens</returns>
        public GameScreen[] GetScreens()
        {
            return _screens.ToArray();
        }

        // Helper draws a translucent black fullscreen sprite, used for fading
        // screens in and out, and for darkening the background behind popups.
        public void FadeBackBufferToBlack(float alpha)
        {
            SpriteBatch.Begin();
            //SpriteBatch.Draw(BlankTexture, GraphicsDevice.Viewport.Bounds, Color.Black * alpha);
            SpriteBatch.End();
        }

        public void Deactivate()
        {
        }

        public bool Activate()
        {
            return false;
        }

        public void DetermineScreenSize()
        {
            Viewport screen = _graphics.GraphicsDevice.Viewport;
            Viewport game;

            // Determine game size based on selected resolution
            switch (_gameResolution)
            {
                case GameResolution.FourToThree:
                    game = new Viewport(0, 0, 1024, 768);
                    break;
                case GameResolution.SixteenToNine:
                default:
                    game = new Viewport(0, 0, 1920, 1080);
                    break;
            }

            // Determine game viewport scaling and positioning based on selected display strategy

            switch (_displayStrategy)
            {
                case DisplayStrategy.ScaleToFit:
                    // 1. Determine which dimension must have letterboxing
                    if (screen.AspectRatio < game.AspectRatio)
                    {
                        // letterbox vertically
                        // Scale game to screen width
                        _gameScale = (float)screen.Width / game.Width;
                        // translate vertically
                        _gameOffset.Y = (screen.Height - game.Height * _gameScale) / 2f;
                        _gameOffset.X = 0;
                    }
                    else
                    {
                        // letterbox horizontally
                        // Scale game to screen height 
                        _gameScale = (float)screen.Height / game.Height;
                        // translate horizontally
                        _gameOffset.X = (screen.Width - game.Width * _gameScale) / 2f;
                        _gameOffset.Y = 0;
                    }
                    break;

                case DisplayStrategy.ScaleToCover:
                    // 1. Determine which dimension must overflow screen 
                    if (screen.AspectRatio < game.AspectRatio)
                    {
                        // overflow horizontally
                        // Scale game to screen height 
                        _gameScale = (float)screen.Height / game.Height;
                        // translate horizontally 
                        _gameOffset.X = (screen.Width - game.Width * _gameScale) / 2f;
                        _gameOffset.Y = 0;
                    }
                    else
                    {
                        // overflow vertically
                        // Scale game to screen width 
                        _gameScale = (float)screen.Width / game.Width;
                        // translate vertically
                        _gameOffset.Y = (screen.Height - game.Height * _gameScale) / 2f;
                        _gameOffset.X = 0;
                    }
                    break;
            }

        }
    }
}
