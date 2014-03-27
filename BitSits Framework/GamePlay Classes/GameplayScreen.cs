using System;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Media;

namespace BitSits_Framework
{
    /// <summary>
    /// This screen implements the actual game logic. It is just a
    /// placeholder to get the idea across: you'll probably want to
    /// put some more interesting gameplay in here!
    /// </summary>
    class GameplayScreen : GameScreen
    {
        #region Fields

        ContentManager content;

        // Meta-level game state.
        private int levelIndex = 0;
        private const int maxLevelIndex = 20;
        private Level level;

        Texture2D overlay, background, gameOverOverlay;
        bool gameOverOverlayIsUp;

        MouseState mouseState, prevMouseState;

        float score;
        SpriteFont scoreFont;

        #endregion

        #region Initialization


        /// <summary>
        /// Constructor.
        /// </summary>
        public GameplayScreen()
        {
            TransitionOnTime = TimeSpan.FromSeconds(0.0);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }


        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public override void LoadContent()
        {
            if (content == null)
                content = new ContentManager(ScreenManager.Game.Services, "Content");

            LoadNextLevel();

            overlay = content.Load<Texture2D>("Graphics/overlay");
            background = content.Load<Texture2D>("Graphics/background1");
            gameOverOverlay = content.Load<Texture2D>("Graphics/gameOver");

            scoreFont = content.Load<SpriteFont>("Fonts/scoreFont");

            MediaPlayer.Play(content.Load<Song>("Audio/Back to old school"));
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = 0.7f;

            // once the load has finished, we use ResetElapsedTime to tell the game's
            // timing mechanism that we have just finished a very long frame, and that
            // it should not try to catch up.
            ScreenManager.Game.ResetElapsedTime();
        }


        /// <summary>
        /// Unload graphics content used by the game.
        /// </summary>
        public override void UnloadContent()
        {
            content.Unload();
        }


        #endregion

        #region Update and Draw


        /// <summary>
        /// Updates the state of the game. This method checks the GameScreen.IsActive
        /// property, so the game will stop updating when the pause menu is active,
        /// or if you tab away to a different application.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                       bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            if (IsActive)
            {
                level.Update(gameTime);
            }
        }


        private void LoadNextLevel()
        {
            if (levelIndex == maxLevelIndex)
            {
                score = level.Score;
                if (!gameOverOverlayIsUp)
                { gameOverOverlayIsUp = true; return; }

                ScreenManager.AddScreen(new BackgroundScreen(), ControllingPlayer);
                ScreenManager.AddScreen(new MainMenuScreen(), ControllingPlayer);
                ExitScreen();
                return;
            }

            score = 0;
            // Unloads the content for the current level before loading the next one.
            if (level != null) { score = level.Score; level.Dispose(); }

            // Load the level.            
            level = new Level(content, levelIndex, (int)score); ++levelIndex;
        }

        private void ReloadCurrentLevel()
        {
            --levelIndex;
            LoadNextLevel();
        }


        /// <summary>
        /// Lets the game respond to player input. Unlike the Update method,
        /// this will only be called when the gameplay screen is active.
        /// </summary>
        public override void HandleInput(InputState input)
        {
            if (input == null) throw new ArgumentNullException("input");

            // Look up inputs for the active player profile.
            int playerIndex = (int)ControllingPlayer.Value;

            KeyboardState keyboardState = input.CurrentKeyboardStates[playerIndex];
            GamePadState gamePadState = input.CurrentGamePadStates[playerIndex];

            prevMouseState = mouseState; mouseState = Mouse.GetState();

            if (prevMouseState.LeftButton == ButtonState.Released &&
                mouseState.LeftButton == ButtonState.Pressed && level.IsSolved)
                LoadNextLevel();
            else
                level.HandleInput(input);
        }


        /// <summary>
        /// Draws the gameplay screen.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            // This game has a blue background. Why? Because!
            ScreenManager.GraphicsDevice.Clear(ClearOptions.Target, Color.Gainsboro, 0, 0);

            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

            spriteBatch.Begin();

            spriteBatch.Draw(background, Vector2.Zero, Color.White);

            DrwaScore(gameTime, spriteBatch);

            if (!gameOverOverlayIsUp)
            {
                level.Draw(gameTime, spriteBatch);

                //if (!ScreenManager.Game.IsActive)
                  //  spriteBatch.Draw(overlay, Vector2.Zero, Color.White);
            }
            else
                spriteBatch.Draw(gameOverOverlay, Vector2.Zero, Color.White);


            spriteBatch.End();

            // If the game is transitioning on or off, fade it out to black.
            if (TransitionPosition > 0) ScreenManager.FadeBackBufferToBlack(255 - TransitionAlpha);
        }

        private void DrwaScore(GameTime gameTime, SpriteBatch spriteBatch)
        {
            float fps = (1000.0f / (float)gameTime.ElapsedRealTime.TotalMilliseconds);

            float rate = (float)gameTime.ElapsedGameTime.TotalSeconds * 10;
            spriteBatch.DrawString(scoreFont, "Score", new Vector2(20, 20), Color.White, 0, 
                Vector2.Zero, .55f, SpriteEffects.None, 1);

            if (score < level.Score) score = Math.Min(level.Score, score + rate);
            else if (score > level.Score) score = Math.Max(level.Score, score - rate);

            spriteBatch.DrawString(scoreFont, score.ToString("00000"), new Vector2(20, 50),  Color.White);

            spriteBatch.DrawString(scoreFont, "Level", new Vector2(370, 20), Color.White, 0,
                Vector2.Zero, .55f, SpriteEffects.None, 1);

            spriteBatch.DrawString(scoreFont, levelIndex.ToString(), new Vector2(385, 50), Color.White);

            //spriteBatch.DrawString(titleFont, "Time", new Vector2(370, 340), Color.White);

            if (level.IsSolved)
                spriteBatch.Draw(overlay, Vector2.Zero, Color.White);
        }


        #endregion
    }
}
