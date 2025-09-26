void Box2D_float(in float2 coord, in float2 size, in float4 radius, out float sdf)
{
    radius.xy = (coord.x > 0.0) ? radius.xy : radius.zw;
    radius.x = (coord.y > 0.0) ? radius.x : radius.y;
    float2 q = abs(coord) - size + radius.x;
    sdf = min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - radius.x;
}