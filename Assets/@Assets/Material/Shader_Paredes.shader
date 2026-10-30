Shader "Halloween/DepthOccluder"
{
    Properties
    {
        _Push ("Empuje hacia atras (m)", Float) = 0.08
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-10" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "Occluder"
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off
            Offset 1, 1

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Push;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                float3 p = i.positionOS.xyz;
                p.y -= _Push; // el plano de AR Foundation tiene su normal en +Y local
                o.positionCS = TransformObjectToHClip(p);
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}