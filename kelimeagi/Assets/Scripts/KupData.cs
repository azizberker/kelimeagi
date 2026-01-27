using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class KupData : MonoBehaviour
{
    [Header("Bilesenler")]
    public TMP_Text harfYazisi;
    public TMP_Text puanYazisi;
    public Image kupGoruntusu;
    
    [Header("Kup Gorseli")]
    public Sprite kupSprite;

    [HideInInspector]
    public char mevcutHarf;
    
    [HideInInspector]
    public int mevcutPuan;

    // MaterialPropertyBlock optimizasyonu icin degiskenler
    private Renderer[] modelRenderers;
    private MaterialPropertyBlock[] propBlocks;
    
    // Rengi hafizada tutuyoruz
    [HideInInspector]
    public Color mevcutRenk = Color.white;
    
    // 2D Image icin baslangic alpha degeri
    private float initialAlpha = 1f;
    
    // Alpha degerleri
    private const float normalAlpha = 0.05f;
    private const float selectedAlpha = 1.0f;
    
    // VFX referanslari
    [SerializeField] private ParticleSystem fireVfx;
    [SerializeField] private ParticleSystem smokeVfx;

    void Awake()
    {
        // Image komponenti bul
        if (kupGoruntusu == null)
        {
            kupGoruntusu = GetComponent<Image>();
            if (kupGoruntusu == null)
            {
                kupGoruntusu = GetComponentInChildren<Image>();
            }
        }

        // Harf yazisi bul
        if (harfYazisi == null)
        {
            harfYazisi = GetComponentInChildren<TMP_Text>();
        }
        
        // Puan yazisi olustur (yoksa)
        if (puanYazisi == null)
        {
            OlusturPuanYazisi();
        }
        
        // VFX_Fire otomatik bul
        if (fireVfx == null)
        {
            Transform vfxTransform = transform.Find("VFX_Fire");
            if (vfxTransform != null)
            {
                fireVfx = vfxTransform.GetComponent<ParticleSystem>();
            }
            else
            {
                fireVfx = GetComponentInChildren<ParticleSystem>(true);
            }
        }
        
        // Baslangicta VFX kapali
        if (fireVfx != null)
        {
            fireVfx.gameObject.SetActive(false);
        }
        
        // VFX_BlackSmoke otomatik bul
        if (smokeVfx == null)
        {
            Transform smokeTransform = transform.Find("VFX_BlackSmoke");
            if (smokeTransform != null)
            {
                smokeVfx = smokeTransform.GetComponent<ParticleSystem>();
            }
        }
        
        // Baslangicta Smoke kapali
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

    void Start()
    {
        // Image komponenti garanti et
        if (kupGoruntusu == null)
        {
            kupGoruntusu = GetComponent<Image>();
            if (kupGoruntusu == null)
            {
                kupGoruntusu = gameObject.AddComponent<Image>();
            }
        }
        
        // Sprite yoksa kupSprite ata
        if (kupGoruntusu.sprite == null && kupSprite != null)
        {
            kupGoruntusu.sprite = kupSprite;
        }
        
        // Tiklanabilir olsun - RENK ATAMA YAPMA, VeriAta halleder
        kupGoruntusu.raycastTarget = true;
    }

    /// <summary>
    /// Sprite bazli veri atama
    /// </summary>
    public void VeriAta(char gelenHarf, Sprite gelenSprite)
    {
        mevcutHarf = gelenHarf;
        
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
            initialAlpha = 1f;
        }
    }

    /// <summary>
    /// Renk bazli veri atama (ana kullanim)
    /// </summary>
    public void VeriAta(char gelenHarf, Color gelenRenk)
    {
        mevcutHarf = gelenHarf;
        
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

        mevcutRenk = gelenRenk;

        // 3D Model varsa MaterialPropertyBlock ile rengi guncelle
        if (modelRenderers != null && modelRenderers.Length > 0)
        {
            UpdateModelColors(false);
        }
        // Yoksa 2D Image rengini degistir
        else if (kupGoruntusu != null)
        {
            kupGoruntusu.color = gelenRenk;
            initialAlpha = gelenRenk.a; // Baslangic alpha degerini kaydet
        }
    }

    /// <summary>
    /// Genisletilmis veri atama - cam efekti destekli
    /// </summary>
    public void VeriAta(char gelenHarf, Color gelenRenk, bool camEfekti, float camSaydamligi = 0.5f, Sprite customSprite = null)
    {
        mevcutHarf = gelenHarf;
        
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

        mevcutRenk = gelenRenk;

        // 3D Model varsa
        if (modelRenderers != null && modelRenderers.Length > 0)
        {
            UpdateModelColors(false);
        }
        // 2D Image
        else if (kupGoruntusu != null)
        {
            if (customSprite != null)
            {
                kupGoruntusu.sprite = customSprite;
            }
            
            Color renkUygula = gelenRenk;
            if (camEfekti)
            {
                renkUygula.a = camSaydamligi;
            }
            kupGoruntusu.color = renkUygula;
            initialAlpha = renkUygula.a;
        }
    }

    // 3D Model Referansi Atama
    public void ModelAta(Renderer[] renderers)
    {
        modelRenderers = renderers;
        
        if (modelRenderers != null && modelRenderers.Length > 0)
        {
            propBlocks = new MaterialPropertyBlock[modelRenderers.Length];
            for (int i = 0; i < modelRenderers.Length; i++)
            {
                propBlocks[i] = new MaterialPropertyBlock();
                if (modelRenderers[i] != null)
                {
                    modelRenderers[i].GetPropertyBlock(propBlocks[i]);
                }
            }
        }

        // 3D model varsa 2D Image seffaflastir
        if (kupGoruntusu != null)
        {
            Color c = kupGoruntusu.color;
            c.a = 0f;
            kupGoruntusu.color = c;
        }
    }

    public Color GetKupRengi()
    {
        if (modelRenderers != null && modelRenderers.Length > 0)
        {
            return mevcutRenk;
        }
        if (kupGoruntusu != null) return kupGoruntusu.color;
        return Color.white;
    }

    public void SetSeciliDurum(bool secili)
    {
        // 3D Model varsa
        if (modelRenderers != null && modelRenderers.Length > 0)
        {
            UpdateModelColors(secili);
        }
        // 2D Image varsa - secim opaklastirma
        else if (kupGoruntusu != null)
        {
            Color c = kupGoruntusu.color;
            if (secili)
            {
                c.a = 1f; // Secili iken tam opak
            }
            else
            {
                c.a = initialAlpha; // Secili degilken eski alpha
            }
            kupGoruntusu.color = c;
        }
        
        // VFX_Fire ac/kapat
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
    /// Yanlis kelime VFX
    /// </summary>
    public void YanlisKelimeVfxBaslat()
    {
        StartCoroutine(YanlisKelimeVfxCoroutine());
    }

    private System.Collections.IEnumerator YanlisKelimeVfxCoroutine()
    {
        if (fireVfx != null)
        {
            fireVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            fireVfx.gameObject.SetActive(false);
        }

        if (smokeVfx != null)
        {
            smokeVfx.gameObject.SetActive(true);
            smokeVfx.Play(true);
        }

        yield return new WaitForSeconds(1f);

        if (smokeVfx != null)
        {
            smokeVfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            smokeVfx.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// MaterialPropertyBlock ile renk guncelleme
    /// </summary>
    private void UpdateModelColors(bool isSelected)
    {
        if (modelRenderers == null || propBlocks == null) return;

        float currentAlpha = isSelected ? selectedAlpha : normalAlpha;
        
        Color colorToApply = mevcutRenk;
        colorToApply.a = currentAlpha;

        for (int i = 0; i < modelRenderers.Length; i++)
        {
            if (modelRenderers[i] == null) continue;
            
            if (i >= propBlocks.Length || propBlocks[i] == null)
            {
                propBlocks[i] = new MaterialPropertyBlock();
            }

            modelRenderers[i].GetPropertyBlock(propBlocks[i]);

            propBlocks[i].SetColor("_MainColor", colorToApply);
            propBlocks[i].SetColor("_BaseColor", colorToApply);
            propBlocks[i].SetColor("_Color", colorToApply);

            modelRenderers[i].SetPropertyBlock(propBlocks[i]);
        }
    }
}
