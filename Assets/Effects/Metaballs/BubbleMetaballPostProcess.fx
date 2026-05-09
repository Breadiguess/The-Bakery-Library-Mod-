sampler uImage0 : register(s0);

float2 uTextureSize;
float uTime;

float4 uBaseColor;
float4 uRimColor;
float4 uPinkColor;
float4 uBlueColor;
float4 uYellowColor;

float uOpacity;
float uRimPower;
float uIridescenceStrength;

float SampleMask(float2 uv)
{
    float4 tex = tex2D(uImage0, uv);
    
    float rgbMask = max(tex.r, max(tex.g, tex.b));
    return saturate(max(tex.a, rgbMask));
}

float Hash(float2 p)
{
    p = frac(p * float2(123.34f, 456.21f));
    p += dot(p, p + 45.32f);
    return frac(p.x * p.y);
}

float SmoothNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);

    float a = Hash(i);
    float b = Hash(i + float2(1.0f, 0.0f));
    float c = Hash(i + float2(0.0f, 1.0f));
    float d = Hash(i + float2(1.0f, 1.0f));

    float2 u = f * f * (3.0f - 2.0f * f);

    return lerp(
        lerp(a, b, u.x),
        lerp(c, d, u.x),
        u.y
    );
}

float4 MainPS(float2 uv : TEXCOORD0) : COLOR0
{
    float mask = SampleMask(uv);

    if (mask <= 0.001f)
        discard;

    float2 texel = 1.0f / max(uTextureSize, float2(1.0f, 1.0f));

    float left = SampleMask(uv - float2(texel.x, 0.0f));
    float right = SampleMask(uv + float2(texel.x, 0.0f));
    float up = SampleMask(uv - float2(0.0f, texel.y));
    float down = SampleMask(uv + float2(0.0f, texel.y));

    float2 gradient = float2(right - left, down - up);
    float gradientLength = length(gradient);

    // Edge/rim from mask slope
    float edge = saturate(gradientLength * 22.0f);

    float outerRim = edge * (1.0f - smoothstep(0.45f, 1.0f, mask));
    outerRim = pow(saturate(outerRim), uRimPower);

    float center = smoothstep(0.25f, 1.0f, mask);

    float noise = SmoothNoise(uv * uTextureSize / 42.0f + float2(uTime * 0.25f, -uTime * 0.18f));

    float bandA = sin((uv.x + uv.y) * 34.0f + noise * 3.0f + uTime * 1.7f) * 0.5f + 0.5f;
    float bandB = sin((uv.x - uv.y) * 46.0f - noise * 2.0f - uTime * 1.15f) * 0.5f + 0.5f;

    float3 iridescentA = lerp(uPinkColor.rgb, uBlueColor.rgb, bandA);
    float3 iridescentB = lerp(uYellowColor.rgb, uRimColor.rgb, bandB);
    float3 iridescence = lerp(iridescentA, iridescentB, noise);

    // Directional shine from upper-left
    float2 safeGradient = gradient;
    if (length(safeGradient) < 0.0001f)
        safeGradient = float2(0.0f, 1.0f);

    float2 normal = normalize(-safeGradient);
    float2 lightDir = normalize(float2(-0.55f, -0.85f));

    float light = saturate(dot(normal, lightDir));
    float highlight = pow(light, 4.0f) * edge;

    // Secondary crescent highlight on the opposite side.
    float backLight = saturate(dot(normal, normalize(float2(0.7f, 0.55f))));
    float secondaryHighlight = pow(backLight, 8.0f) * edge * 0.55f;

    float3 color = uBaseColor.rgb;

    // Iridescence mostly lives on the membrane, not the center.
    color = lerp(color, iridescence, outerRim * uIridescenceStrength);

    // Bright rim.
    color = lerp(color, uRimColor.rgb, outerRim * 0.9f);

    // Specular-ish highlights.
    color += uRimColor.rgb * highlight * 0.85f;
    color += uBlueColor.rgb * secondaryHighlight * 0.45f;

    // Center is very transparent, rim is more visible.
    float alpha = mask;
    alpha *= lerp(0.18f, 0.82f, outerRim);
    alpha += highlight * 0.22f;
    alpha += secondaryHighlight * 0.10f;

    // Keep the middle faintly visible.
    alpha += center * 0.08f;

    alpha = saturate(alpha * uOpacity);

    if (alpha <= 0.001f)
        discard;

    return float4(color * alpha, alpha);
}

technique BubbleMetaballPostProcess
{
    pass Pass0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}