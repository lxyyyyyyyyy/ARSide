Shader "Custom/GetDepthShader"
{
	SubShader{

		Pass{
			CGPROGRAM
			#pragma vertex vert  
			#pragma fragment frag  
			#include "UnityCG.cginc"  

			sampler2D _CameraDepthTexture;  // 内置深度图
			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 scrPos : TEXCOORD1;
			};

			v2f vert(appdata_base v)    // appdata_base内置，有Position,Normal,Texcoord这些
			{
				v2f f;
				f.pos = UnityObjectToClipPos(v.vertex);
				f.scrPos = ComputeScreenPos(f.pos);
				return f;
			}

			half4 frag(v2f f) : COLOR
			{
                // 不用linear
				// float depthValue = tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(f.scrPos)).r;
                // return half4(depthValue, depthValue, depthValue, 1);       // 所以深度图里存的就是MVP×过以后 又用1减过的值，越近越大
                
                float depthValue = Linear01Depth(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(f.scrPos)).r); // linear01就是世界的z/far
			    return half4(depthValue, depthValue, depthValue, 1);
			}
			ENDCG
		}
	}
		FallBack "Diffuse"
}