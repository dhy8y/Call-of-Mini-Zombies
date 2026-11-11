// Mod: thin outline around a character's silhouette, plus a glow when it is hit.
//
// The outline is drawn on a copy of the mesh in two passes:
//   1. OUTLINE - the mesh pushed outwards, back faces only, so just the rim shows
//   2. GLOW    - adds light on the body itself while _Flash is above zero
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
  Name "OUTLINE"
  // Only the back faces of the pushed-out copy are drawn. They sit behind the
  // character, so the character hides all of it except the rim that sticks
  // out around the silhouette. Needs no stencil buffer, which many phones
  // do not provide.
  Cull Front
  ZTest LEqual
  ZWrite On
  CGPROGRAM
  #pragma vertex vertHull
  #pragma fragment fragOutline
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
