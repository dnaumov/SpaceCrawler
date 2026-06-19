/// <summary>
/// Gradient field calculated over the arena grid each simulation tick.
/// Food gradients:  SUM[ 1 / distSq ]
/// Cell gradients:  SUM[ 1 / distSq ]
/// Toxic gradients: SUM[ 1 / distSq ] (based on toxic zone positions)
/// </summary>
public sealed class GradientField
{
    private readonly int _gridW;
    private readonly int _gridH;
    private readonly float _cellW;
    private readonly float _cellH;
    private readonly float[] _foodGrad;
    private readonly float[] _cellGrad;
    private readonly float[] _toxicGrad;

    public GradientField(float arenaW, float arenaH, int gridW = 24, int gridH = 14)
    {
        _gridW     = gridW;
        _gridH     = gridH;
        _cellW     = arenaW / gridW;
        _cellH     = arenaH / gridH;
        _foodGrad  = new float[gridW * gridH];
        _cellGrad  = new float[gridW * gridH];
        _toxicGrad = new float[gridW * gridH];
    }

    public void Recalculate(
        IReadOnlyList<Vec2> foods,
        IReadOnlyList<(Vec2 pos, bool alive)> cells,
        IReadOnlyList<EnvironmentZone> zones)
    {
        for (var gy = 0; gy < _gridH; gy++)
        {
            for (var gx = 0; gx < _gridW; gx++)
            {
                var cx = (gx + 0.5f) * _cellW;
                var cy = (gy + 0.5f) * _cellH;

                float fSum = 0f, cSum = 0f, tSum = 0f;

                foreach (var f in foods)
                {
                    var dx = f.X - cx;
                    var dy = f.Y - cy;
                    fSum += 1f / (dx * dx + dy * dy + 1f);
                }

                foreach (var (pos, alive) in cells)
                {
                    if (!alive)
                    {
                        continue;
                    }

                    var dx = pos.X - cx;
                    var dy = pos.Y - cy;
                    cSum += 1f / (dx * dx + dy * dy + 1f);
                }

                foreach (var zone in zones)
                {
                    if (zone.Type != EnvironmentType.Toxic)
                    {
                        continue;
                    }

                    var zcx = zone.X + zone.W * 0.5f;
                    var zcy = zone.Y + zone.H * 0.5f;
                    var dx = zcx - cx;
                    var dy = zcy - cy;
                    tSum += 1f / (dx * dx + dy * dy + 1f);
                }

                var idx = gy * _gridW + gx;
                _foodGrad[idx]  = fSum;
                _cellGrad[idx]  = cSum;
                _toxicGrad[idx] = tSum;
            }
        }
    }

    public float FoodGradAt(Vec2 pos, float arenaW, float arenaH)
        => SampleAt(_foodGrad, pos, arenaW, arenaH);

    public float CellGradAt(Vec2 pos, float arenaW, float arenaH)
        => SampleAt(_cellGrad, pos, arenaW, arenaH);

    public float ToxicGradAt(Vec2 pos, float arenaW, float arenaH)
        => SampleAt(_toxicGrad, pos, arenaW, arenaH);

    private float SampleAt(float[] grid, Vec2 pos, float arenaW, float arenaH)
    {
        var gx = Math.Clamp((int)(pos.X / arenaW * _gridW), 0, _gridW - 1);
        var gy = Math.Clamp((int)(pos.Y / arenaH * _gridH), 0, _gridH - 1);
        return grid[gy * _gridW + gx];
    }
}
