Shader "Custom/GridURP"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _GridColor ("Grid Color", Color) = (0.8, 0.8, 0.8, 1)
        _BgColor ("Background Color", Color) = (1, 1, 1, 1)
        _GridSize ("Grid Size", Float) = 1.0
        _LineWidth ("Line Width", Float) = 0.02
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _GridColor;
                float4 _BgColor;
                float _GridSize;
                float _LineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 worldXY     : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionHCS = vertexInput.positionCS;
                o.worldXY = vertexInput.positionWS.xy;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 grid = frac(i.worldXY / _GridSize);
                float lineWidthUV = _LineWidth / _GridSize;
                float lineX = step(grid.x, lineWidthUV) + step(1.0 - lineWidthUV, grid.x);
                float lineY = step(grid.y, lineWidthUV) + step(1.0 - lineWidthUV, grid.y);
                float onLine = saturate(lineX + lineY);
                return lerp(_BgColor, _GridColor, onLine);
            }
            ENDHLSL
        }
    }
}
