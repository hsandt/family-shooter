using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FamilyShooter
{
    public static class Extensions
    {
        public static float ToAngle(this Vector2 vector)
        {
            return (float) Math.Atan2(vector.Y, vector.X);
        }

        public static Vector2 Rotated(this Vector2 vector, float angle)
        {
            Quaternion quat = Quaternion.CreateFromAxisAngle(Vector3.Backward, angle);
            return Vector2.Transform(vector, quat);
        }

        /// Return copy of vector scaled to a certain magnitude
        /// (Tutorial uses it but never indicated it's a custom extension, so I had to write it myself)
        public static Vector2 ScaleTo(this Vector2 vector, float magnitude)
        {
            // Tutorial source always uses `return vector * (length / vector.Length());`
            // but I want to avoid crash on Zero
            if (vector != Vector2.Zero)
            {
                return Vector2.Normalize(vector) * magnitude;
            }

            return Vector2.Zero;
        }

        /// Return vector displaced towards target by a distance maxDelta at most
        public static Vector2 Towards(this Vector2 vector, Vector2 target, float maxDelta)
        {
            Vector2 delta = target - vector;
            float deltaLengthSquared = delta.LengthSquared();

            if (deltaLengthSquared <= maxDelta * maxDelta)
            {
                return target;
            }

            // if deltaLengthSquared is 0f, we've returned early above, so it's safe to divide
            return vector + maxDelta / MathF.Sqrt(deltaLengthSquared) * delta;
        }

        public static float NextFloat(this Random rand, float minValue, float maxValue)
        {
            return (float) rand.NextDouble() * (maxValue - minValue) + minValue;
        }

        public static Vector2 NextVector2(this Random rand, float minLength, float maxLength)
        {
            // Custom implementation via rotation. Tutorial project source uses NextDouble and trigonometry
            Quaternion randomRotationQuat = Quaternion.CreateFromYawPitchRoll(0f, 0f, rand.NextFloat(0f, MathHelper.TwoPi));
            float length = rand.NextFloat(minLength, maxLength);
            return Vector2.Transform(new Vector2(length, 0f), randomRotationQuat);
        }

        public static void DrawLine(this SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness = 2f)
        {
            Vector2 delta = end - start;
            spriteBatch.Draw(Art.Pixel, start, null, color, delta.ToAngle(), 0.5f * Vector2.UnitY, new Vector2(delta.Length(), thickness), SpriteEffects.None, 0f);
        }

        public static void DrawRect(this SpriteBatch spriteBatch, Vector2 corner1, Vector2 corner2, Color color)
        {
            spriteBatch.Draw(Art.Pixel, corner1, null, color, 0f, Vector2.Zero, corner2 - corner1, SpriteEffects.None, 0f);
        }
    }
}
