using Microsoft.Xna.Framework;

namespace Factory;

/// <summary>
/// Контролер гравця: створення, перевірка колізій при будівництві.
/// </summary>
public class PlayerController
{
    public int PlayerId { get; private set; }

    public void Initialize(Registry registry)
    {
        PlayerId = registry.CreateEntity();
        registry.Transforms[PlayerId] = new TransformComponent(0f, 0f, 1);
        registry.Velocities[PlayerId] = new VelocityComponent(0f, 0f);
        registry.Sprites[PlayerId] = new SpriteComponent(Color.Black, 32, 32);
        registry.Collisions[PlayerId] = new CollisionComponent(32, 32, true);
        registry.GridMovements[PlayerId] = new GridMovementComponent(160f);
    }

    public void ApplyInput(Registry registry, InputSystem input)
    {
        registry.Velocities[PlayerId] = new VelocityComponent(input.MovementInput.X, input.MovementInput.Y);
    }

    /// <summary>
    /// Перевіряє, чи не перетинається блок на позиції (tileX, tileY) з гравцем.
    /// </summary>
    public bool CanPlaceBlock(Registry registry, int tileX, int tileY, float tileSize)
    {
        var playerTrans = registry.Transforms[PlayerId];
        var playerCol = registry.Collisions[PlayerId];

        var playerBox = new Rectangle((int)playerTrans.X, (int)playerTrans.Y, playerCol.Width, playerCol.Height);
        var blockBox = new Rectangle((int)(tileX * tileSize), (int)(tileY * tileSize), (int)tileSize, (int)tileSize);

        return !playerBox.Intersects(blockBox);
    }

    public Vector2 GetCenter(Registry registry)
    {
        var t = registry.Transforms[PlayerId];
        return new Vector2(t.X + 16f, t.Y + 16f);
    }
}
