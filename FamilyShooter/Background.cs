using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FamilyShooter;

public class Background
{
    public Background()
    {
    }

    public void Update()
    {
        // Apply parallax motion
        // velocity += acceleration * (float)GameRoot.GameTime.ElapsedGameTime.TotalSeconds;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // This must be called in a sprite batch context using SamplerState.LinearWrap
        // https://gamedev.stackexchange.com/questions/34072/xna-how-to-draw-some-sprites-tiled-wrapped-and-others-not
        // Tile to fill screen
        spriteBatch.Draw(Art.BGSky, Vector2.Zero, new Rectangle(0, 0, (int)GameRoot.ScreenSize.X, (int)GameRoot.ScreenSize.Y), Color.White);

        // Draw individual clouds
        spriteBatch.Draw(Art.BGCloud1, new Vector2(10f, 10f), Color.White);
        spriteBatch.Draw(Art.BGCloud2, new Vector2(500f, 200f), Color.White);
        spriteBatch.Draw(Art.BGCloud3, new Vector2(50f, 400f), Color.White);

        // Color color = new Color(30, 30, 139, 85);   // dark blue (tutorial)

        Color color = new Color(47, 47, 255, 85);   // lighter blue, more visible
        // Color smoothedColor = new Color(0, 255, 0, 85);   // test to demonstrate smoothed points
        // Color nonSmoothedColor = new Color(255, 0, 0, 85);   // test to demonstrate non-smoothed points
    }
}
