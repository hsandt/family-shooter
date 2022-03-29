using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FamilyShooter
{
    public abstract class Entity
    {
        /* Sprite */
        protected Texture2D image;

        /// Color tint, including transparency
        protected Color color = Color.White;

        /* Transform & Physics */
        public Vector2 Position;
        public Vector2 Velocity;
        public float Orientation;
        public float CollisionRadius = 20f;

        /// True if entity was destroyed and should be deleted
        public bool IsExpired = false;

        public Vector2 Size => image == null ? Vector2.Zero : new Vector2(image.Width, image.Height);

        public abstract void Update();

        public virtual void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(image, Position, null, color, Orientation, Size / 2f, 1f, SpriteEffects.None, 0f);
        }
    }
}