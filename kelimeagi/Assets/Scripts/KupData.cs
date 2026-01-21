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

        if (kupGoruntusu != null)
        {
            kupGoruntusu.color = gelenRenk;
        }
    }
}
