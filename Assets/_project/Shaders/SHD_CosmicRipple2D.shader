Shader "Constellation/CosmicRipple2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _DistortCenter ("Ripple Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _DistortStrength ("Ripple Strength", Range(0, 0.25)) = 0.05
        _DistortRadius ("Ripple Radius", Range(0.01, 2)) = 0.6
        _DistortSpeed ("Ripple Speed", Range(0, 20)) = 8
        _GlowColor ("Glow Color", Color) = (0.35, 0.95, 1, 1)
        _GlowStrength ("Glow Strength", Range(0, 2)) = 0.4
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest Always
        Lighting Off

        Pass
        {
            Name "CosmicRipple"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color  : COLOR;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float4 _DistortCenter;
            float _DistortStrength;
            float _DistortRadius;
            float _DistortSpeed;
            fixed4 _GlowColor;
            float _GlowStrength;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 delta = i.uv - _DistortCenter.xy;
                float distance = length(delta);

                // Radial wave that falls off with distance from the impact point.
                float wave = sin(distance * _DistortRadius * 40.0 - _Time.y * _DistortSpeed);
                float falloff = saturate(1.0 - distance / max(_DistortRadius, 0.0001));
                float2 offset = normalize(delta + 1e-5) * wave * falloff * _DistortStrength;

                float2 distortedUv = i.uv + offset;
                fixed4 tex = tex2D(_MainTex, distortedUv);
                fixed4 result = tex * i.color;

                // Light the wave crest so the ripple reads against dark space.
                float crest = saturate(wave * falloff);
                result.rgb += _GlowColor.rgb * crest * _GlowStrength;
                result.a = saturate(result.a + crest * _GlowStrength * 0.5);

                return result;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
