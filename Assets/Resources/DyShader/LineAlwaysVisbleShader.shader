Shader "Custom/LineAlwaysVisbleShader"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_ZTestAddValue("ZTest Add Value",float) = 0
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4
	}

		SubShader
		{
			Tags { "RenderType" = "Opaque" }
			LOD 200
			ZTest[_ZTest]

			Pass
			{

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct vertexOutput
			{
				float4 pos : SV_POSITION;
				float4 posInObjectCoords : TEXCOORD0;
			};

			float _ZTestAddValue;

			vertexOutput vert(appdata_full input)
			{
				vertexOutput output;

				float4 clipPos = UnityObjectToClipPos(input.vertex);
				//�������ʱ��������һ��ֵ��			
				#if UNITY_REVERSED_Z
					clipPos.z += _ZTestAddValue;
					clipPos.z = min(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);//�޶��ڷ�Χ��
				#else
					clipPos.z -= _ZTestAddVal;
					clipPos.z = max(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);//�޶��ڷ�Χ��
				#endif
				output.pos = clipPos;

				output.posInObjectCoords = input.texcoord;
				return output;
			}

			sampler2D _MainTex;
			fixed4 _Color;

			float4 frag(vertexOutput input) : COLOR
			{
				float4 col = _Color;
				return col;
			}

				ENDCG
			}
		}
			FallBack "Diffuse"
}