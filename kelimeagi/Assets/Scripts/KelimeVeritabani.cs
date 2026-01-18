using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Türkçe kelime veritabanı - kelime doğrulama ve kontrol sistemi
/// </summary>
public class KelimeVeritabani : MonoBehaviour
{
    public static KelimeVeritabani Instance { get; private set; }

    [Header("Ayarlar")]
    public TextAsset kelimeDosyasi; // Resources'dan yüklenecek kelimeler.txt
    public int maxKelimeUzunlugu = 7;
    public int minKelimeUzunlugu = 2;

    private HashSet<string> kelimeler = new HashSet<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            KelimelerYukle();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void KelimelerYukle()
    {
        if (kelimeDosyasi == null)
        {
            // Resources klasöründen yüklemeyi dene
            kelimeDosyasi = Resources.Load<TextAsset>("kelimeler");
        }

        if (kelimeDosyasi != null)
        {
            string[] satirlar = kelimeDosyasi.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            
            foreach (string satir in satirlar)
            {
                string kelime = satir.Trim().ToUpper();
                if (kelime.Length >= minKelimeUzunlugu && kelime.Length <= maxKelimeUzunlugu)
                {
                    kelimeler.Add(kelime);
                }
            }
            
            Debug.Log($"Kelime veritabanı yüklendi: {kelimeler.Count} kelime");
        }
        else
        {
            Debug.LogWarning("Kelime dosyası bulunamadı! Resources/kelimeler.txt oluşturun.");
            // Varsayılan kelimeler ekle
            VarsayilanKelimeleriEkle();
        }
    }

    void VarsayilanKelimeleriEkle()
    {
        // Temel Türkçe kelimeler (yedek liste)
        string[] temelKelimeler = {
            "EV", "SU", "GÖZ", "EL", "BAŞ", "GÜN", "YOL", "SÖZ", "KAR", "YAZ",
            "KIŞ", "YAĞ", "TAŞ", "DAĞ", "DENİZ", "ORMAN", "KENT", "KÖPRÜ",
            "ARABA", "KAPAK", "KALEM", "KİTAP", "MASA", "KAPI", "PENCERE",
            "ANNE", "BABA", "KARDEŞ", "ARKADAŞ", "OKUL", "SINIF", "ÖĞRETMEN",
            "DOKTOR", "MÜHENDİS", "AVUKAT", "POLİS", "ASKER", "PİLOT",
            "ELMA", "ARMUT", "PORTAKAL", "MUZ", "ÇİLEK", "KİRAZ", "ÜZÜM",
            "EKMEK", "SÜT", "PEYNİR", "YUMURTA", "ET", "BALIK", "TAVUK",
            "KIRMIZI", "MAVİ", "YEŞİL", "SARI", "BEYAZ", "SİYAH", "MOR",
            "BİR", "İKİ", "ÜÇ", "DÖRT", "BEŞ", "ALTI", "YEDİ", "SEKİZ",
            "AY", "GÜN", "YIL", "SAAT", "DAKİKA", "SANİYE", "HAFTA",
            "GÜZEL", "İYİ", "KÖTÜ", "BÜYÜK", "KÜÇÜK", "UZUN", "KISA",
            "SICAK", "SOĞUK", "ILІК", "YENİ", "ESKİ", "GENÇ", "YAŞLI"
        };

        foreach (string kelime in temelKelimeler)
        {
            kelimeler.Add(kelime);
        }
        
        Debug.Log($"Varsayılan kelimeler yüklendi: {kelimeler.Count} kelime");
    }

    /// <summary>
    /// Kelimenin geçerli olup olmadığını kontrol eder
    /// </summary>
    public bool KelimeGecerliMi(string kelime)
    {
        if (string.IsNullOrEmpty(kelime)) return false;
        return kelimeler.Contains(kelime.ToUpper());
    }

    /// <summary>
    /// Verilen harflerden oluşturulabilecek kelimeleri bulur
    /// </summary>
    public List<string> BulunabilecekKelimeler(char[] mevcutHarfler)
    {
        List<string> bulunanKelimeler = new List<string>();
        HashSet<char> harfSeti = new HashSet<char>(mevcutHarfler);

        foreach (string kelime in kelimeler)
        {
            if (KelimeOlusturulabilirMi(kelime, mevcutHarfler))
            {
                bulunanKelimeler.Add(kelime);
            }
        }

        return bulunanKelimeler;
    }

    /// <summary>
    /// Verilen harflerden bu kelime oluşturulabilir mi?
    /// </summary>
    public bool KelimeOlusturulabilirMi(string kelime, char[] mevcutHarfler)
    {
        // Her harfin kaç kez kullanılabilir olduğunu say
        Dictionary<char, int> harfSayilari = new Dictionary<char, int>();
        foreach (char h in mevcutHarfler)
        {
            if (harfSayilari.ContainsKey(h))
                harfSayilari[h]++;
            else
                harfSayilari[h] = 1;
        }

        // Kelimedeki her harfin mevcut olup olmadığını kontrol et
        foreach (char h in kelime.ToUpper())
        {
            if (!harfSayilari.ContainsKey(h) || harfSayilari[h] <= 0)
            {
                return false;
            }
            harfSayilari[h]--;
        }

        return true;
    }

    /// <summary>
    /// Grid'de en az bir geçerli kelime oluşturulabilir mi?
    /// </summary>
    public bool GridGecerliMi(char[] gridHarfleri, int minKelimeSayisi = 3)
    {
        List<string> bulunanlar = BulunabilecekKelimeler(gridHarfleri);
        Debug.Log($"Grid kontrolü: {bulunanlar.Count} kelime bulundu");
        
        if (bulunanlar.Count >= minKelimeSayisi)
        {
            Debug.Log($"Örnek kelimeler: {string.Join(", ", bulunanlar.Take(5))}");
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Toplam kelime sayısını döndürür
    /// </summary>
    public int ToplamKelimeSayisi()
    {
        return kelimeler.Count;
    }
}
