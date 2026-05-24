using System;
using System.Collections.Generic;
using System.Threading;

namespace Factory;

public class WorldManager
{
    private readonly Dictionary<long, Chunk> _chunks = new();
    private readonly object _lock = new();

    private Thread _loaderThread;
    private bool _isRunning = false;

    private int _playerChunkX = 0;
    private int _playerChunkY = 0;

    private const int LoadRadius = GameConfig.LoadRadius;

    public void Start()
    {
        _isRunning = true;
        _loaderThread = new Thread(UpdateLoader)
        {
            IsBackground = true,
            Name = "ChunkLoaderThread"
        };
        _loaderThread.Start();
        Console.WriteLine("Chunk loader thread started.");
    }

    public void Stop()
    {
        _isRunning = false;
        if (_loaderThread != null && _loaderThread.IsAlive)
        {
            _loaderThread.Join(1000);
        }
        Console.WriteLine("Chunk loader thread stopped.");
    }

    public void UpdatePlayerPosition(float worldX, float worldY, float tileSize)
    {
        int tileX = (int)Math.Floor(worldX / tileSize);
        int tileY = (int)Math.Floor(worldY / tileSize);

        lock (_lock)
        {
            _playerChunkX = tileX >> 4;
            _playerChunkY = tileY >> 4;
        }
    }

    public static long GetChunkKey(int cx, int cy)
    {
        return ((long)cx << 32) | ((long)(uint)cy);
    }

    private void UpdateLoader()
    {
        while (_isRunning)
        {
            int centerCx, centerCy;
            lock (_lock)
            {
                centerCx = _playerChunkX;
                centerCy = _playerChunkY;
            }

            bool generatedAny = false;

            for (int dy = -LoadRadius; dy <= LoadRadius; dy++)
            {
                for (int dx = -LoadRadius; dx <= LoadRadius; dx++)
                {
                    if (!_isRunning) break;

                    int cx = centerCx + dx;
                    int cy = centerCy + dy;
                    long key = GetChunkKey(cx, cy);

                    bool exists;
                    lock (_lock)
                    {
                        exists = _chunks.ContainsKey(key);
                    }

                    if (!exists)
                    {
                        Chunk newChunk = GenerateChunk(cx, cy);

                        lock (_lock)
                        {
                            _chunks[key] = newChunk;
                        }

                        Console.WriteLine($"[Thread] Chunk generated: ({cx}, {cy})");
                        generatedAny = true;
                        Thread.Sleep(10);
                    }
                }
            }

            if (!generatedAny)
            {
                List<long> keysToRemove = new List<long>();
                lock (_lock)
                {
                    foreach (var pair in _chunks)
                    {
                        var chunk = pair.Value;
                        int distVal = Math.Max(Math.Abs(chunk.ChunkX - centerCx), Math.Abs(chunk.ChunkY - centerCy));
                        if (distVal > LoadRadius + 2)
                        {
                            keysToRemove.Add(pair.Key);
                        }
                    }

                    foreach (var key in keysToRemove)
                    {
                        _chunks.Remove(key);
                    }
                }
                if (keysToRemove.Count > 0)
                {
                    Console.WriteLine($"[Thread] Unloaded {keysToRemove.Count} distant chunks.");
                }

                Thread.Sleep(100);
            }
        }
    }

    private Chunk GenerateChunk(int cx, int cy)
    {
        var chunk = new Chunk(cx, cy);

        for (int y = 0; y < Chunk.Size; y++)
        {
            for (int x = 0; x < Chunk.Size; x++)
            {
                int gx = (cx << 4) + x;
                int gy = (cy << 4) + y;

                // Шаховий візерунок: tile ID 1 або 2 (обидва використовують "grass1")
                int tileType = ((gx + gy) % 2 == 0) ? 1 : 2;
                chunk.SetTile(x, y, 0, tileType);

                chunk.SetTile(x, y, 1, 0);
                chunk.SetTile(x, y, 2, 0);
            }
        }

        return chunk;
    }

    public int GetTile(int gx, int gy, int z)
    {
        int cx = gx >> 4;
        int cy = gy >> 4;
        int tx = gx & 15;
        int ty = gy & 15;

        long key = GetChunkKey(cx, cy);

        lock (_lock)
        {
            if (_chunks.TryGetValue(key, out Chunk chunk))
            {
                return chunk.GetTile(tx, ty, z);
            }
        }
        return 0;
    }

    public void SetTile(int gx, int gy, int z, int tileType)
    {
        int cx = gx >> 4;
        int cy = gy >> 4;
        int tx = gx & 15;
        int ty = gy & 15;

        long key = GetChunkKey(cx, cy);

        lock (_lock)
        {
            if (_chunks.TryGetValue(key, out Chunk chunk))
            {
                chunk.SetTile(tx, ty, z, tileType);
            }
        }
    }

    public List<Chunk> GetActiveChunksCopy()
    {
        lock (_lock)
        {
            return new List<Chunk>(_chunks.Values);
        }
    }
}
