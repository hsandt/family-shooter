using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FamilyShooter;

public class Background
{
    /* Const */

    private const float CLOUD_SPEED = 10f;

    /* State */

    private readonly List<Vector2>[] m_CloudPositionListsPerSpriteIndex = new List<Vector2>[3];

    public Background()
    {
        for (int i = 0; i < 3; i++)
        {
            m_CloudPositionListsPerSpriteIndex[i] = new List<Vector2>();
            for (int j = 0; j < 3; j++)
            {
                float x = 600f * i + 300f * j;
                float y = 10f * i + 300f * (i + 1) / 2f * j * j;
                m_CloudPositionListsPerSpriteIndex[i].Add(new Vector2(x, y));
            }
        }
    }

    public void Update()
    {
        for (int i = 0; i < 3; i++)
        {
            Texture2D currentCloudTexture = Art.BGClouds[i];
            int wrapWidth = currentCloudTexture.Width;
            int wrapHeight = currentCloudTexture.Height;

            for (int j = 0; j < m_CloudPositionListsPerSpriteIndex[i].Count; j++)
            {
                Vector2 cloudPosition = m_CloudPositionListsPerSpriteIndex[i][j];

                // Move cloud slowly
                Vector2 moveDirection = Vector2.Normalize(new Vector2(1f, 0.1f * i + 0.05f * j));
                float moveSpeed = CLOUD_SPEED * (1f + 0.3f * i) + (1f + 0.2f * j);
                float deltaTime = (float)GameRoot.GameTime.ElapsedGameTime.TotalSeconds;
                cloudPosition += moveSpeed * deltaTime * moveDirection;

                // Wrap around screen
                // Remember to pad with one width/height of the sprite itself, so we only warp it to the other side
                // when fully out of view
                cloudPosition.X = (cloudPosition.X + wrapWidth) % (GameRoot.ScreenSize.X + wrapWidth) - wrapWidth;
                cloudPosition.Y = (cloudPosition.Y + wrapHeight) % (GameRoot.ScreenSize.Y + wrapHeight) - wrapHeight;
                m_CloudPositionListsPerSpriteIndex[i][j] = cloudPosition;
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // This must be called in a sprite batch context using SamplerState.LinearWrap
        // https://gamedev.stackexchange.com/questions/34072/xna-how-to-draw-some-sprites-tiled-wrapped-and-others-not
        // Tile to fill screen
        spriteBatch.Draw(Art.BGSky, Vector2.Zero, new Rectangle(0, 0, (int)GameRoot.ScreenSize.X, (int)GameRoot.ScreenSize.Y), Color.White);

        // Draw individual clouds
        for (int i = 0; i < 3; i++)
        {
            foreach (Vector2 cloudPosition in m_CloudPositionListsPerSpriteIndex[i])
            {
                spriteBatch.Draw(Art.BGClouds[i], cloudPosition, Color.White);
            }
        }
    }
}
