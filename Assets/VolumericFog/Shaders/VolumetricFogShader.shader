Shader "Unlit/VolumetricFogShader"
{
    Properties
    {
        _VolumeScatter("VolumeScatter", 3D) = "white" {}

    // 光散射开销太大，封印！
    /*_colA("colA", Color) = (1,1,1,1)
    _colB("colB", Color) = (1,1,1,1)
    _colorOffset1("colorOffset1",float) = 0
    _colorOffset2("colorOffset2",float) = 0
    _lightAbsorptionTowardSun("lightAbsorptionTowardSun",float) = 0
    _darknessThreshold("darknessThreshold",float) = 0

    _FogNoiseTilling("FogNoiseTilling",Vector) = (1,1,1,1)
    _FogNoiseSpd("_FogNoiseSpd",Vector) = (0,0,0,0)*/
    _FogTilling("FogTilling",Vector) = (1,1,1,1)
    _FogSpd("FogSpd",Vector) = (0,0,0,0)

    _FogDensity("_FogDensity",float) = 10
    _Contrast("Contrast",float) = 5
    _phaseParams("phaseParams",Vector) = (0.75,0.25,0.29,0.6)


    }
        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Blend SrcColor OneMinusSrcColor
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define MAIN_LIGHT_CALCULATE_SHADOWS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
            #define STEP_TIME 8
            //#define CUBE_SIZE 0.1
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos:TEXCOORD1;
                float4 screenPos :TEXCOORD2;
            };

            TEXTURE2D_X_FLOAT(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);
            //TEXTURE2D(_CameraOpaqueTexture); SAMPLER(sampler_CameraOpaqueTexture);
            TEXTURE3D(_VolumeScatter); SAMPLER(sampler_VolumeScatter);
            /*float _lightAbsorptionTowardSun;
            float4 _colA;
            float4 _colB;
            float _colorOffset1;
            float _colorOffset2;
            float transmittance;
            float _darknessThreshold;
            half3 _FogNoiseTilling;
            half3 _FogNoiseSpd;*/
            half3 _FogTilling;
            half _FogDensity;
            half _Contrast;
            half3 _FogSpd;
            float3 _boundsMin;
            float3 _boundsMax;
            float4 _phaseParams;
            Matrix _InverseProjectionMatrix;
            Matrix _InverseViewMatrix;
            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = TransformObjectToHClip(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }
            int ihash(int n)
            {
                n = (n << 13) ^ n;
                return (n * (n * n * 15731 + 789221) + 1376312589) & 2147483647;
            }

            float frand(int n)
            {
                return ihash(n) / 2147483647.0;
            }
            float2 cellNoise(int2 p)
            {
                int i = p.y * 256 + p.x;
                return float2(frand(i), frand(i + 57)) - 0.5;//*2.0-1.0;
            }

            //计算世界空间坐标
            float4 GetWorldSpacePosition(float depth, float2 uv)
            {
                // 屏幕空间 --> 视锥空间
                float4 view_vector = mul(_InverseProjectionMatrix, float4(2.0 * uv - 1.0, depth, 1.0));
                view_vector.xyz /= view_vector.w;
                //视锥空间 --> 世界空间
                float4x4 l_matViewInv = _InverseViewMatrix;
                float4 world_vector = mul(l_matViewInv, float4(view_vector.xyz, 1));
                return world_vector;
            }
            float2 rayBoxDst(float3 boundsMin, float3 boundsMax,
                //世界相机位置      反向世界空间光线方向
                float3 rayOrigin, float3 invRaydir)
            {
                float3 t0 = (boundsMin - rayOrigin) * invRaydir;
                float3 t1 = (boundsMax - rayOrigin) * invRaydir;
                float3 tmin = min(t0, t1);
                float3 tmax = max(t0, t1);

                float dstA = max(max(tmin.x, tmin.y), tmin.z); //进入点
                float dstB = min(tmax.x, min(tmax.y, tmax.z)); //出去点

                float dstToBox = max(0, dstA);
                float dstInsideBox = max(0, dstB - dstToBox);
                return float2(dstToBox, dstInsideBox);
            }
            float sampleDensity(float3 rayPos)
            {
                float3 uvw = rayPos * _FogTilling.xyz + _FogSpd.xyz * _Time.x;
                /*float4 shapeNoise = SAMPLE_TEXTURE3D(_VolumeScatter, sampler_VolumeScatter, uvw);
                uvw = rayPos * _FogNoiseTilling.xyz + _FogNoiseSpd.xyz * _Time.x + shapeNoise.xyz;*/
                float4 shapeNoise = SAMPLE_TEXTURE3D(_VolumeScatter, sampler_VolumeScatter, uvw);
                return shapeNoise;
            }

            // 光散射封印
            //float3 lightmarch(float3 position, float dstTravelled)
            //{
            //    float3 dirToLight = _MainLightPosition.xyz;
            //    //灯光方向与边界框求交，超出部分不计算
            //    float dstInsideBox = rayBoxDst(_boundsMin, _boundsMax, position, 1 / dirToLight).y;
            //    float stepSize = dstInsideBox / 8;
            //    float totalDensity = 0;

            //    for (int step = 0; step < 2; step++) //灯光步进次数
            //    {
            //        position += dirToLight * stepSize; //向灯光步进
            //        totalDensity += max(0, sampleDensity(position) * stepSize); //步进的时候采样噪音累计受灯光影响密度
            //    }
            //    float transmittance = exp(-totalDensity * _lightAbsorptionTowardSun);
            //    //将重亮到暗映射为 3段颜色 ,亮->灯光颜色 中->ColorA 暗->ColorB
            //    float3 cloudColor = lerp(_colA, _MainLightColor, saturate(transmittance * _colorOffset1));
            //    cloudColor = lerp(_colB, cloudColor, saturate(pow(transmittance * _colorOffset2, 3)));
            //    return _darknessThreshold + transmittance * (1 - _darknessThreshold) * cloudColor;
            //}
            float hg(float a, float g)
            {
                float g2 = g * g;
                return (1 - g2) / (4 * 3.1415 * pow(1 + g2 - 2 * g * (a), 1.5));
            }
            float phase(float a)
            {
                float blend = 0.5;
                float hgBlend = hg(a, _phaseParams.x) * (1 - blend) + hg(a, -_phaseParams.y) * blend;
                return _phaseParams.z + hgBlend * _phaseParams.w;
            }
            half4 frag(v2f i) : SV_Target
            {
                half2 screenPos = i.screenPos.xy / i.screenPos.w;
                half2 uv = i.uv;

                //rebuild world position according to depth
                float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture,sampler_CameraDepthTexture, screenPos).r;
                depth = Linear01Depth(depth, _ZBufferParams);

                float2 positionNDC = screenPos * 2 - 1;
                float3 farPosNDC = float3(positionNDC.xy,1) * _ProjectionParams.z;
                float4 viewPos = mul(unity_CameraInvProjection,farPosNDC.xyzz);
                viewPos.xyz *= depth;
                float4 worldPos = mul(UNITY_MATRIX_I_V,viewPos);
                float3 rayPos = i.worldPos;

                float3 worldViewDir = worldPos - rayPos;
                float depthEyeLinear = length(worldViewDir);
                worldViewDir = normalize(worldViewDir);


                float2 rayToContainerInfo = rayBoxDst(_boundsMin, _boundsMax, rayPos, (1 / worldViewDir));
                float dstToBox = rayToContainerInfo.x; //相机到容器的距离
                float dstInsideBox = rayToContainerInfo.y; //返回光线是否在容器中
                //相机到物体的距离 - 相机到容器的距离，这里跟 光线是否在容器中 取最小，过滤掉一些无效值
                float dstLimit = min(depthEyeLinear - dstToBox, dstInsideBox);
                //return half4(dstLimit, dstLimit, dstLimit, 1);
                rayPos += worldViewDir * dstToBox;

                //向灯光方向的散射更强一些
                float cosAngle = dot(worldViewDir, _MainLightPosition.xyz);
                float3 phaseVal = phase(cosAngle);

                //half3 sceneColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenPos).rgb;
                /*float cloud = cloudRayMarching(rayPos, worldViewDir, dstLimit);
                float cloud =*/

                float sumDensity = 1;
                float3 lightEnergy = 0;
                float _dstTravelled = 0;
                float stepSize = 1 / _FogDensity;
                float3 entryPoint = rayPos;
                //worldViewDir *= stepSize;//每次步进间隔
                UNITY_LOOP
                for (int j = 0; j < 4; j++)
                {
                    rayPos = entryPoint + (worldViewDir * _dstTravelled);
                    if (_dstTravelled >= dstLimit)
                    {
                        break;
                    }
                    float density = sampleDensity(rayPos);
                    if (density > 0)
                    {
                        // 光散射负荷太大，所以封印
                        //float3 lightTransmittance = lightmarch(rayPos, _dstTravelled);
                        float3 lightTransmittance = 1;

                        lightEnergy += density * stepSize * sumDensity * lightTransmittance * phaseVal;
                        sumDensity *= exp(-density * stepSize);
                        if (sumDensity < 0.01)
                            break;
                    }
                    _dstTravelled += stepSize;
                }

                return  float4(lightEnergy, sumDensity);
            }
            ENDHLSL
        }
    }
}