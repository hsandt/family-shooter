﻿using System;
using BloomPostprocess;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace FamilyShooter
{
    public class GameRoot : Game
    {
        // Constants
        private readonly Color PAUSE_OVERLAY_BACKGROUND = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        public static GameRoot Instance { get; private set; }
        public static Viewport Viewport => Instance.GraphicsDevice.Viewport;
        public static Vector2 ScreenSize => new Vector2(Viewport.Width, Viewport.Height);

        public static GameTime GameTime { get; private set; }
        public static TimeSpan InGameTimeSpan { get; private set; }

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        public static ParticleManager<ParticleState> ParticleManager { get; private set; }

        // private BloomComponent bloom;

        private const int maxGridPoints = 1600;
        // public static Grid Grid;
        public static Background Background;

        // Game State

        /// Is the game paused?
        private bool m_IsPaused = false;

        // Cheat

        /// Is god mode active?
        public bool IsGodModeActive { get; private set; }

        public GameRoot()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;

            // bloom = new BloomComponent(this);
            // Components.Add(bloom);
            // it's all bad on Linux because it only works with DirectX shaders
            // For cross-platform Bloom, see:
            // https://github.com/Kosmonaut3d/BloomFilter-for-Monogame-and-XNA
            // bloom.Settings = new BloomSettings(null, 0.25f, 4f, 2f, 1f, 1.5f, 1f);
            // bloom.Settings = new BloomSettings(null, 1f, 0f, 0f, 1f, 0f, 1f);
            // bloom.Settings = BloomSettings.PresetSettings[0];

            Content.RootDirectory = "Content";
            IsMouseVisible = true;  // show custom cursor

            Instance = this;
        }

        protected override void Initialize()
        {
            base.Initialize();

            // This must be done *after* base call so LoadContent has been called
            EntityManager.Add(PlayerShip.Instance);

            // Set custom cursor
            // Hardware cursor is faster / more reactive than software cursor suggested in tutorial
            Mouse.SetCursor(MouseCursor.FromTexture2D(Art.Pointer, 0, 0));

            // The normal code for a BGM should be:
            // MediaPlayer.IsRepeating = true;
            // MediaPlayer.Play(Sound.Music);

            // But a known limitation of MonoGame adds a perceptible pause between loops, so we prefer loading Music
            // as a SoundEffect, then create a looping instance of it, as the cost of extra space usage in build
            // (and most likely RAM since we are not streaming).
            // https://community.monogame.net/t/background-music-delayed-when-looping/8892/8
            // https://gamedev.stackexchange.com/questions/20313/eliminate-delay-between-looping-xna-songs
            // https://www.reddit.com/r/gamedev/comments/1dd3bu/looping_background_music_in_cxna/
            SoundEffectInstance musicInstance = Sound.Music.CreateInstance();
            musicInstance.IsLooped = true;
            musicInstance.Play();

            ParticleManager = new ParticleManager<ParticleState>(1024 * 20, ParticleState.UpdateParticle);

            // No grid in this game, use Background instead
            /*
            Vector2 gridSpacing = new Vector2(MathF.Sqrt((float)Viewport.Width * Viewport.Height / maxGridPoints));
            // unlike Tutorial, we are using grid cell coordinates not pixels, so we must divide everything
            // by gridSpacing.X
            Rectangle cellBounds = new Rectangle((int)(Viewport.Bounds.Left / gridSpacing.X), (int)(Viewport.Bounds.Top / gridSpacing.X),
                (int)(Viewport.Bounds.Width / gridSpacing.X), (int)(Viewport.Bounds.Height / gridSpacing.X));
            Grid = new Grid(cellBounds, gridSpacing);
            */
            Background = new Background();

            InGameTimeSpan = TimeSpan.Zero;
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            Art.Load(Content);
            Sound.Load(Content);
        }

        protected override void Update(GameTime gameTime)
        {
            GameTime = gameTime;

            base.Update(gameTime);

            // Input must always be updated
            Input.Update();

            // Avoid exiting immediately when holding Escape while app is launching
            // by skipping the first two frames (time 0 and TargetElapsedTime)
            if (gameTime.TotalGameTime > TargetElapsedTime && Input.WasKeyPressed(Keys.X) | Input.WasButtonPressed(Buttons.Back))
            {
                Exit();
                return;
            }

            if (Input.WasKeyPressed(Keys.Escape) | Input.WasKeyPressed(Keys.P) | Input.WasButtonPressed(Buttons.Start))
            {
                m_IsPaused ^= true;
            }

            if (Input.WasKeyPressed(Keys.G) | Input.WasButtonPressed(Buttons.LeftTrigger))
            {
                IsGodModeActive ^= true;
            }

            if (!m_IsPaused)
            {
                // Update in-game stuff
                InGameTimeSpan += GameTime.ElapsedGameTime;

                EntityManager.Update();
                EnemySpawner.Update();
                PlayerStatus.Update();
                ParticleManager.Update();
                // Grid.Update();
                Background.Update();
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            // it's very blurry, disabled for now
            // bloom.BeginDraw();

            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(SpriteSortMode.Texture, samplerState: SamplerState.LinearWrap);
            Background.Draw(_spriteBatch);
            _spriteBatch.End();

            // Additive doesn't work anymore on top of standard background, so use default blend mode
            _spriteBatch.Begin(SpriteSortMode.Texture);
            EntityManager.Draw(_spriteBatch);
            _spriteBatch.End();

            // This must absolutely be Additive for particles which use black as background
            // It is not required for text which is white anyway
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);

            ParticleManager.Draw(_spriteBatch);

            _spriteBatch.DrawString(Art.Font, $"Lives: {PlayerStatus.Lives}", new Vector2(10, 8), Color.White);
            DrawRightAlignedString($"Score: {PlayerStatus.Score}", ScreenSize.X - 10, 8);
            DrawRightAlignedString($"Multiplier: {PlayerStatus.CurrentMultiplier}", ScreenSize.X - 10, 38);
            DrawRightAlignedString($"High Score: {PlayerStatus.HighScore}", ScreenSize.X - 10, 68);

            if (PlayerStatus.IsGameOver)
            {
                string gameOverText = $"Game Over\nYour Score: {PlayerStatus.Score}\nHigh Score: {PlayerStatus.HighScore}";
                DrawCenterAlignedString(gameOverText);
            }

            // Tutorial did this:
            // draw custom mouse cursor
            // _spriteBatch.Draw(Art.Pointer, Input.MousePosition, Color.White);
            // but Software cursor lags behind hardware cursor, so we prefer setting custom cursor

            // Removed Grid on this project, replaced with background sky
            // Grid.Draw(_spriteBatch);

            _spriteBatch.End();

            if (m_IsPaused)
            {
                // Default to Deferred, AlphaBlend, better for overlay than Additive
                _spriteBatch.Begin();

                // Draw Pause overlay
                _spriteBatch.DrawRect(Vector2.Zero, ScreenSize, PAUSE_OVERLAY_BACKGROUND);
                DrawCenterAlignedString("Pause");

                _spriteBatch.End();
            }

            if (IsGodModeActive)
            {
                _spriteBatch.Begin();

                DrawHorizontalCenterAlignedString("God Mode", MathF.Round(ScreenSize.Y / 2f + 10f));

                _spriteBatch.End();
            }

            // If bloom is enabled, applies bloom here
            base.Draw(gameTime);
        }

        private void DrawRightAlignedString(string text, float x, float y)
        {
            var textWidth = Art.Font.MeasureString(text).X;
            _spriteBatch.DrawString(Art.Font, text, new Vector2(x - textWidth, y), Color.White);
        }

        private void DrawHorizontalCenterAlignedString(string text, float y)
        {
            var textSize = Art.Font.MeasureString(text);

            // Round position to integer pixel to avoid blurry text
            // (can also .Round() in place)
            float rawPositionX = ScreenSize.X / 2f - textSize.X / 2f;
            float roundedPositionX = MathF.Round(rawPositionX);

            _spriteBatch.DrawString(Art.Font, text, new Vector2(roundedPositionX, y), Color.White);
        }

        private void DrawCenterAlignedString(string text)
        {
            var textSize = Art.Font.MeasureString(text);

            // Round position to integer pixel to avoid blurry text
            // (can also .Round() in place)
            Vector2 rawPosition = ScreenSize / 2f - textSize / 2f;
            Vector2 roundedPosition = Vector2.Round(rawPosition);

            _spriteBatch.DrawString(Art.Font, text, roundedPosition, Color.White);
        }
    }
}
