using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Türkçe harf yönetim sistemi - zorluk ve frekans bazlı harf seçimi
/// </summary>
public class HarfYoneticisi : MonoBehaviour
{
    public static HarfYoneticisi Instance { get; private set; }

    // Sesli harfler
    private static readonly char[] sesliHarfler = { 'A', 'E', 'I', 'İ', 'O', 'Ö', 'U', 'Ü' };
    
    // Harf ağırlıkları (yüksek = sık çıkar)
    private static readonly Dictionary<char, int> harfAgirliklari = new Dictionary<char, int>
    {
        // Çok kolay - çok sık çıkar (ağırlık: 10)
        { 'A', 10 }, { 'E', 10 }, { 'İ', 8 }, { 'I', 6 },
        { 'K', 8 }, { 'L', 8 }, { 'R', 8 }, { 'N', 8 }, { 'T', 7 },
        
        // Kolay - sık çıkar (ağırlık: 6-7)
        { 'M', 6 }, { 'S', 6 }, { 'D', 6 }, { 'Y', 6 }, { 'B', 5 },
        
        // Orta - normal çıkar (ağırlık: 4-5)
        { 'O', 5 }, { 'U', 5 }, { 'C', 4 }, { 'Ö', 4 }, { 'Ü', 4 },
        
        // Zor - az çıkar (ağırlık: 2-3)
        { 'Ç', 3 }, { 'G', 3 }, { 'H', 3 }, { 'P', 3 }, { 'Z', 2 },
        
        // Çok zor - çok az çıkar (ağırlık: 1)
        { 'F', 1 }, { 'Ğ', 1 }, { 'J', 1 }, { 'Ş', 2 }, { 'V', 1 }
    };

    // Toplam ağırlık (önbellek)
    private int toplamAgirlik = 0;
    private List<char> agirlikliHarfListesi = new List<char>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            HazirlaAgirlikliListe();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void HazirlaAgirlikliListe()
    {
        agirlikliHarfListesi.Clear();
        toplamAgirlik = 0;

        foreach (var kvp in harfAgirliklari)
        {
            for (int i = 0; i < kvp.Value; i++)
            {
                agirlikliHarfListesi.Add(kvp.Key);
            }
            toplamAgirlik += kvp.Value;
        }
    }

    /// <summary>
    /// Ağırlıklı rastgele harf seçer (zor harfler daha az çıkar)
    /// </summary>
    public char RastgeleHarfSec()
    {
        int rastgeleIndex = Random.Range(0, agirlikliHarfListesi.Count);
        return agirlikliHarfListesi[rastgeleIndex];
    }

    /// <summary>
    /// Grid için 25 harf üretir (minimum sesli harf garantili)
    /// </summary>
    public char[] GridIcinHarfUret(int adet = 25, int minSesliHarf = 7)
    {
        char[] harfler = new char[adet];
        int sesliSayisi = 0;

        // Önce minimum sesli harfleri yerleştir
        List<int> pozisyonlar = new List<int>();
        for (int i = 0; i < adet; i++) pozisyonlar.Add(i);
        
        // Rastgele pozisyonlara sesli harfleri koy
        for (int i = 0; i < minSesliHarf; i++)
        {
            int rastgelePozIndex = Random.Range(0, pozisyonlar.Count);
            int pozisyon = pozisyonlar[rastgelePozIndex];
            pozisyonlar.RemoveAt(rastgelePozIndex);
            
            harfler[pozisyon] = sesliHarfler[Random.Range(0, sesliHarfler.Length)];
            sesliSayisi++;
        }

        // Kalan pozisyonları ağırlıklı rastgele harflerle doldur
        foreach (int pozisyon in pozisyonlar)
        {
            char yeniHarf = RastgeleHarfSec();
            harfler[pozisyon] = yeniHarf;
            
            if (SesliMi(yeniHarf))
            {
                sesliSayisi++;
            }
        }

        Debug.Log($"Grid oluşturuldu: {adet} harf, {sesliSayisi} sesli harf");
        return harfler;
    }

    /// <summary>
    /// Harfin sesli olup olmadığını kontrol eder
    /// </summary>
    public bool SesliMi(char harf)
    {
        foreach (char sesli in sesliHarfler)
        {
            if (harf == sesli) return true;
        }
        return false;
    }

    /// <summary>
    /// Harf zorluğunu döndürür (1-10, düşük = zor)
    /// </summary>
    public int HarfZorlugu(char harf)
    {
        if (harfAgirliklari.TryGetValue(harf, out int agirlik))
        {
            return agirlik;
        }
        return 5; // Varsayılan orta zorluk
    }

    // ==================== HARF PUAN SİSTEMİ ====================
    
    // Harf puanları (zor harfler = yüksek puan)
    private static readonly Dictionary<char, int> harfPuanlari = new Dictionary<char, int>
    {
        // Çok kolay harfler - 1 puan
        { 'A', 1 }, { 'E', 1 }, { 'İ', 1 }, { 'I', 1 },
        
        // Kolay harfler - 2 puan
        { 'K', 2 }, { 'L', 2 }, { 'R', 2 }, { 'N', 2 }, { 'T', 2 },
        
        // Normal harfler - 3 puan
        { 'M', 3 }, { 'S', 3 }, { 'D', 3 }, { 'Y', 3 }, { 'B', 3 },
        
        // Orta-zor harfler - 4-5 puan
        { 'O', 4 }, { 'U', 4 }, { 'C', 4 }, { 'Ö', 5 }, { 'Ü', 5 },
        
        // Zor harfler - 6-7 puan
        { 'Ç', 6 }, { 'G', 6 }, { 'H', 6 }, { 'P', 6 }, { 'Z', 7 },
        
        // Çok zor harfler - 8-9 puan
        { 'F', 8 }, { 'Ğ', 9 }, { 'J', 9 }, { 'Ş', 7 }, { 'V', 8 }
    };

    /// <summary>
    /// Harfin puan değerini döndürür (1-9, kolay = düşük, zor = yüksek)
    /// </summary>
    public int GetHarfPuani(char harf)
    {
        char upperHarf = char.ToUpper(harf);
        if (harfPuanlari.TryGetValue(upperHarf, out int puan))
        {
            return puan;
        }
        return 3; // Varsayılan orta puan
    }
}
