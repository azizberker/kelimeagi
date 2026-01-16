using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class KupData : MonoBehaviour
{
    [Header("Bileşenler")]
    public TMP_Text harfYazisi;
    public Image kupGoruntusu;

    [HideInInspector]
    public char mevcutHarf;

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
    }

    /// <summary>
    /// Harfi ve sprite'ı atar (YENİ - sprite bazlı)
    /// </summary>
    public void VeriAta(char gelenHarf, Sprite gelenSprite)
    {
        mevcutHarf = gelenHarf;
        
        if (harfYazisi != null)
        {
            harfYazisi.text = gelenHarf.ToString();
        }

        if (kupGoruntusu != null && gelenSprite != null)
        {
            kupGoruntusu.sprite = gelenSprite;
            kupGoruntusu.color = Color.white; // Sprite'ın orijinal renkleri korunsun
        }
    }

    /// <summary>
    /// Eski metod - renk bazlı (geriye uyumluluk için)
    /// </summary>
    public void VeriAta(char gelenHarf, Color gelenRenk)
    {
        mevcutHarf = gelenHarf;
        
        if (harfYazisi != null)
        {
            harfYazisi.text = gelenHarf.ToString();
        }

        if (kupGoruntusu != null)
        {
            kupGoruntusu.color = gelenRenk;
        }
    }
}