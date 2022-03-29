using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FamilyShooter
{
    public class PlayerShip : Entity
    {
        private static PlayerShip instance;

        public static PlayerShip Instance => instance ??= new PlayerShip();

        private const float speed = 8f;

        private const int cooldownFrames = 6;
        private int cooldownRemaining = 0;
        private static readonly Random rand = new Random();

        private const float bulletSpeed = 11f;
        private const float bulletSpawnForwardOffsetDistance = 25f;
        private const float bulletSpawnOrthogonalOffsetDistance = 8f;
        private const float bulletMaxDeviationAngleDeg = 2.29f;  // 0.04 rad * 57.29578 = 2.29 deg

        private const int respawnDuration = 90;  // frames (must be at least 1 to avoid scoring on death)
        private const int respawnDurationOnGameOver = 300;  // frames (must be at least 1 to avoid scoring on death)
        private const int EXPLOSION_PFX_COUNT = 1200;
        private int framesUntilRespawn = 0;  // frames
        public bool IsDead => framesUntilRespawn > 0;

        public PlayerShip()
        {
            image = Art.Player;
            Position = GameRoot.ScreenSize / 2f;
            CollisionRadius = 10;
        }

        public void Kill()
        {
            framesUntilRespawn = PlayerStatus.IsGameOver ? respawnDurationOnGameOver : respawnDuration;

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

            // reset spawner intensity
            EnemySpawner.Reset();

            PlayerStatus.LoseLife();

            // Clear screen to help player comeback
            // Note: bullets stay, but could be cleared too to avoid strange effects
            EntityManager.ClearAllEnemiesAndObstacles();

            // Added myself
            // offset on Z to try some 3D impulse
            // not so good, it moves instantly then takes time to come back...
            // even with dampingModifier set to preserve low damping
            GameRoot.Grid.ApplyExplosiveForce(60f * 5000f, new Vector3(Position, -1f), 150f, dampingModifier: 1f);
        }

        public override void Update()
        {
            if (IsDead)
            {
                framesUntilRespawn--;
                if (framesUntilRespawn == 0)
                {
                    GameRoot.Grid.ApplyDirectedForce(60f * 5000f * Vector3.Backward, new Vector3(Position, 0f), 50f);
                }
                return;
            }

            // Movement
            Velocity = speed * Input.GetMovementDirection();
            Position += Velocity;
            Position = Vector2.Clamp(Position, Size / 2f, GameRoot.ScreenSize - Size / 2f);

            if (Velocity != Vector2.Zero)
            {
                Orientation = Velocity.ToAngle();
            }

            // Fire
            if (cooldownRemaining > 0)
            {
                --cooldownRemaining;
            }

            Vector2 aimDirection = Input.GetAimDirection();
            if (aimDirection != Vector2.Zero)
            {
                if (cooldownRemaining <= 0)
                {
                    cooldownRemaining = cooldownFrames;

                    // My implementation

                    /*
                    // Spawn 2 bullets, each offset from center spawn position, orthogonally to fire orientation (not Ship Orientation!)
                    Vector2 forwardOffset = bulletSpawnForwardOffsetDistance * aimDirection;

                    float aimOrientation = aimDirection.ToAngle();
                    // Note: use non-deviated aim direction for orthogonal offset, as "turret" positions do not depend on firing spread
                    Vector2 orthogonalOffset = MathUtil.FromPolar(aimOrientation + MathHelper.PiOver2, bulletSpawnOrthogonalOffsetDistance);

                    // Random deviation
                    // https://github.com/MonoGame/MonoGame/pull/3789
                    // will add MathHelper.Random.NextFloat directly to MonoGame, but for now we use a custom extension of System.Random
                    float deviation = MathHelper.ToRadians(rand.NextFloat(-bulletMaxDeviationAngleDeg, bulletMaxDeviationAngleDeg));
                    float deviatedAimOrientation = aimOrientation + deviation;
                    Vector2 deviatedAimDirection = MathUtil.FromPolar(deviatedAimOrientation, 1f);

                    EntityManager.Add(new Bullet(Position + forwardOffset - orthogonalOffset, bulletSpeed * deviatedAimDirection));
                    EntityManager.Add(new Bullet(Position + forwardOffset + orthogonalOffset, bulletSpeed * deviatedAimDirection));
                    */

                    // Alternative: Tutorial implementation that leverages Transform

                    float aimAngle = aimDirection.ToAngle();
                    Quaternion aimQuat = Quaternion.CreateFromYawPitchRoll(0f, 0f, aimAngle);
                    // OR
                    // Quaternion aimQuat = Quaternion.CreateFromAxisAngle(Vector3.Backward, aimAngle);

                    // Sum two randoms to increase probability density at the center
                    float randomSpread = MathHelper.ToRadians(rand.NextFloat(-bulletMaxDeviationAngleDeg, bulletMaxDeviationAngleDeg) + rand.NextFloat(-bulletMaxDeviationAngleDeg, bulletMaxDeviationAngleDeg));
                    Vector2 bulletVelocity = MathUtil.FromPolar(aimAngle + randomSpread, bulletSpeed);

                    Vector2 baseOffsetLeft = new Vector2(bulletSpawnForwardOffsetDistance, -bulletSpawnOrthogonalOffsetDistance);
                    Vector2 rotatedOffsetLeft = Vector2.Transform(baseOffsetLeft, aimQuat);
                    EntityManager.Add(new Bullet(Position + rotatedOffsetLeft, bulletVelocity));

                    Vector2 baseOffsetRight = new Vector2(bulletSpawnForwardOffsetDistance, bulletSpawnOrthogonalOffsetDistance);
                    Vector2 rotatedOffsetRight = Vector2.Transform(baseOffsetRight, aimQuat);
                    EntityManager.Add(new Bullet(Position + rotatedOffsetRight, bulletVelocity));

                    Sound.GetRandomShot().Play(0.2f, rand.NextFloat(-0.2f, 0.2f), 0f);
                }
            }

            // PFX
            MakeExhaustFire();
        }

        private void MakeExhaustFire()
        {
            if (Velocity.LengthSquared() > 0.1f)
            {
                Color sideColor = new Color(200, 38, 9);    // deep red
                Color midColor = new Color(255, 187, 30);   // orange-yellow
                const float alpha = 0.7f;

                Quaternion rot = Quaternion.CreateFromYawPitchRoll(0f, 0f, Orientation);
                double t = GameRoot.GameTime.TotalGameTime.TotalSeconds;

                Vector2 backwardDir = - MathUtil.FromPolar(Orientation, 1f);
                Vector2 baseVel = 3f * backwardDir;
                // Vector2 backwardDirRandomized = - MathUtil.FromPolar(Orientation + MathHelper.ToRadians(rand.NextFloat(-5f, 5f)), 1f);

                // Our idea: angle variation (can also work with Quaternion rotation)
                // float angleVariation = 10f * (float)Math.Sin((float)t / 0.3f * MathHelper.TwoPi);
                // Vector2 backwardDirSideLeft = - MathUtil.FromPolar(Orientation + MathHelper.ToRadians(angleVariation), 1f);
                // Vector2 backwardDirSideRight = - MathUtil.FromPolar(Orientation + MathHelper.ToRadians(-angleVariation), 1f);

                // Tutorial idea: orthogonal component
                Vector2 orthogonalVelOffset = 0.6f * new Vector2(baseVel.Y, -baseVel.X) * (float)Math.Sin(t * 10);

                float lifetime = 60f;

                // Vector2 particleVel = 10f * backwardDirRandomized;
                // tutorial prefer randomizing offset vector rather than angle
                Vector2 particleVel = 3f * backwardDir + rand.NextVector2(0f, 1f);
                // or add Vector2.Transform(new Vector2(-25, 0), rot)
                Vector2 particleSpawnPos = Position + 25f * backwardDir;
                // of course it's not Enemy, just to avoid the None slow down effect
                var state = new ParticleState(particleVel, ParticleType.Enemy);

                GameRoot.ParticleManager.CreateParticle(Art.LineParticle, particleSpawnPos, Color.White * alpha, lifetime, new Vector2(0.5f, 1f), state);
                GameRoot.ParticleManager.CreateParticle(Art.Glow, particleSpawnPos, midColor * alpha, lifetime, new Vector2(0.5f, 1f), state);

                // Vector2 particleLeftVel = 10f * backwardDirSideLeft;
                Vector2 particleLeftVel = baseVel + orthogonalVelOffset;
                var stateLeft = new ParticleState(particleLeftVel, ParticleType.Enemy);

                GameRoot.ParticleManager.CreateParticle(Art.LineParticle, particleSpawnPos, Color.White * alpha, lifetime, new Vector2(0.5f, 1f), stateLeft);
                GameRoot.ParticleManager.CreateParticle(Art.Glow, particleSpawnPos, sideColor * alpha, lifetime, new Vector2(0.5f, 1f), stateLeft);

                // Vector2 particleRightVel = 10f * backwardDirSideRight;
                Vector2 particleRightVel = baseVel - orthogonalVelOffset;
                var stateRight = new ParticleState(particleRightVel, ParticleType.Enemy);

                GameRoot.ParticleManager.CreateParticle(Art.LineParticle, particleSpawnPos, Color.White * alpha, lifetime, new Vector2(0.5f, 1f), stateRight);
                GameRoot.ParticleManager.CreateParticle(Art.Glow, particleSpawnPos, sideColor * alpha, lifetime, new Vector2(0.5f, 1f), stateRight);
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsDead)
            {
                base.Draw(spriteBatch);
            }
        }
    }
}