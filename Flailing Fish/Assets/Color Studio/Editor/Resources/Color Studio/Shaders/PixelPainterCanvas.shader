Shader "Color Studio/PixelPainterCanvas"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "black" {}
        _TranspTex ("Transparent Pattern", 2D) = "black" {}
		_Color ("Color", Color) = (1,1,1,1)
        _CursorPos ("Cursor", Vector) = (0,0,0)
        _CursorPos1 ("Cursor1", Vector) = (-1,0,0)
        _CursorPos2 ("Cursor2", Vector) = (-1,0,0)
        _CursorPos3 ("Cursor3", Vector) = (-1,0,0)

        _CursorColor ("Cursor Color", Color) = (1,1,1,1)
        _GridWidth ("Grid Width", Float) = 0.002
        _ZoomRect ("Zoom Data", Vector) = (1,0,0,0)
        _TileSize("Tile Size Factor", Vector) = (1,1,1,1)
	}
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#define DOT_WIDTH 0.015
            #define BORDER_WIDTH 0.7
            #define BORDER_INNER (BORDER_WIDTH+0.05)
            #define BORDER_OUTER 0.9
            #define GRID_COLOR fixed4(0.7.xxx, 0.45)
            #define MIRROR_COLOR fixed4(0.2,0.5,0.7,0.5)
			#include "UnityCG.cginc"

			struct appdata
			{
                float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
                float2 clipUV : TEXCOORD1;
			};

            
            sampler2D _GUIClipTexture;
            uniform float4x4 unity_GUIClipTextureMatrix;

			sampler2D _MainTex;
			float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            fixed4 _Color, _CursorColor;
            float4 _CursorPos, _CursorPos1, _CursorPos2, _CursorPos3;
            float _GridWidth;
            float4 _ZoomRect;
            float4 _TileSize;
            sampler2D _TranspTex;

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
                float3 eyePos = UnityObjectToViewPos(v.vertex);
                o.clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0));
				return o;
			}

            float4 DrawCursor(v2f i, fixed4 color, float4 cursorPos) {
    
				// cursor
                float2 xy = i.uv.xy - cursorPos.xy;
                float2 cursor = abs(xy);
                float inside = cursor.x < cursorPos.z && cursor.y < cursorPos.w;
                float border = inside && (cursor.x > cursorPos.z * BORDER_WIDTH || cursor.y > cursorPos.w * BORDER_WIDTH);
                color = lerp(color, GRID_COLOR, border);

                // inner border
                float rectBorder = border && (cursor.x > cursorPos.z * BORDER_INNER || cursor.y > cursorPos.w * BORDER_INNER);
                color = lerp(color, _CursorColor, rectBorder);

                return color;
            }

			float4 frag (v2f i) : SV_Target {
                float2 uvSprite = lerp(_ZoomRect.xy, _ZoomRect.zw, i.uv);
                fixed4 texColor = tex2D(_MainTex, uvSprite);

                texColor *= _Color;

                #if !UNITY_COLORSPACE_GAMMA
                    texColor.rgb = LinearToGammaSpace(texColor.rgb);
                #endif

                texColor.a *= tex2D(_GUIClipTexture, i.clipUV).a;

                fixed4 color = texColor;

                // pattern for transparent color
                float aspect = _MainTex_TexelSize.z / _MainTex_TexelSize.w;
                float2 patternUV = float2(i.uv.x * aspect, i.uv.y);
                float dither = tex2D(_TranspTex, patternUV * 16).a;
                color = lerp(color, fixed4(1,1,1,0.1), dither * (color.a == 0));

                // mirrored cursor
                color = DrawCursor(i, color, _CursorPos);
                float2 mirrorDelta = abs(uvSprite.xy - 0.5);
                mirrorDelta.x *= aspect;
                if (_CursorPos1.x>=-999) {
                    color = DrawCursor(i, color, _CursorPos1);
                    color = lerp(color, MIRROR_COLOR, mirrorDelta.x < 0.003);
                }
                if (_CursorPos2.y>=-999) {
                    color = DrawCursor(i, color, _CursorPos2);
                    color = lerp(color, MIRROR_COLOR, mirrorDelta.y < 0.003);
                }
                if (_CursorPos3.x>=-999) {
                    color = DrawCursor(i, color, _CursorPos1);
                    color = DrawCursor(i, color, _CursorPos2);
                    color = DrawCursor(i, color, _CursorPos3);
                    color = lerp(color, MIRROR_COLOR, any(mirrorDelta.xy < 0.003));
                }

                // grid
                if (_TileSize.w) {
                    float2 gridUV = i.uv.xy + _TileSize.xy * 0.5;
                    float2 dd = abs(_TileSize.xy * 0.5 - fmod(gridUV, _TileSize.xy)) * _TileSize.z * 0.002;
				    dd /= fwidth(gridUV);
                    color = lerp(color, GRID_COLOR, (dd.x < _GridWidth || dd.y < _GridWidth) );
                }
                color = saturate(color);

				return color;
			}

			ENDCG
		}
	}
}

