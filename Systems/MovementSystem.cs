using System;
using Microsoft.Xna.Framework;

namespace Factory;

/// <summary>
/// Система руху: обробляє GridMovement (покроковий) та вільний рух з колізіями.
/// </summary>
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

            // Покроковий рух по клітинках
            if (registry.HasGridMovement(entityId))
            {
                var gridMove = registry.GridMovements[entityId];

                int curGridX = (int)Math.Round(transform.X / tileSize);
                int curGridY = (int)Math.Round(transform.Y / tileSize);

                if (!gridMove.IsMoving)
                {
                    float vxInput = velocity.VX;
                    float vyInput = velocity.VY;

                    int dirX = 0;
                    int dirY = 0;

                    if (Math.Abs(vxInput) > 0.001f)
                        dirX = Math.Sign(vxInput);
                    else if (Math.Abs(vyInput) > 0.001f)
                        dirY = Math.Sign(vyInput);

                    if (dirX != 0 || dirY != 0)
                    {
                        int targetGridX = curGridX + dirX;
                        int targetGridY = curGridY + dirY;

                        int targetTileType = world.GetTile(targetGridX, targetGridY, 1);
                        if (targetTileType == 0)
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

                    if (Math.Abs(dx) <= step)
                        gridNewX = targetX;
                    else
                        gridNewX += Math.Sign(dx) * step;

                    if (Math.Abs(dy) <= step)
                        gridNewY = targetY;
                    else
                        gridNewY += Math.Sign(dy) * step;

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
                continue;
            }

            // Вільний рух
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
                float targetX = transform.X + vx * dt;
                if (CollisionSystem.CheckTileCollision(targetX, transform.Y, col.Width, col.Height, transform.Z, world, tileSize, out float resolvedX, true, vx > 0))
                {
                    newX = resolvedX;
                    velocity.VX = 0;
                }
                else
                {
                    newX = targetX;
                }

                float targetY = transform.Y + vy * dt;
                if (CollisionSystem.CheckTileCollision(newX, targetY, col.Width, col.Height, transform.Z, world, tileSize, out float resolvedY, false, vy > 0))
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

            registry.Transforms[entityId] = new TransformComponent(newX, newY, transform.Z);
            registry.Velocities[entityId] = velocity;
        }
    }
}
