sampler baseTexture : register(s0);
sampler noiseTexture : register(s1);

matrix uWorldViewProjection;
float uTime;
float uIntensity;
float3 uSecondaryColor;

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
    VertexShaderOutput output = (VertexShaderOutput)0;
    output.Position = mul(input.Position, uWorldViewProjection);
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;

    float2 uv1 = float2(coords.x * 2.5 - uTime * 2.4, coords.y * 1.2);
    float2 uv2 = float2(coords.x * 4.0 - uTime * 1.6, coords.y * 2.0 + 0.35);

    float noise1 = tex2D(noiseTexture, uv1).r;
    float noise2 = tex2D(noiseTexture, uv2).r;
    float combinedNoise = lerp(noise1, noise2, 0.5);

    float verticalProfile = sin(coords.y * 3.14159265);
    float bodyMask = pow(saturate(verticalProfile), lerp(2.2, 0.85, combinedNoise));

    float lengthFade = smoothstep(0.0, 0.12, coords.x) * smoothstep(1.0, 0.85, coords.x);

    float hotCore = pow(saturate(verticalProfile), 5.0) * combinedNoise;

    float3 baseCol = input.Color.rgb;
    float3 finalRgb = lerp(baseCol, uSecondaryColor, saturate(hotCore * 1.6 + combinedNoise * 0.35));

    float alpha = saturate(bodyMask * lengthFade * input.Color.a * (0.45 + combinedNoise * 0.9));

    return float4(finalRgb * alpha * uIntensity, alpha * 0.45);
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}