Shader "CustomRenderTexture/NormalMap Generator"
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
			#pragma vertex CustomRenderTextureVertexShader
			#pragma fragment frag
			#pragma target 4.5

			CBUFFER_START(BaseColorParameters)
				float4 _Sector;
			CBUFFER_END

			Texture2D _HeightMap;

			float4 frag(v2f_customrendertexture IN) : COLOR
			{
				const int3 d = int3(-1, 0, 1);
				int2 p = int2(IN.globalTexcoord.xy * float2(_CustomRenderTextureWidth, _CustomRenderTextureHeight));
				float dx = (_HeightMap.Load(int3(p + d.zy, 0)).r - _HeightMap.Load(int3(p + d.xy, 0)).r) * 0.5;
				float dy = (_HeightMap.Load(int3(p + d.yz, 0)).r - _HeightMap.Load(int3(p + d.yx, 0)).r) * 0.5;
				return float4(dx, dy, 1.0, 1.0);
			}
			ENDCG
		}
	}
}