/// <summary>2D vector with basic arithmetic.</summary>
public readonly record struct Vec2(float X, float Y)
{
    public static Vec2 Zero => new(0, 0);

    public float LengthSq => X * X + Y * Y;
    public float Length => MathF.Sqrt(LengthSq);

    public Vec2 Normalized()
    {
        var len = Length;
        return len < 1e-6f ? Zero : new Vec2(X / len, Y / len);
    }

    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2 operator *(Vec2 v, float s) => new(v.X * s, v.Y * s);
    public static Vec2 operator *(float s, Vec2 v) => new(v.X * s, v.Y * s);
    public static Vec2 operator /(Vec2 v, float s) => new(v.X / s, v.Y / s);

    public static float Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Y * b.Y;

    public Vec2 MoveToward(Vec2 target, float maxDelta)
    {
        var diff = target - this;
        var dist = diff.Length;
        return dist <= maxDelta ? target : this + diff.Normalized() * maxDelta;
    }

    public Vec2 Rotated(float angle)
    {
        var cos = MathF.Cos(angle);
        var sin = MathF.Sin(angle);
        return new Vec2(X * cos - Y * sin, X * sin + Y * cos);
    }

    public float DistanceSq(Vec2 other) => (this - other).LengthSq;

    public override string ToString() => $"({X:F2}, {Y:F2})";
}
