using System;
using Microsoft.Xna.Framework;

namespace FamilyShooter
{
    public class Bullet : Entity
    {
        private static Random rand = new Random();

        private const float speed = 8f;

        public Bullet(Vector2 spawnPosition, Vector2 velocity)
        {
            Position = spawnPosition;
            Velocity = velocity;
            Orientation = Velocity.ToAngle();

            image = Art.Bullet;
            CollisionRadius = 8f;
        }

        public override void Update()
        {
            Position += Velocity;

            // Check for dead zone: when bullet completely leaves screen, destroy it
            if (Position.X < - Size.X / 2f || Position.X > GameRoot.ScreenSize.X + Size.X / 2f ||
                Position.Y < - Size.Y / 2f || Position.Y > GameRoot.ScreenSize.Y + Size.Y / 2f)
            {
                IsExpired = true;

                // Explosion PFX for style
                for (int i = 0; i < 30; i++)
                {
                    // Technically, angle should be opposite of wall/corner touched, but to simplify it can be any angle
                    // although half of them will leave screen and won't be visible
                    ParticleState state = new ParticleState { Velocity = rand.NextVector2(0f, 9f), Type = ParticleType.Bullet, LengthMultiplier = 1f };
                    GameRoot.ParticleManager.CreateParticle(Art.LineParticle, Position, Color.LightBlue, 50f, new Vector2(1f), state);
                }
            }

            // Alternative from tutorial
            // I'd recommend Inflating by Size.X, Size.Y to ensure bullet has completely left screen
            // if (!GameRoot.Viewport.Bounds.Contains(Position.ToPoint()))
            // {
            //     IsExpired = true;
            // }

            // Remember to multiply by 60 frames, so 0.5 -> 30
            // For some reason, not enough since fixing anchor stiffness and damping, so increased
            // GameRoot.Grid.ApplyExplosiveForce(30f * Velocity.Length(), new Vector3(Position, 0f), 80f);
            GameRoot.Grid.ApplyExplosiveForce(150f * Velocity.Length(), new Vector3(Position, 0f), 80f);
        }
    }
}