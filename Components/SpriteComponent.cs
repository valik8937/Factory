using Microsoft.Xna.Framework;

namespace Factory;

public struct SpriteComponent
{
    public Color Color;
    public int Width;
    public int Height;

    public SpriteComponent(Color color, int width, int height)
    {
        Color = color;
        Width = width;
        Height = height;
    }
}
