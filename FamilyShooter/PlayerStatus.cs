using System.IO;
using System.Xml.Schema;

namespace FamilyShooter
{
    public static class PlayerStatus
    {
        private const string highScoreFilename = "highscore.txt";

        // amount of time it takes, in seconds, for a multiplier to expire
        private const float multiplierExpiryTime = 0.8f;  // seconds
        private const int maxMultiplier = 20;
        private const int extraLifeRequiredAdditionalScore = 2000;
        // private const int extraCompanionRequiredAdditionalScore = 500;
        private const int extraCompanionRequiredAdditionalScore = 10;  // TEST many companions
        private const int scoreReceivedInsteadOfNewCompanionWhenAllCompanionSlotsFull = 5;

        public static int Lives { get; private set; }
        public static int Score { get; private set; }
        public static int HighScore { get; private set; }
        public static int CurrentMultiplier { get; private set; }
        public static float TimeBeforeMultiplierExpiry { get; private set; }  // seconds

        // Note: we count lives, not extra lives, no Lives == 0 is game over indeed
        public static bool IsGameOver => Lives == 0;

        private static float scoreForExtraLife;
        private static float scoreForExtraCompanion;

        static PlayerStatus()
        {
            HighScore = LoadHighScore();
            Init();
        }

        public static void Init()
        {
            if (Score > HighScore)
            {
                HighScore = Score;
                SaveHighScore(HighScore);
            }

            Lives = 4;
            Score = 0;
            ResetMultiplier();

            scoreForExtraLife = extraLifeRequiredAdditionalScore;
            scoreForExtraCompanion = extraCompanionRequiredAdditionalScore;
        }

        public static void ResetMultiplier()
        {
            CurrentMultiplier = 1;
            TimeBeforeMultiplierExpiry = 0;
        }

        public static void Update()
        {
            if (TimeBeforeMultiplierExpiry > 0)  // or CurrentMultiplier > 1
            {
                TimeBeforeMultiplierExpiry -= (float) GameRoot.GameTime.ElapsedGameTime.TotalSeconds;
                if (TimeBeforeMultiplierExpiry <= 0)
                {
                    ResetMultiplier();
                }
            }
        }

        private static void AddScore(int addedScore)
        {
            Score += addedScore;

            // loop just in case we gained enough score to gain 2+ lives at once!
            while (Score >= scoreForExtraLife)
            {
                // we've reached a new threshold, add extra life and extend threshold for next time
                scoreForExtraLife += extraLifeRequiredAdditionalScore;
                Lives++;
            }
        }

        public static void AddScoreForDestruction(Enemy enemy)
        {
            // Tutorial suggests to prevent scoring when dead (can only happen with stray bullet hitting
            // a newly spawned enemy since enemy clearance on death), which may look inconsistent but helps
            // avoid getting extra score life after game over (maybe better to check if LAST life has been lost).
            // Update: in fact it was because WasShot was called for screen clearance on death.
            // Now it matters less as we call SilentKill instead, so I'll comment this out so we can still get the
            // points on stray bullets; but replace it with a GameOver check so we don't in case of full GameOver.
            // if (PlayerShip.Instance.IsDead)
            if (IsGameOver)
            {
                return;
            }

            AddScore(enemy.RewardScore * CurrentMultiplier);

            // loop just in case we gained enough score to gain 2+ companion eggs at once,
            // but to avoid overlap, only spawn one companion egg even if you should have more
            bool hasSpawnedCompanionEgg = false;
            while (Score >= scoreForExtraCompanion)
            {
                scoreForExtraCompanion += extraCompanionRequiredAdditionalScore;
                if (!hasSpawnedCompanionEgg)
                {
                    EntityManager.SpawnCompanionEgg(enemy.Position);
                    hasSpawnedCompanionEgg = true;
                }
            }

            // Unlike tutorial, I call it directly here
            IncreaseMultiplier();
        }

        public static void AddScoreForPickingExtraCompanionWhenAllSlotsAreFull()
        {
            if (IsGameOver)
            {
                return;
            }

            AddScore(scoreReceivedInsteadOfNewCompanionWhenAllCompanionSlotsFull * CurrentMultiplier);

            // Note that we don't check scoreForExtraCompanion since spawning a companion egg after picking it
            // would be weird! We'll just keep the extra score and spawn it later on next enemy destruction.
        }

        private static void IncreaseMultiplier()
        {
            if (CurrentMultiplier < maxMultiplier)
            {
                CurrentMultiplier++;
            }
            TimeBeforeMultiplierExpiry = multiplierExpiryTime;
        }

        public static void LoseLife()
        {
            Lives--;
            ResetMultiplier();

            // Don't call Init() on Lives <= 0 here anymore, to leave time to player to see game over message and
            // high score. Re-Init on PlayerShip.Update at Respawn time instead
        }

        private static int LoadHighScore()
        {
            // return the saved high score if possible and return 0 otherwise
            int score;
            return File.Exists(highScoreFilename) && int.TryParse(File.ReadAllText(highScoreFilename), out score) ? score : 0;
        }

        private static void SaveHighScore(int score)
        {
            File.WriteAllText(highScoreFilename, score.ToString());
        }
    }
}