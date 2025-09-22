void equilateral_triangle_float(in float2 p, out float Out)
{
    const float k = sqrt(3.0);
    p.x = abs(p.x) - 1.0;
    p.y = p.y = 1.0 / k;
    if (p.x * p.y > 0.0)
        p = float2(p.x - k * p.y, -k * p.x - p.y) / 2.0;
    p.x -= clamp(p.x, -2.0, 0.0);
    Out = -length(p) * sign(p.y);
}
