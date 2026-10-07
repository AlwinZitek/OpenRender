Shader "Custom/Grid"
{
    
    Properties
    {
        _GridColor ("Grid Color", Color) = (0.8, 0.8, 0.8, 1)
        _BgColor ("Background Color", Color) = (1, 1, 1, 1)
        _GridSize ("Grid Size", Float) = 1.0
        _LineWidth ("Line Width", Float) = 0.02
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _GridColor;
            float4 _BgColor;
            float _GridSize;
            float _LineWidth;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

v2f vert(appdata v)
{
    v2f o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = mul(unity_ObjectToWorld, v.vertex).xy;
    return o;
}

fixed4 frag(v2f i) : SV_Target
{
    float2 grid = frac(i.uv / _GridSize);
    float lineWidthUV = _LineWidth / _GridSize;
    float lineX = step(grid.x, lineWidthUV) + step(1.0 - lineWidthUV, grid.x);
    float lineY = step(grid.y, lineWidthUV) + step(1.0 - lineWidthUV, grid.y);
    float onLine = saturate(lineX + lineY);
    return lerp(_BgColor, _GridColor, onLine);
}
            ENDCG
        }
    }
}