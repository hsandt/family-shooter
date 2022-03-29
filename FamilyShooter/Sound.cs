using System;
using System.Linq;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;

namespace FamilyShooter
{
    public static class Sound
    {
        private static readonly Random rand = new Random();

        public static Song Music { get; private set; }

        public static SoundEffect[] explosions { get; private set; }
        public static SoundEffect GetRandomExplosion() => explosions[rand.Next(explosions.Length)];

        public static SoundEffect[] shots { get; private set; }
        public static SoundEffect GetRandomShot() => shots[rand.Next(shots.Length)];

        public static SoundEffect[] spawns { get; private set; }
        public static SoundEffect GetRandomSpawn() => spawns[rand.Next(spawns.Length)];

        public static void Load(ContentManager content)
        {
            Music = content.Load<Song>("Sound/Music");

            explosions = Enumerable.Range(1, 8).Select(x => content.Load<SoundEffect>($"Sound/explosion-{x:00}")).ToArray();
            shots = Enumerable.Range(1, 4).Select(x => content.Load<SoundEffect>($"Sound/shoot-{x:00}")).ToArray();
            spawns = Enumerable.Range(1, 8).Select(x => content.Load<SoundEffect>($"Sound/spawn-{x:00}")).ToArray();
        }
    }
}