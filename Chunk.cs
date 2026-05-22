namespace Factory;


public struct Chunk
{
    public const int Size = 16; // 16x16 клітинок
    public const int Layers = 3; // 0 - земля, 1 - перешкоди, 2 - декор зверху
    public const int TotalTiles = Size * Size * Layers;

    public int ChunkX { get; }
    public int ChunkY { get; }
    
    // Плоский масив для тайлів
    public int[] Tiles { get; }

    public Chunk(int chunkX, int chunkY)
    {
        ChunkX = chunkX;
        ChunkY = chunkY;
        Tiles = new int[TotalTiles];
    }

    // Хелпер для перерахунку 3D координат у 1D індекс
    public static int GetIndex(int x, int y, int z)
    {
        // x: 0..15, y: 0..15, z: 0..Layers-1
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
}
