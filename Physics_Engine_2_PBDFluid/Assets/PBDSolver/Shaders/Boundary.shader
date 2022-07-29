Shader "Instanced/URP/Boundary" {
	Properties{
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}
		SubShader{
			Tags { "RenderType" = "Opaque" }
			LOD 200

			CGPROGRAM
			// Physically based Standard lighting model
			#pragma surface surf Standard addshadow fullforwardshadows
			#pragma multi_compile_instancing
			#pragma instancing_options procedural:setup

			sampler2D _MainTex;
			float BoundarySize;
			float radius;
			struct Input {
				float2 uv_MainTex;
			};

		
			

  //struct SolidParticle
  //  {
  //      float3 position;
  //      float3 velocity;
  //      float3 forces;
  //      float density;
  //      float mass;
  //      float pressure;
       
  //  };



		#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			//StructuredBuffer<SolidParticle> _boundaryParticles;
			StructuredBuffer<float4> _positionsBuffer;
		#endif

			void setup()
			{
			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				float3 pos = _positionsBuffer[unity_InstanceID];
				float size = radius;

				unity_ObjectToWorld._11_21_31_41 = float4(size, 0, 0, 0);
				unity_ObjectToWorld._12_22_32_42 = float4(0, size, 0, 0);
				unity_ObjectToWorld._13_23_33_43 = float4(0, 0, size, 0);
				unity_ObjectToWorld._14_24_34_44 = float4(pos.xyz, 1);
				unity_WorldToObject = unity_ObjectToWorld;
				unity_WorldToObject._14_24_34 *= -1;
				unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
			#endif
			}

			half _Glossiness;
			half _Metallic;

			void surf(Input IN, inout SurfaceOutputStandard o) {
				float4 col = float4(1, 1, 1, 1);
				#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED

				//col = float4(_boundaryParticles[unity_InstanceID].position.x/100 +0.1,_boundaryParticles[unity_InstanceID].position.y / 100 + 0.1,1,1);
				col = float4(1,1,1,1);
				#endif
				
				o.Albedo = col.rgb;
				o.Metallic = _Metallic;
				o.Smoothness = _Glossiness;
				o.Alpha = col.a;

			}
			ENDCG
		}
			FallBack "Diffuse"
}