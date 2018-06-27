Shader "Custom/TerrainChunk" 
{
	Properties 
	{
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_HeightmapScale("Heightmap Scale", Float) = 100

		_MainTex0 ("Albedo 1 (RGB)", 2D) = "white" {}
		_Normal0 ("Normal 1", 2D) = "white" {}
		_Detail0("Detail 1 (RGB)", 2D) = "white" {}

		_MainTex1("Albedo 2 (RGB)", 2D) = "white" {}
		_Normal1("Normal 2", 2D) = "white" {}
		_Detail1("Detail 2 (RGB)", 2D) = "white" {}

		_MainTex2("Albedo 3 (RGB)", 2D) = "white" {}
		_Normal2("Normal 3", 2D) = "white" {}
		_Detail2("Detail 3 (RGB)", 2D) = "white" {}

		_MainTex3("Albedo 4 (RGB)", 2D) = "white" {}
		_Normal3("Normal 4", 2D) = "white" {}
		_Detail3("Detail 4 (RGB)", 2D) = "white" {}

		_Splatmap("Splat Map", 2D) = "white" {}
 	}
	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows vertex:vert
		#pragma target 4.6
		#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

		#include "noiseSimplex.cginc"

		#define MERGE_NAME(A, B) A ## B

		CBUFFER_START(MaterialLayers)
			float4 _Normalmap_ST;
			float4 _MainTex0_ST;
			float4 _Detail0_ST;
			float4 _Normal0_ST;
			float4 _MainTex1_ST;
			float4 _Detail1_ST;
			float4 _Normal1_ST;
			float4 _MainTex2_ST;
			float4 _Detail2_ST;
			float4 _Normal2_ST;
			float4 _MainTex3_ST;
			float4 _Detail3_ST;
			float4 _Normal3_ST;

			half _Glossiness;
			half _Metallic;

			float4 _Sector;
			float _HeightmapScale;
		CBUFFER_END

		sampler2D _Heightmap;
		sampler2D _Splatmap;
		sampler2D _Normalmap;

		sampler2D _MainTex0;
		sampler2D _Detail0;
		sampler2D _Normal0;
		sampler2D _MainTex1;
		sampler2D _Detail1;
		sampler2D _Normal1;
		sampler2D _MainTex2;
		sampler2D _Detail2;
		sampler2D _Normal2;
		sampler2D _MainTex3;
		sampler2D _Detail3;
		sampler2D _Normal3;

		struct Input {
			float2 uv_Splatmap;
		};

		void vert(inout appdata_full v) {
			float height = tex2Dlod(_Heightmap, float4(v.texcoord.xy, 0, 0));
			v.vertex.y += height * _HeightmapScale;
			float3 normal = tex2Dlod(_Normalmap, float4(v.texcoord.xy, 0, 0)).xyz;
			normal = normal * 2 - 1;
			v.normal = normal;
		}

		UNITY_INSTANCING_BUFFER_START(Props)
		UNITY_INSTANCING_BUFFER_END(Props)

		void materialLayer(
			float2 position, 
			float height,
			float splat,
			float3 normal,
			float3 layerAlbedo,
			float3 layerDetail,
			float3 layerNormalmap,
			float3 layerNormal,
			inout float3 albedo
		)
		{
			albedo += (layerAlbedo * 0.5 + layerDetail * 0.5) * splat;

			//float albedoNoise = snoise(position);
			//albedoNoise = albedoNoise * 0.3 + 0.7;
			//albedo *= albedoNoise;
		}

		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			float2 uv = IN.uv_Splatmap;
			float2 position = _Sector.xy + uv;

			float height = tex2D(_Heightmap, uv * _Normalmap_ST.xy + _Normalmap_ST.zw).x;
			float3 normalmap = tex2D(_Normalmap, uv * _Normalmap_ST.xy + _Normalmap_ST.zw).xyz;
			float3 normal = normalmap * 2 - 1;
			float4 splat = tex2D(_Splatmap, uv * _Normalmap_ST.xy + _Normalmap_ST.zw);

			// Temporary
			//splat = normalize(
			//	float4(
			//		abs(snoise(position * 2) * 2 - 1),
			//		abs(snoise(position * 3)),
			//		abs(snoise(position * 4)),
			//		abs(snoise(position * 5))
			//	)
			//);
			splat = float4(1, 0, 0, 0);
			// End

#define DECODE_LAYER(index)\
			float3 MERGE_NAME(albedo, index) = tex2D(MERGE_NAME(_MainTex, index), uv * MERGE_NAME(MERGE_NAME(_MainTex, index), _ST.xy) + MERGE_NAME(MERGE_NAME(_MainTex, index), _ST.zw)).xyz;\
			float3 MERGE_NAME(detail, index) = tex2D(MERGE_NAME(_Detail, index), uv * MERGE_NAME(MERGE_NAME(_Detail, index), _ST.xy) + MERGE_NAME(MERGE_NAME(_Detail, index), _ST.zw)).xyz;\
			float3 MERGE_NAME(normalmap, index) = tex2D(MERGE_NAME(_Normal, index), uv * MERGE_NAME(MERGE_NAME(_Normal, index), _ST.xy) + MERGE_NAME(MERGE_NAME(_Normal, index), _ST.zw)).xyz;\
			float3 MERGE_NAME(normal, index) = MERGE_NAME(normalmap, index) * 2 - 1;

			DECODE_LAYER(0)
			DECODE_LAYER(1)
			DECODE_LAYER(2)
			DECODE_LAYER(3)
#undef DECODE_LAYER

			float3 albedo = float3(0, 0, 0);
			materialLayer(position, height, splat[0], normal, albedo0, detail0, normalmap0, normal0, albedo);
			materialLayer(position, height, splat[1], normal, albedo1, detail1, normalmap1, normal1, albedo);
			materialLayer(position, height, splat[2], normal, albedo2, detail2, normalmap2, normal2, albedo);
			materialLayer(position, height, splat[3], normal, albedo3, detail3, normalmap3, normal3, albedo);

			normalmap = normalize(float3(
				normalmap0.xy * splat.x
				+ normalmap1.xy * splat.y
				+ normalmap2.xy * splat.z
				+ normalmap3.xy * splat.w
				+ normalmap.xy,
				normalmap.z
				));
			normal = normalmap * 2 - 1;

			// Albedo comes from a texture tinted by color
			o.Albedo = albedo.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = 1;
			o.Normal = normal;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
