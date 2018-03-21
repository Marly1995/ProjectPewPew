﻿Shader "XRay Shaders/Diffuse-XRay-Stenciled"
{
	Properties
	{
		_Color("Main Color", Color) = (1,1,1,1)
		_EdgeColor("XRay Edge Color", Color) = (0,0,0,0)
		_MainTex("Base (RGB)", 2D) = "white" {}
	}
		SubShader
	{
		Tags
	{
		"Queue" = "Geometry-1"
		"RenderType" = "Opaque"
		"XRay" = "ColoredOutline"
	}
		LOD 200

		CGPROGRAM
#pragma surface surf Lambert

	sampler2D _MainTex;
	fixed4 _Color;

	struct Input {
		float2 uv_MainTex;
	};

	void surf(Input IN, inout SurfaceOutput o)
	{
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
		o.Albedo = c.rgb;
		o.Alpha = c.a;
	}
	ENDCG
	}

		Fallback "Legacy Shaders/VertexLit"
}