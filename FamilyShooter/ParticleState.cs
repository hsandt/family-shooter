using System;
using Microsoft.Xna.Framework;

namespace FamilyShooter
{
    public enum ParticleType { None, Enemy, Bullet, IgnoreGravity }
 
    public struct ParticleState
    {
        public Vector2 Velocity;
        public ParticleType Type;
        public float LengthMultiplier;
        
        public ParticleState(Vector2 velocity, ParticleType type, float lengthMultiplier = 1f)
        {
            Velocity = velocity;
            Type = type;
            LengthMultiplier = lengthMultiplier;
        }
        
        public static void UpdateParticle(ParticleManager<ParticleState>.Particle particle)
        {
            var vel = particle.State.Velocity;
     
            particle.Position += vel;
            particle.Orientation = vel.ToAngle();

            float speed = vel.Length();
            float alpha = Math.Min(1, Math.Min(particle.PercentLife * 2, speed * 1f));
            alpha *= alpha;

            Color particleTint = particle.Tint;
            particleTint.A = (byte)(255 * alpha);
            particle.Tint = particleTint;
 
            particle.Scale.X = particle.State.LengthMultiplier * Math.Min(Math.Min(1f, 0.2f * speed + 0.1f), alpha);
            
            // denormalized floats cause significant performance issues
            if (Math.Abs(vel.X) + Math.Abs(vel.Y) < 0.00000000001f)
                vel = Vector2.Zero;

            // particles gradually slow down
            if (particle.State.Type == ParticleType.Enemy)
            {
                vel *= 0.94f;       
            }
            else if (particle.State.Type == ParticleType.Bullet)
            {
                vel *= 0.94f;       
            }
            else if (particle.State.Type == ParticleType.IgnoreGravity)
            {
                vel *= 0.94f;       
            }
            else
            {
                // Player death particles slow down more slowly to be more epic
                vel *= 0.96f;
            }

            if (particle.State.Type != ParticleType.IgnoreGravity)
            {
                foreach (var blackHole in EntityManager.BlackHoles)
                {
                    Vector2 particleToBlackHole = blackHole.Position - particle.Position;
                    float distance = particleToBlackHole.Length();
                    Vector2 unit = particleToBlackHole / distance;
                    // when distance is >> 100, this ~ 10,000 / d^2
                    // when distance is << 100, this ~ 1
                    float addedSpeed = 10_000f / (distance * distance + 10_000f);
                    vel += addedSpeed * unit;

                    if (distance < 400f)
                    {
                        // add tangential component for stylish whirlpool effect
                        Vector2 tangentialVector = new Vector2(unit.Y, -unit.X);
                        vel += 45f * tangentialVector / (distance + 100f);
                    }
                }
            }

            Vector2 pos = particle.Position;
            int screenWidth = (int)GameRoot.ScreenSize.X;
            int screenHeight = (int)GameRoot.ScreenSize.Y;
            
            // Bounce on screen edges
            if (pos.X < 0 && vel.X < 0 || pos.X > screenWidth && vel.X > 0)
            {
                // tutorial doesn't check vel.X sign but sets it to +/-Abs anyway
                vel.X *= -1;
            }
            
            if (pos.Y < 0 && vel.Y < 0 || pos.Y > screenWidth && vel.Y > 0)
            {
                vel.Y *= -1;
            }
            
            particle.State.Velocity = vel;
        }
    }
}