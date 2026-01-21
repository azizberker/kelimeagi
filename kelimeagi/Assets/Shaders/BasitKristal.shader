Shader "Custom/BasitKristal"
{
    Properties
    {
        _MainColor ("Ana Renk", Color) = (1,1,1,1)
        _RimColor ("Parilma Rengi", Color) = (1,1,1,1)
        _RimPower ("Parilma Gucu", Range(0.5, 8.0)) = 3.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha // Saydamlık için
        ZWrite Off // Arkasını göstermesi için

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR; // Partikül renkleri için şart
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 viewDir : TEXCOORD0;
                float3 normal : TEXCOORD1;
                fixed4 color : COLOR; // Partikül rengini taşı
            };

            float4 _MainColor;
            float4 _RimColor;
            float _RimPower;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = WorldSpaceViewDir(v.vertex);
                o.color = v.color; // Rengi taşı
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 normal = normalize(i.normal);
                float3 viewDir = normalize(i.viewDir);
                
                // 1. Kenar Çizgisi (Rim)
                float NdotV = 1.0 - saturate(dot(viewDir, normal));
                float rim = smoothstep(0.6, 1.0, NdotV);
                
                // 2. Parlaklık (Specular)
                float3 lightDir = normalize(float3(0.5, 1.0, -0.5));
                float3 halfVector = normalize(lightDir + viewDir);
                float NdotH = saturate(dot(normal, halfVector));
                float specular = pow(NdotH, 30.0) * 1.5;
                
                fixed4 col = _MainColor * i.color; // Vertex rengiyle çarp (Partikül için önemli)
                
                // Renk Hesaplama:
                float3 finalColor = (_RimColor.rgb * rim * 0.8) + (float3(1,1,1) * specular);
                
                // Alpha (Saydamlık):
                float alpha = (col.a * 0.02) + (rim * col.a * 0.4);
                alpha = max(alpha, specular); 
                
                finalColor += col.rgb * 0.1;
                
                // Partikül fade-out'u için vertex alpha'sını kullan
                alpha *= i.color.a; 
                
                return fixed4(finalColor, alpha);
            }
            ENDCG
        }
    }
}
