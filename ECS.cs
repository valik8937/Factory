using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Factory;


public struct TransformComponent
{
    public float X;
    public float Y;
    public int Z; // Шар рендерингу/колізії (наприклад, 0 - земля, 1 - перешкоди, 2 - верхній декор)

    public TransformComponent(float x, float y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

public struct VelocityComponent
{
    public float VX;
    public float VY;

    public VelocityComponent(float vx, float vy)
    {
        VX = vx;
        VY = vy;
    }
}

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

public struct CollisionComponent
{
    public int Width;
    public int Height;
    public bool IsActive;

    public CollisionComponent(int width, int height, bool isActive = true)
    {
        Width = width;
        Height = height;
        IsActive = isActive;
    }
}

public struct GridMovementComponent
{
    public bool IsMoving;
    public int TargetGridX;
    public int TargetGridY;
    public float Speed; // Швидкість руху в пікселях на секунду (наприклад, 128f = 4 клітинки на секунду)

    public GridMovementComponent(float speed = 128f)
    {
        IsMoving = false;
        TargetGridX = 0;
        TargetGridY = 0;
        Speed = speed;
    }
}

public class Registry
{
    private int _nextEntityId = 1;
    private readonly HashSet<int> _entities = new();

    public readonly Dictionary<int, TransformComponent> Transforms = new();
    public readonly Dictionary<int, VelocityComponent> Velocities = new();
    public readonly Dictionary<int, SpriteComponent> Sprites = new();
    public readonly Dictionary<int, CollisionComponent> Collisions = new();
    public readonly Dictionary<int, GridMovementComponent> GridMovements = new();

    public int CreateEntity()
    {
        int id = _nextEntityId++;
        _entities.Add(id);
        return id;
    }

    public void DestroyEntity(int entityId)
    {
        _entities.Remove(entityId);
        Transforms.Remove(entityId);
        Velocities.Remove(entityId);
        Sprites.Remove(entityId);
        Collisions.Remove(entityId);
        GridMovements.Remove(entityId);
    }

    public bool HasTransform(int id) => Transforms.ContainsKey(id);
    public bool HasVelocity(int id) => Velocities.ContainsKey(id);
    public bool HasSprite(int id) => Sprites.ContainsKey(id);
    public bool HasCollision(int id) => Collisions.ContainsKey(id);
    public bool HasGridMovement(int id) => GridMovements.ContainsKey(id);

    public IEnumerable<int> Entities => _entities;
}

public static class MovementSystem
{
    public static void Update(Registry registry, WorldManager world, float dt, float tileSize)
    {
        foreach (int entityId in registry.Entities)
        {
            if (!registry.HasTransform(entityId) || !registry.HasVelocity(entityId))
                continue;

            var transform = registry.Transforms[entityId];
            var velocity = registry.Velocities[entityId];

            // 1. Покроковий рух по клітинках (якщо у сутності є GridMovementComponent)
            if (registry.HasGridMovement(entityId))
            {
                var gridMove = registry.GridMovements[entityId];

                // Отримуємо поточні координати в сітці за світовою позицією
                int curGridX = (int)Math.Round(transform.X / tileSize);
                int curGridY = (int)Math.Round(transform.Y / tileSize);

                if (!gridMove.IsMoving)
                {
                    float vxInput = velocity.VX;
                    float vyInput = velocity.VY;

                    int dirX = 0;
                    int dirY = 0;

                    // Зчитуємо напрямок вводу (пріоритет осі X, потім Y)
                    if (Math.Abs(vxInput) > 0.001f)
                    {
                        dirX = Math.Sign(vxInput);
                    }
                    else if (Math.Abs(vyInput) > 0.001f)
                    {
                        dirY = Math.Sign(vyInput);
                    }

                    if (dirX != 0 || dirY != 0)
                    {
                        int targetGridX = curGridX + dirX;
                        int targetGridY = curGridY + dirY;

                        // Перевіряємо колізію з тайлами на шарі 1 (стіни/блокування)
                        int targetTileType = world.GetTile(targetGridX, targetGridY, 1);
                        if (targetTileType == 0) // Шлях вільний — починаємо плавний перехід
                        {
                            gridMove.IsMoving = true;
                            gridMove.TargetGridX = targetGridX;
                            gridMove.TargetGridY = targetGridY;
                        }
                    }
                }

                if (gridMove.IsMoving)
                {
                    float targetX = gridMove.TargetGridX * tileSize;
                    float targetY = gridMove.TargetGridY * tileSize;

                    float dx = targetX - transform.X;
                    float dy = targetY - transform.Y;
                    float step = gridMove.Speed * dt;

                    float gridNewX = transform.X;
                    float gridNewY = transform.Y;

                    // Рух по осі X
                    if (Math.Abs(dx) <= step)
                        gridNewX = targetX;
                    else
                        gridNewX += Math.Sign(dx) * step;

                    // Рух по осі Y
                    if (Math.Abs(dy) <= step)
                        gridNewY = targetY;
                    else
                        gridNewY += Math.Sign(dy) * step;

                    // Якщо дійшли до цільової клітинки — зупиняємось
                    if (Math.Abs(gridNewX - targetX) < 0.001f && Math.Abs(gridNewY - targetY) < 0.001f)
                    {
                        gridNewX = targetX;
                        gridNewY = targetY;
                        gridMove.IsMoving = false;
                    }

                    transform.X = gridNewX;
                    transform.Y = gridNewY;

                    registry.Transforms[entityId] = transform;
                }

                registry.GridMovements[entityId] = gridMove;
                continue; // Пропускаємо звичайну фізику
            }

            // 2. Стандартний вільний рух (для звичайних сутностей)
            float vx = velocity.VX;
            float vy = velocity.VY;

            if (Math.Abs(vx) < 0.001f && Math.Abs(vy) < 0.001f)
                continue;

            float newX = transform.X;
            float newY = transform.Y;

            bool hasCollision = registry.HasCollision(entityId);
            CollisionComponent col = hasCollision ? registry.Collisions[entityId] : default;

            if (hasCollision && col.IsActive)
            {
                // Рух по осі X
                float targetX = transform.X + vx * dt;
                if (CheckTileCollision(targetX, transform.Y, col.Width, col.Height, transform.Z, world, tileSize, out float resolvedX, true, vx > 0))
                {
                    newX = resolvedX;
                    velocity.VX = 0;
                }
                else
                {
                    newX = targetX;
                }

                // Рух по осі Y
                float targetY = transform.Y + vy * dt;
                if (CheckTileCollision(newX, targetY, col.Width, col.Height, transform.Z, world, tileSize, out float resolvedY, false, vy > 0))
                {
                    newY = resolvedY;
                    velocity.VY = 0;
                }
                else
                {
                    newY = targetY;
                }
            }
            else
            {
                newX += vx * dt;
                newY += vy * dt;
            }

            // Оновлюємо компоненти
            registry.Transforms[entityId] = new TransformComponent(newX, newY, transform.Z);
            registry.Velocities[entityId] = velocity;
        }
    }

    private static bool CheckTileCollision(
        float x, float y, int w, int h, int layer, 
        WorldManager world, float tileSize, out float resolvedVal, bool isXAxis, bool isMovingPositive)
    {
        resolvedVal = isXAxis ? x : y;
        bool collided = false;

        int startX = (int)Math.Floor(x / tileSize);
        int endX = (int)Math.Floor((x + w - 0.1f) / tileSize);
        int startY = (int)Math.Floor(y / tileSize);
        int endY = (int)Math.Floor((y + h - 0.1f) / tileSize);

        // Перевіряємо всі тайли, які перетинаються з коліжн-боксом
        for (int ty = startY; ty <= endY; ty++)
        {
            for (int tx = startX; tx <= endX; tx++)
            {
                // Шар 1 - це фізичні перешкоди (колізії). Але перевіримо і шар 0 на випадок порожнечі, 
                // та шар 1 на наявність твердих блоків (наприклад, 1 - фіолетовий блок).
                int tileType = world.GetTile(tx, ty, 1); // 1 = Solid obstacle layer
                
                if (tileType > 0) // Якщо там є блок
                {
                    collided = true;
                    if (isXAxis)
                    {
                        if (isMovingPositive) // Рух вправо -> вдаряємося лівим боком тайлу
                            resolvedVal = tx * tileSize - w;
                        else // Рух вліво -> вдаряємося правим боком тайлу
                            resolvedVal = (tx + 1) * tileSize;
                    }
                    else
                    {
                        if (isMovingPositive) // Рух вниз -> вдаряємося верхнім боком тайлу
                            resolvedVal = ty * tileSize - h;
                        else // Рух вгору -> вдаряємося нижнім боком тайлу
                            resolvedVal = (ty + 1) * tileSize;
                    }
                    return true;
                }
            }
        }

        return collided;
    }
}
