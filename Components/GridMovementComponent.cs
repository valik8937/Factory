namespace Factory;

public struct GridMovementComponent
{
    public bool IsMoving;
    public int TargetGridX;
    public int TargetGridY;
    public float Speed;

    public GridMovementComponent(float speed = 128f)
    {
        IsMoving = false;
        TargetGridX = 0;
        TargetGridY = 0;
        Speed = speed;
    }
}
