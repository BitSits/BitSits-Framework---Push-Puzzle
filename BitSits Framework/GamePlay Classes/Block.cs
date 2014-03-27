using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace BitSits_Framework
{
    enum BlockState
    { Ground, Active, Die, Return }

    class Block
    {
        public static Vector2 BoardPosition;

        public static bool IsActive { get; private set; }

        public bool Select { get; set; }

        public bool IsSolved { get { return position == destPosition; } }

        public bool NotInPlace { get { return position != oriPosition; } }

        public const int Width = 30;
        public const int Height = 30;

        public bool ShowScore { get; private set; }
        private float time, scoreShowTime = 5f;
        private SpriteFont scoreFont;

        public BlockState State { get; set; }

        private Texture2D texture, selectTextture, background;
        private Vector2 position, oriPosition, destPosition, maxSlidePos, direction;

        public int BlockNumber { get; private set; }

        public Block(ContentManager content, Vector2 position, char number)
        {
            this.position = oriPosition = position;
            BlockNumber = number >= 'a' ? number - 'a' + 10 : number - '0';
            State = BlockState.Ground;
            IsActive = false;

            texture = content.Load<Texture2D>("Graphics/PuzzleBlocks/" + number);
            selectTextture = content.Load<Texture2D>("Graphics/PuzzleBlocks/select");
            background = content.Load<Texture2D>("Graphics/PuzzleBlocks/background");

            scoreFont = content.Load<SpriteFont>("Fonts/blockScoreFont");
        }

        public void SetDirection(int N)
        {
            Point blockIndex = new Point((BlockNumber) % N, (BlockNumber) / N);
            destPosition = BoardPosition + new Vector2(blockIndex.X * Width, blockIndex.Y * Height);

            direction = new Vector2(destPosition.X != position.X ? destPosition.X < position.X ? -1 : 1 : 0,
                destPosition.Y != position.Y ? destPosition.Y < position.Y ? -1 : 1 : 0);

            if (direction.X != 0) { blockIndex = new Point(direction.X < 0 ? 0 : (N - 1), blockIndex.Y); }
            else { blockIndex = new Point(blockIndex.X, direction.Y < 0 ? 0 : (N - 1)); }

            maxSlidePos = BoardPosition + new Vector2(blockIndex.X * Width, blockIndex.Y * Height);
        }

        public Rectangle BoundingRectangle
        { get { return new Rectangle((int)position.X, (int)position.Y, Width, Height); } }

        public Rectangle BoundingRectMouse
        { get { Rectangle a = BoundingRectangle; a.Inflate(1, 1); return a; } }

        public void Update(GameTime gameTime)
        {
            ShowScore = false;
            if (State == BlockState.Active)
            {
                Move(direction, maxSlidePos);
                if (position == maxSlidePos)
                {
                    State = BlockState.Die;
                    if (maxSlidePos == destPosition) ShowScore = true;
                }
            }
            else if (State == BlockState.Return)
            {
                Move(direction * -1, oriPosition);
                if (position == oriPosition) State = BlockState.Ground;
            }

            if (ShowScore || time > 0)
            {
                if (ShowScore) time = 0;
                time += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (time > scoreShowTime) time = 0;
            }
        }

        private void Move(Vector2 direction, Vector2 destination)
        {
            IsActive = true;
            position += direction * 15;

            float small, big;

            if (direction.X != 0)
            {
                small = position.X; big = destination.X;
                if (direction.X < 0) { big = position.X; small = destination.X; }
            }
            else
            {
                small = position.Y; big = destination.Y;
                if (direction.Y < 0) { big = position.Y; small = destination.Y; }
            }

            if (small >= big) { position = destination; IsActive = false; }
        }

        public void Collision(GameTime gameTime, Rectangle bounds)
        {
            if (State != BlockState.Active) return;

            State = BlockState.Die; IsActive = false;

            if (direction.X != 0) { position.X = bounds.Left - direction.X * Width; }
            else if (direction.Y != 0) { position.Y = bounds.Top - direction.Y * Height; }

            if (IsSolved) ShowScore = true;
            if (ShowScore || time > 0)
            {
                if (ShowScore) time = 0;
                time += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (time > scoreShowTime) time = 0;
            }
        }

        public void DrawAtDestination(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture, destPosition, new Color(Color.White, .3f));
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(background, position, Color.White);
            spriteBatch.Draw(texture, position, Color.CornflowerBlue);

            if (Select) spriteBatch.Draw(selectTextture, position, Color.White);

            if (time > 0)
                spriteBatch.DrawString(scoreFont, "+" + BlockNumber.ToString(),
                    destPosition - new Vector2(0, time * 25), Color.White);
        }
    }
}
