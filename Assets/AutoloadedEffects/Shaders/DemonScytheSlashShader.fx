sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

float globalTime;
float noiseScroll;
float edgeIntensity;
float burstProgress;

float3 edgeColor;
float3 coreColor;

matrix uWorldViewProjection;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    output.Position = mul(input.Position, uWorldViewProjection);
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TextureCoordinates;

    float noiseA = tex2D(
        uImage1,
        float2(uv.x * 2.8 + globalTime * noiseScroll, uv.y * 0.8)
    ).r;

    float noiseB = tex2D(
        uImage1,
        float2(uv.x * 6.0 - globalTime * noiseScroll * 0.55, uv.y * 1.7)
    ).g;

    float noise = saturate(noiseA * 0.65 + noiseB * 0.35);

    float center = saturate(1.0 - abs(uv.y - 0.5) * 2.0);
    float edge = pow(center, 0.45);
    float sharpCore = pow(center, 3.5);

    float flicker = 0.82 + sin(globalTime * 9.0 + uv.x * 18.0) * 0.18;
    float intensity = saturate((0.55 + noise * 0.8) * flicker);

    float3 color = lerp(edgeColor, coreColor, sharpCore);
    color *= intensity;
    color += edgeColor * edge * edgeIntensity * 0.35;

    float alpha = input.Color.a * edge * (0.7 + noise * 0.45);

    return float4(color * input.Color.rgb, alpha);
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
