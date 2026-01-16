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
    
    [Header("Slot Animasyon Ayarları")]
    public float slotAnimasyonSuresi = 2f;
    public float sutunGecikme = 0.2f;
    public float harfDegismeSuresi = 0.06f;
    
    [Header("Grid Ayarları")]
    public int satirSayisi = 5;
    public int sutunSayisi = 5;
    public int minKelimeSayisi = 3;

    private List<KupData> tumKupler = new List<KupData>();
    private char[] mevcutHarfler;
    private bool animasyonDevam = false;
    private string alfabe = "ABCÇDEFGĞHIİJKLMNOÖPRSŞTUÜVYZ";

    void Start()
    {
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
        
        for (int i = 0; i < toplamKup; i++)
        {
            GameObject yeniKup = Instantiate(kupPrefabi);
            yeniKup.transform.SetParent(gridKutusu, false);
            
            KupData veriScripti = yeniKup.GetComponent<KupData>();
            if (veriScripti != null)
            {
                tumKupler.Add(veriScripti);
                
                // Başlangıç rengi ata
                Color baslangicRenk = RastgeleRenkSec();
                char baslangicHarf = alfabe[Random.Range(0, alfabe.Length)];
                veriScripti.VeriAta(baslangicHarf, baslangicRenk);
            }
        }
        
        // Slot animasyonu ile başla
        HarfleriYenidenOlustur();
    }

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
        
        // Geçerli harfler üret
        do
        {
            denemeSayisi++;
            
            if (HarfYoneticisi.Instance != null)
            {
                mevcutHarfler = HarfYoneticisi.Instance.GridIcinHarfUret(tumKupler.Count, 7);
            }
            else
            {
                mevcutHarfler = RastgeleHarfUret(tumKupler.Count);
            }
            
            if (KelimeVeritabani.Instance != null)
            {
                gecerliGrid = KelimeVeritabani.Instance.GridGecerliMi(mevcutHarfler, minKelimeSayisi);
            }
            else
            {
                gecerliGrid = true;
            }
            
        } while (!gecerliGrid && denemeSayisi < maxDeneme);
        
        // Her sütun için slot animasyonu başlat
        for (int sutun = 0; sutun < sutunSayisi; sutun++)
        {
            StartCoroutine(SutunSlotAnimasyonu(sutun));
            yield return new WaitForSeconds(sutunGecikme);
        }
        
        // Animasyonların bitmesini bekle
        yield return new WaitForSeconds(slotAnimasyonSuresi + 0.3f);
        
        animasyonDevam = false;
        Debug.Log("Grid oluşturuldu!");
    }

    IEnumerator SutunSlotAnimasyonu(int sutunIndex)
    {
        // Bu sütundaki küpleri bul
        List<KupData> sutunKupleri = new List<KupData>();
        
        for (int satir = 0; satir < satirSayisi; satir++)
        {
            int index = satir * sutunSayisi + sutunIndex;
            if (index < tumKupler.Count)
            {
                sutunKupleri.Add(tumKupler[index]);
            }
        }
        
        float gecenSure = 0f;
        float sonHarfDegisme = 0f;
        
        // Slot dönme animasyonu (sadece harf ve renk değişir, pozisyon değişmez)
        while (gecenSure < slotAnimasyonSuresi)
        {
            gecenSure += Time.deltaTime;
            float ilerleme = gecenSure / slotAnimasyonSuresi;
            
            // Yavaşlama eğrisi (başta hızlı, sonda yavaş)
            float hizCarpani = Mathf.Lerp(1f, 0.05f, ilerleme * ilerleme);
            float dinamikHarfSuresi = harfDegismeSuresi / hizCarpani;
            
            // Harfleri değiştir
            if (gecenSure - sonHarfDegisme > dinamikHarfSuresi)
            {
                sonHarfDegisme = gecenSure;
                
                for (int i = 0; i < sutunKupleri.Count; i++)
                {
                    // Rastgele harf ve renk
                    char rastgeleHarf = alfabe[Random.Range(0, alfabe.Length)];
                    Color rastgeleRenk = RastgeleRenkSec();
                    sutunKupleri[i].VeriAta(rastgeleHarf, rastgeleRenk);
                    
                    // Küçük ölçek efekti (titreme)
                    RectTransform rect = sutunKupleri[i].GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        float scale = 1f + Random.Range(-0.05f, 0.05f) * hizCarpani;
                        rect.localScale = Vector3.one * scale;
                    }
                }
            }
            
            yield return null;
        }
        
        // Son harfleri ata ve yerleşme animasyonu
        for (int i = 0; i < sutunKupleri.Count; i++)
        {
            int globalIndex = i * sutunSayisi + sutunIndex;
            
            if (globalIndex < mevcutHarfler.Length)
            {
                StartCoroutine(SonYerlesmeAnimasyonu(sutunKupleri[i], 
                    mevcutHarfler[globalIndex], RastgeleRenkSec()));
            }
        }
    }

    IEnumerator SonYerlesmeAnimasyonu(KupData kup, char harf, Color renk)
    {
        RectTransform rect = kup.GetComponent<RectTransform>();
        
        // Harfi ata
        kup.VeriAta(harf, renk);
        
        // Bounce ölçek animasyonu
        float sure = 0.25f;
        float gecen = 0f;
        
        // Önce küçült
        rect.localScale = Vector3.one * 0.7f;
        
        while (gecen < sure)
        {
            gecen += Time.deltaTime;
            float t = gecen / sure;
            
            // Elastic/Bounce easing
            float scale;
            if (t < 0.5f)
            {
                // Büyüme
                scale = Mathf.Lerp(0.7f, 1.15f, t * 2f);
            }
            else
            {
                // Yerine oturma
                float bounceT = (t - 0.5f) * 2f;
                scale = Mathf.Lerp(1.15f, 1f, bounceT);
            }
            
            rect.localScale = Vector3.one * scale;
            yield return null;
        }
        
        rect.localScale = Vector3.one;
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
        char[] harfler = new char[adet];
        for (int i = 0; i < adet; i++)
        {
            harfler[i] = alfabe[Random.Range(0, alfabe.Length)];
        }
        return harfler;
    }

    public char[] MevcutHarfleriAl()
    {
        return mevcutHarfler;
    }

    public void GridiYenile()
    {
        HarfleriYenidenOlustur();
    }
}