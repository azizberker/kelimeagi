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
    public Color[] renkListesi; // Rastgele atanacak renkler
    
    [Header("Slot Animasyon Ayarları")]
    public float slotAnimasyonSuresi = 2f;
    public float sutunGecikme = 0.2f;
    public float harfDegismeSuresi = 0.06f;
    
    [Header("Grid Ayarları")]
    public int satirSayisi = 5;
    public int sutunSayisi = 5;
    public int minKelimeSayisi = 3;
    public Vector2 kupBoyutu = new Vector2(100, 100);

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
            
            // Küp boyutunu ayarla
            RectTransform kupRect = yeniKup.GetComponent<RectTransform>();
            if (kupRect != null)
            {
                kupRect.sizeDelta = kupBoyutu;
            }
            
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
        
        for (int sutun = 0; sutun < sutunSayisi; sutun++)
        {
            StartCoroutine(SutunSlotAnimasyonu(sutun));
            yield return new WaitForSeconds(sutunGecikme);
        }
        
        yield return new WaitForSeconds(slotAnimasyonSuresi + 0.3f);
        
        animasyonDevam = false;
        Debug.Log("Grid oluşturuldu!");
    }

    IEnumerator SutunSlotAnimasyonu(int sutunIndex)
    {
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
        
        while (gecenSure < slotAnimasyonSuresi)
        {
            gecenSure += Time.deltaTime;
            float ilerleme = gecenSure / slotAnimasyonSuresi;
            float hizCarpani = Mathf.Lerp(1f, 0.05f, ilerleme * ilerleme);
            float dinamikHarfSuresi = harfDegismeSuresi / hizCarpani;
            
            if (gecenSure - sonHarfDegisme > dinamikHarfSuresi)
            {
                sonHarfDegisme = gecenSure;
                
                for (int i = 0; i < sutunKupleri.Count; i++)
                {
                    char rastgeleHarf = alfabe[Random.Range(0, alfabe.Length)];
                    Color rastgeleRenk = RastgeleRenkSec();
                    sutunKupleri[i].VeriAta(rastgeleHarf, rastgeleRenk);
                    
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
        
        kup.VeriAta(harf, renk);
        
        float sure = 0.25f;
        float gecen = 0f;
        
        rect.localScale = Vector3.one * 0.7f;
        
        while (gecen < sure)
        {
            gecen += Time.deltaTime;
            float t = gecen / sure;
            
            float scale;
            if (t < 0.5f)
            {
                scale = Mathf.Lerp(0.7f, 1.15f, t * 2f);
            }
            else
            {
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

    // UILineDrawer için yardımcı fonksiyonlar
    public KupData GetKupAtIndex(int index)
    {
        if (index >= 0 && index < tumKupler.Count)
        {
            return tumKupler[index];
        }
        return null;
    }

    public char RastgeleHarfAl()
    {
        if (HarfYoneticisi.Instance != null)
        {
            char[] harfler = HarfYoneticisi.Instance.GridIcinHarfUret(1, 0);
            return harfler[0];
        }
        return alfabe[Random.Range(0, alfabe.Length)];
    }

    public Color RastgeleRenkAl()
    {
        return RastgeleRenkSec();
    }

    public List<KupData> TumKuplerAl()
    {
        return tumKupler;
    }
}