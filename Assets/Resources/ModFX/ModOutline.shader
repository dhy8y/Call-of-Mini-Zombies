// Mod: thin outline around a character's silhouette, plus a glow when it is hit.
//
// The outline follows the outer shape only (no lines between an arm and the
// body behind it). It works in four passes drawn on a copy of the mesh:
//   1. MARK    - tags every pixel covered by the character in the stencil buffer
//   2. OUTLINE - draws the mesh pushed outwards, only where the tag is missing
//   3. CLEAR   - removes the tag so the next character gets its own outline
//   4. GLOW    - adds light on the body itself while _Flash is above zero
Shader "ModFX/ModOutline" {
Properties {
 _OutlineColor ("Outline Color", Color) = (0,0,0,1)
 _OutlineWidth ("Outline Width (pixels on a 720p screen)", Float) = 1.6
 _FlashColor ("Glow Color", Color) = (1,0.9,0.6,1)
 _Flash ("Glow Amount", Range(0,1)) = 0
 _FlashWidth ("Extra Width While Glowing", Float) = 1.75
 _BodyGlow ("Body Glow Strength", Range(0,1)) = 0.45
}
SubShader {
 // After every opaque object, so the outline lands on top of the scenery.
 Tags { "Queue"="Geometry+60" "RenderType"="Opaque" "IgnoreProjector"="True" }

 CGINCLUDE
 #include "UnityCG.cginc"

 fixed4 _OutlineColor;
 float _OutlineWidth;
 fixed4 _FlashColor;
 float _Flash;
 float _FlashWidth;
 float _BodyGlow;

 struct appdata {
  float4 vertex : POSITION;
  float3 normal : NORMAL;
 };

 struct v2f {
  float4 pos : SV_POSITION;
 };

 v2f vertBody (appdata v) {
  v2f o;
  o.pos = UnityObjectToClipPos(v.vertex);
  return o;
 }

 // Pushes each vertex along its normal, measured in screen pixels so the line
 // keeps the same thickness whether the character is near or far.
 v2f vertHull (appdata v) {
  v2f o;
  o.pos = UnityObjectToClipPos(v.vertex);
  float3 viewNormal = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
  float2 dir = TransformViewToProjection(viewNormal.xy);
  float len = max(length(dir), 0.00001);
  float pixels = _OutlineWidth * (1.0 + _FlashWidth * saturate(_Flash)) * _ScreenParams.y / 720.0;
  o.pos.xy += (dir / len) * pixels * 2.0 / _ScreenParams.xy * o.pos.w;
  return o;
 }

 fixed4 fragNothing (v2f i) : SV_Target {
  return fixed4(0,0,0,0);
 }

 fixed4 fragOutline (v2f i) : SV_Target {
  fixed3 c = lerp(_OutlineColor.rgb, _FlashColor.rgb, saturate(_Flash));
  return fixed4(c, 1);
 }

 fixed4 fragGlow (v2f i) : SV_Target {
  return fixed4(_FlashColor.rgb * saturate(_Flash) * _BodyGlow, 1);
 }
 ENDCG

 Pass {
  Name "MARK"
  Cull Off
  ZTest Always
  ZWrite Off
  ColorMask 0
  Stencil {
   Ref 128
   ReadMask 128
   WriteMask 128
   Comp Always
   Pass Replace
  }
  CGPROGRAM
  #pragma vertex vertBody
  #pragma fragment fragNothing
  ENDCG
 }

 Pass {
  Name "OUTLINE"
  Cull Off
  ZTest LEqual
  ZWrite On
  Stencil {
   Ref 128
   ReadMask 128
   WriteMask 128
   Comp NotEqual
   Pass Keep
  }
  CGPROGRAM
  #pragma vertex vertHull
  #pragma fragment fragOutline
  ENDCG
 }

 Pass {
  Name "CLEAR"
  Cull Off
  ZTest Always
  ZWrite Off
  ColorMask 0
  Stencil {
   Ref 0
   ReadMask 128
   WriteMask 128
   Comp Always
   Pass Replace
  }
  CGPROGRAM
  #pragma vertex vertHull
  #pragma fragment fragNothing
  ENDCG
 }

 Pass {
  Name "GLOW"
  Cull Back
  ZTest LEqual
  ZWrite Off
  Offset -1, -1
  Blend One One
  CGPROGRAM
  #pragma vertex vertBody
  #pragma fragment fragGlow
  ENDCG
 }
}
Fallback Off
}
