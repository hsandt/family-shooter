using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FamilyShooter
{
    public class CompanionShip : Entity
    {
        private const float MAX_SPEED_TO_TARGET = 800f;
        private const float OFFSET_FROM_PLAYER_SHIP = 50f;
        private const int EXPLOSION_PFX_COUNT = 1200;

        private const float bulletSpeed = 11f;
        private const float bulletSpawnForwardOffsetDistance = 35f;

        private static readonly Random rand = new Random();

        /* State */

        public bool IsAttachedToPlayerShip => m_AttachmentIndex >= 0;

        private int m_AttachmentIndex;

        /// Angle it should be located at around the Player Ship (only if IsAttachedToPlayerShip is true)
        private float angleAroundPlayerShip;

        public CompanionShip()
        {
            image = Art.CompanionShip;
            color = new Color(20, 255, 0);
            Position = GameRoot.ScreenSize / 2f;
            CollisionRadius = 5;

            m_AttachmentIndex = -1;
            angleAroundPlayerShip = 0f;
        }

        public override void Update()
        {
            if (IsAttachedToPlayerShip && !PlayerShip.Instance.IsDead)
            {
                // on the right of player ship
                Vector2 offset = OFFSET_FROM_PLAYER_SHIP * Vector2.UnitX;
                Vector2 rotatedOffset = offset.Rotated(angleAroundPlayerShip);
                Vector2 targetPosition = PlayerShip.Instance.Position + rotatedOffset;

                // Movement: no need to even set Velocity, just move position at max speed toward target
                float maxMotion = MAX_SPEED_TO_TARGET * (float)GameRoot.GameTime.ElapsedGameTime.TotalSeconds;
                Position = Position.Towards(targetPosition, maxMotion);
                Orientation = angleAroundPlayerShip;
            }
            else
            {

            }
        }

        public void OnAttachToPlayerShipWith(int attachmentIndex)
        {
            m_AttachmentIndex = attachmentIndex;
        }

        public void OnDetachFromPlayerShip()
        {
            m_AttachmentIndex = -1;
        }

        public void SetBaseAngleAroundPlayerShip(float baseAngleAroundPlayerShip)
        {
            angleAroundPlayerShip = baseAngleAroundPlayerShip + GetAngleOffsetFromIndex();
        }

        private float GetAngleOffsetFromIndex()
        {
            switch (m_AttachmentIndex)
            {
                // a case for every index < MAX_COMPANIONS_COUNT
                case 0:
                    return MathHelper.ToRadians(25f);
                case 1:
                    return MathHelper.ToRadians(-25f);
                case 2:
                    return MathHelper.ToRadians(50f);
                case 3:
                    return MathHelper.ToRadians(-50f);
                default:
                    throw new ArgumentOutOfRangeException(nameof(m_AttachmentIndex), $"Unsupported attachment index {m_AttachmentIndex}");
            }

            // return 0f;
        }

        public void Shoot(float aimAngle)
        {
            Quaternion aimQuat = Quaternion.CreateFromAxisAngle(Vector3.Backward, aimAngle);
            Vector2 bulletVelocity = MathUtil.FromPolar(aimAngle, bulletSpeed);
            Vector2 baseOffset = new Vector2(bulletSpawnForwardOffsetDistance, 0f);
            Vector2 rotatedOffset = Vector2.Transform(baseOffset, aimQuat);

            EntityManager.Add(new Bullet(Position + rotatedOffset, bulletVelocity));

            Sound.GetRandomShot().Play(0.2f, rand.NextFloat(-0.2f, 0.2f), 0f);
        }

        public void Kill()
        {
            IsExpired = true;

            if (IsAttachedToPlayerShip)
            {
                PlayerShip.Instance.DetachCompanion(this);
            }

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

            GameRoot.Grid.ApplyExplosiveForce(60f * 500f, new Vector3(Position, -80f), 150f, dampingModifier: 1f);
        }
    }
}