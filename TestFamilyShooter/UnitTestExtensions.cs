using Microsoft.Xna.Framework;
using FamilyShooter;
using Xunit;

namespace TestFamilyShooter
{
    public class UnitTestExtensions
    {
        [Fact]
        public void TestScaleToUnitX()
        {
            Assert.Equal(new Vector2(5f, 0f), Vector2.UnitX.ScaleTo(5f));
        }

        [Fact]
        public void TestScaleToUnitY()
        {
            Assert.Equal(new Vector2(0f, 5f), Vector2.UnitY.ScaleTo(5f));
        }
    }
}