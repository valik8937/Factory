#nullable enable
using System.Collections.Generic;

namespace Factory;

/// <summary>
/// Каталог тайлів. Зіставляє ID тайла з його властивостями:
/// назва текстури, чи має колізію, на якому шарі за замовчуванням.
/// </summary>
public static class TileRegistry
{
    public static readonly Dictionary<int, TileDef> Tiles = new()
    {
        // ID 0 = порожній тайл (повітря)
        [0] = new TileDef("empty", null, false, 0),

        // Шар 0 — земля
        [1] = new TileDef("grass_a", "grass1", false, 0),
        [2] = new TileDef("grass_b", "grass1", false, 0),

        // Шар 1 — перешкоди (стіни)
        [3] = new TileDef("brick", "brick", true, 1),

        // Шар 2 — дах
        [4] = new TileDef("roof", "brick", false, 2),
    };

    public static TileDef Get(int tileId)
    {
        return Tiles.TryGetValue(tileId, out var def) ? def : Tiles[0];
    }

    public static bool HasCollision(int tileId)
    {
        return Tiles.TryGetValue(tileId, out var def) && def.HasCollision;
    }
}

public record TileDef(string Name, string? TextureName, bool HasCollision, int DefaultLayer);
