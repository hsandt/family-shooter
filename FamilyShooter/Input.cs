using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FamilyShooter
{
    public static class Input
    {
        private static KeyboardState keyboardState, lastKeyboardState;
        private static MouseState mouseState, lastMouseState;
        private static GamePadState gamePadState, lastGamePadState;

        private static bool isAimingWithMouse = false;

        public static Vector2 MousePosition => GetMousePosition(mouseState);
        public static Vector2 LastMousePosition => GetMousePosition(lastMouseState);

        private static Vector2 GetMousePosition(MouseState _mouseState) => new Vector2(_mouseState.X, _mouseState.Y);

        public static void Update()
        {
            lastKeyboardState = keyboardState;
            lastMouseState = mouseState;
            lastGamePadState = gamePadState;

            keyboardState = Keyboard.GetState();
            mouseState = Mouse.GetState();
            gamePadState = GamePad.GetState(PlayerIndex.One);

            // If the player pressed one of the arrow keys or is using a gamepad to aim, we want to disable mouse aiming. Otherwise,
            // if the player moves the mouse, enable mouse aiming.
            if (new[] {Keys.Left, Keys.Right, Keys.Up, Keys.Down}.Any(x => keyboardState.IsKeyDown(x)) ||
                gamePadState.ThumbSticks.Right != Vector2.Zero)
            {
                isAimingWithMouse = false;
            }
            else if (MousePosition != LastMousePosition)
            {
                isAimingWithMouse = true;
            }
        }

        public static bool WasKeyPressed(Keys key)
        {
            return lastKeyboardState.IsKeyUp(key) && keyboardState.IsKeyDown(key);
        }

        public static bool WasButtonPressed(Buttons key)
        {
            return lastGamePadState.IsButtonUp(key) && gamePadState.IsButtonDown(key);
        }

        public static Vector2 GetMovementDirection()
        {
            Vector2 direction = gamePadState.ThumbSticks.Left;
            direction.Y *= -1;

            if (keyboardState.IsKeyDown(Keys.A))
            {
                direction.X -= 1;
            }

            if (keyboardState.IsKeyDown(Keys.D))
            {
                direction.X += 1;
            }

            if (keyboardState.IsKeyDown(Keys.W))
            {
                direction.Y -= 1;
            }

            if (keyboardState.IsKeyDown(Keys.S))
            {
                direction.Y += 1;
            }

            // Design: clamp the length to 1
            // For arcade games, keeping a diagonal of (1, 1) may also be good,
            // but since we use direct Thumbstick input, we are already using normalized direction
            // Note that low stick amplitude will result in slower motion.
            if (direction.LengthSquared() > 1f)
            {
                direction.Normalize();
            }

            return direction;
        }

        public static Vector2 GetKeyboardGamePadAimDirection()
        {
            Vector2 direction = gamePadState.ThumbSticks.Right;
            direction.Y *= -1;

            if (keyboardState.IsKeyDown(Keys.Left))
            {
                direction.X -= 1;
            }

            if (keyboardState.IsKeyDown(Keys.Right))
            {
                direction.X += 1;
            }

            if (keyboardState.IsKeyDown(Keys.Up))
            {
                direction.Y -= 1;
            }

            if (keyboardState.IsKeyDown(Keys.Down))
            {
                direction.Y += 1;
            }

            // ! Unlike Unity, Normalize of Vector2.Zero will fail with DIV by 0
            // We could also add a stick amplitude threshold (deadzone) here to avoid
            // normalizing very small input values to 1, causing directional instability.
            // For now, we count on middleware/MonoGame deadzone.
            if (direction != Vector2.Zero)
            {
                direction.Normalize();
            }

            return direction;
        }

        public static Vector2 GetMouseAimDirection()
        {
            // Note that World position = Screen position in this game
            Vector2 direction = MousePosition - PlayerShip.Instance.Position;

            // ! Unlike Unity, Normalize of Vector2.Zero will fail with DIV by 0
            // We could also add a diff threshold here to avoid
            // normalizing very small diff values to 1, causing directional instability
            // when moving the mouse very close to the ship.
            if (direction != Vector2.Zero)
            {
                direction.Normalize();
            }

            return direction;
        }

        /// Return fire aiming direction (Vector2.Zero if not firing)
        public static Vector2 GetAimDirection()
        {
            return isAimingWithMouse ? GetMouseAimDirection() : GetKeyboardGamePadAimDirection();
        }

        public static bool WasBombButtonPressed()
        {
            return WasButtonPressed(Buttons.LeftTrigger) || WasButtonPressed(Buttons.RightTrigger) ||
                   WasKeyPressed(Keys.Space);
        }
    }
}