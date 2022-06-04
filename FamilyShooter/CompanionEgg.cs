using System;
using Microsoft.Xna.Framework;

namespace FamilyShooter
{
    public class CompanionEgg : Entity
    {
        private const int EXPLOSION_PFX_COUNT = 1200;

        private static readonly Random rand = new Random();

        public CompanionEgg()
        {
            image = Art.CompanionEgg;
            Position = GameRoot.ScreenSize / 2f;
            CollisionRadius = 5;
        }

        public void Kill()
        {
            IsExpired = true;

            // PFX
            // kind of yellow
            Color particleColorYellow = new Color(0.8f, 0.8f, 0.4f);

            for (int i = 0; i < EXPLOSION_PFX_COUNT; i++)
            {
                float particleSpeed = 18f * (1f - 1f / rand.NextFloat(1f, 10f));
                Color particleColor = Color.Lerp(Color.White, particleColorYellow, rand.NextFloat(0f, 1f));
                var state = new ParticleState
                {
                    Velocity = rand.NextVector2(particleSpeed, particleSpeed),
                    Type = ParticleType.None,
                    LengthMultiplier = 1f
                };

                GameRoot.ParticleManager.CreateParticle(Art.LineParticle, Position, particleColor, 190f, new Vector2(1.5f), state);
            }

            // GameRoot.Grid.ApplyExplosiveForce(60f * 500f, new Vector3(Position, -80f), 150f, dampingModifier: 1f);
        }

        public void Clear()
        {
            IsExpired = true;
        }

        public override void Update()
        {
        }
    }
}