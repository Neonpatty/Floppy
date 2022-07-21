Shader "Color Studio/ColorWheel"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_CenterWidth ("Center Width", Float) = 0.2
		_Saturation ("Saturation", Float) = 1.0
        _MinBrightness("Min Brightness", Float) = 0
        _MaxBrightness("Max Brightness", Float) = 1
		_Key0Pos ("Primary", Vector) = (0,0,0)
		_Key0Hue ("Selected Hue", Float) = 0
		_Key1Pos("Complementary Pos", Vector) = (0,0,0)
		_Key1Hue("Complementary Hue", Float) = 0
		_Key2Pos("Complementary Pos 2", Vector) = (0,0,0)
		_Key2Hue("Complementary Hue 2", Float) = 0
		_Key3Pos("Complementary Pos 3", Vector) = (0,0,0)
		_Key3Hue("Complementary Hue 3", Float) = 0
		_KeyColorsCount("Custom Colors Count", Int) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ CW_CUSTOM CW_COMPLEMENTARY CW_SPLIT_COMPLEMENTARY CW_ANALOGOUS CW_TETRADIC
			#include "UnityCG.cginc"

			#define PI 3.1415927
			#define PI2 (3.1415927 * 2.0)
			#define DOT_WIDTH 0.02

			#if CW_TETRADIC
				#define MAIN_COLORS 4
			#elif CW_SPLIT_COMPLEMENTARY || CW_ANALOGOUS 
				#define MAIN_COLORS 3
			#elif CW_COMPLEMENTARY
				#define MAIN_COLORS 2
			#else
				#define MAIN_COLORS 1
			#endif


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
			float _Saturation, _MinBrightness, _MaxBrightness;
			float3 _Cursor;
			float  _CenterWidth;
			float2 _Key0Pos, _Key0PosPrev;
			float  _Key0Hue, _Key0HuePrev;
			float2 _Key1Pos, _Key1PosPrev;
			float  _Key1Hue, _Key1HuePrev;
			float2 _Key2Pos, _Key2PosPrev;
			float  _Key2Hue, _Key2HuePrev;
			float2 _Key3Pos, _Key3PosPrev;
			float  _Key3Hue, _Key3HuePrev;
			float  _AnimTime;
			int    _KeyColorsCount;
			float4 _KeyColorsData[128];
			float3 _KeyColors[128];

	inline float3 HUEtoRGB(in float H) {
    	float R = abs(H * 6 - 3) - 1;
    	float G = 2 - abs(H * 6 - 2);
    	float B = 2 - abs(H * 6 - 4);
    	return saturate(float3(R,G,B));
  	}

   	inline float3 HSLtoRGB(in float3 HSL) {
	    float3 RGB = HUEtoRGB(HSL.x);
    	float C = (1 - abs(2 * HSL.z - 1)) * HSL.y;
    	return (RGB - 0.5) * C + HSL.z;
  	}

  	inline float3 HSVtoRGB(in float3 HSV) {
  	 	float3 RGB = HUEtoRGB(HSV.x);
    	return ((RGB - 1) * HSV.y + 1) * HSV.z;
  	}

  	inline float DrawLine(float2 p, float2 v, float2 w) {
	  	float l2 = dot(v-w, v-w);
	  	float t = dot(p-v, w-v) / l2;
	  	if (t<0 || t>1) return 1;
  		float2 proj = v + t * (w - v);
  		float dist = dot(p - proj, p - proj);
  	    return clamp(dist / 0.0001, 0.65, 1);
  	}

  	inline float3 DrawDot(float3 rgb, float2 uv, float4 keyData) {
		float2 sdd = uv - keyData.xy;
		float d = dot(sdd, sdd);
		int z = (int)keyData.z;
		float cursor = 1.0 + (z >> 8);
		float dotWidth = DOT_WIDTH + cursor * 0.001;
		float dotColor = (z & 15) / 9.0;
		dotWidth *= dotWidth;
		if (d < dotWidth) {
			return dotColor;
		}
		return lerp(rgb, 1.0 - dotColor, saturate(0.0001 / abs(d - dotWidth)) * cursor * 2.0);
	}

		v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				float3 eyePos = UnityObjectToViewPos(v.vertex);
				o.clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0));
				return o;
			}
			
			half4 frag (v2f i) : SV_Target {
				float2 dd = i.uv - 0.5;
				float d = dot(dd, dd);
				if (d <= 0.5*0.5) {
					float t = _AnimTime;
                    
                    #if CW_CUSTOM

                    float2 key0Pos = _KeyColorsData[0].xy;
                    #if CW_COMPLEMENTARY || CW_SPLIT_COMPLEMENTARY || CW_ANALOGOUS || CW_TETRADIC
                    float2 key1Pos =  _KeyColorsData[1].xy;
                    #endif
                    #if CW_SPLIT_COMPLEMENTARY || CW_ANALOGOUS || CW_TETRADIC
                    float2 key2Pos = _KeyColorsData[2].xy;
                    #endif
                    #if CW_TETRADIC
                    float2 key3Pos = _KeyColorsData[3].xy;
                    #endif

                    #else

					float2 key0Pos = _KeyColorsData[0].xy = lerp(_Key0PosPrev, _Key0Pos, t);
					#if CW_COMPLEMENTARY || CW_SPLIT_COMPLEMENTARY || CW_ANALOGOUS || CW_TETRADIC
					float2 key1Pos =  _KeyColorsData[1].xy = lerp(_Key1PosPrev, _Key1Pos, t);
					#endif
					#if CW_SPLIT_COMPLEMENTARY || CW_ANALOGOUS || CW_TETRADIC
					float2 key2Pos = _KeyColorsData[2].xy = lerp(_Key2PosPrev, _Key2Pos, t);
					#endif
					#if CW_TETRADIC
					float2 key3Pos = _KeyColorsData[3].xy = lerp(_Key3PosPrev, _Key3Pos, t);
					#endif

                    #endif

					float angle = atan2(dd.y, dd.x);
					float hue = (angle + PI) / PI2;
					d = sqrt(d);
					float light = (d - _CenterWidth) / (1.0 - _CenterWidth);
					if (light < 0) {
						float2 h0 = float2(0,999);
						for (int k=0;k<_KeyColorsCount;k++) {
							float d1 = dot(i.uv-_KeyColorsData[k].xy,i.uv-_KeyColorsData[k].xy);
							float2 h1 = float2(_KeyColorsData[k].w,d1);
							h0 = lerp(h1, h0, h1.y > h0.y);
						}
						hue = h0.x;
						light = saturate(0.2 / d) * 0.5;
					}  else if (light > 0.32) {
						light = 0.4;
						hue = floor(hue * 32) / 32.0;
					}  else {
						light = 0.5;
					}

                    light = _MinBrightness + (_MaxBrightness - _MinBrightness) * light;
					float4 color = float4(HSLtoRGB(float3(hue,_Saturation,light)), 1.0);

					// connect line
					#if CW_COMPLEMENTARY
					color.rgb *= DrawLine(i.uv, key0Pos, key1Pos);
					#elif CW_ANALOGOUS
					color.rgb *= DrawLine(i.uv, key0Pos, key1Pos);
					color.rgb *= DrawLine(i.uv, key0Pos, key2Pos);
					#elif CW_SPLIT_COMPLEMENTARY
					color.rgb *= DrawLine(i.uv, key0Pos, key1Pos);
					color.rgb *= DrawLine(i.uv, key1Pos, key2Pos);
					color.rgb *= DrawLine(i.uv, key2Pos, key0Pos);
					#elif CW_TETRADIC
					color.rgb *= DrawLine(i.uv, key0Pos, key1Pos);
					color.rgb *= DrawLine(i.uv, key1Pos, key2Pos);
					color.rgb *= DrawLine(i.uv, key2Pos, key3Pos);
					color.rgb *= DrawLine(i.uv, key3Pos, key0Pos);
					#endif

					// outer ring
					color.rgb += saturate(0.002 / abs(d - 0.5)) * 0.5;

					// middle ring
					color.rgb = lerp(color.rgb, 0.7, 0.5 * saturate(0.001 / abs(d - 0.25 - _CenterWidth * 0.5)));

					// inner ring
					color.rgb = lerp(color.rgb, 0.0.xxx, saturate(0.002 / abs(d - _CenterWidth)) * 0.5);

					// dots
					for (int k=0;k<_KeyColorsCount;k++) {
						color.rgb = DrawDot(color.rgb, i.uv, _KeyColorsData[k]);
					}

					color.a *= tex2D(_GUIClipTexture, i.clipUV).a;
					return color;
				}
				return 0.0.xxxx;
			}

			ENDCG
		}
	}
}

