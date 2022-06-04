using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FamilyShooter
{
    public class EntityManager
    {
        // if you add a lst here, make sure to update it on Update when entities are Expired
        private static List<Entity> entities = new List<Entity>();

        private static List<CompanionEgg> companionEggs = new List<CompanionEgg>();

        private static List<CompanionShip> companionShips = new List<CompanionShip>();
        public static List<CompanionShip> CompanionShips => companionShips;

        private static List<Enemy> enemies = new List<Enemy>();
        private static List<Bullet> bullets = new List<Bullet>();
        private static List<BlackHole> blackHoles = new List<BlackHole>();
        public static List<BlackHole> BlackHoles => blackHoles;

        public static int BlackHoleCount => blackHoles.Count;

        /// Track if we are iterating on entities
        private static bool isUpdating = false;

        /// Intermediate list storing entities to be added once the update iteration is over
        /// This allows us avoid adding entities during iteration on entities, which would change
        /// the iterated container and cause an exception
        private static readonly List<Entity> addedEntities = new List<Entity>();

        public static int Count => entities.Count;

        public static void Add(Entity entity)
        {
            if (!isUpdating)
            {
                AddEntity(entity);
            }
            else
            {
                addedEntities.Add(entity);
            }
        }

        public static void Update()
        {
            isUpdating = true;

            HandleCollisions();

            foreach (var entity in entities)
            {
                entity.Update();
            }

            isUpdating = false;

            foreach (var entity in addedEntities)
            {
                AddEntity(entity);
            }

            addedEntities.Clear();

            // May not be super efficient, I guess the best is to have a double buffer or so
            entities = entities.Where(x => !x.IsExpired).ToList();
            bullets = bullets.Where(x => !x.IsExpired).ToList();
            enemies = enemies.Where(x => !x.IsExpired).ToList();
            companionEggs = companionEggs.Where(x => !x.IsExpired).ToList();
            companionShips = companionShips.Where(x => !x.IsExpired).ToList();
            blackHoles = blackHoles.Where(x => !x.IsExpired).ToList();
        }

        private static void AddEntity(Entity entity)
        {
            entities.Add(entity);

            switch (entity)
            {
                case Bullet bullet:
                    bullets.Add(bullet);
                    break;
                case CompanionEgg companionEgg:
                    companionEggs.Add(companionEgg);
                    break;
                case CompanionShip companionShip:
                    companionShips.Add(companionShip);
                    break;
                case Enemy enemy:
                    enemies.Add(enemy);
                    break;
                case BlackHole blackHole:
                    blackHoles.Add(blackHole);
                    break;
            }
        }

        public static void SpawnCompanionEgg(Vector2 position)
        {
            var companionEgg = new CompanionEgg { Position = position };
            Add(companionEgg);
        }

        private static void TrySpawnCompanionShipAndAttachToPlayerShip()
        {
            CompanionShip companionShip = PlayerShip.Instance.TryAttachCompanion();
            if (companionShip != null)
            {
                Add(companionShip);
            }
        }

        private static bool IsColliding(Entity a, Entity b, float extraRadius = 0f)
        {
            // Apply positive extra radius to make it easier to collide (or negative extra radius to make it harder)
            float radiusSum = a.CollisionRadius + b.CollisionRadius + extraRadius;
            return !a.IsExpired && !b.IsExpired && Vector2.DistanceSquared(a.Position, b.Position) < radiusSum * radiusSum;
        }

        private static void HandleCollisions()
        {
            // handle collision between enemies
            for (int i = 0; i < enemies.Count - 1; i++)
            {
                // triangle iteration to avoid redundant / same entity collision check
                for (int j = i + 1; j < enemies.Count; j++)
                {
                    if (IsColliding(enemies[i], enemies[j]))
                    {
                        enemies[i].HandleCollision(enemies[j]);
                        enemies[j].HandleCollision(enemies[i]);
                    }
                }
            }

            // handle collision between bullets and ...
            foreach (Bullet bullet in bullets)
            {
                // ... player ship (only for friendly-fire bullets)
                if (bullet.CanHitPlayerShip && IsColliding(PlayerShip.Instance, bullet))
                {
                    PlayerShip.Instance.Kill();
                    bullet.IsExpired = true;
                }

                // ... companion egg (only for friendly-fire bullets)
                foreach (CompanionEgg companionEgg in companionEggs)
                {
                    if (bullet.CanHitPlayerShip && IsColliding(companionEgg, bullet))
                    {
                        companionEgg.Kill();
                        bullet.IsExpired = true;
                    }
                }

                // ... companion ship (only for friendly-fire bullets)
                foreach (CompanionShip companionShip in companionShips)
                {
                    if (bullet.CanHitPlayerShip && IsColliding(companionShip, bullet))
                    {
                        companionShip.Kill();
                        bullet.IsExpired = true;
                    }
                }

                // ... enemies
                // do this before Player being killed in case deaths are simultaneous and player has no lives left,
                // but gets an extra life just on this frame thanks a final shot
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (IsColliding(enemies[i], bullet))
                    {
                        enemies[i].WasShot();
                        bullet.IsExpired = true;
                    }
                }
            }

            // handle collision between black holes and ...
            for (int i = 0; i < blackHoles.Count; i++)
            {
                // ... enemies
                for (int j = 0; j < enemies.Count; j++)
                {
                    if (IsColliding(blackHoles[i], enemies[j]))
                    {
                        // Note: black hole is NOT destroyed!
                        enemies[j].WasShot();
                    }
                }

                // ... bullets
                for (int j = 0; j < bullets.Count; j++)
                {
                    if (IsColliding(blackHoles[i], bullets[j]))
                    {
                        blackHoles[i].WasShot();
                        bullets[j].IsExpired = true;
                    }
                }

                // ... player ship
                // (this checks IsExpired, in case player bullet destroyed black hole this frame,
                // to avoid killing player)
                if (IsColliding(blackHoles[i], PlayerShip.Instance))
                {
                    // Note: black hole is NOT destroyed now,
                    // but if we destroy all entities on player death anyway, it doesn't matter too much
                    PlayerShip.Instance.Kill();
                    break;
                }
            }

            // handle collision between the player and enemies
            for (int i = 0; i < enemies.Count - 1; i++)
            {
                // Note asymmetry: only active enemies can kill player ship,
                // but they can be shot by player, as to advantage player
                if (enemies[i].IsActive && IsColliding(enemies[i], PlayerShip.Instance))
                {
                    PlayerShip.Instance.Kill();
                    break;
                }
            }

            // handle collision between player ship and companion egg (pick up)
            foreach (CompanionEgg companionEgg in companionEggs)
            {
                // extra radius helps catching companion, since Player Ship hurtbox is made small to avoid damages
                if (IsColliding(companionEgg, PlayerShip.Instance, extraRadius: 40f))
                {
                    // Player Ship collects companion egg

                    // Destroy egg silently ("hatch")
                    companionEgg.Clear();

                    // Spawn companion and attach to player ship
                    TrySpawnCompanionShipAndAttachToPlayerShip();
                }
            }
        }

        public static void ClearAllEntitiesOnScreen()
        {
            // keep particles for death feedback
            enemies.ForEach(x => x.ClearWithExplosion());
            blackHoles.ForEach(x => x.Kill());
            bullets.ForEach(x => x.IsExpired = true);
        }

        public static IEnumerable<Entity> GetNearbyEntities(Vector2 position, float radius)
        {
            return entities.Where(x => Vector2.DistanceSquared(position, x.Position) < radius * radius);
        }

        public static void Draw(SpriteBatch spriteBatch)
        {
            foreach (var entity in entities)
            {
                entity.Draw(spriteBatch);
            }
        }
    }
}