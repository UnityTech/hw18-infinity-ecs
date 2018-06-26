Shader "Custom/TerrainChunk" {
	Properties {
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_HeightmapScale("Heightmap Scale", Float) = 100
		_MainTex0 ("Albedo 1 (RGB)", 2D) = "white" {}
		_NormalTex0 ("Normal 1", 2D) = "white" {}
 	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows vertex:vert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		#include "noiseSimplex.cginc"

		sampler2D _Heightmap;
		sampler2D _Normalmap;
		float4 _Normalmap_ST;

		sampler2D _MainTex0;
		float4 _MainTex0_ST;
		sampler2D _Normal0;
		float4 _Normal0_ST;

		float4 _Sector;

		struct Input {
			float2 uv_MainTex0_ST;
		};

		half _Glossiness;
		half _Metallic;
		float _HeightmapScale;

		void vert(inout appdata_full v) {
			float height = tex2Dlod(_Heightmap, float4(v.texcoord.xy, 0, 0));
			v.vertex.y += height * _HeightmapScale;
			float3 normal = tex2Dlod(_Normalmap, float4(v.texcoord.xy, 0, 0)).xyz;
			normal = normal * 2 - 1;
			v.normal = normal;
		}

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void material0Update(float2 position, inout float3 albedo, inout float3 normal0)
		{
			float albedoNoise = snoise(position);
			albedoNoise = albedoNoise * 0.3 + 0.7;
			albedo *= albedoNoise;
		}

		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			float2 uv = IN.uv_MainTex0_ST;
			float2 position = _Sector.xy + uv;

			float3 normal = tex2D(_Normalmap, uv * _Normalmap_ST.xy + _Normalmap_ST.zw).xyz;
			float3 albedo0 = tex2D(_MainTex0, uv * _MainTex0_ST.xy + _MainTex0_ST.zw).xyz;
			float3 normal0 = tex2D(_Normal0, uv * _Normal0_ST.xy + _Normal0_ST.zw).xyz;
			normal0 = normalize(float3(normal0.xy + normal.xy, normal.z));
			material0Update(position, albedo0, normal0);

			// Albedo comes from a texture tinted by color
			o.Albedo = albedo0.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = 1;
			//o.Normal = normal * 2 - 1;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
