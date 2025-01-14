﻿Shader "InteriorMapping/InteriorMapping2DAtlas"
{
    Properties
    {
        _RoomTex ("RoomTex", 2d) = "white" {}
        _Rooms ("Room Atlas Rows&Cols (XY)", Vector) = (1,1,0,0)
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float2 _Rooms;
            float4 _RoomTex_ST;
            CBUFFER_END

            TEXTURE2D(_RoomTex);
            SAMPLER(sampler_RoomTex);

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 tangentViewDir : TEXCOORD1;
            };

            float2 rand2(float co)
            {
                return frac(sin(co * float2(12.9898, 78.233)) * 43758.5453);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _RoomTex);

                // get tangent space camera vector
                float4 objCam = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0));
                float3 viewDir = v.vertex.xyz - objCam.xyz;
                float tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                float3 bitangent = cross(v.normal.xyz, v.tangent.xyz) * tangentSign;
                o.tangentViewDir = float3(
                    dot(viewDir, v.tangent.xyz),
                    dot(viewDir, bitangent),
                    dot(viewDir, v.normal)
                );
                o.tangentViewDir *= _RoomTex_ST.xyx;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // room uvs
                float2 roomUV = frac(i.uv);
                float2 roomIndexUV = floor(i.uv);

                // randomize the room
                float2 n = floor(rand2(roomIndexUV.x + roomIndexUV.y * (roomIndexUV.x + 1)) * _Rooms.xy);
                //float2 n = floor(_Rooms.xy);
                roomIndexUV += n;

                // get room depth from room atlas alpha
                float farFrac = SAMPLE_TEXTURE2D(_RoomTex, sampler_RoomTex, (roomIndexUV + 0.5) / _Rooms).a;

                // Specify depth manually
                // float farFrac = _RoomDepth;

                //remap [0,1] to [+inf,0]
                //->if input _RoomDepth = 0    -> depthScale = 0      (inf depth room)
                //->if input _RoomDepth = 0.5  -> depthScale = 1
                //->if input _RoomDepth = 1    -> depthScale = +inf   (0 volume room)
                float depthScale = 1.0 / (1.0 - farFrac) - 1.0;

                // raytrace box from view dir
                // normalized box space's ray start pos is on trinagle surface, where z = -1
                float3 pos = float3(roomUV * 2 - 1, -1);
                // transform input ray dir from tangent space to normalized box space
                i.tangentViewDir.z *= -depthScale;
                // 预先处理倒数  t=(1-p)/view=1/view-p/view
                float3 id = 1.0 / i.tangentViewDir;
                float3 k = abs(id) - pos * id;
                float kMin = min(min(k.x, k.y), k.z);
                pos += kMin * i.tangentViewDir;

                // remap from [-1,1] to [0,1] room depth
                float interp = pos.z * 0.5 + 0.5;

                // account for perspective in "room" textures
                // assumes camera with an fov of 53.13 degrees (atan(0.5))
                // visual result = transform nonlinear depth back to linear
                float realZ = saturate(interp) / depthScale + 1;
                interp = 1.0 - (1.0 / realZ);
                interp *= depthScale + 1.0;

                // iterpolate from wall back to near wall
                float2 interiorUV = pos.xy * lerp(1.0, farFrac, interp);
                interiorUV = interiorUV * 0.5 + 0.5;

                // sample room atlas texture
                float4 room = SAMPLE_TEXTURE2D(_RoomTex, sampler_RoomTex, (roomIndexUV + interiorUV.xy) / _Rooms);
                return float4(room.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}