using System.Collections.Generic;

namespace Factory;

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
