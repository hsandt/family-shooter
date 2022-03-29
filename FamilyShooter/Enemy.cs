using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FamilyShooter
{
    public class Enemy : Entity
    {
        private static Random rand = new Random();

        /* Parameters */
        private const int durationUntilActive = 60;  // frames
        private readonly Color transparentWhite = new Color(1f, 1f, 1f, 0f);
        const float friction = 0.8f;
        const float moveRandomlyAngleMaxDeltaDeg = 5.73f;  // 0.1 rad in deg
        const int moveRandomlyAngleChangePeriod = 6; // frames
        private const int EXPLOSION_PFX_COUNT = 60;

        public int RewardScore { get; private set; }

        /* State */
        private int timeUntilActive = durationUntilActive;  // frames
        public bool IsActive => timeUntilActive <= 0;

        private List<IEnumerator<int>> behaviours = new List<IEnumerator<int>>();

        public Enemy(Texture2D image, Vector2 position, int rewardScore)
        {
            this.image = image;
            Position = position;
            CollisionRadius = image.Width / 2f;
            color = Color.Transparent;

            RewardScore = rewardScore;
        }

        public override void Update()
        {
            if (timeUntilActive > 0)
            {
                --timeUntilActive;

                // ! Use Transparent White, not Transparent which is black and would mess up the lerp
                // Tutorial suggests to multiply Color, but in fact it also multiplies all components, so same issue
                // On a black background, I guess it would still work though
                color = Color.Lerp(Color.White, transparentWhite, (float) timeUntilActive / durationUntilActive);
            }
            else
            {
                // Active behaviour
                ApplyBehaviours();
            }

            // Movement
            Position += Velocity;

            // For now, clamp to screen (enemies should not leave on their own)
            Position = Vector2.Clamp(Position, Size / 2f, GameRoot.ScreenSize - Size / 2f);

            // Friction
            // If accel is constant (and same direction), friction will make velocity reach a terminal velocity
            // equal to:
            // v_terminal = accel * friction / (1-friction)
            Velocity *= friction;
        }

        public void ClearWithExplosion()
        {
            // PlayerStatus.AddScoreForDestruction has some check for living Player,
            // but still cleaner to distinguish scoring shot from silent clearance on player ship death
            // to avoid scoring a lot on death via auto-clean
            IsExpired = true;

            // just for style (no score)
            PlayExplosionPFX();
            Sound.GetRandomExplosion().Play(0.5f, rand.NextFloat(-0.2f, 0.2f), 0f);
        }

        public void WasShot()
        {
            IsExpired = true;

            PlayerStatus.AddScoreForDestruction(this);

            PlayExplosionPFX();
            Sound.GetRandomExplosion().Play(0.5f, rand.NextFloat(-0.2f, 0.2f), 0f);
        }

        private void PlayExplosionPFX()
        {
            float hue1 = rand.NextFloat(0, 6);
            float hue2 = (hue1 + rand.NextFloat(0, 2)) % 6f;
            Color color1 = ColorUtil.HSVToColor(hue1, 0.5f, 1);
            Color color2 = ColorUtil.HSVToColor(hue2, 0.5f, 1);

            for (int i = 0; i < EXPLOSION_PFX_COUNT; i++)
            {
                float speed = 18f * (1f - 1 / rand.NextFloat(1f, 10f));
                var state = new ParticleState()
                {
                    Velocity = rand.NextVector2(speed, speed),
                    Type = ParticleType.Enemy,
                    LengthMultiplier = 1f
                };

                Color color = Color.Lerp(color1, color2, rand.NextFloat(0, 1));
                GameRoot.ParticleManager.CreateParticle(Art.LineParticle, Position, color, 190f, new Vector2(1.5f), state);
            }
        }

        private void AddBehaviour(IEnumerable<int> behaviour)
        {
            behaviours.Add(behaviour.GetEnumerator());
        }

        private void ApplyBehaviours()
        {
            for (int i = 0; i < behaviours.Count; i++)
            {
                if (!behaviours[i].MoveNext())
                {
                    // Behaviour ended, remove it and remember to post-decrement i as were are still iterating on it
                    behaviours.RemoveAt(i--);
                }
            }
        }

        public void HandleCollision(Enemy other)
        {
            var distance = Position - other.Position;
            Velocity += 10f * distance / (distance.LengthSquared() + 1f);
        }

        /* Behaviours */

        IEnumerable<int> FollowPlayer(float acceleration = 1f)
        {
            while (true)
            {
                Velocity += (PlayerShip.Instance.Position - Position).ScaleTo(acceleration);
                if (Velocity != Vector2.Zero)
                {
                    Orientation = Velocity.ToAngle();
                }

                yield return 0;
            }
        }

        IEnumerable<int> MoveRandomly(float speed = 0.4f)
        {
            float directionAngle = rand.NextFloat(0f, MathHelper.TwoPi);

            while (true)
            {
                directionAngle += MathHelper.ToRadians(rand.NextFloat(-moveRandomlyAngleMaxDeltaDeg, moveRandomlyAngleMaxDeltaDeg));
                directionAngle = MathHelper.WrapAngle(directionAngle);

                for (int i = 0; i < moveRandomlyAngleChangePeriod; i++)
                {
                    // Tutorial suggests +=, but I rather set Velocity directly for a clearer effect
                    // Velocity += MathUtil.FromPolar(directionAngle, speed);
                    Velocity = MathUtil.FromPolar(directionAngle, speed);
                    Orientation -= 0.05f;  // cosmetic rotation

                    var bounds = GameRoot.Viewport.Bounds;
                    // anti-inflate to shrink (Tutorial say full extent, but that leaves a margin,
                    // so divided by 2 to actually change direction when touching screen edge)
                    bounds.Inflate(-image.Width / 2, -image.Height / 2);

                    // if enemy edge is touching bounds, move away by moving toward center with some
                    // deviation
                    if (!bounds.Contains(Position.ToPoint()))
                    {
                        // deviation of pi/2 is a little big, we may hit screen edge again but that's fine
                        directionAngle = (GameRoot.ScreenSize / 2f - Position).ToAngle() +
                                         rand.NextFloat(-MathHelper.PiOver2, MathHelper.PiOver2);
                    }

                    yield return 0;
                }
            }
        }

        /* Enemy types */

        public static Enemy CreateSeeker(Vector2 position)
        {
            var enemy = new Enemy(Art.Seeker, position, 3);
            enemy.AddBehaviour(enemy.FollowPlayer());

            return enemy;
        }

        public static Enemy CreateWanderer(Vector2 position)
        {
            var enemy = new Enemy(Art.Wanderer, position, 1);
            enemy.AddBehaviour(enemy.MoveRandomly());

            return enemy;
        }
    }
}