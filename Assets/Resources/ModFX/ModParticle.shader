// Mod: soft unlit sprite used by the impact particles and the ground marks.
Shader "ModFX/ModParticle" {
Properties {
 _MainTex ("Texture", 2D) = "white" {}
 _Color ("Tint", Color) = (1,1,1,1)
}
SubShader {
 Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
 Cull Off
 ZWrite Off
 Lighting Off
 Blend SrcAlpha OneMinusSrcAlpha
 // Ground marks sit right on a surface: pull them forward so they do not flicker.
 Offset -1, -1

 Pass {
  CGPROGRAM
  #pragma vertex vert
  #pragma fragment frag
  #include "UnityCG.cginc"

  sampler2D _MainTex;
  float4 _MainTex_ST;
  fixed4 _Color;

  struct appdata {
   float4 vertex : POSITION;
   fixed4 color : COLOR;
   float2 uv : TEXCOORD0;
  };

  struct v2f {
   float4 pos : SV_POSITION;
   fixed4 color : COLOR;
   float2 uv : TEXCOORD0;
  };

  v2f vert (appdata v) {
   v2f o;
   o.pos = UnityObjectToClipPos(v.vertex);
   o.color = v.color * _Color;
   o.uv = TRANSFORM_TEX(v.uv, _MainTex);
   return o;
  }

  fixed4 frag (v2f i) : SV_Target {
   return tex2D(_MainTex, i.uv) * i.color;
  }
  ENDCG
 }
}
Fallback Off
}
