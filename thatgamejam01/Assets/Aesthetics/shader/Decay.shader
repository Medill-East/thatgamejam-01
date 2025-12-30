Shader "Custom/Decay"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f{
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _DecayTime;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            v2f vert (appdata v){
                v2f o;
                o.uv = v.uv;
				float4 uv = float4(0, 0, 0, 1);
                uv.xy = float2(1, _ProjectionParams.x) * (v.uv.xy * float2( 2, 2) - float2(1, 1));
				o.vertex = uv; 
                return o;
            }

            fixed4 frag (v2f i) : SV_Target{
				float4 color = tex2D(_MainTex, i.uv);
                return color * (1.0f - 1.0f / (30.0f * _DecayTime));
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
