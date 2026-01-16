using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GridGenerator : MonoBehaviour
{
    [Header("Ayarlar")]
    public GameObject kupPrefabi;
    public Transform gridKutusu;
    
    [Header("Renk Paleti")]
    public Color[] renkListesi;
    
    [Header("Animasyon Ayarları")]
    public float slotAnimasyonSuresi = 1.5f;
    public float satirGecikme = 0.1f;
    public int slotDonusSayisi = 3;
    
    [Header("Grid Ayarları")]
    public int satirSayisi = 5;
    public int sutunSayisi = 5;
    public int minKelimeSayisi = 3; // Minimum oluşturulabilir kelime sayısı

    // Oluşturulan küpler
    private List<KupData> tumKupler = new List<KupData>();
    private char[] mevcutHarfler;
    private bool animasyonDevam = false;

    void Start()
    {
        // Yönetici scriptlerinin yüklenmesini bekle
        StartCoroutine(BaslangicGecikme());
    }

    IEnumerator BaslangicGecikme()
    {
        yield return new WaitForSeconds(0.1f);
        TahtayiOlustur();
    }

    void TahtayiOlustur()
    {
        int toplamKup = satirSayisi * sutunSayisi;
        
        // Küpleri oluştur
        for (int i = 0; i < toplamKup; i++)
        {
            GameObject yeniKup = Instantiate(kupPrefabi);
            yeniKup.transform.SetParent(gridKutusu, false);
            
            KupData veriScripti = yeniKup.GetComponent<KupData>();
            if (veriScripti != null)
            {
                tumKupler.Add(veriScripti);
            }
        }
        
        // İlk harfleri ata
        HarfleriYenidenOlustur();
    }

    /// <summary>
    /// Harfleri yeniden oluşturur ve kelime kontrolü yapar
    /// </summary>
    public void HarfleriYenidenOlustur()
    {
        if (animasyonDevam) return;
        
        StartCoroutine(SlotMakinesAnimasyonu());
    }

    IEnumerator SlotMakinesAnimasyonu()
    {
        animasyonDevam = true;
        int maxDeneme = 10;
        int denemeSayisi = 0;
        bool gecerliGrid = false;
        
        do
        {
            denemeSayisi++;
            
            // Yeni harfler üret
            if (HarfYoneticisi.Instance != null)
            {
                mevcutHarfler = HarfYoneticisi.Instance.GridIcinHarfUret(tumKupler.Count, 7);
            }
            else
            {
                // Yedek: rastgele harf
                mevcutHarfler = RastgeleHarfUret(tumKupler.Count);
            }
            
            // Kelime kontrolü yap
            if (KelimeVeritabani.Instance != null)
            {
                gecerliGrid = KelimeVeritabani.Instance.GridGecerliMi(mevcutHarfler, minKelimeSayisi);
            }
            else
            {
                gecerliGrid = true; // Veritabanı yoksa geçerli kabul et
            }
            
            if (!gecerliGrid)
            {
                Debug.Log($"Grid geçersiz (Deneme {denemeSayisi}), yeniden oluşturuluyor...");
            }
            
        } while (!gecerliGrid && denemeSayisi < maxDeneme);
        
        if (!gecerliGrid)
        {
            Debug.LogWarning("Maksimum deneme sayısına ulaşıldı, grid kabul edildi.");
        }
        
        // Slot makinesi animasyonu - satır satır
        for (int satir = 0; satir < satirSayisi; satir++)
        {
            // Bu satırdaki küpleri bul
            List<KupData> satirKupleri = new List<KupData>();
            for (int sutun = 0; sutun < sutunSayisi; sutun++)
            {
                int index = satir * sutunSayisi + sutun;
                if (index < tumKupler.Count)
                {
                    satirKupleri.Add(tumKupler[index]);
                }
            }
            
            // Bu satır için slot animasyonu başlat
            StartCoroutine(SatirSlotAnimasyonu(satirKupleri, satir));
            
            // Bir sonraki satır için bekle
            yield return new WaitForSeconds(satirGecikme);
        }
        
        // Tüm animasyonların bitmesini bekle
        yield return new WaitForSeconds(slotAnimasyonSuresi);
        
        animasyonDevam = false;
        Debug.Log("Grid oluşturuldu!");
    }

    IEnumerator SatirSlotAnimasyonu(List<KupData> kupler, int satirIndex)
    {
        float gecenSure = 0f;
        int donusSayaci = 0;
        string alfabe = "ABCÇDEFGĞHIİJKLMNOÖPRSŞTUÜVYZ";
        
        // Slot döndürme animasyonu
        while (gecenSure < slotAnimasyonSuresi)
        {
            gecenSure += Time.deltaTime;
            
            // Animasyon hızı (başta hızlı, sona doğru yavaşlar)
            float hiz = Mathf.Lerp(0.05f, 0.3f, gecenSure / slotAnimasyonSuresi);
            
            if (Time.time % hiz < Time.deltaTime)
            {
                donusSayaci++;
                
                foreach (KupData kup in kupler)
                {
                    // Rastgele harf göster (dönerken)
                    char rastgeleHarf = alfabe[Random.Range(0, alfabe.Length)];
                    Color rastgeleRenk = RastgeleRenkSec();
                    kup.VeriAta(rastgeleHarf, rastgeleRenk);
                }
            }
            
            yield return null;
        }
        
        // Son harfleri ata
        for (int i = 0; i < kupler.Count; i++)
        {
            int globalIndex = satirIndex * sutunSayisi + i;
            if (globalIndex < mevcutHarfler.Length)
            {
                char sonHarf = mevcutHarfler[globalIndex];
                Color sonRenk = RastgeleRenkSec();
                
                // Son atama animasyonu (hafif büyüme efekti)
                StartCoroutine(SonAtamaAnimasyonu(kupler[i], sonHarf, sonRenk));
            }
        }
    }

    IEnumerator SonAtamaAnimasyonu(KupData kup, char harf, Color renk)
    {
        RectTransform rect = kup.GetComponent<RectTransform>();
        Vector3 orijinalOlcek = rect.localScale;
        
        // Küçül
        rect.localScale = orijinalOlcek * 0.8f;
        
        // Harfi ata
        kup.VeriAta(harf, renk);
        
        // Büyü (bounce efekti)
        float sure = 0.2f;
        float gecen = 0f;
        
        while (gecen < sure)
        {
            gecen += Time.deltaTime;
            float t = gecen / sure;
            
            // Bounce easing
            float bounce = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
            rect.localScale = orijinalOlcek * bounce;
            
            yield return null;
        }
        
        rect.localScale = orijinalOlcek;
    }

    Color RastgeleRenkSec()
    {
        if (renkListesi != null && renkListesi.Length > 0)
        {
            Color renk = renkListesi[Random.Range(0, renkListesi.Length)];
            if (renk.a < 0.01f) renk.a = 1f;
            return renk;
        }
        return Color.white;
    }

    char[] RastgeleHarfUret(int adet)
    {
        string alfabe = "ABCÇDEFGĞHIİJKLMNOÖPRSŞTUÜVYZ";
        char[] harfler = new char[adet];
        
        for (int i = 0; i < adet; i++)
        {
            harfler[i] = alfabe[Random.Range(0, alfabe.Length)];
        }
        
        return harfler;
    }

    /// <summary>
    /// Mevcut harfleri döndürür
    /// </summary>
    public char[] MevcutHarfleriAl()
    {
        return mevcutHarfler;
    }

    /// <summary>
    /// Grid'i yeniler (dışarıdan çağrılabilir)
    /// </summary>
    public void GridiYenile()
    {
        HarfleriYenidenOlustur();
    }
}