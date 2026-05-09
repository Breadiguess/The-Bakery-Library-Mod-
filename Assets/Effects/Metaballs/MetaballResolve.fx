sampler uImage0 : register(s0);

float4 uColor;
float uThreshold;
float uEdgeSoftness;

float4 MainPS(float2 uv : TEXCOORD0) : COLOR0
{
    float field = tex2D(uImage0, uv).r;

    float softness = max(uEdgeSoftness, 0.0001f);
    
    float alpha = smoothstep(
        uThreshold,
        uThreshold + softness,
        field
    );

    alpha *= uColor.a;

    if (alpha <= 0.001f)
        discard;
    
    return float4(uColor.rgb * alpha, alpha);
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}