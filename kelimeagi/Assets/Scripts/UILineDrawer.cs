using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;

public class UILineDrawer : MonoBehaviour
{
    [Header("UI Referansları")]
    public TMP_Text puanYazisi;
    public Canvas anaCanvas;

    [Header("Oyun Ayarları")]
    public int minKelimeUzunluk = 3; // Minimum 3 harf gerekli
    public float swapSuresi = 0.25f;
    public float patlamaSuresi = 0.3f;

    // Seçili küp
    private KupData seciliKup = null;
    private RectTransform canvasRect;
    private GridGenerator gridGen;
    private GameObject hataPaneli;
    
    // Puan
    private int toplamPuan = 0;
    
    // Animasyon kilidi
    private bool animasyonDevam = false;

    void Start()
    {
        if (anaCanvas != null)
        {
            canvasRect = anaCanvas.GetComponent<RectTransform>();
        }
        else
        {
            anaCanvas = FindAnyObjectByType<Canvas>();
            if (anaCanvas != null)
            {
                canvasRect = anaCanvas.GetComponent<RectTransform>();
            }
        }
        
        gridGen = FindAnyObjectByType<GridGenerator>();
        GuncellePuanYazisi();
    }

    void Update()
    {
        if (animasyonDevam) return;
        
        bool tiklandiMi = false;

        if (Touchscreen.current != null)
        {
            tiklandiMi = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        }
        else if (Mouse.current != null)
        {
            tiklandiMi = Mouse.current.leftButton.wasPressedThisFrame;
        }

        if (tiklandiMi)
        {
            KupaTiklandi();
        }
    }

    Vector2 GetPointerPosition()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
        return Vector2.zero;
    }

    void KupaTiklandi()
    {
        KupData tiklananKup = TiklananKupuBul();
        
        if (tiklananKup == null) return;
        
        if (seciliKup == null)
        {
            SecKupu(tiklananKup);
        }
        else if (seciliKup == tiklananKup)
        {
            SecimKaldir();
        }
        else
        {
            if (gridGen != null && gridGen.KomsuMu(seciliKup, tiklananKup))
            {
                StartCoroutine(SwapYap(seciliKup, tiklananKup));
            }
            else
            {
                SecimKaldir();
                SecKupu(tiklananKup);
            }
        }
    }

    void SecKupu(KupData kup)
    {
        seciliKup = kup;
        
        if (kup.kupGoruntusu != null)
        {
            kup.kupGoruntusu.color = new Color(1f, 0.9f, 0.5f, 1f);
        }
        
        RectTransform rect = kup.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one * 1.1f;
        }
    }

    void SecimKaldir()
    {
        if (seciliKup != null)
        {
            if (seciliKup.kupGoruntusu != null)
            {
                seciliKup.kupGoruntusu.color = Color.white;
            }
            
            RectTransform rect = seciliKup.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.one;
            }
            
            seciliKup = null;
        }
    }

    KupData TiklananKupuBul()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = GetPointerPosition();

        List<RaycastResult> sonuclar = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, sonuclar);

        foreach (RaycastResult sonuc in sonuclar)
        {
            KupData kup = sonuc.gameObject.GetComponent<KupData>();
            if (kup == null)
            {
                kup = sonuc.gameObject.GetComponentInParent<KupData>();
            }
            
            if (kup != null)
            {
                return kup;
            }
        }

        return null;
    }

    // ==================== SWAP MEKANİĞİ ====================

    System.Collections.IEnumerator SwapYap(KupData kup1, KupData kup2)
    {
        animasyonDevam = true;
        SecimKaldir();
        
        // Swap pozisyonlarını kaydet
        Vector2Int pos1 = gridGen.GetKupPosition(kup1);
        Vector2Int pos2 = gridGen.GetKupPosition(kup2);
        
        // Harfleri swap et
        char tempHarf = kup1.mevcutHarf;
        kup1.mevcutHarf = kup2.mevcutHarf;
        kup2.mevcutHarf = tempHarf;
        
        if (kup1.harfYazisi != null) kup1.harfYazisi.text = kup1.mevcutHarf.ToString();
        if (kup2.harfYazisi != null) kup2.harfYazisi.text = kup2.mevcutHarf.ToString();
        
        // Görsel swap animasyonu
        RectTransform rect1 = kup1.GetComponent<RectTransform>();
        RectTransform rect2 = kup2.GetComponent<RectTransform>();
        Vector2 visualPos1 = rect1.anchoredPosition;
        Vector2 visualPos2 = rect2.anchoredPosition;
        
        yield return StartCoroutine(SwapAnimasyonu(rect1, rect2, visualPos1, visualPos2));
        
        // Pozisyonları geri al (harfler değişti, küpler aynı yerde)
        rect1.anchoredPosition = visualPos1;
        rect2.anchoredPosition = visualPos2;
        
        // SADECE swap yapılan satır ve sütunları kontrol et!
        HashSet<int> kontrolSatirlari = new HashSet<int> { pos1.x, pos2.x };
        HashSet<int> kontrolSutunlari = new HashSet<int> { pos1.y, pos2.y };
        
        List<KupData> bulunanKelime = KelimeBul(kontrolSatirlari, kontrolSutunlari);
        
        if (bulunanKelime.Count >= minKelimeUzunluk)
        {
            // Kelime bulundu!
            Debug.Log($"✓ {bulunanKelime.Count} harfli kelime bulundu!");
            yield return StartCoroutine(KupleriPatlat(bulunanKelime));
        }
        else
        {
            // Kelime yok, geri al
            Debug.Log("✗ Kelime bulunamadı");
            
            tempHarf = kup1.mevcutHarf;
            kup1.mevcutHarf = kup2.mevcutHarf;
            kup2.mevcutHarf = tempHarf;
            
            if (kup1.harfYazisi != null) kup1.harfYazisi.text = kup1.mevcutHarf.ToString();
            if (kup2.harfYazisi != null) kup2.harfYazisi.text = kup2.mevcutHarf.ToString();
            
            StartCoroutine(YanlisSwapEfekti(kup1, kup2));
        }
        
        animasyonDevam = false;
    }

    System.Collections.IEnumerator SwapAnimasyonu(RectTransform rect1, RectTransform rect2, Vector2 pos1, Vector2 pos2)
    {
        float gecen = 0f;
        
        while (gecen < swapSuresi)
        {
            gecen += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, gecen / swapSuresi);
            
            rect1.anchoredPosition = Vector2.Lerp(pos1, pos2, t);
            rect2.anchoredPosition = Vector2.Lerp(pos2, pos1, t);
            
            yield return null;
        }
    }

    // ==================== KELİME BULMA ====================
    
    // TÜM satır ve sütunlarda kelime ara (COMBO için)
    List<KupData> KelimeBul(HashSet<int> satirlar, HashSet<int> sutunlar)
    {
        if (gridGen == null) return new List<KupData>();
        
        // TÜM bulunan kelimelerin küplerini topla (HashSet ile tekrarları önle)
        HashSet<KupData> tumBulunanKupler = new HashSet<KupData>();
        List<string> bulunanKelimeler = new List<string>();
        
        // Belirtilen satırları kontrol et (YATAY kelimeler)
        foreach (int satir in satirlar)
        {
            List<KupData> satirKupleri = gridGen.GetSatirdakiKupler(satir);
            var (kelimeKupleri, kelimeStr) = EnUzunKelimeyiBul(satirKupleri);
            
            if (kelimeKupleri.Count >= minKelimeUzunluk)
            {
                foreach (var kup in kelimeKupleri)
                {
                    tumBulunanKupler.Add(kup);
                }
                bulunanKelimeler.Add(kelimeStr + " (yatay)");
            }
        }
        
        // Belirtilen sütunları kontrol et (DİKEY kelimeler)
        foreach (int sutun in sutunlar)
        {
            List<KupData> sutunKupleri = gridGen.GetSutundakiKupler(sutun);
            var (kelimeKupleri, kelimeStr) = EnUzunKelimeyiBul(sutunKupleri);
            
            if (kelimeKupleri.Count >= minKelimeUzunluk)
            {
                foreach (var kup in kelimeKupleri)
                {
                    tumBulunanKupler.Add(kup);
                }
                bulunanKelimeler.Add(kelimeStr + " (dikey)");
            }
        }
        
        // Bulunan kelimeleri logla
        if (bulunanKelimeler.Count > 0)
        {
            string comboText = bulunanKelimeler.Count > 1 ? " 🔥 COMBO!" : "";
            Debug.Log($"Kelime(ler) bulundu: {string.Join(", ", bulunanKelimeler)}{comboText}");
        }
        
        return new List<KupData>(tumBulunanKupler);
    }
    
    // Bir satır/sütunda en uzun geçerli kelimeyi bul
    (List<KupData>, string) EnUzunKelimeyiBul(List<KupData> kupler)
    {
        List<KupData> enUzunKupleri = new List<KupData>();
        string enUzunKelime = "";
        
        if (kupler.Count < minKelimeUzunluk) return (enUzunKupleri, enUzunKelime);
        
        // String oluştur
        string harfler = "";
        foreach (var kup in kupler)
        {
            harfler += kup.mevcutHarf;
        }
        harfler = harfler.ToUpper();
        
        // En uzun kelimeden başla, kısa kelimelere doğru git
        for (int uzunluk = kupler.Count; uzunluk >= minKelimeUzunluk; uzunluk--)
        {
            for (int baslangic = 0; baslangic <= kupler.Count - uzunluk; baslangic++)
            {
                string altString = harfler.Substring(baslangic, uzunluk);
                
                if (KelimeVeritabani.Instance != null && KelimeVeritabani.Instance.KelimeGecerliMi(altString))
                {
                    // Bu kelimeyi döndür (en uzun bulundu)
                    List<KupData> kelimeKupleri = new List<KupData>();
                    for (int i = baslangic; i < baslangic + uzunluk; i++)
                    {
                        kelimeKupleri.Add(kupler[i]);
                    }
                    return (kelimeKupleri, altString);
                }
            }
        }
        
        return (enUzunKupleri, enUzunKelime);
    }

    // ==================== PATLAMA ====================

    System.Collections.IEnumerator KupleriPatlat(List<KupData> patlayanKupler)
    {
        // Kelimeyi oluştur
        string kelime = "";
        foreach (KupData kup in patlayanKupler)
        {
            if (kup != null) kelime += kup.mevcutHarf;
        }
        Debug.Log($"Kelime: {kelime}");
        
        // Harf puanlarını topla
        int harfPuanlari = 0;
        foreach (KupData kup in patlayanKupler)
        {
            if (kup != null)
            {
                harfPuanlari += kup.mevcutPuan;
            }
        }
        
        // Kelime bonusu
        int kelimeBonus = patlayanKupler.Count >= 4 ? patlayanKupler.Count * 2 : 0;
        int kazanilanPuan = harfPuanlari + kelimeBonus;
        
        // 1. Harfleri ekranın altına topla
        yield return StartCoroutine(HarfleriTopla(patlayanKupler));
        
        // 2. Kelimeyi göster ve puan ekle
        yield return StartCoroutine(KelimeyiGoster(kelime, kazanilanPuan));
        
        // 3. Patlama
        foreach (KupData kup in patlayanKupler)
        {
            if (kup != null)
            {
                StartCoroutine(KupPatlamaAnimasyonu(kup));
            }
        }
        
        toplamPuan += kazanilanPuan;
        GuncellePuanYazisi();
        
        yield return new WaitForSeconds(patlamaSuresi + 0.1f);
        
        // 4. Küpleri eski yerlerine döndür ve yeni harfler ver
        foreach (KupData kup in patlayanKupler)
        {
            if (kup != null)
            {
                kup.gameObject.SetActive(true);
                
                RectTransform rect = kup.GetComponent<RectTransform>();
                
                // Eski pozisyona dön
                OrijinalPozisyon orijPoz = kup.GetComponent<OrijinalPozisyon>();
                if (orijPoz != null)
                {
                    rect.anchoredPosition = orijPoz.pozisyon;
                    Destroy(orijPoz);
                }
                
                rect.localScale = Vector3.zero;
                rect.localRotation = Quaternion.identity;
                
                char yeniHarf = gridGen.RastgeleHarfAl();
                Color yeniRenk = gridGen.RastgeleRenkAl();
                kup.VeriAta(yeniHarf, yeniRenk);
                
                if (kup.kupGoruntusu != null)
                {
                    Color c = kup.kupGoruntusu.color;
                    c.a = 1f;
                    kup.kupGoruntusu.color = c;
                }
                if (kup.harfYazisi != null)
                {
                    Color c = kup.harfYazisi.color;
                    c.a = 1f;
                    kup.harfYazisi.color = c;
                }
                
                StartCoroutine(KupBelirmeAnimasyonu(kup));
            }
        }
    }

    // Harfleri ekranın altına topla
    System.Collections.IEnumerator HarfleriTopla(List<KupData> kupler)
    {
        // Her küpün orijinal pozisyonunu kaydet
        Dictionary<KupData, Vector2> orijinalPozisyonlar = new Dictionary<KupData, Vector2>();
        float ortalamaX = 0f;
        float minY = float.MaxValue;
        
        foreach (KupData kup in kupler)
        {
            if (kup != null)
            {
                RectTransform rect = kup.GetComponent<RectTransform>();
                Vector2 pos = rect.anchoredPosition;
                orijinalPozisyonlar[kup] = pos;
                ortalamaX += pos.x;
                if (pos.y < minY) minY = pos.y;
            }
        }
        
        if (kupler.Count > 0)
            ortalamaX /= kupler.Count;
        
        // Hedef: Mevcut pozisyonların 500 piksel ALTINDA, yatayda ortalanmış
        float hedefY = minY - 500f;
        
        float sure = 0.4f;
        float gecen = 0f;
        
        while (gecen < sure)
        {
            gecen += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, gecen / sure);
            
            int index = 0;
            foreach (KupData kup in kupler)
            {
                if (kup != null && orijinalPozisyonlar.ContainsKey(kup))
                {
                    RectTransform rect = kup.GetComponent<RectTransform>();
                    
                    // Her harf yan yana dizilsin
                    float xOffset = (index - (kupler.Count - 1) / 2f) * 80;
                    Vector2 kupHedef = new Vector2(ortalamaX + xOffset, hedefY);
                    
                    rect.anchoredPosition = Vector2.Lerp(orijinalPozisyonlar[kup], kupHedef, t);
                    
                    // Biraz büyüt
                    rect.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.2f, t);
                    
                    index++;
                }
            }
            
            yield return null;
        }
        
        // Orijinal pozisyonları sakla (geri dönüş için)
        foreach (var kvp in orijinalPozisyonlar)
        {
            if (kvp.Key != null)
            {
                kvp.Key.gameObject.AddComponent<OrijinalPozisyon>().pozisyon = kvp.Value;
            }
        }
    }

    // Kelimeyi ve puanı göster
    System.Collections.IEnumerator KelimeyiGoster(string kelime, int puan)
    {
        // Kelime yazısı oluştur
        GameObject kelimeObj = new GameObject("KelimeGosterge");
        kelimeObj.transform.SetParent(canvasRect, false);
        
        TMP_Text kelimeText = kelimeObj.AddComponent<TMP_Text>();
        kelimeText.text = $"{kelime}\n+{puan}";
        kelimeText.fontSize = 48;
        kelimeText.fontStyle = FontStyles.Bold;
        kelimeText.color = new Color(1f, 1f, 0f, 1f); // Sarı
        kelimeText.alignment = TextAlignmentOptions.Center;
        
        RectTransform rect = kelimeObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -200);
        rect.sizeDelta = new Vector2(400, 150);
        
        // Yeşil efekt
        StartCoroutine(EkraniRenklendir(new Color(0f, 1f, 0f, 1f)));
        
        // Animasyon - büyü ve kaybol
        float sure = 0.8f;
        float gecen = 0f;
        
        while (gecen < sure)
        {
            gecen += Time.deltaTime;
            float t = gecen / sure;
            
            // Büyüme
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.3f;
            rect.localScale = Vector3.one * scale;
            
            // Son kısımda kaybol
            if (t > 0.6f)
            {
                float alpha = 1f - (t - 0.6f) / 0.4f;
                kelimeText.color = new Color(1f, 1f, 0f, alpha);
            }
            
            yield return null;
        }
        
        Destroy(kelimeObj);
    }

    System.Collections.IEnumerator KupPatlamaAnimasyonu(KupData kup)
    {
        RectTransform rect = kup.GetComponent<RectTransform>();
        Image kupImg = kup.kupGoruntusu;
        TMP_Text harfText = kup.harfYazisi;
        
        float gecen = 0f;
        Vector3 baslangicOlcek = rect.localScale;
        
        while (gecen < patlamaSuresi)
        {
            gecen += Time.deltaTime;
            float t = gecen / patlamaSuresi;
            
            float olcek = Mathf.Lerp(1f, 1.5f, t);
            rect.localScale = baslangicOlcek * olcek;
            rect.localRotation = Quaternion.Euler(0, 0, t * 90f);
            
            float alpha = Mathf.Lerp(1f, 0f, t);
            if (kupImg != null)
            {
                Color c = kupImg.color;
                c.a = alpha;
                kupImg.color = c;
            }
            if (harfText != null)
            {
                Color c = harfText.color;
                c.a = alpha;
                harfText.color = c;
            }
            
            yield return null;
        }
        
        kup.gameObject.SetActive(false);
    }

    System.Collections.IEnumerator KupBelirmeAnimasyonu(KupData kup)
    {
        RectTransform rect = kup.GetComponent<RectTransform>();
        
        float sure = 0.3f;
        float gecen = 0f;
        
        while (gecen < sure)
        {
            gecen += Time.deltaTime;
            float t = gecen / sure;
            
            float scale = t < 0.7f 
                ? Mathf.Lerp(0f, 1.15f, t / 0.7f)
                : Mathf.Lerp(1.15f, 1f, (t - 0.7f) / 0.3f);
            
            rect.localScale = Vector3.one * scale;
            yield return null;
        }
        
        rect.localScale = Vector3.one;
    }

    // ==================== EFEKTLER ====================

    System.Collections.IEnumerator YanlisSwapEfekti(KupData kup1, KupData kup2)
    {
        StartCoroutine(EkraniRenklendir(new Color(1f, 0f, 0f, 1f)));
        
        for (int i = 0; i < 2; i++)
        {
            if (kup1.kupGoruntusu != null) kup1.kupGoruntusu.color = new Color(1f, 0.4f, 0.4f, 1f);
            if (kup2.kupGoruntusu != null) kup2.kupGoruntusu.color = new Color(1f, 0.4f, 0.4f, 1f);
            yield return new WaitForSeconds(0.08f);
            
            if (kup1.kupGoruntusu != null) kup1.kupGoruntusu.color = Color.white;
            if (kup2.kupGoruntusu != null) kup2.kupGoruntusu.color = Color.white;
            yield return new WaitForSeconds(0.08f);
        }
    }

    System.Collections.IEnumerator EkraniRenklendir(Color hedefRenk)
    {
        if (hataPaneli == null) HataPaneliOlustur();
        
        Image img = hataPaneli.GetComponent<Image>();
        if (img == null) yield break;

        hataPaneli.SetActive(true);

        float sure = 0.1f;
        float gecen = 0f;

        while (gecen < sure)
        {
            gecen += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 0.25f, gecen / sure);
            img.color = new Color(hedefRenk.r, hedefRenk.g, hedefRenk.b, alpha);
            yield return null;
        }

        sure = 0.15f;
        gecen = 0f;
        while (gecen < sure)
        {
            gecen += Time.deltaTime;
            float alpha = Mathf.Lerp(0.25f, 0f, gecen / sure);
            img.color = new Color(hedefRenk.r, hedefRenk.g, hedefRenk.b, alpha);
            yield return null;
        }

        hataPaneli.SetActive(false);
    }

    void HataPaneliOlustur()
    {
        if (canvasRect == null) return;

        hataPaneli = new GameObject("HataPaneli");
        hataPaneli.transform.SetParent(canvasRect, false);
        hataPaneli.transform.SetAsLastSibling();

        Image img = hataPaneli.AddComponent<Image>();
        img.color = new Color(1f, 0f, 0f, 0f);
        img.raycastTarget = false;

        RectTransform rect = hataPaneli.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        
        hataPaneli.SetActive(false);
    }

    void GuncellePuanYazisi()
    {
        if (puanYazisi != null)
        {
            puanYazisi.text = $"Puan: {toplamPuan}";
        }
    }
}
