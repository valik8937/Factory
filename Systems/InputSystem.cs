using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace Factory;

/// <summary>
/// Система вводу: обробляє клавіатуру, мишу, перемикання шарів.
/// </summary>
public class InputSystem
{
    private KeyboardState _prevKState;
    private MouseState _prevMState;

    public int ActiveLayer { get; private set; } = 1;

    public bool IsPlaceBlock { get; private set; }
    public bool IsRemoveBlock { get; private set; }
    public int TargetTileX { get; private set; }
    public int TargetTileY { get; private set; }
    public Vector2 MovementInput { get; private set; }
    public Vector2 MouseWorld { get; private set; }
    public bool ExitPressed { get; private set; }

    public void Update(Camera camera, GraphicsDevice graphicsDevice, float tileSize)
    {
        var kState = Keyboard.GetState();
        var mState = Mouse.GetState();

        // Вихід
        ExitPressed = GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
                      || kState.IsKeyDown(Keys.Escape);

        // Рух
        float speed = 200f;
        float vx = 0;
        float vy = 0;

        if (kState.IsKeyDown(Keys.Left) || kState.IsKeyDown(Keys.A)) vx = -speed;
        if (kState.IsKeyDown(Keys.Right) || kState.IsKeyDown(Keys.D)) vx = speed;
        if (kState.IsKeyDown(Keys.Up) || kState.IsKeyDown(Keys.W)) vy = -speed;
        if (kState.IsKeyDown(Keys.Down) || kState.IsKeyDown(Keys.S)) vy = speed;

        if (Math.Abs(vx) > 0.001f && Math.Abs(vy) > 0.001f)
        {
            vx *= 0.7071f;
            vy *= 0.7071f;
        }

        MovementInput = new Vector2(vx, vy);

        // Перемикання шарів (клавіші 1, 2, 3)
        if (kState.IsKeyDown(Keys.D1) && !_prevKState.IsKeyDown(Keys.D1)) ActiveLayer = 0;
        if (kState.IsKeyDown(Keys.D2) && !_prevKState.IsKeyDown(Keys.D2)) ActiveLayer = 1;
        if (kState.IsKeyDown(Keys.D3) && !_prevKState.IsKeyDown(Keys.D3)) ActiveLayer = 2;

        // Координати миші у світі
        MouseWorld = camera.ScreenToWorld(new Vector2(mState.X, mState.Y), graphicsDevice);
        TargetTileX = (int)Math.Floor(MouseWorld.X / tileSize);
        TargetTileY = (int)Math.Floor(MouseWorld.Y / tileSize);

        // Кнопки миші
        IsPlaceBlock = mState.LeftButton == ButtonState.Pressed;
        IsRemoveBlock = mState.RightButton == ButtonState.Pressed;

        _prevKState = kState;
        _prevMState = mState;
    }

    /// <summary>
    /// Повертає назву активного шару для UI.
    /// </summary>
    public string GetActiveLayerName()
    {
        return ActiveLayer switch
        {
            0 => "GROUND (LAYER 0) - NO COLLISION",
            1 => "OBSTACLE (LAYER 1) - SOLID BLOCK",
            2 => "ROOF (LAYER 2) - TRANSPARENT",
            _ => "UNKNOWN"
        };
    }
}
