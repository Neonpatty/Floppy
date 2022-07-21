Shader "Color Studio/Palette"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		_Shades("Shades", Float) = 8
		_CursorPos("Cursor Pos", Vector) = (-1,0,0)
		_Aspect("Aspect", Float) = 1
		_Saturation("Saturation", Float) = 1.0
		_MinBrightness("Min Brightness", Float) = 0
		_MaxBrightness("Max Brightness", Float) = 1
		_ColorCount("Color Count", Int) = 1
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#define DOT_WIDTH 0.015

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
                float2 clipUV : TEXCOORD1;
			};

            
            sampler2D _GUIClipTexture;
            uniform float4x4 unity_GUIClipTextureMatrix;

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Color;
			float _Shades;
			float4 _CursorPos;
			float4 _Colors[256];
			int _ColorCount;
			float _Aspect;
			float _Saturation, _MinBrightness, _MaxBrightness;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
                float3 eyePos = UnityObjectToViewPos(v.vertex);
                o.clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0));
				return o;
			}
			
			float4 frag (v2f i) : SV_Target {
				float fi = i.uv.x * _ColorCount;
				int ci = (int)fi;
				float4 color = _Colors[ci];
				float fs = i.uv.y * _Shades;
				float cs = floor(fs);
				float t = (cs + 0.5) / _Shades;
				t = _MinBrightness + (_MaxBrightness - _MinBrightness) * t;
				float C = (1 - abs(2 * t - 1)) * _Saturation;
    			color.rgb = (color.rgb - 0.5) * C + t;

				// cursor
            
    				float2 sdd = i.uv - _CursorPos.xy;
				sdd.y /= _Aspect;
				float d = sqrt(dot(sdd, sdd));
				color.rgb = lerp(color.rgb, 1.0 - color.rgb, saturate(0.0005 / abs(d - DOT_WIDTH)));
                color.a *= tex2D(_GUIClipTexture, i.clipUV).a;
				return color;
			}

			ENDCG
		}
	}
}

