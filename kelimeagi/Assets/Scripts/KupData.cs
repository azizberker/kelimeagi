using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class KupData : MonoBehaviour
{
    [Header("Bileşenler")]
    public TMP_Text harfYazisi;
    public TMP_Text puanYazisi; // Sağ alt köşedeki puan
    public Image kupGoruntusu;
    
    [Header("Küp Görseli")]
    public Sprite kupSprite; // PNG görselini buraya sürükle!

    [HideInInspector]
    public char mevcutHarf;
    
    [HideInInspector]
    public int mevcutPuan;

    // YENİ: MaterialPropertyBlock optimizasyonu için değişkenler
    private Renderer[] modelRenderers;
    private MaterialPropertyBlock[] propBlocks;
    
    // Rengi hafızada tutuyoruz çünkü PropertyBlock'tan okumak yerine yeniden set edeceğiz
    [HideInInspector]
    public Color mevcutRenk = Color.white;
    
    // İstenen alpha değerleri (Orijinal koddaki 0.05f ve 1.0f)
    private const float normalAlpha = 0.05f;
    private const float selectedAlpha = 1.0f;
    
    // VFX_Fire referansı (Seçildiğinde alev efekti)
    [SerializeField] private ParticleSystem fireVfx;
    
    // VFX_BlackSmoke referansı (Yanlış kelime için duman efekti)
    [SerializeField] private ParticleSystem smokeVfx;

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
        
        // VFX_Fire'i otomatik bul (boşsa)
        if (fireVfx == null)
        {
            Transform vfxTransform = transform.Find("VFX_Fire");
            if (vfxTransform != null)
            {
                fireVfx = vfxTransform.GetComponent<ParticleSystem>();
            }
            else
            {
                // Child içinde herhangi bir yerde olabilir
                fireVfx = GetComponentInChildren<ParticleSystem>(true);
            }
        }
        
        // Başlangıçta VFX kapalı olsun
        if (fireVfx != null)
        {
            fireVfx.gameObject.SetActive(false);
        }
        
        // VFX_BlackSmoke'u otomatik bul (boşsa)
        if (smokeVfx == null)
        {
            Transform smokeTransform = transform.Find("VFX_BlackSmoke");
            if (smokeTransform != null)
            {
                smokeVfx = smokeTransform.GetComponent<ParticleSystem>();
            }
        }
        
        // Başlangıçta Smoke kapalı olsun
        if (smokeVfx != null)
        {
            smokeVfx.gameObject.SetActive(false);
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

    // 3D Model Referansı Atama
    public void ModelAta(Renderer[] renderers)
    {
        modelRenderers = renderers;
        
        // Renderers geldiyse PropertyBlock dizisini hazırla
        if (modelRenderers != null && modelRenderers.Length > 0)
        {
            propBlocks = new MaterialPropertyBlock[modelRenderers.Length];
            for (int i = 0; i < modelRenderers.Length; i++)
            {
                // Her renderer için temiz bir block oluştur
                propBlocks[i] = new MaterialPropertyBlock();
                if (modelRenderers[i] != null)
                {
                    // Mevcut takılı renk varsa onu alıp block'a işleyebiliriz,
                    // ama VeriAta çağrılacağı için sıfırdan başlamak daha güvenli.
                    // Yine de renderer üzerinde hali hazırda bir override varsa korumak için:
                    modelRenderers[i].GetPropertyBlock(propBlocks[i]);
                }
            }
        }

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
        // 2D Image kullanıyoruz - görünür olmalı
        if (kupGoruntusu == null)
        {
            kupGoruntusu = GetComponent<Image>();
            if (kupGoruntusu == null)
            {
                kupGoruntusu = gameObject.AddComponent<Image>();
            }
        }
        
        // Sprite atanmışsa Image'a uygula
        if (kupSprite != null && kupGoruntusu != null)
        {
            kupGoruntusu.sprite = kupSprite;
        }
        
        // Image görünür ve tıklanabilir olsun
        kupGoruntusu.color = Color.white; 
        kupGoruntusu.raycastTarget = true;
    }

    /// <summary>
    /// Eski metod - renk bazlı (geriye uyumluluk ve ana kullanım)
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

        // Rengi sakla (Normal alpha ile değil, ham renk olarak saklıyoruz, uygulama anında alpha verilecek)
        mevcutRenk = gelenRenk;

        // 3D Model varsa MaterialPropertyBlock ile rengi güncelle
        if (modelRenderers != null && modelRenderers.Length > 0)
        {
            UpdateModelColors(false); // Başlangıçta seçili değil
        }
        // Yoksa 2D Image rengini değiştir (SADECE 3D MODEL YOKSA)
        else if (kupGoruntusu != null && (modelRenderers == null || modelRenderers.Length == 0))
        {
            kupGoruntusu.color = gelenRenk;
        }
    }

    public Color GetKupRengi()
    {
        // 3D model varsa saklanan rengi dön
        if (modelRenderers != null && modelRenderers.Length > 0)
        {
            return mevcutRenk;
        }
        if (kupGoruntusu != null) return kupGoruntusu.color;
        return Color.white;
    }

    public void SetSeciliDurum(bool secili)
    {
        // Seçim durumuna göre Alpha'yı değiştiriyoruz, renk sabit kalıyor
        if (modelRenderers != null && modelRenderers.Length > 0)
        {
            UpdateModelColors(secili);
        }
        
        // VFX_Fire aç/kapat
        if (fireVfx != null)
        {
            if (secili)
            {
                fireVfx.gameObject.SetActive(true);
                fireVfx.Play(true);
            }
            else
            {
                fireVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                fireVfx.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Yanlış kelime oluştuğunda: Alev söner, duman çıkar, 1 saniye bekler, duman biter.
    /// </summary>
    public void YanlisKelimeVfxBaslat()
    {
        StartCoroutine(YanlisKelimeVfxCoroutine());
    }

    private System.Collections.IEnumerator YanlisKelimeVfxCoroutine()
    {
        // 1. Alevi durdur ve kapat
        if (fireVfx != null)
        {
            fireVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            fireVfx.gameObject.SetActive(false);
        }

        // 2. Dumanı aç ve başlat
        if (smokeVfx != null)
        {
            smokeVfx.gameObject.SetActive(true);
            smokeVfx.Play(true);
        }

        // 3. 1 saniye bekle
        yield return new WaitForSeconds(1f);

        // 4. Dumanı durdur ve kapat
        if (smokeVfx != null)
        {
            smokeVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            smokeVfx.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// MaterialPropertyBlock kullanarak tüm rendererların rengini ve alphasını günceller.
    /// material instance oluşturmaz, performansı korur.
    /// </summary>
    private void UpdateModelColors(bool isSelected)
    {
        if (modelRenderers == null || propBlocks == null) return;

        // Hedef alpha: Seçiliyse tam opak (1.0), değilse cam gibi (0.05)
        float currentAlpha = isSelected ? selectedAlpha : normalAlpha;
        
        // Uygulanacak renk
        Color colorToApply = mevcutRenk;
        colorToApply.a = currentAlpha;

        for (int i = 0; i < modelRenderers.Length; i++)
        {
            if (modelRenderers[i] == null) continue;
            
            // Eğer block dizisi senkronize değilse veya null ise güvenli oluştur
            if (i >= propBlocks.Length || propBlocks[i] == null)
            {
                propBlocks[i] = new MaterialPropertyBlock();
            }

            // Mevcut block verisini al (başka propertyler varsa silinmesin)
            modelRenderers[i].GetPropertyBlock(propBlocks[i]);

            // Shader'ın kullanabileceği tüm olası renk parametrelerine aynı rengi basıyoruz.
            // Bu sayede "HasProperty" kontrolüne gerek kalmadan tek seferde işlem yapılır.
            propBlocks[i].SetColor("_MainColor", colorToApply);
            propBlocks[i].SetColor("_BaseColor", colorToApply);
            propBlocks[i].SetColor("_Color", colorToApply);

            // Block'u renderera uygula
            modelRenderers[i].SetPropertyBlock(propBlocks[i]);
        }
    }
}
