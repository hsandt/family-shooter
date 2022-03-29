using System;
using BloomPostprocess;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace FamilyShooter
{
    public class GameRoot : Game
    {
        public static GameRoot Instance { get; private set; }
        public static Viewport Viewport => Instance.GraphicsDevice.Viewport;
        public static Vector2 ScreenSize => new Vector2(Viewport.Width, Viewport.Height);

        public static GameTime GameTime { get; private set; }

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        public static ParticleManager<ParticleState> ParticleManager { get; private set; }

        // private BloomComponent bloom;

        private const int maxGridPoints = 1600;
        public static Grid Grid;

        public GameRoot()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;

            // bloom = new BloomComponent(this);
            // Components.Add(bloom);
            // it's all bad!
            // bloom.Settings = new BloomSettings(null, 0.25f, 4f, 2f, 1f, 1.5f, 1f);
            // bloom.Settings = new BloomSettings(null, 1f, 0f, 0f, 1f, 0f, 1f);
            // bloom.Settings = BloomSettings.PresetSettings[0];

            Content.RootDirectory = "Content";
            IsMouseVisible = true;  // show custom cursor

            Instance = this;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;

            base.Initialize();

            // This must be done *after* base call so LoadContent has been called
            EntityManager.Add(PlayerShip.Instance);

            // Set custom cursor
            // Hardware cursor is faster / more reactive than software cursor suggested in tutorial
            Mouse.SetCursor(MouseCursor.FromTexture2D(Art.Pointer, 0, 0));

            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(Sound.Music);

            ParticleManager = new ParticleManager<ParticleState>(1024 * 20, ParticleState.UpdateParticle);

            Vector2 gridSpacing = new Vector2(MathF.Sqrt((float)Viewport.Width * Viewport.Height / maxGridPoints));
            // unlike Tutorial, we are using grid cell coordinates not pixels, so we must divide everything
            // by gridSpacing.X
            Rectangle cellBounds = new Rectangle((int)(Viewport.Bounds.Left / gridSpacing.X), (int)(Viewport.Bounds.Top / gridSpacing.X),
                (int)(Viewport.Bounds.Width / gridSpacing.X), (int)(Viewport.Bounds.Height / gridSpacing.X));
            Grid = new Grid(cellBounds, gridSpacing);
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

            Input.Update();

            // Avoid exiting immediately when holding Escape while app is launching
            // by skipping the first two frames (time 0 and TargetElapsedTime)
            if (gameTime.TotalGameTime > TargetElapsedTime && (Input.WasButtonPressed(Buttons.Back) || Input.WasKeyPressed(Keys.Escape)))
            {
                Exit();
            }

            EntityManager.Update();
            EnemySpawner.Update();
            PlayerStatus.Update();
            ParticleManager.Update();
            Grid.Update();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // it's very blurry, disabled for now
            // bloom.BeginDraw();

            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(SpriteSortMode.Texture, BlendState.Additive);
            EntityManager.Draw(_spriteBatch);
            _spriteBatch.End();

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);

            ParticleManager.Draw(_spriteBatch);

            _spriteBatch.DrawString(Art.Font, $"Lives: {PlayerStatus.Lives}", new Vector2(5), Color.White);
            DrawRightAlignedString($"Score: {PlayerStatus.Score}", 5);
            DrawRightAlignedString($"Multiplier: {PlayerStatus.CurrentMultiplier}", 35);
            DrawRightAlignedString($"High Score: {PlayerStatus.HighScore}", 65);

            if (PlayerStatus.IsGameOver)
            {
                string gameOverText = $"Game Over\nYour Score: {PlayerStatus.Score}\nHigh Score: {PlayerStatus.HighScore}";
                DrawCenterAlignedString(gameOverText);
            }

            // Tutorial did this:
            // draw custom mouse cursor
            // _spriteBatch.Draw(Art.Pointer, Input.MousePosition, Color.White);
            // but Software cursor lags behind hardware cursor, so we prefer setting custom cursor

            Grid.Draw(_spriteBatch);

            _spriteBatch.End();

            // Applies bloom
            base.Draw(gameTime);
        }

        private void DrawRightAlignedString(string text, float y)
        {
            var textWidth = Art.Font.MeasureString(text).X;
            _spriteBatch.DrawString(Art.Font, text, new Vector2(ScreenSize.X - textWidth - 5, y), Color.White);
        }

        private void DrawCenterAlignedString(string text)
        {
            var textSize = Art.Font.MeasureString(text);
            _spriteBatch.DrawString(Art.Font, text, ScreenSize / 2f - textSize / 2f, Color.White);
        }
    }
}
