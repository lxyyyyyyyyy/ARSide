Shader "Custom/NormalShader"
{
	Properties
	{
		_LineLength("LineLength",float) = 0.3
		_LineColor("LineColor",COLOR) = (1,0,0,1)
	}
		SubShader
	{
		Pass
		{
			Tags { "RenderType" = "Opaque" }
			LOD 200

			CGPROGRAM
				#pragma target 5.0
				#pragma vertex VS_Main
				#pragma fragment FS_Main
				#pragma geometry GS_Main
				#include "UnityCG.cginc" 

				float _LineLength;
				fixed4 _LineColor;

				struct GS_INPUT
				{
					float4    pos       : POSITION;
					float3    normal    : NORMAL;
					fixed4    color		: COLOR;
				};

				struct FS_INPUT
				{
					float4    pos       : POSITION;
					fixed4    color		: COLOR;
				};

				GS_INPUT VS_Main(appdata_full v)
				{
					GS_INPUT output = (GS_INPUT)0;
					output.pos = mul(unity_ObjectToWorld, v.vertex);
					output.normal = v.normal;
					output.color = v.color;
					float3 worldNormal = UnityObjectToWorldNormal(v.normal);
					output.normal = normalize(worldNormal);
					return output;
				}

				[maxvertexcount(4)]
				void GS_Main(point GS_INPUT p[1], inout LineStream<FS_INPUT> triStream)
				{
					FS_INPUT pIn;
					pIn.pos = mul(UNITY_MATRIX_VP, p[0].pos);
					pIn.color = p[0].color;
					triStream.Append(pIn);

					FS_INPUT pIn1;
					float4 pos = p[0].pos + float4(p[0].normal,0) * _LineLength;
					pIn1.pos = mul(UNITY_MATRIX_VP, pos);
					pIn1.color = p[0].color;
					triStream.Append(pIn1);
				}

				fixed4 FS_Main(FS_INPUT input) : COLOR
				{
					return input.color;
				}
			ENDCG
		}
	}
}