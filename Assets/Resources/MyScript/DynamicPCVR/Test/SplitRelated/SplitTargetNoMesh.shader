Shader "Custom/SplitTargetNoMesh" {
	Properties{
		_PointSize("Point Size", Float) = 4.0
		[Toggle(USE_DISTANCE)]_UseDistance("Scale by distance?", float) = 0
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
					fixed4 color : COLOR;
				};

				struct v2f
				{
					float4 vertex : SV_POSITION;
					fixed4 color : COLOR;
				};

				float _PointSize;

				struct g2f
				{
					float4 vertex : SV_POSITION;
					fixed4 color : COLOR;
				};

				[maxvertexcount(4)]
				void geom(point v2f i[1], inout TriangleStream<g2f> triStream)
				{
					g2f o;
					float4 v = i[0].vertex;

					float2 p = _PointSize * 0.001;
					p.y *= _ScreenParams.x / _ScreenParams.y;

					o.vertex = UnityObjectToClipPos(v);
					#ifdef USE_DISTANCE
					o.vertex += float4(-p.x, p.y, 0, 0);
					#else
					o.vertex += float4(-p.x, p.y, 0, 0) * o.vertex.w;
					#endif
					o.color = i[0].color;
					triStream.Append(o);

					o.vertex = UnityObjectToClipPos(v);
					#ifdef USE_DISTANCE
					o.vertex += float4(-p.x, -p.y, 0, 0);
					#else
					o.vertex += float4(-p.x, -p.y, 0, 0) * o.vertex.w;
					#endif
					o.color = i[0].color;
					triStream.Append(o);

					o.vertex = UnityObjectToClipPos(v);
					#ifdef USE_DISTANCE
					o.vertex += float4(p.x, p.y, 0, 0);
					#else
					o.vertex += float4(p.x, p.y, 0, 0) * o.vertex.w;
					#endif
					o.color = i[0].color;
					triStream.Append(o);

					o.vertex = UnityObjectToClipPos(v);
					#ifdef USE_DISTANCE
					o.vertex += float4(p.x, -p.y, 0, 0);
					#else
					o.vertex += float4(p.x, -p.y, 0, 0) * o.vertex.w;
					#endif
					o.color = i[0].color;
					triStream.Append(o);

				}

				v2f vert(appdata v)
				{
					v2f o;
					o.vertex = v.vertex;
					o.color = v.color;
					return o;
				}

				fixed4 frag(g2f i) : SV_Target
				{
					return i.color;

				}
				ENDCG
			}
		}
}
