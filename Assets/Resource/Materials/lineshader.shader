Shader "MyShader/RenderDepth"
{
	Properties
	{
		_Color("MainColor",Color)=(1,1,1,1)
	}

	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct VertexData
			{
				float4 pos:POSITION;
			};

			struct V2F
			{
				float4 pos:POSITION;
			};

			V2F vert(VertexData v)
			{
				V2F res;
				res.pos = UnityObjectToClipPos(v.pos);
				return res;
			}

			float4 _Color;

			fixed4 frag(V2F v) :SV_Target
			{
				fixed4 col = _Color;
				return col;
			}
			ENDCG
		}
	}
}
