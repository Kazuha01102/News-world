Shader "NightHome/Opaque Vertex Surface" {
 Properties {
  _Color ("Tint", Color) = (1,1,1,1)
  _MainTex ("Albedo", 2D) = "white" {}
  _Glossiness ("Smoothness", Range(0,1)) = 0.08
 }
 SubShader {
  Tags { "RenderType"="Opaque" "Queue"="Geometry" }
  LOD 200
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
  #pragma target 3.0
  sampler2D _MainTex; fixed4 _Color; half _Glossiness;
  struct Input { float2 uv_MainTex; fixed4 tint; };
  void vert(inout appdata_full v, out Input o) { UNITY_INITIALIZE_OUTPUT(Input,o); o.tint=v.color; }
  void surf(Input IN, inout SurfaceOutputStandard o) {
   o.Albedo=tex2D(_MainTex,IN.uv_MainTex).rgb*_Color.rgb*IN.tint.rgb;
   o.Smoothness=_Glossiness; o.Metallic=0; o.Occlusion=1; o.Alpha=1;
  }
  ENDCG
 }
 FallBack "Diffuse"
}
