using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FamilyShooter
{
    public class BlackHole : Entity
    {
        private static Random rand = new Random();

        private int hitpoints = 10;

        private float sprayAngle = 0;

        /* Parameters*/
        private const int SHOT_PFX_COUNT = 30;

        public BlackHole(Vector2 position)
        {
            image = Art.BlackHole;
            Position = position;
            CollisionRadius = image.Width / 2f;
        }

        public void WasShot()
        {
            hitpoints--;

            float hue = (float)(3 * GameRoot.InGameTimeSpan.TotalSeconds % 6);
            Color particleColor = ColorUtil.HSVToColor(hue, 0.25f, 1f);
            // add start offset just to avoid having particles spawned exactly on the same star branches
            // so we have more varied angles
            float startOffset = rand.NextFloat(0, MathHelper.TwoPi / SHOT_PFX_COUNT);

            for (int i = 0; i < SHOT_PFX_COUNT; i++)
            {
                Vector2 sprayVel = MathUtil.FromPolar(MathHelper.TwoPi * i / SHOT_PFX_COUNT + startOffset, rand.NextFloat(8, 16));
                var state = new ParticleState
                {
                    Velocity = sprayVel,
                    Type = ParticleType.IgnoreGravity,
                    LengthMultiplier = 1f
                };

                GameRoot.ParticleManager.CreateParticle(Art.LineParticle, Position, particleColor, 190f, new Vector2(1.5f), state);
            }

            if (hitpoints <= 0)
                IsExpired = true;
        }

        public void Kill()
        {
            hitpoints = 0;
            WasShot();
        }

        public override void Update()
        {
            var entities = EntityManager.GetNearbyEntities(Position, 250);

            foreach (var entity in entities)
            {
                if (entity is Enemy && !(entity as Enemy).IsActive)
                    continue;

                // bullets are repelled by black holes and everything else is attracted
                if (entity is Bullet)
                    entity.Velocity += (entity.Position - Position).ScaleTo(0.3f);
                else
                {
                    var dPos = Position - entity.Position;
                    var length = dPos.Length();

                    entity.Velocity += dPos.ScaleTo(MathHelper.Lerp(2, 0, length / 250f));
                }
            }

            // Spray PFX
            Color particleColor = ColorUtil.HSVToColor(5, 0.5f, 0.8f);  // light purple
            const float period = 0.8f;
            // The black holes spray some orbiting particles. The spray toggles on and off every quarter second.
            if ((GameRoot.InGameTimeSpan.Milliseconds / 250) % 2 == 0)
            {
                // Note: unlike Tutorial, I consider spray angle the angle of the spawn position, and velocity is tangential,
                // instead of the reverse
                sprayAngle -= MathHelper.TwoPi * (float)GameRoot.GameTime.ElapsedGameTime.TotalSeconds / period;
                Vector2 normalVector = MathUtil.FromPolar(sprayAngle, 1f);
                Vector2 position = Position + 2f * normalVector + rand.NextVector2(4, 8);;
                var state = new ParticleState
                {
                    Velocity = rand.NextFloat(12f, 15f) * new Vector2(normalVector.Y, -normalVector.X),
                    Type = ParticleType.Enemy,
                    LengthMultiplier = 1f
                };

                GameRoot.ParticleManager.CreateParticle(Art.LineParticle, position, particleColor, 190f, new Vector2(1.5f), state);
            }

            // GameRoot.Grid.ApplyImplosiveForce(60f * (MathF.Sin(sprayAngle / 2) * 10f + 20f), new Vector3(Position, 0f), 200f);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // make the size of the black hole pulsate
            float scale = 1 + 0.1f * (float)Math.Sin(10 * GameRoot.InGameTimeSpan.TotalSeconds);
            spriteBatch.Draw(image, Position, null, color, Orientation, Size / 2f, scale, 0, 0);
        }
    }
}