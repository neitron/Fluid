// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "CS/GpuInstancing" 
{

	Properties 
	{
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}

	SubShader 
	{
		Tags 
		{ 
			"RenderType" = "Opaque" 
			"ForceNoShadowCasting" = "True"
		}

		LOD 200

		CGPROGRAM
		
			#include "Fluid.cginc"

			// Physically based Standard lighting model, and enable shadows on all light types
			#pragma surface surf NoLighting vertex:vert noshadow noforwardadd
			#pragma multi_compile_instancing
			#pragma instancing_options procedural:setup

			// Use shader model 3.0 target, to get nicer looking lighting
			#pragma target 3.5

			#if SHADER_TARGET >= 35 && (defined(SHADER_API_D3D11) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_XBOXONE) || defined(SHADER_API_PSSL) || defined(SHADER_API_SWITCH) || defined(SHADER_API_VULKAN) || (defined(SHADER_API_METAL) && defined(UNITY_COMPILER_HLSLCC)))
				#define SUPPORT_STRUCTUREDBUFFER
			#endif

			#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) && defined(SUPPORT_STRUCTUREDBUFFER)
				#define ENABLE_INSTANCING
			#endif

			fixed4 _Color;
			half _Metallic;
			half _Glossiness;
			
			float4x4 _LocalToWorld;
			float4x4 _WorldToLocal;

			fixed4x4 _Scale;


			struct Input 
			{
				float2 uv_MainTex;
				int hash;
				fixed4 color;
			};


			#if defined(ENABLE_INSTANCING)

				StructuredBuffer<Particle> _ParticlesBuffer;

			#endif


			void vert(inout appdata_full  v, out Input o)
			{
				UNITY_INITIALIZE_OUTPUT(Input, o);
				#if defined(ENABLE_INSTANCING)

					uint id = unity_InstanceID;

					v.vertex = mul(_Scale, v.vertex);
					v.vertex.xyz += _ParticlesBuffer[id].position;
					o.hash = _ParticlesBuffer[id].hash;

					o.color = _Color;
					o.color.r = 128.0f / ((float)id + 1); //o.hash / 2147483647.0f;
					o.color.b = -o.color.r;


				#endif
			}


			void setup()
			{
				unity_ObjectToWorld = _LocalToWorld;
				unity_WorldToObject = _WorldToLocal;
			}


			fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten)
			{
				fixed4 c;
				c.rgb = s.Albedo;
				c.a = s.Alpha;
				return c;
			}


			void surf (Input IN, inout SurfaceOutput o)
			{
				float c = IN.hash / 2147483647.0f;
				o.Albedo = IN.color;
			}
		


		ENDCG
	}
}
