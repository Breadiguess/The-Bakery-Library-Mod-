#define MAX_METABALLS 64

float4 uMetaballs[MAX_METABALLS];

int uMetaballCount;

float2 uScreenSize;

float4 uDrawBounds;

float FieldContribution(float2 pixelPosition, float4 metaball)
{
    float2 center = metaball.xy;
    float radius = max(metaball.z, 0.001f);
    float strength = metaball.w;

    float distanceToCenter = distance(pixelPosition, center);

    if (distanceToCenter >= radius)
        return 0.0f;

    float normalizedDistance = distanceToCenter / radius;
    float falloff = 1.0f - normalizedDistance;

    falloff *= falloff;

    return falloff * strength;
}

float4 MainPS(float2 uv : TEXCOORD0) : COLOR0
{
    float2 pixelPosition = uDrawBounds.xy + uv * uDrawBounds.zw;

    float field = 0.0f;

    [loop]
    for (int i = 0; i < uMetaballCount; i++)
        field += FieldContribution(pixelPosition, uMetaballs[i]);

    return float4(field, field, field, field);
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}