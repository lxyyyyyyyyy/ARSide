﻿Shader "Custom/PointCloudGeom_1" {
	Properties {
		[NoScaleOffset]_MainTex ("Texture", 2D) = "white" {}
		_PointSize("Point Size", Float) = 4.0
		_Color ("PointCloud Color", Color) = (1, 1, 1, 1)
		[Toggle(USE_DISTANCE)]_UseDistance ("Scale by distance?", float) = 0
	}

	SubShader
	{
		Cull Off
		Pass 
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma shader_feature USE_DISTANCE
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			float _PointSize;
			fixed4 _Color;

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;

			struct g2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			[maxvertexcount(4)]
			void geom(point v2f i[1], inout TriangleStream<g2f> triStream)
			{
				g2f o;
				float4 v = i[0].vertex;
				v.y = -v.y;

				// TODO: interpolate uvs on quad
				float2 uv = i[0].uv;
				float2 p = _PointSize * 0.001;
				p.y *= _ScreenParams.x / _ScreenParams.y;
				
				o.vertex = UnityObjectToClipPos(v);
				#ifdef USE_DISTANCE
				o.vertex += float4(-p.x, p.y, 0, 0);
				#else
				o.vertex += float4(-p.x, p.y, 0, 0) * o.vertex.w;
				#endif
				o.uv = uv;
				triStream.Append(o);

				o.vertex = UnityObjectToClipPos(v);
				#ifdef USE_DISTANCE
				o.vertex += float4(-p.x, -p.y, 0, 0);
				#else
				o.vertex += float4(-p.x, -p.y, 0, 0) * o.vertex.w;
				#endif
				o.uv = uv;
				triStream.Append(o);

				o.vertex = UnityObjectToClipPos(v);
				#ifdef USE_DISTANCE
				o.vertex += float4(p.x, p.y, 0, 0);
				#else
				o.vertex += float4(p.x, p.y, 0, 0) * o.vertex.w;
				#endif
				o.uv = uv;
				triStream.Append(o);

				o.vertex = UnityObjectToClipPos(v);
				#ifdef USE_DISTANCE
				o.vertex += float4(p.x, -p.y, 0, 0);
				#else
				o.vertex += float4(p.x, -p.y, 0, 0) * o.vertex.w;
				#endif
				o.uv = uv;
				triStream.Append(o);

			}

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = v.vertex;
				o.uv = v.uv;
				return o;
			}

			fixed4 frag (g2f i) : SV_Target
			{
				return tex2D(_MainTex, i.uv) * _Color;
				
			}
			ENDCG
		}
	}
}
