using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Factory;


public class Camera
{
    public Vector2 Position { get; set; }
    public float Zoom { get; set; }
    
    public Camera(Vector2 startPosition, float zoom = 2.0f)
    {
        Position = startPosition;
        Zoom = zoom;
    }

    // Плавно рухає камеру до цілі
    public void Follow(Vector2 target, float lerpFactor)
    {
        Position = Vector2.Lerp(Position, target, lerpFactor);
    }

    // Генерація матриці перетворення для SpriteBatch.Begin
    public Matrix GetViewMatrix(GraphicsDevice graphicsDevice)
    {
        float width = graphicsDevice.Viewport.Width;
        float height = graphicsDevice.Viewport.Height;

        // Послідовність: зсув у центр координат світу -> масштаб -> зсув у центр екрана
        return Matrix.CreateTranslation(new Vector3(-Position.X, -Position.Y, 0)) *
               Matrix.CreateScale(new Vector3(Zoom, Zoom, 1)) *
               Matrix.CreateTranslation(new Vector3(width / 2f, height / 2f, 0));
    }

    // Переклад екранних координат миші у координати світу
    public Vector2 ScreenToWorld(Vector2 screenPos, GraphicsDevice graphicsDevice)
    {
        Matrix viewMatrix = GetViewMatrix(graphicsDevice);
        Matrix invertMatrix = Matrix.Invert(viewMatrix);
        return Vector2.Transform(screenPos, invertMatrix);
    }
}
