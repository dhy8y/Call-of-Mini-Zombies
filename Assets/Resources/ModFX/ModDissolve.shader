// Mod: dead bodies break apart into pixels before they disappear.
// The texture is cut into square cells (the models use pixel-art textures, so
// cells line up with their pixels). Each cell vanishes at its own random
// moment as _Dissolve goes from 0 to 1, with a glowing rim just before it goes.
Shader "ModFX/ModDissolve" {
Properties {
 _MainTex ("Texture", 2D) = "white" {}
 _Dissolve ("Dissolve", Range(0,1)) = 0
 _EdgeColor ("Edge Glow", Color) = (1,0.55,0.1,1)
 _EdgeWidth ("Edge Width", Range(0,0.5)) = 0.12
 _Cells ("Pixels Across", Float) = 32
}
SubShader {
 Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" }
 Pass {
  Cull Off
  CGPROGRAM
  #pragma vertex vert
  #pragma fragment frag
  #include "UnityCG.cginc"

  sampler2D _MainTex;
  float4 _MainTex_ST;
  float _Dissolve;
  fixed4 _EdgeColor;
  float _EdgeWidth;
  float _Cells;

  struct appdata {
   float4 vertex : POSITION;
   float2 uv : TEXCOORD0;
  };

  struct v2f {
   float4 pos : SV_POSITION;
   float2 uv : TEXCOORD0;
  };

  v2f vert (appdata v) {
   v2f o;
   o.pos = UnityObjectToClipPos(v.vertex);
   o.uv = TRANSFORM_TEX(v.uv, _MainTex);
   return o;
  }

  float hash (float2 p) {
   return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
  }

  fixed4 frag (v2f i) : SV_Target {
   fixed4 col = tex2D(_MainTex, i.uv);
   float2 cell = floor(i.uv * _Cells);
   // Spread the thresholds so nothing is gone at 0 and everything is gone at 1.
   float threshold = hash(cell) * (1.0 - _EdgeWidth) + _EdgeWidth;
   float left = threshold - _Dissolve;
   clip(left);
   float rim = saturate(1.0 - left / max(_EdgeWidth, 0.0001));
   col.rgb = lerp(col.rgb, _EdgeColor.rgb * 1.6, rim * step(0.001, _Dissolve));
   col.a = 1;
   return col;
  }
  ENDCG
 }
}
Fallback Off
}
