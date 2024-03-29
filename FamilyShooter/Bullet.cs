using System;
using Microsoft.Xna.Framework;

namespace FamilyShooter
{
    public class Bullet : Entity
    {
        private static Random rand = new Random();

        /* Const */
        private const int BOUNCES_PER_BULLET = 1;
        private const int EXPLOSION_ON_WALL_PFX_COUNT = 30;

        /* State */
        private int m_BouncesLeft;
        private bool m_CanHitPlayerShip;
        public bool CanHitPlayerShip => m_CanHitPlayerShip;

        public Bullet(Vector2 spawnPosition, Vector2 velocity)
        {
            Position = spawnPosition;
            Velocity = velocity;
            Orientation = Velocity.ToAngle();

            image = Art.BulletYellow;
            CollisionRadius = 8f;

            m_BouncesLeft = BOUNCES_PER_BULLET;
            m_CanHitPlayerShip = false;
        }

        public override void Update()
        {
            Position += Velocity;

            // Check for dead zone: when bullet moves toward a wall and completely leaves screen in that direction, destroy it
            // The velocity check makes sure we don't destroy bullets that spawn outside screen and come inside,
            // which only happens when shooting a bullet while touching the screen edge and aiming at this edge,
            // which makes the bullet bounce but immediately hit the same wall again and be destroyed;
            // instead, with the velocity check, bullet will enter screen properly and generally destroy the Player Ship
            // due to friendly fire, being worse for the player but more consistent with behavior when shooting a wall
            // from a short distance.
            bool hitVerticalWall = Velocity.X < 0f && Position.X < - Size.X / 2f || Velocity.X > 0f && Position.X > GameRoot.ScreenSize.X + Size.X / 2f;
            bool hitHorizontalWall = Velocity.Y < 0f && Position.Y < -Size.Y / 2f || Velocity.Y > 0f && Position.Y > GameRoot.ScreenSize.Y + Size.Y / 2f;
            if (hitVerticalWall || hitHorizontalWall)
            {
                if (m_BouncesLeft <= 0)
                {
                    ExplodeOnWall();
                }
                else
                {
                    Bounce(hitVerticalWall, hitHorizontalWall);
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
            // GameRoot.Grid.ApplyExplosiveForce(150f * Velocity.Length(), new Vector3(Position, 0f), 80f);
        }

        private void ExplodeOnWall()
        {
            IsExpired = true;

            // Explosion PFX for style
            for (int i = 0; i < EXPLOSION_ON_WALL_PFX_COUNT; i++)
            {
                // Technically, angle should be opposite of wall/corner touched, but to simplify it can be any angle
                // although half of them will leave screen and won't be visible
                ParticleState state = new ParticleState
                    { Velocity = rand.NextVector2(0f, 9f), Type = ParticleType.Bullet, LengthMultiplier = 1f };
                GameRoot.ParticleManager.CreateParticle(Art.LineParticle, Position, Color.LightBlue, 50f, new Vector2(1f),
                    state);
            }
        }

        private void Bounce(bool hitVerticalWall, bool hitHorizontalWall)
        {
            if (hitVerticalWall)
            {
                Velocity.X *= -1;
            }
            if (hitHorizontalWall)
            {
                Velocity.Y *= -1;
            }

            // reorient them so their tail is still at the back
            Orientation = Velocity.ToAngle();

            // after bouncing once, bullets become friendly-fire with different shape and color
            m_CanHitPlayerShip = true;
            image = Art.BulletEnemyRed;

            --m_BouncesLeft;
        }
    }
}
