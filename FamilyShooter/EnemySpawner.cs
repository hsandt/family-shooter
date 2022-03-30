using System;
using Microsoft.Xna.Framework;

namespace FamilyShooter
{
    public static class EnemySpawner
    {
        private static Random rand = new Random();
        private const float initialInverseSpawnChance = 60f;
        private static float inverseSpawnChance = initialInverseSpawnChance;
        private const float initialInverseBlackHoleSpawnChance = 600f;
        private const int maxBlackHolesCount = 2;

        public static void Update()
        {
            // Avoid spawning enemies while player ship is dead / game over or it may come back right on an active enemy,
            // even with screen clearance
            if (!PlayerShip.Instance.IsDead)
            {
                // Floor to int. Upper bound is exclusive. inverseSpawnChance must be >= 1 at all times.
                if (rand.Next((int)inverseSpawnChance) == 0)
                {
                    SpawnEnemy(Enemy.CreateSeeker(GetRandomSpawnPosition()));
                }
                if (rand.Next((int)inverseSpawnChance) == 0)
                {
                    SpawnEnemy(Enemy.CreateWanderer(GetRandomSpawnPosition()));
                }

                // slowly increase the spawn rate as time progresses, until 1/20 frames (in average, 1 spawn of each enemy type
                // every 1/3 s)
                if (inverseSpawnChance > 20)
                {
                    // every 200 frames (3.3s, reduce inverseSpawnChance by 1, so after 40*3.3=133.3 s, we reach max spawn)
                    inverseSpawnChance -= 0.005f;
                }

                if (EntityManager.BlackHoleCount < maxBlackHolesCount &&
                    rand.Next((int) initialInverseBlackHoleSpawnChance) == 0)
                {
                    EntityManager.Add(new BlackHole(GetRandomSpawnPosition()));
                }
            }
        }

        private static void SpawnEnemy(Enemy enemy)
        {
            EntityManager.Add(enemy);
            Sound.GetRandomSpawn().Play(0.5f, rand.NextFloat(-0.2f, 0.2f), 0f);
        }

        private static Vector2 GetRandomSpawnPosition()
        {
            // pixels are precise enough so no need for Random.NextFloat, Next integer is okay
            Vector2 pos = new Vector2(rand.Next((int)GameRoot.ScreenSize.X), rand.Next((int)GameRoot.ScreenSize.Y));
            return pos;
        }

        public static void Reset()
        {
            inverseSpawnChance = initialInverseSpawnChance;
        }
    }
}