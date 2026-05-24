using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Factory;

public class Camera
{
    public Vector2 Position { get; set; }
    public float Zoom { get; set; }

    public Camera(Vector2 startPosition, float zoom = GameConfig.CameraZoom)
    {
        Position = startPosition;
        Zoom = zoom;
    }

    public void Follow(Vector2 target, float lerpFactor = GameConfig.CameraLerp)
    {
        Position = Vector2.Lerp(Position, target, lerpFactor);
    }

    public Matrix GetViewMatrix(GraphicsDevice graphicsDevice)
    {
        float width = graphicsDevice.Viewport.Width;
        float height = graphicsDevice.Viewport.Height;

        return Matrix.CreateTranslation(new Vector3(-Position.X, -Position.Y, 0)) *
               Matrix.CreateScale(new Vector3(Zoom, Zoom, 1)) *
               Matrix.CreateTranslation(new Vector3(width / 2f, height / 2f, 0));
    }

    public Vector2 ScreenToWorld(Vector2 screenPos, GraphicsDevice graphicsDevice)
    {
        Matrix viewMatrix = GetViewMatrix(graphicsDevice);
        Matrix invertMatrix = Matrix.Invert(viewMatrix);
        return Vector2.Transform(screenPos, invertMatrix);
    }
}
