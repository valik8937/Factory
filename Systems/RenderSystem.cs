using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Factory;

/// <summary>
/// Система рендерингу: відмальовує всі шари тайлів, сутності, селектор.
/// Використовує TextureManager для текстур замість хардкод-кольорів.
/// </summary>
public class RenderSystem
{
    private readonly TextureManager _textures;
    private readonly Texture2D _pixel;

    public RenderSystem(TextureManager textures, Texture2D pixel)
    {
        _textures = textures;
        _pixel = pixel;
    }

    public void Draw(
        SpriteBatch sb,
        List<Chunk> activeChunks,
        Registry registry,
        int playerId,
        Camera camera,
        GraphicsDevice graphicsDevice,
        InputSystem input,
        float tileSize)
    {
        sb.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            null, null, null,
            camera.GetViewMatrix(graphicsDevice)
        );

        DrawLayer0(sb, activeChunks, tileSize);
        DrawLayer1(sb, activeChunks, registry, playerId, tileSize);
        DrawLayer2(sb, activeChunks, tileSize);
        DrawTargetSelector(sb, input, tileSize);

        sb.End();
    }

    private void DrawLayer0(SpriteBatch sb, List<Chunk> activeChunks, float tileSize)
    {
        foreach (var chunk in activeChunks)
        {
            for (int y = 0; y < Chunk.Size; y++)
            {
                for (int x = 0; x < Chunk.Size; x++)
                {
                    int tileType = chunk.GetTile(x, y, 0);
                    if (tileType > 0)
                    {
                        int gx = (chunk.ChunkX << 4) + x;
                        int gy = (chunk.ChunkY << 4) + y;
                        TileRenderer.Draw(sb, _textures, tileType, gx, gy, tileSize, false);
                    }
                }
            }
        }
    }

    private void DrawLayer1(SpriteBatch sb, List<Chunk> activeChunks, Registry registry, int playerId, float tileSize)
    {
        var items = new List<RenderItem>();

        // Збираємо тайли шару 1
        foreach (var chunk in activeChunks)
        {
            for (int y = 0; y < Chunk.Size; y++)
            {
                for (int x = 0; x < Chunk.Size; x++)
                {
                    int tileType = chunk.GetTile(x, y, 1);
                    if (tileType > 0)
                    {
                        int gx = (chunk.ChunkX << 4) + x;
                        int gy = (chunk.ChunkY << 4) + y;
                        float sortY = (gy + 1) * tileSize;

                        int localGx = gx, localGy = gy, localTileType = tileType;

                        items.Add(new RenderItem
                        {
                            SortY = sortY,
                            DrawAction = () => TileRenderer.Draw(sb, _textures, localTileType, localGx, localGy, tileSize, false)
                        });
                    }
                }
            }
        }

        // Збираємо динамічні сутності на шарі 1
        foreach (int entityId in registry.Entities)
        {
            if (!registry.HasTransform(entityId) || !registry.HasSprite(entityId))
                continue;

            var transform = registry.Transforms[entityId];
            if (transform.Z != 1) continue;

            var sprite = registry.Sprites[entityId];
            float sortY = transform.Y + sprite.Height;

            int localId = entityId;
            var localTransform = transform;
            var localSprite = sprite;

            items.Add(new RenderItem
            {
                SortY = sortY,
                DrawAction = () => DrawEntity(sb, localId, localTransform, localSprite, playerId)
            });
        }

        items.Sort((a, b) => a.SortY.CompareTo(b.SortY));
        foreach (var item in items)
            item.DrawAction();
    }

    private void DrawLayer2(SpriteBatch sb, List<Chunk> activeChunks, float tileSize)
    {
        foreach (var chunk in activeChunks)
        {
            for (int y = 0; y < Chunk.Size; y++)
            {
                for (int x = 0; x < Chunk.Size; x++)
                {
                    int tileType = chunk.GetTile(x, y, 2);
                    if (tileType > 0)
                    {
                        int gx = (chunk.ChunkX << 4) + x;
                        int gy = (chunk.ChunkY << 4) + y;
                        TileRenderer.Draw(sb, _textures, tileType, gx, gy, tileSize, true);
                    }
                }
            }
        }
    }

    private void DrawTargetSelector(SpriteBatch sb, InputSystem input, float tileSize)
    {
        TileRenderer.DrawTargetSelector(sb, _pixel, input.TargetTileX, input.TargetTileY, tileSize);
    }

    private void DrawEntity(SpriteBatch sb, int entityId, TransformComponent transform, SpriteComponent sprite, int playerId)
    {
        if (entityId == playerId)
        {
            // Гравець: використовуємо текстуру "test-man"
            if (_textures.TryGetSourceRect("test-man", out var srcRect))
            {
                var destRect = new Rectangle((int)transform.X, (int)transform.Y, sprite.Width, sprite.Height);
                sb.Draw(_textures.Atlas, destRect, srcRect, Color.White);
            }
            else
            {
                // Fallback: чорний квадрат з білою рамкою (якщо текстури немає)
                int px = (int)transform.X;
                int py = (int)transform.Y;
                sb.Draw(_pixel, new Rectangle(px, py, sprite.Width, sprite.Height), new Color(24, 24, 28));
                sb.Draw(_pixel, new Rectangle(px, py, sprite.Width, 1), Color.White);
                sb.Draw(_pixel, new Rectangle(px, py + sprite.Height - 1, sprite.Width, 1), Color.White);
                sb.Draw(_pixel, new Rectangle(px, py, 1, sprite.Height), Color.White);
                sb.Draw(_pixel, new Rectangle(px + sprite.Width - 1, py, 1, sprite.Height), Color.White);
                sb.Draw(_pixel, new Rectangle(px + 6, py + 8, 5, 5), Color.Aqua);
                sb.Draw(_pixel, new Rectangle(px + 21, py + 8, 5, 5), Color.Aqua);
            }
        }
        else
        {
            sb.Draw(_pixel, new Rectangle((int)transform.X, (int)transform.Y, sprite.Width, sprite.Height), sprite.Color);
        }
    }

    private struct RenderItem
    {
        public float SortY;
        public System.Action DrawAction;
    }
}
