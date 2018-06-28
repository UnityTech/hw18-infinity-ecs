Shader "CustomRenderTexture/HeightMap Generator"
{
	Properties
	{
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

			float4 frag(v2f_customrendertexture IN) : COLOR
			{
				float height = terrainHeight(IN.globalTexcoord.xy + _Sector.xy);
				return float4(height, height, height, 1.0);
			}
			ENDCG
		}
	}
}