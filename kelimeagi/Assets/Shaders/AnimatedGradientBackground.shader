Shader "Custom/AuraFinalv2"
{
    Properties
    {
        [Header(Colors)]
        _Color1 ("Color 1", Color) = (0.3, 0.1, 0.6, 1)
        _Color2 ("Color 2", Color) = (0.2, 0.2, 0.7, 1)
        _Color3 ("Color 3", Color) = (0.4, 0.1, 0.5, 1)
        
        [Header(Positions (X and Y))]
        // Koordinat Sistemi: 0,0 ekranın tam ortasıdır.
        _Pos1 ("Blob 1 Position", Vector) = (-0.5, 0.5, 0, 0) // Sol Üst
        _Pos2 ("Blob 2 Position", Vector) = (0.6, 0.1, 0, 0)  // Sağ Orta
        _Pos3 ("Blob 3 Position", Vector) = (0.0, -0.6, 0, 0) // Alt Orta

        [Header(Blob Settings)]
        _BlobScale ("Blob Size", Range(0.5, 3.0)) = 1.0 // Blobların büyüklüğü
        _FlowSpeed ("Flow Speed", Range(0, 2)) = 0.5    // Hareket hızı
        
        [Header(Grain Settings)]
        _DotSize ("Dot Size", Range(10, 100)) = 60.0 
        _DotIntensity ("Dot Intensity", Range(0, 0.5)) = 0.12 
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend One One // Additive karışım
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            float4 _Color1, _Color2, _Color3;
            float4 _Pos1, _Pos2, _Pos3; // Konum değişkenleri
            float _DotSize, _DotIntensity, _FlowSpeed, _BlobScale;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Seyrek ve yuvarlak noktalar
            float sparseDots(float2 uv, float time) {
                float2 scaledUV = uv * _DotSize;
                scaledUV.x += time * 0.5;
                scaledUV.y += sin(time * 0.7 + scaledUV.x * 0.1) * 2.0; 
                float noise = sin(scaledUV.x) * cos(scaledUV.y) * sin(scaledUV.x * 0.7 + scaledUV.y * 0.3 + time);
                return smoothstep(0.95, 0.98, noise);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y * _FlowSpeed;

                // --- Aspect Ratio Düzeltmesi ---
                // UV'yi [-1, 1] aralığına çekiyoruz ki (0,0) tam merkez olsun
                float2 uv = i.uv * 2.0 - 1.0;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                uv.x *= aspect;

                // --- Hareketli Bloblar ---
                // Kullanıcının girdiği pozisyon (_Pos.xy) + Sinüs dalgası (Hareket)
                
                // Blob 1
                float2 pos1 = _Pos1.xy + float2(sin(t*0.8)*0.2, cos(t*0.7)*0.15);
                float dist1 = length(uv - pos1);
                float mask1 = smoothstep(1.2 * _BlobScale, 0.1, dist1); 

                // Blob 2
                float2 pos2 = _Pos2.xy + float2(cos(t*0.6)*0.25, sin(t*0.9)*0.2);
                float dist2 = length(uv - pos2);
                float mask2 = smoothstep(1.0 * _BlobScale, 0.0, dist2);

                // Blob 3
                float2 pos3 = _Pos3.xy + float2(sin(t*1.1)*0.2, cos(t*0.5)*0.15);
                float dist3 = length(uv - pos3);
                float mask3 = smoothstep(1.1 * _BlobScale, 0.2, dist3);

                // Renkleri birleştir
                float4 finalColor = _Color1 * mask1 + _Color2 * mask2 * 0.8 + _Color3 * mask3 * 0.6;

                // --- Noktalar ---
                float dots = sparseDots(i.uv, t);
                finalColor += dots * _DotIntensity * float4(1,1,1,1);

                return max(finalColor, float4(0,0,0,1));
            }
            ENDCG
        }
    }
}