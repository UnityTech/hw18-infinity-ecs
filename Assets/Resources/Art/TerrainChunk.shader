// Upgrade NOTE: replaced 'defined FOG_COMBINED_WITH_WORLD_POS' with 'defined (FOG_COMBINED_WITH_WORLD_POS)'

Shader "Custom/TerrainChunk" 
{
	Properties 
	{
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_HeightmapScale("Heightmap Scale", Float) = 50
		_Heightmap ("Heightmap", 2D) = "black" {}
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
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGINCLUDE

		#pragma target 4.6
		#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

		#pragma multi_compile_instancing
		#pragma multi_compile_fog
		//#pragma multi_compile_fwdadd_fullshadows // Don't support shadows for now

		#include "HLSLSupport.cginc"
		#define UNITY_INSTANCED_LOD_FADE
		#define UNITY_INSTANCED_SH
		#define UNITY_INSTANCED_LIGHTMAPSTS
		#include "UnityShaderVariables.cginc"
		#include "UnityShaderUtilities.cginc"

		#include "UnityCG.cginc"
		#include "Lighting.cginc"
		#include "UnityPBSLighting.cginc"
		#include "AutoLight.cginc"

		#include "noiseSimplex.cginc"

		#define MERGE_NAME(A, B) A ## B
		#define _ChunkTextureSize float2(255, 255)
		#define LOAD_TEXTURE2D_LOD(tex, coord, lod) tex.Load(int3(coord, lod))

		CBUFFER_START(MaterialLayers)
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

			float _Glossiness;
			float _Metallic;

			float4 _Sector;
			float _HeightmapScale;
		CBUFFER_END

		SamplerState s_point_clamp;
		SamplerState s_linear_wrap;
		SamplerState s_trilinear_repeat;

		Texture2D _Heightmap;
		Texture2D _Splatmap;
		Texture2D _Normalmap;

		Texture2D _MainTex0;
		Texture2D _Detail0;
		Texture2D _Normal0;
		Texture2D _MainTex1;
		Texture2D _Detail1;
		Texture2D _Normal1;
		Texture2D _MainTex2;
		Texture2D _Detail2;
		Texture2D _Normal2;
		Texture2D _MainTex3;
		Texture2D _Detail3;
		Texture2D _Normal3;

		struct Input 
		{
			float2 uv_Splatmap;
		};

		struct v2f_surf {
			UNITY_POSITION(pos);
			float2 texcoord : TEXCOORD0; // _Splatmap
			float4 tSpace0 : TEXCOORD1;
			float4 tSpace1 : TEXCOORD2;
			float4 tSpace2 : TEXCOORD3;
			UNITY_VERTEX_INPUT_INSTANCE_ID
			UNITY_VERTEX_OUTPUT_STEREO
		};

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

		v2f_surf vert_surf(appdata_full v)
		{
			UNITY_SETUP_INSTANCE_ID(v);
			v2f_surf o;
			UNITY_INITIALIZE_OUTPUT(v2f_surf, o);
			UNITY_TRANSFER_INSTANCE_ID(v, o);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

			float height = LOAD_TEXTURE2D_LOD(_Heightmap, _ChunkTextureSize * v.texcoord.xy, 0).r;
			v.vertex.y += height * _HeightmapScale;
			float3 normal = LOAD_TEXTURE2D_LOD(_Normalmap, _ChunkTextureSize * v.texcoord.xy, 0).xyz;
			normal = normal * 2 - 1;
			v.normal = normal;

			o.pos = UnityObjectToClipPos(v.vertex);
			o.texcoord.xy = v.texcoord;
			float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
			float3 worldNormal = UnityObjectToWorldNormal(v.normal);
			fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
			fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
			fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
			o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
			o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
			o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
#ifdef DYNAMICLIGHTMAP_ON
			o.lmap.zw = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif
#ifdef LIGHTMAP_ON
			o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#endif

			// SH/ambient and vertex lights
#ifndef LIGHTMAP_ON
#if UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
			o.sh = 0;
			// Approximated illumination from non-important point lights
#ifdef VERTEXLIGHT_ON
			o.sh += Shade4PointLights(
				unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
				unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
				unity_4LightAtten0, worldPos, worldNormal);
#endif
			o.sh = ShadeSHPerVertex(worldNormal, o.sh);
#endif
#endif // !LIGHTMAP_ON

			UNITY_TRANSFER_LIGHTING(o, v.texcoord1.xy); // pass shadow and, possibly, light cookie coordinates to pixel shader
#ifdef FOG_COMBINED_WITH_TSPACE
			UNITY_TRANSFER_FOG_COMBINED_WITH_TSPACE(o, o.pos); // pass fog coordinates to pixel shader
#elif defined (FOG_COMBINED_WITH_WORLD_POS)
			UNITY_TRANSFER_FOG_COMBINED_WITH_WORLD_POS(o, o.pos); // pass fog coordinates to pixel shader
#else
			UNITY_TRANSFER_FOG(o, o.pos); // pass fog coordinates to pixel shader
#endif
			return o;
		}

		float4 frag_surf(v2f_surf IN) : SV_Target
		{
			UNITY_SETUP_INSTANCE_ID(IN);
			// prepare and unpack data
			Input surfIN;
	#ifdef FOG_COMBINED_WITH_TSPACE
			UNITY_EXTRACT_FOG_FROM_TSPACE(IN);
	#elif defined (FOG_COMBINED_WITH_WORLD_POS)
			UNITY_EXTRACT_FOG_FROM_WORLD_POS(IN);
	#else
			UNITY_EXTRACT_FOG(IN);
	#endif
	#ifdef FOG_COMBINED_WITH_TSPACE
			UNITY_RECONSTRUCT_TBN(IN);
	#else
			UNITY_EXTRACT_TBN(IN);
	#endif
			UNITY_INITIALIZE_OUTPUT(Input,surfIN);
			surfIN.uv_Splatmap.x = 1.0;
			surfIN.uv_Splatmap = IN.texcoord.xy;
			float3 worldPos = float3(IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w);
	#ifndef USING_DIRECTIONAL_LIGHT
			fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
	#else
			fixed3 lightDir = _WorldSpaceLightPos0.xyz;
	#endif
			float3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
	#ifdef UNITY_COMPILER_HLSL
			SurfaceOutputStandard o = (SurfaceOutputStandard)0;
	#else
			SurfaceOutputStandard o;
	#endif


			// Surface
			float2 uv = IN.texcoord;
			float2 position = _Sector.xy + uv;

			float height = LOAD_TEXTURE2D_LOD(_Heightmap, _ChunkTextureSize * IN.texcoord, 0).r;
			float3 normalmap = LOAD_TEXTURE2D_LOD(_Normalmap, _ChunkTextureSize * IN.texcoord, 0);
			float3 normal = normalmap * 2 - 1;
			float4 splat = LOAD_TEXTURE2D_LOD(_Splatmap, _ChunkTextureSize * IN.texcoord, 0);

			if (length(splat) < 0.5)
				splat = abs(float4(
					snoise(position * 2),
					snoise(position * 3),
					snoise(position * 4),
					snoise(position * 5)
				));

#define DECODE_LAYER(index)\
			float3 MERGE_NAME(albedo, index) = MERGE_NAME(_MainTex, index).Sample(s_trilinear_repeat, uv * MERGE_NAME(MERGE_NAME(_MainTex, index), _ST.xy) + MERGE_NAME(MERGE_NAME(_MainTex, index), _ST.zw)).xyz;\
			float3 MERGE_NAME(detail, index) = MERGE_NAME(_Detail, index).Sample(s_trilinear_repeat,uv * MERGE_NAME(MERGE_NAME(_Detail, index), _ST.xy) + MERGE_NAME(MERGE_NAME(_Detail, index), _ST.zw)).xyz;\
			float3 MERGE_NAME(normalmap, index) = MERGE_NAME(_Normal, index).Sample(s_trilinear_repeat,uv * MERGE_NAME(MERGE_NAME(_Normal, index), _ST.xy) + MERGE_NAME(MERGE_NAME(_Normal, index), _ST.zw)).xyz;\
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
			//o.Normal = float3(0, 1, 0);
			o.Normal = normal; // default float3(0, 0, 1)
			o.Emission = 0.0;
			o.Occlusion = 1.0;
			// End surface

			// For debug
			//return float4(normal * 0.5 + 0.5, 1);






			// compute lighting & shadowing factor
			UNITY_LIGHT_ATTENUATION(atten, IN, worldPos)
				fixed4 c = 0;
			float3 worldN;
			worldN.x = dot(_unity_tbn_0, o.Normal);
			worldN.y = dot(_unity_tbn_1, o.Normal);
			worldN.z = dot(_unity_tbn_2, o.Normal);
			worldN = normalize(worldN);
			o.Normal = worldN;

			// Setup lighting environment
			UnityGI gi;
			UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
			gi.indirect.diffuse = 0;
			gi.indirect.specular = 0;
			gi.light.color = _LightColor0.rgb;
			gi.light.dir = lightDir;
			// Call GI (lightmaps/SH/reflections) lighting function
			UnityGIInput giInput;
			UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
			giInput.light = gi.light;
			giInput.worldPos = worldPos;
			giInput.worldViewDir = worldViewDir;
			giInput.atten = atten;
	#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
			giInput.lightmapUV = IN.lmap;
	#else
			giInput.lightmapUV = 0.0;
	#endif
	#if UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
			giInput.ambient = IN.sh;
	#else
			giInput.ambient.rgb = 0.0;
	#endif
			giInput.probeHDR[0] = unity_SpecCube0_HDR;
			giInput.probeHDR[1] = unity_SpecCube1_HDR;
	#if defined(UNITY_SPECCUBE_BLENDING) || defined(UNITY_SPECCUBE_BOX_PROJECTION)
			giInput.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
	#endif
	#ifdef UNITY_SPECCUBE_BOX_PROJECTION
			giInput.boxMax[0] = unity_SpecCube0_BoxMax;
			giInput.probePosition[0] = unity_SpecCube0_ProbePosition;
			giInput.boxMax[1] = unity_SpecCube1_BoxMax;
			giInput.boxMin[1] = unity_SpecCube1_BoxMin;
			giInput.probePosition[1] = unity_SpecCube1_ProbePosition;
	#endif
			LightingStandard_GI(o, giInput, gi);

			// realtime lighting: call lighting function
			c += LightingStandard(o, worldViewDir, gi);
			UNITY_APPLY_FOG(_unity_fogCoord, c); // apply fog
			UNITY_OPAQUE_ALPHA(c.a);
			return c;
		}

		UNITY_INSTANCING_BUFFER_START(Props)
		UNITY_INSTANCING_BUFFER_END(Props)

		ENDCG

		Pass
		{
			Name "FORWARD"
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
			
			#pragma vertex vert_surf
			#pragma fragment frag_surf

			ENDCG
		}
	}
	FallBack "Diffuse"
}
