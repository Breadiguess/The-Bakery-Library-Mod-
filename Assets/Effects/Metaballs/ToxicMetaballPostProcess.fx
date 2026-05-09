sampler uImage0 : register(s0);

float2 uScreenSize;
float uTime;

float4 uPrimaryColor;
float4 uSecondaryColor;
float4 uHighlightColor;

float Hash(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float SmoothNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);

    float a = Hash(i);
    float b = Hash(i + float2(1.0, 0.0));
    float c = Hash(i + float2(0.0, 1.0));
    float d = Hash(i + float2(1.0, 1.0));

    float2 u = f * f * (3.0 - 2.0 * f);

    return lerp(
        lerp(a, b, u.x),
        lerp(c, d, u.x),
        u.y
    );
}

float SampleMask(float2 uv)
{
    float4 tex = tex2D(uImage0, uv);

    float rgbMask = max(tex.r, max(tex.g, tex.b));
    return saturate(max(tex.a, rgbMask));
}

float4 MainPS(float2 uv : TEXCOORD0) : COLOR0
{
    float field = SampleMask(uv);

    if (field <= 0.001f)
        discard;

    float alpha = saturate(field);

    float noise = SmoothNoise(uv * uScreenSize / 32.0f + float2(uTime * 0.7f, uTime * -0.35f));
    float verticalWave = sin(uv.y * 30.0f - uTime * 4.0f) * 0.5f + 0.5f;

    float colorLerp = saturate(noise * 0.35f + verticalWave * 0.65f);
    float3 baseColor = lerp(uSecondaryColor.rgb, uPrimaryColor.rgb, colorLerp);

    float2 texel = 1.0f / uScreenSize;

    float left = SampleMask(uv - float2(texel.x, 0.0f));
    float right = SampleMask(uv + float2(texel.x, 0.0f));
    float up = SampleMask(uv - float2(0.0f, texel.y));
    float down = SampleMask(uv + float2(0.0f, texel.y));

    float2 gradient = float2(right - left, down - up);
    float edgeStrength = saturate(length(gradient) * 18.0f);

    float rim = edgeStrength * (1.0f - smoothstep(0.55f, 1.0f, field));

    float3 finalColor = lerp(baseColor, uHighlightColor.rgb, rim * 0.65f);

    return float4(finalColor * alpha, alpha);
}

technique Technique1
{
    pass Pass0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}