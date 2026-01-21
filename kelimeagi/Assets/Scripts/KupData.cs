using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class KupData : MonoBehaviour
{
    [Header("Bileşenler")]
    public TMP_Text harfYazisi;
    public TMP_Text puanYazisi; // Sağ alt köşedeki puan
    public Image kupGoruntusu;

    [HideInInspector]
    public char mevcutHarf;
    
    [HideInInspector]
    public int mevcutPuan;

    void Awake()
    {
        if (harfYazisi == null)
        {
            harfYazisi = GetComponentInChildren<TMP_Text>();
        }

        if (kupGoruntusu == null)
        {
            kupGoruntusu = GetComponent<Image>();
            if (kupGoruntusu == null)
            {
                kupGoruntusu = GetComponentInChildren<Image>();
            }
        }
        
        // Puan yazısını oluştur (yoksa)
        if (puanYazisi == null)
        {
            OlusturPuanYazisi();
        }
    }
    
    void OlusturPuanYazisi()
    {
        GameObject puanObj = new GameObject("PuanYazisi");
        puanObj.transform.SetParent(transform, false);
        
        puanYazisi = puanObj.AddComponent<TMP_Text>();
        puanYazisi.fontSize = 14;
        puanYazisi.fontStyle = FontStyles.Bold;
        puanYazisi.color = new Color(1f, 1f, 1f, 0.7f);
        puanYazisi.alignment = TextAlignmentOptions.BottomRight;
        
        RectTransform rect = puanObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(2, 2);
        rect.offsetMax = new Vector2(-4, -4);
    }

    /// <summary>
    /// Harfi ve sprite'ı atar (YENİ - sprite bazlı)
    /// </summary>
    public void VeriAta(char gelenHarf, Sprite gelenSprite)
    {
        mevcutHarf = gelenHarf;
        
        // Puanı hesapla
        if (HarfYoneticisi.Instance != null)
        {
            mevcutPuan = HarfYoneticisi.Instance.GetHarfPuani(gelenHarf);
        }
        else
        {
            mevcutPuan = 1;
        }
        
        if (harfYazisi != null)
        {
            harfYazisi.text = gelenHarf.ToString();
        }
        
        if (puanYazisi != null)
        {
            puanYazisi.text = mevcutPuan.ToString();
        }

        if (kupGoruntusu != null && gelenSprite != null)
        {
            kupGoruntusu.sprite = gelenSprite;
            kupGoruntusu.color = Color.white;
        }
    }

    // 3D Model Referansı
    private Renderer[] modelRenderers;

    public void ModelAta(Renderer[] renderers)
    {
        modelRenderers = renderers;
        // 3D model varsa 2D Image'ı gizle veya şeffaflaştır
        if (kupGoruntusu != null)
        {
            Color c = kupGoruntusu.color;
            c.a = 0f; // Tamamen şeffaf yap
            kupGoruntusu.color = c;
        }
    }

    void Start()
    {
        // 3D Obje kullandığımız için Raycast'in tutması adına görünmez bir Image (Hitbox) ekliyoruz
        if (kupGoruntusu == null)
        {
            kupGoruntusu = GetComponent<Image>();
            if (kupGoruntusu == null)
            {
                kupGoruntusu = gameObject.AddComponent<Image>();
            }
        }
        
        // Image'ı tamamen şeffaf yap ama raycast target açık kalsın
        kupGoruntusu.color = new Color(0, 0, 0, 0); 
        kupGoruntusu.raycastTarget = true;
    }

    /// <summary>
    /// Eski metod - renk bazlı (geriye uyumluluk için)
    /// </summary>
    public void VeriAta(char gelenHarf, Color gelenRenk)
    {
        mevcutHarf = gelenHarf;
        
        // Puanı hesapla
        if (HarfYoneticisi.Instance != null)
        {
            mevcutPuan = HarfYoneticisi.Instance.GetHarfPuani(gelenHarf);
        }
        else
        {
            mevcutPuan = 1;
        }
        
        if (harfYazisi != null)
        {
            harfYazisi.text = gelenHarf.ToString();
        }
        
        if (puanYazisi != null)
        {
            puanYazisi.text = mevcutPuan.ToString();
        }

        // 3D Model varsa onun rengini değiştir
        if (modelRenderers != null && modelRenderers.Length > 0)
        {
            foreach (var rend in modelRenderers)
            {
                if (rend != null)
                {
                    // Yeni Kristal Shader için
                    if (rend.material.HasProperty("_MainColor"))
                    {
                        rend.material.SetColor("_MainColor", gelenRenk);
                    }
                    // URP Shader
                    else if (rend.material.HasProperty("_BaseColor"))
                    {
                        rend.material.SetColor("_BaseColor", gelenRenk);
                    }
                    // Standart Shader
                    else 
                    {
                        rend.material.color = gelenRenk;
                    }
                }
            }
            // Rengi atadıktan sonra saydamlık durumunu resetle (Seçili değilsin)
            SetSeciliDurum(false);
        }
        // Yoksa 2D Image rengini değiştir (SADECE 3D MODEL YOKSA)
        else if (kupGoruntusu != null && (modelRenderers == null || modelRenderers.Length == 0))
        {
            kupGoruntusu.color = gelenRenk;
        }
    }

    public Color GetKupRengi()
    {
        if (modelRenderers != null && modelRenderers.Length > 0 && modelRenderers[0] != null)
        {
            if (modelRenderers[0].material.HasProperty("_MainColor"))
                return modelRenderers[0].material.GetColor("_MainColor");
            if (modelRenderers[0].material.HasProperty("_BaseColor"))
                return modelRenderers[0].material.GetColor("_BaseColor");
            return modelRenderers[0].material.color;
        }
        if (kupGoruntusu != null) return kupGoruntusu.color;
        return Color.white;
    }

    public void SetSeciliDurum(bool secili)
    {
        // Seçili olunca rengi biraz daha koyu/opak yapalım ki belli olsun
        if (modelRenderers != null && modelRenderers.Length > 0)
        {
            foreach(var rend in modelRenderers)
            {
                if (rend == null) continue;
                
                // Shaderımız _MainColor'ın Alpha'sını kullanıyor
                if (rend.material.HasProperty("_MainColor"))
                {
                    Color c = rend.material.GetColor("_MainColor");
                    // Seçiliyken alpha 1 (Opak), değilse eski transparent hali
                    c.a = secili ? 1.0f : 0.05f; 
                    rend.material.SetColor("_MainColor", c);
                }
            }
        }
    }
}
