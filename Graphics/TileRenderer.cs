using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Factory;

/// <summary>
/// Відмальовка одного тайла через TextureManager.
/// Більше ніяких хардкод-кольорів — усе через текстури.
/// </summary>
public static class TileRenderer
{
    /// <summary>
    /// Малює тайл за його ID. Підтримує прозорість для шару даху.
    /// </summary>
    public static void Draw(SpriteBatch sb, TextureManager textures, int tileId, int gx, int gy, float tileSize, bool isTransparent)
    {
        var def = TileRegistry.Get(tileId);
        if (def.TextureName == null)
            return;

        int x = (int)(gx * tileSize);
        int y = (int)(gy * tileSize);
        int size = (int)tileSize;
        var destRect = new Rectangle(x, y, size, size);

        Color tint = isTransparent ? Color.White * 0.55f : Color.White;
        textures.Draw(sb, def.TextureName, destRect, tint);
    }

    /// <summary>
    /// Малює таргет-селектор (жовту рамку під мишкою).
    /// </summary>
    public static void DrawTargetSelector(SpriteBatch sb, Texture2D pixel, int tx, int ty, float tileSize)
    {
        int x = (int)(tx * tileSize);
        int y = (int)(ty * tileSize);
        int size = (int)tileSize;

        Color frameColor = Color.Yellow * 0.7f;

        sb.Draw(pixel, new Rectangle(x, y, size, 1), frameColor);
        sb.Draw(pixel, new Rectangle(x, y + size - 1, size, 1), frameColor);
        sb.Draw(pixel, new Rectangle(x, y, 1, size), frameColor);
        sb.Draw(pixel, new Rectangle(x + size - 1, y, 1, size), frameColor);
    }
}
