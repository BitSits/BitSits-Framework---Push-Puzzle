using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Input;
using System.IO;

namespace BitSits_Framework
{
    class Level : IDisposable
    {
        #region Fields

        public ContentManager Content { get; private set; }

        private List<Block> blocks = new List<Block>();

        MouseState mouseState, prevMouseState;
        Point mousePos;

        SoundEffect resetSound;
        SoundEffect[] hitSounds = new SoundEffect[3];
        Random hitIndex;

        List<List<int>> connectedBlocks = new List<List<int>>();

        Rectangle resetRect;
        bool isResetSelect;
        Texture2D resetTexture, puzzleBoard, resetSelect;

        public bool IsSolved { get; private set; }

        float time, entryTime = 0.5f, scoreTime, maxScoreTime = 5f;

        bool reduceScore;
        public int Score { get;private set; }
        int tempScore, reducedScore;
        SpriteFont scoreFont;

        #endregion

        #region Initialization


        public Level(ContentManager content, int levelIndex, int score)
        {
            Content = content;
            hitIndex = new Random(9876);
            Score = score;

            LoadTiles(levelIndex);
        }

        /// <summary>
        /// Iterates over every tile in the structure file and loads its
        /// appearance and behavior. This method also validates that the
        /// file is well-formed with a player start point, exit, etc.
        /// </summary>
        /// <param name="path">
        /// The absolute path to the level file to be loaded.
        /// </param>
        private void LoadTiles(int levelIndex)
        {
            // Load the level and ensure all of the lines are the same length.
            int width;
            List<string> lines = new List<string>();
            lines = Content.Load<List<string>>("Levels/" + levelIndex.ToString("00"));

            width = lines[0].Length;
            for (int i = 1; i < lines.Count; i++)
            {
                if (lines[i].Length != width)
                    throw new Exception(
                        String.Format("The length of line {0} is different from all preceeding lines.", 
                        lines.Count));
            }

            // Loop over every tile position,
            for (int y = 0; y < lines.Count; ++y)
            {
                for (int x = 0; x < lines[0].Length; ++x)
                {
                    // to load each tile.
                    char tileType = lines[y][x];
                    LoadTile(tileType, x, y);
                }
            }

            SetBlocks();
            if (blocks.Count <= 4) puzzleBoard = Content.Load<Texture2D>("Graphics/grid4");
            else if (blocks.Count <= 9) puzzleBoard = Content.Load<Texture2D>("Graphics/grid9");
            else if (blocks.Count <= 16) puzzleBoard = Content.Load<Texture2D>("Graphics/grid16");

            hitSounds[0] = Content.Load<SoundEffect>("Audio/hit0");
            hitSounds[1] = Content.Load<SoundEffect>("Audio/hit1");
            hitSounds[2] = Content.Load<SoundEffect>("Audio/hit2");
            resetSound = Content.Load<SoundEffect>("Audio/reset");

            scoreFont = Content.Load<SpriteFont>("Fonts/blockScoreFont");
        }

        private void SetBlocks()
        {
            int N = (int)Math.Sqrt(blocks.Count);
            foreach (Block block in blocks) block.SetDirection(N);

            for (int i = 0; i < blocks.Count; i++)
            {
                connectedBlocks.Add(new List<int>());

                for (int j = 0; j < blocks.Count; j++)
                {
                    if (i != j && blocks[i].BoundingRectMouse.Intersects(blocks[j].BoundingRectMouse))
                    {
                        connectedBlocks[i].Add(j);
                    }
                }
            }

            for (int i = 0; i < connectedBlocks.Count; i++)
            {
                for (int j = 0; j < connectedBlocks[i].Count; j++)
                {
                    for (int k = 0; k < connectedBlocks[connectedBlocks[i][j]].Count; k++)
                    {
                        int number = connectedBlocks[connectedBlocks[i][j]][k];
                        if (!connectedBlocks[i].Contains(number) && number != i)
                            connectedBlocks[i].Add(number);
                    }
                }
            }
        }

        /// <summary>
        /// Loads an individual tile's appearance and behavior.
        /// </summary>
        /// <param name="tileType">
        /// The character loaded from the structure file which
        /// indicates what should be loaded.
        /// </param>
        /// <param name="x">
        /// The X location of this tile in tile space.
        /// </param>
        /// <param name="y">
        /// The Y location of this tile in tile space.
        /// </param>
        /// <returns>The loaded tile.</returns>
        private void LoadTile(char tileType, int x, int y)
        {
            switch (tileType)
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case 'a':
                case 'b':
                case 'c':
                case 'd':
                case 'e':
                case 'f':
                    blocks.Add(new Block(Content, GetPosition(x, y), tileType)); return;

                case 'X':
                    Block.BoardPosition = GetPosition(x, y); return;

                case 'R':
                    {
                        Vector2 resetPos = GetPosition(x, y);
                        resetTexture = Content.Load<Texture2D>("Graphics/reset");
                        resetSelect = Content.Load<Texture2D>("Graphics/resetSelect");
                        resetRect = new Rectangle((int)resetPos.X, (int)resetPos.Y, 
                            resetTexture.Width, resetTexture.Height);
                        return;
                    }
            }
        }

        public Vector2 GetPosition(int x, int y)
        {
            Rectangle rect = new Rectangle(x * Block.Width, y * Block.Height, Block.Width, Block.Height);
            return new Vector2(rect.Left, rect.Top);
        }

        public void Dispose() { }


        #endregion

        #region Update and HandleInput

        public void Update(GameTime gameTime)
        {
            if (time < entryTime)
            { time += (float)gameTime.ElapsedGameTime.TotalSeconds; return; }

            isResetSelect = false;
            if (resetRect.Contains(mousePos))
            {
                isResetSelect = true;
                if (prevMouseState.LeftButton == ButtonState.Released &&
                    mouseState.LeftButton == ButtonState.Pressed)
                {
                    resetSound.Play();

                    reduceScore = false;
                    foreach (Block block in blocks)
                    {
                        block.State = BlockState.Return;
                        if (block.NotInPlace) reduceScore = true;
                    }
                }
            }

            foreach (Block block in blocks) block.Select = false;

            for (int i = 0; i < blocks.Count; i++)
            {
                blocks[i].Update(gameTime);

                // Draw Select box when in Ground
                if (blocks[i].BoundingRectMouse.Contains(mousePos) && blocks[i].State == BlockState.Ground)
                {
                    blocks[i].Select = true;
                    for (int j = 0; j < connectedBlocks[i].Count; j++)
                    {
                        blocks[connectedBlocks[i][j]].Select = true;
                    }
                }

                if (prevMouseState.LeftButton == ButtonState.Released &&
                    mouseState.LeftButton == ButtonState.Pressed)
                {
                    // Activate
                    if (blocks[i].State == BlockState.Ground && blocks[i].BoundingRectMouse.Contains(mousePos)
                        && !Block.IsActive)
                    {
                        blocks[i].State = BlockState.Active;

                        hitSounds[hitIndex.Next(3)].Play();

                        for (int j = 0; j < connectedBlocks[i].Count; j++)
                        {
                            blocks[connectedBlocks[i][j]].State = BlockState.Active;
                        }
                    }
                }

                // Collision Check
                foreach (Block block2 in blocks)
                    if (blocks[i] != block2 && blocks[i].BoundingRectangle.Intersects(block2.BoundingRectangle) &&
                        blocks[i].State == BlockState.Active && block2.State == BlockState.Die)
                        blocks[i].Collision(gameTime, block2.BoundingRectangle);
            }

            IsSolved = true;
            foreach (Block block in blocks)
            {
                if (!block.IsSolved) IsSolved = false;

                if (block.ShowScore) { tempScore += block.BlockNumber; Score += block.BlockNumber; }
            }

            if (reduceScore || scoreTime > 0)
            {
                if (reduceScore)
                {
                    reducedScore = tempScore + 10;
                    tempScore = 0;
                    Score -= reducedScore; reduceScore = false; scoreTime = 0;
                }

                scoreTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (scoreTime > maxScoreTime) scoreTime = 0;
            }
        }

        public void HandleInput(InputState input)
        {
            prevMouseState = mouseState;
            mouseState = Mouse.GetState();
            mousePos = new Point(mouseState.X, mouseState.Y);
        }

        #endregion

        #region Draw

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            foreach (Block block in blocks) block.DrawAtDestination(gameTime, spriteBatch);

            foreach (Block block in blocks) block.Draw(gameTime, spriteBatch);

            spriteBatch.Draw(puzzleBoard, Block.BoardPosition, Color.White);

            spriteBatch.Draw(resetTexture, resetRect, Color.White);

            if (isResetSelect)
                spriteBatch.Draw(resetSelect, resetRect, Color.White);

            if (scoreTime > 0)
                spriteBatch.DrawString(scoreFont, "-" + reducedScore.ToString(),
                    new Vector2(resetRect.X, resetRect.Y) - new Vector2(0, scoreTime * 25), Color.White);
        }

        #endregion
    }
}
