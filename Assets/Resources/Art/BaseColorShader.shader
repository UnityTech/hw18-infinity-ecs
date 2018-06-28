Shader "CustomRenderTexture/BaseColor Generator"
{
	Properties
	{
		_HeightMap("HeightMap", 2D) = "white" {}
	}

	SubShader
	{
		Lighting Off
		Blend One Zero

		Pass
		{
			CGPROGRAM
			#include "UnityCustomRenderTexture.cginc"
			#include "common.cginc"
			#pragma vertex CustomRenderTextureVertexShader
			#pragma fragment frag
			#pragma target 4.5

			CBUFFER_START(BaseColorParameters)
				float4 _Sector;
			CBUFFER_END

			Texture2D _HeightMap;

			float4 frag(v2f_customrendertexture IN) : COLOR
			{
				float height = _HeightMap.Load(int3(IN.globalTexcoord.xy * float2(_CustomRenderTextureWidth, _CustomRenderTextureHeight), 0));
				float3 color = terrainBaseColor(IN.globalTexcoord.xy + _Sector.xy, height);
				return float4(color, 1.0);
			}
			ENDCG
		}
	}
}