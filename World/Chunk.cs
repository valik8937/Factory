namespace Factory;

public struct Chunk
{
    public const int Size = GameConfig.ChunkSize;
    public const int Layers = GameConfig.ChunkLayers;
    public const int TotalTiles = Size * Size * Layers;

    public int ChunkX { get; }
    public int ChunkY { get; }
    public int[] Tiles { get; }

    public Chunk(int chunkX, int chunkY)
    {
        ChunkX = chunkX;
        ChunkY = chunkY;
        Tiles = new int[TotalTiles];
    }

    public static int GetIndex(int x, int y, int z)
    {
        return (z * Size * Size) + (y * Size) + x;
    }

    public int GetTile(int x, int y, int z)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size || z < 0 || z >= Layers)
            return 0;
        return Tiles[GetIndex(x, y, z)];
    }

    public void SetTile(int x, int y, int z, int tileType)
    {
        if (x >= 0 && x < Size && y >= 0 && y < Size && z >= 0 && z < Layers)
        {
            Tiles[GetIndex(x, y, z)] = tileType;
        }
    }

    /// <summary>
    /// Перевіряє, чи тайл на шарі 1 має колізію (дивиться в TileRegistry).
    /// </summary>
    public static bool IsSolid(int tileId) => TileRegistry.HasCollision(tileId);
}
