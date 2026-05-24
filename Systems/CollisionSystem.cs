using System;

namespace Factory;

/// <summary>
/// Система колізій: перевірка AABB сутності проти тайлів шару колізій.
/// </summary>
public static class CollisionSystem
{
    /// <summary>
    /// Перевіряє колізію bounding box (x, y, w, h) з тайлами на вказаному шарі.
    /// Повертає true якщо є колізія, і записує в resolvedVal скориговану координату.
    /// </summary>
    public static bool CheckTileCollision(
        float x, float y, int w, int h, int layer,
        WorldManager world, float tileSize, out float resolvedVal,
        bool isXAxis, bool isMovingPositive)
    {
        resolvedVal = isXAxis ? x : y;

        int startX = (int)Math.Floor(x / tileSize);
        int endX = (int)Math.Floor((x + w - 0.1f) / tileSize);
        int startY = (int)Math.Floor(y / tileSize);
        int endY = (int)Math.Floor((y + h - 0.1f) / tileSize);

        for (int ty = startY; ty <= endY; ty++)
        {
            for (int tx = startX; tx <= endX; tx++)
            {
                int tileType = world.GetTile(tx, ty, layer);

                if (tileType > 0)
                {
                    if (isXAxis)
                    {
                        if (isMovingPositive)
                            resolvedVal = tx * tileSize - w;
                        else
                            resolvedVal = (tx + 1) * tileSize;
                    }
                    else
                    {
                        if (isMovingPositive)
                            resolvedVal = ty * tileSize - h;
                        else
                            resolvedVal = (ty + 1) * tileSize;
                    }
                    return true;
                }
            }
        }

        return false;
    }
}
