using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FamilyShooter
{
    public static class Art
    {
        public static Texture2D Player { get; private set; }
        public static Texture2D CompanionEgg { get; private set; }
        public static Texture2D CompanionShip { get; private set; }
        public static Texture2D Seeker { get; private set; }
        public static Texture2D Wanderer { get; private set; }
        public static Texture2D Bullet { get; private set; }
        public static Texture2D BlackHole { get; private set; }
        public static Texture2D LineParticle { get; private set; }
        public static Texture2D Glow { get; private set; }
        public static Texture2D BGSky { get; private set; }
        public static Texture2D BGCloud1 { get; private set; }
        public static Texture2D BGCloud2 { get; private set; }
        public static Texture2D BGCloud3 { get; private set; }
        public static Texture2D Pixel { get; private set; }
        public static Texture2D Pointer { get; private set; }
        public static SpriteFont Font { get; private set; }

        public static void Load(ContentManager content)
        {
            Player = content.Load<Texture2D>("Art/PlayerShip");
            CompanionEgg = content.Load<Texture2D>("Art/CompanionEgg");
            CompanionShip = content.Load<Texture2D>("Art/CompanionShip");
            Seeker = content.Load<Texture2D>("Art/Seeker");
            Wanderer = content.Load<Texture2D>("Art/Wanderer");
            Bullet = content.Load<Texture2D>("Art/Bullet");
            BlackHole = content.Load<Texture2D>("Art/Black Hole");
            LineParticle = content.Load<Texture2D>("Art/Laser");
            Glow = content.Load<Texture2D>("Art/Glow");

            // Background
            BGSky = content.Load<Texture2D>("Art/BG_Sky");
            BGCloud1 = content.Load<Texture2D>("Art/BG_Cloud1");
            BGCloud2 = content.Load<Texture2D>("Art/BG_Cloud2");
            BGCloud3 = content.Load<Texture2D>("Art/BG_Cloud3");

            // Create pixel procedurally
            Pixel = new Texture2D(Player.GraphicsDevice, 1, 1);
            Pixel.SetData(new[] {Color.White});

            Pointer = content.Load<Texture2D>("Art/Pointer");
            Font = content.Load<SpriteFont>("Font/Nova Square");
        }
    }
}