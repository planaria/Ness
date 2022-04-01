Shader "Unlit/PanelBackground"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            float4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float Noise3(float3 x)
            {
                float y = frac(sin(dot(x, float3(3.312068, 8.1068, 1.0803))) * 1080.0543);
                y = frac(sin(y * 2.0384) * 640.06032);
                y = frac(sin(y * 2.0384) * 640.06032);
                return 2.0 * y - 1.0;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 d = pow(abs(i.uv - 0.5) / 1.5, 4.0);
                float n = (Noise3(float3(i.uv, _Time.x)) - 0.5) * 0.2;
                float4 col = _Color * (1.0 - pow(d.x + d.y, 1.0 / 4.0) * (1.0 + n));
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
