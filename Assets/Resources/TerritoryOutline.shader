Shader "Territory/SelectionOutline"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,0,1)
        _Width ("Width in world units", Float) = 0.03
        [HideInInspector] _Offset ("Camera plane direction", Vector) = (0,0,0,0)
        [HideInInspector] _StencilComp ("Stencil comparison", Float) = 6
        [HideInInspector] _StencilOp ("Stencil operation", Float) = 0
        [HideInInspector] _ColorMask ("Color mask", Float) = 15
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZWrite Off
            ZTest Always
            Cull Off
            ColorMask [_ColorMask]
            Stencil
            {
                Ref 64
                ReadMask 64
                WriteMask 64
                Comp [_StencilComp]
                Pass [_StencilOp]
            }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float _Width;
            float4 _Offset;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 positionVS = TransformWorldToView(positionWS);
                positionVS.xy += _Offset.xy * _Width;
                output.positionCS = mul(UNITY_MATRIX_P, float4(positionVS, 1));
                return output;
            }
            half4 frag(Varyings input) : SV_Target { return _Color; }
            ENDHLSL
        }
    }
}
