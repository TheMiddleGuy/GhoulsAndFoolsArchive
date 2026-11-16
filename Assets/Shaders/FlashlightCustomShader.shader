Shader "Custom/FlashlightCustomShader"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
		_EmissionColor("Emission Color", Color) = (0,0,0,1)
		_EmissionIntensity("Emission Intensity", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend Mode", Integer) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination Blend Mode", Integer) = 10
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            //ZWrite Off                //DONT uncomment until i say so

            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Forward / Forward+ support
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"

            #define MAX_FLASHLIGHTS 4

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
				float4 _EmissionColor;
				float _EmissionIntensity;
            CBUFFER_END

            // Multiplayer flashlight globals
            int _FlashlightCount;

            float3 _FlashlightPosWS[MAX_FLASHLIGHTS];
            float4x4 _FlashlightWorldToLocal[MAX_FLASHLIGHTS];

            float _FlashIntensity[MAX_FLASHLIGHTS];

            float _SphereRadius[MAX_FLASHLIGHTS];
            float _SphereSoftness[MAX_FLASHLIGHTS];

            float _BeamLength[MAX_FLASHLIGHTS];
            float _BeamStartHalfWidth[MAX_FLASHLIGHTS];
            float _BeamStartHalfHeight[MAX_FLASHLIGHTS];
            float _BeamEndHalfWidth[MAX_FLASHLIGHTS];
            float _BeamEndHalfHeight[MAX_FLASHLIGHTS];
            float _BeamSoftness[MAX_FLASHLIGHTS];

            float4 _FlashColor;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            float SphereMask(float3 worldPos, int i)
            {
                float dist = distance(worldPos, _FlashlightPosWS[i]);
                return saturate(1.0 - smoothstep(_SphereRadius[i] - _SphereSoftness[i], _SphereRadius[i], dist));
            }

            float FrustumMask(float3 worldPos, int i)
            {
                float3 localPos = mul(_FlashlightWorldToLocal[i], float4(worldPos, 1.0)).xyz;

                float z = localPos.z;
                if (z < 0.0 || z > _BeamLength[i])
                    return 0.0;

                float t = saturate(z / max(_BeamLength[i], 0.0001));

                float halfW = lerp(_BeamStartHalfWidth[i], _BeamEndHalfWidth[i], t);
                float halfH = lerp(_BeamStartHalfHeight[i], _BeamEndHalfHeight[i], t);

                float dx = abs(localPos.x) - halfW;
                float dy = abs(localPos.y) - halfH;

                float outside = max(dx, dy);

                float mask = 1.0 - smoothstep(0.0, _BeamSoftness[i], outside);
                float forwardFade = 1.0 - smoothstep(_BeamLength[i] * 0.7, _BeamLength[i], z);

                return saturate(mask * forwardFade);
            }

            float SurfaceFacingMask(float3 normalWS, float3 worldPos, int i)
            {
                float3 toLight = normalize(_FlashlightPosWS[i] - worldPos);
                return saturate(dot(normalWS, toLight));
            }

            float3 ComputeSceneLighting(float3 baseColor, InputData inputData)
            {
                float3 normalWS = normalize(inputData.normalWS);

                // Tiny baseline so "dark" is not full void
                float3 lighting = baseColor * 0.02;

                #if defined(_ADDITIONAL_LIGHTS)

                // Forward+ path
                #if USE_CLUSTER_LIGHT_LOOP
                UNITY_LOOP for (uint lightIndex = 0;
                    lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS);
                    lightIndex++)
                {
                    Light light = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));

                    float3 dir = normalize(light.direction);
                    float ndl = saturate(dot(normalWS, dir));

                    lighting += baseColor * light.color * ndl
                              * light.distanceAttenuation
                              * light.shadowAttenuation;
                }
                #endif

                // Regular Forward path
                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));

                    float3 dir = normalize(light.direction);
                    float ndl = saturate(dot(normalWS, dir));

                    lighting += baseColor * light.color * ndl
                              * light.distanceAttenuation
                              * light.shadowAttenuation;
                LIGHT_LOOP_END

                #endif

                return lighting;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                float3 baseColor = baseSample.rgb;
                float alpha = baseSample.a;
                float3 emission = _EmissionColor.rgb * _EmissionIntensity;

                // Required by Unity's Forward+ light loop
                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normalize(IN.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                float3 lighting = ComputeSceneLighting(baseColor, inputData);

                float totalLight = 0.0;

                for (int i = 0; i < _FlashlightCount; i++)
                {
                    float sphere = SphereMask(IN.positionWS, i);
                    float beam = FrustumMask(IN.positionWS, i);

                    float mask = max(sphere, beam);

                    float facing = lerp(0.35, 1.0,
                        SurfaceFacingMask(normalize(IN.normalWS), IN.positionWS, i));

                    totalLight += mask * facing * _FlashIntensity[i];
                }

                totalLight = saturate(totalLight);

                //float ambient = 0.05;
                //float3 darkColor = lighting * ambient;

                // Flashlight reveals the scene lighting
                //float3 finalColor = lerp(darkColor, lighting, totalLight);

                float ambient = 0.2;

				// scene lights stay visible in darkness
				float3 darkColor = lighting * ambient;

				// flashlight is independent from scene lights
				float3 litColor = baseColor;
                float3 finalColor = lerp(darkColor, litColor, totalLight);

				// subtle warm tint from flashlight
				finalColor += _FlashColor.rgb * totalLight * 0.05;

				// the emission
				finalColor += emission;
				
                return half4(finalColor, alpha);
            }

            ENDHLSL
        }
    }
}