namespace Factory;

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
