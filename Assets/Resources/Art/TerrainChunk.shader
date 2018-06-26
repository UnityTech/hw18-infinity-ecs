Shader "Custom/TerrainChunk" {
	Properties {
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_HeightmapScale("Heightmap Scale", Float) = 100
		_Heightmap ("Heightmap", 2D) = "black" {}
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

		sampler2D _MainTex;
		sampler2D _Heightmap;

		sampler2D _MainTex0;
		float4 _MainTex0_ST;
		sampler2D _Normal0;
		float4 _Normal0_ST;

		float4 _Sector;

		struct Input {
			float2 uv_MainTex;
		};

		half _Glossiness;
		half _Metallic;
		float _HeightmapScale;

		void vert(inout appdata_full v) {
			float height = tex2Dlod(_Heightmap, float4(v.texcoord.xy, 0, 0));
			v.vertex.y += height * _HeightmapScale;
		}

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void material0Update(float2 uv, inout float3 albedo)
		{
			float albedoNoise = snoise(uv * 3);
			albedoNoise = albedoNoise * 0.3 + 0.4;
			albedo *= albedoNoise;
		}

		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			float2 uv = IN.uv_MainTex;
			float2 position = _Sector + uv;

			float3 albedo0 = tex2D(_MainTex0, IN.uv_MainTex * _MainTex0_ST.xy + _MainTex0_ST.zw).xyz;
			material0Update(position, albedo0);

			// Albedo comes from a texture tinted by color
			o.Albedo = albedo0.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = 1;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
