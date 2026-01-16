using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;

public class UILineDrawer : MonoBehaviour
{
    [Header("Çizgi Ayarları")]
    public float cizgiKalinligi = 12f;
    public Color cizgiRengi = new Color(1f, 0.9f, 0f, 1f);
    public Color glowRengi = new Color(1f, 0.6f, 0f, 0.3f);
    
    [Header("Soft Efekt Ayarları")]
    public int glowKatmanSayisi = 3;
    public float glowGenislemeCarpani = 2f;

    [Header("Bekleme Süresi Ayarları")]
    public float kupSecmeSuresi = 0.5f; // Küpün üzerinde ne kadar durulacak (saniye)

    [Header("UI Referansları")]
    public TMP_Text seciliKelimeYazisi;
    public Canvas anaCanvas;

    // Seçilen küpler ve çizgi parçaları
    private List<KupData> seciliKupler = new List<KupData>();
    private List<GameObject> cizgiParcalari = new List<GameObject>();
    private GameObject parmakCizgisi;
    
    private bool suruklemeAktif = false;
    private RectTransform canvasRect;
    
    // Bekleme mekanizması için
    private KupData bekleyenKup = null;
    private float beklemeSuresi = 0f;
    private GameObject beklemeGostergesi = null;
    
    // Trail efekti için
    private List<Vector2> trailNoktalari = new List<Vector2>();
    private List<GameObject> trailParcalari = new List<GameObject>();
    private int maxTrailNokta = 15; // Trail'deki maksimum nokta
    private float minNoktaMesafe = 8f; // Noktalar arası minimum mesafe

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
    }

    void Update()
    {
        bool dokunmaBasladi = false;
        bool dokunmaDevam = false;
        bool dokunmaBitti = false;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            var touch = Touchscreen.current.primaryTouch;
            dokunmaBasladi = touch.press.wasPressedThisFrame;
            dokunmaDevam = touch.press.isPressed && !dokunmaBasladi;
            dokunmaBitti = touch.press.wasReleasedThisFrame;
        }
        else if (Mouse.current != null)
        {
            dokunmaBasladi = Mouse.current.leftButton.wasPressedThisFrame;
            dokunmaDevam = Mouse.current.leftButton.isPressed && !dokunmaBasladi;
            dokunmaBitti = Mouse.current.leftButton.wasReleasedThisFrame;
        }

        if (dokunmaBasladi)
        {
            SuruklemeyeBasla();
        }
        else if (dokunmaDevam && suruklemeAktif)
        {
            SuruklemeDevam();
        }
        else if (dokunmaBitti)
        {
            // Mouse/parmak bırakıldığında her zaman trail'i temizle
            TemizleTrail();
            
            if (suruklemeAktif)
            {
                SuruklemeySonlandir();
            }
        }
        
        // Eğer mouse basılı değilse trail'i temizle
        bool mouseBasili = (Mouse.current != null && Mouse.current.leftButton.isPressed) ||
                          (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed);
        if (!mouseBasili && trailParcalari.Count > 0)
        {
            TemizleTrail();
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

    Vector2 EkranPozisyonunuCanvasaÇevir(Vector2 ekranPoz)
    {
        Vector2 lokalPoz;
        Camera kamera = anaCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : anaCanvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, ekranPoz, kamera, out lokalPoz);
        return lokalPoz;
    }

    Vector2 GetKupCanvasPozisyonu(KupData kup)
    {
        RectTransform kupRect = kup.GetComponent<RectTransform>();
        if (kupRect != null)
        {
            Vector3 dunyaPoz = kupRect.position;
            Camera kamera = anaCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : anaCanvas.worldCamera;
            Vector2 ekranPoz = RectTransformUtility.WorldToScreenPoint(kamera, dunyaPoz);
            return EkranPozisyonunuCanvasaÇevir(ekranPoz);
        }
        return Vector2.zero;
    }

    void SuruklemeyeBasla()
    {
        suruklemeAktif = true;
        TemizleSecim();
        bekleyenKup = null;
        beklemeSuresi = 0f;
    }

    void SuruklemeDevam()
    {
        // Parmağın üzerinde olduğu küpü bul
        KupData uzerindekiKup = TiklananKupuBul();
        
        if (uzerindekiKup != null)
        {
            // Zaten seçilmiş bir küpse, atla
            if (seciliKupler.Contains(uzerindekiKup))
            {
                bekleyenKup = null;
                beklemeSuresi = 0f;
            }
            // Aynı küpün üzerinde duruyorsak, süreyi artır
            else if (bekleyenKup == uzerindekiKup)
            {
                beklemeSuresi += Time.deltaTime;
                
                // Bekleme göstergesini güncelle
                GuncelleBeklemeGostergesi(uzerindekiKup, beklemeSuresi / kupSecmeSuresi);
                
                // Süre dolduğunda küpü seç
                if (beklemeSuresi >= kupSecmeSuresi)
                {
                    KupuSec(uzerindekiKup);
                    bekleyenKup = null;
                    beklemeSuresi = 0f;
                    SilBeklemeGostergesi();
                }
            }
            // Yeni bir küpün üzerine geldik
            else
            {
                bekleyenKup = uzerindekiKup;
                beklemeSuresi = 0f;
                SilBeklemeGostergesi();
            }
        }
        else
        {
            // Küpün üzerinde değiliz
            bekleyenKup = null;
            beklemeSuresi = 0f;
            SilBeklemeGostergesi();
        }

        // Parmağı takip eden çizgiyi güncelle
        GuncelleParmakCizgisi();
        
        // Trail efektini güncelle (Fruit Ninja stili)
        GuncelleTrail();
    }

    void GuncelleBeklemeGostergesi(KupData kup, float ilerleme)
    {
        if (beklemeGostergesi == null)
        {
            beklemeGostergesi = new GameObject("BeklemeGostergesi");
            beklemeGostergesi.transform.SetParent(canvasRect, false);
            
            Image img = beklemeGostergesi.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.5f);
            img.raycastTarget = false;
        }

        RectTransform rect = beklemeGostergesi.GetComponent<RectTransform>();
        Vector2 kupPoz = GetKupCanvasPozisyonu(kup);
        
        // Yuvarlak dolum efekti (büyüyen daire)
        float boyut = 80f * ilerleme;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = kupPoz;
        rect.sizeDelta = new Vector2(boyut, boyut);
        
        // Renk değişimi (şeffaftan opaka)
        Image gostergeImg = beklemeGostergesi.GetComponent<Image>();
        gostergeImg.color = new Color(cizgiRengi.r, cizgiRengi.g, cizgiRengi.b, 0.3f + (0.4f * ilerleme));
    }

    void SilBeklemeGostergesi()
    {
        if (beklemeGostergesi != null)
        {
            Destroy(beklemeGostergesi);
            beklemeGostergesi = null;
        }
    }

    void SuruklemeySonlandir()
    {
        suruklemeAktif = false;
        bekleyenKup = null;
        beklemeSuresi = 0f;
        SilBeklemeGostergesi();
        
        string seciliKelime = SecilenKelimeyiAl();
        Debug.Log($"Seçilen Kelime: {seciliKelime}");

        // Çizgileri temizle
        if (parmakCizgisi != null)
        {
            Destroy(parmakCizgisi);
            parmakCizgisi = null;
        }
        
        // Trail'i hemen temizle
        TemizleTrail();

        // Kelime doğrulama
        Debug.Log($"Seçili küp sayısı: {seciliKupler.Count}");
        Debug.Log($"KelimeVeritabani Instance: {(KelimeVeritabani.Instance != null ? "VAR" : "YOK")}");
        
        if (seciliKupler.Count >= 2)
        {
            string kelimeUpper = seciliKelime.ToUpper();
            Debug.Log($"Kontrol edilen kelime (uppercase): '{kelimeUpper}'");
            
            if (KelimeVeritabani.Instance != null)
            {
                bool gecerliMi = KelimeVeritabani.Instance.KelimeGecerliMi(kelimeUpper);
                Debug.Log($"Kelime geçerli mi: {gecerliMi}");
                
                if (gecerliMi)
                {
                    Debug.Log($"✓ Geçerli kelime: {kelimeUpper} - Küpler patlatılıyor!");
                    // Küpleri patlat
                    StartCoroutine(KupleriPatlat(new List<KupData>(seciliKupler)));
                }
                else
                {
                    Debug.Log($"✗ Geçersiz kelime: {kelimeUpper}");
                    // Yanlış kelime efekti (kırmızı titreme)
                    StartCoroutine(YanlisKelimeEfekti());
                }
            }
            else
            {
                Debug.LogError("KelimeVeritabani.Instance NULL! Kelime doğrulama yapılamıyor.");
            }
        }
        else
        {
            Debug.Log("Yeterli küp seçilmedi (minimum 2 küp gerekli)");
        }

        StartCoroutine(CizgiyiAnimasyonluTemizle());
    }

    System.Collections.IEnumerator KupleriPatlat(List<KupData> patlayanKupler)
    {
        // 1. Patlama efekti
        foreach (KupData kup in patlayanKupler)
        {
            if (kup != null)
            {
                StartCoroutine(KupPatlamaAnimasyonu(kup));
            }
        }
        
        // Patlama animasyonunun bitmesini bekle
        yield return new WaitForSeconds(0.4f);
        
        // 2. Grid'i yeniden düzenle - en alttaki küpü kaldır
        yield return StartCoroutine(GridiYenidenDuzenle(patlayanKupler));
    }

    System.Collections.IEnumerator KupPatlamaAnimasyonu(KupData kup)
    {
        RectTransform rect = kup.GetComponent<RectTransform>();
        Image kupImg = kup.kupGoruntusu;
        TMP_Text harfText = kup.harfYazisi;
        
        float sure = 0.35f;
        float gecen = 0f;
        
        Vector3 baslangicOlcek = rect.localScale;
        Vector3 baslangicPozisyon = rect.anchoredPosition;
        
        while (gecen < sure)
        {
            gecen += Time.deltaTime;
            float t = gecen / sure;
            
            // Büyüme + döndürme + solma
            float olcek = Mathf.Lerp(1f, 1.8f, t);
            rect.localScale = baslangicOlcek * olcek;
            
            // Döndürme
            rect.localRotation = Quaternion.Euler(0, 0, t * 180f);
            
            // Solma
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
        
        // Küpü gizle
        kup.gameObject.SetActive(false);
    }

    System.Collections.IEnumerator GridiYenidenDuzenle(List<KupData> patlayanKupler)
    {
        GridGenerator gridGen = FindAnyObjectByType<GridGenerator>();
        if (gridGen == null) yield break;
        
        // En alttaki satırdaki küpleri bul ve yok et
        int sutunSayisi = gridGen.sutunSayisi;
        int satirSayisi = gridGen.satirSayisi;
        
        // Her sütun için yukarıdan aşağı işle
        for (int sutun = 0; sutun < sutunSayisi; sutun++)
        {
            // Bu sütunda patlayan küp var mı?
            List<int> patlayanSatirlar = new List<int>();
            
            for (int satir = 0; satir < satirSayisi; satir++)
            {
                int index = satir * sutunSayisi + sutun;
                KupData kup = gridGen.GetKupAtIndex(index);
                
                if (kup != null && patlayanKupler.Contains(kup))
                {
                    patlayanSatirlar.Add(satir);
                }
            }
            
            if (patlayanSatirlar.Count > 0)
            {
                // Bu sütunda düşme animasyonu ve yenileme yap
                StartCoroutine(SutunDusurAnimasyonu(sutun, patlayanSatirlar.Count, gridGen));
            }
        }
        
        yield return new WaitForSeconds(0.8f);
    }

    System.Collections.IEnumerator SutunDusurAnimasyonu(int sutunIndex, int patlayanSayisi, GridGenerator gridGen)
    {
        int sutunSayisi = gridGen.sutunSayisi;
        int satirSayisi = gridGen.satirSayisi;
        
        // Aktif küpleri bul ve yukarıdan aşağı sırala
        List<KupData> aktifKupler = new List<KupData>();
        
        for (int satir = satirSayisi - 1; satir >= 0; satir--)
        {
            int index = satir * sutunSayisi + sutunIndex;
            KupData kup = gridGen.GetKupAtIndex(index);
            
            if (kup != null && kup.gameObject.activeSelf)
            {
                aktifKupler.Add(kup);
            }
        }
        
        // En alttaki küpleri (patlayanSayisi kadar) yeniden oluştur
        for (int satir = satirSayisi - 1; satir >= satirSayisi - patlayanSayisi; satir--)
        {
            int index = satir * sutunSayisi + sutunIndex;
            KupData kup = gridGen.GetKupAtIndex(index);
            
            if (kup != null)
            {
                // Küpü yeniden etkinleştir ve yeni harf/renk ata
                kup.gameObject.SetActive(true);
                
                RectTransform rect = kup.GetComponent<RectTransform>();
                Image kupImg = kup.kupGoruntusu;
                TMP_Text harfText = kup.harfYazisi;
                
                // Ölçeği ve rotasyonu sıfırla
                rect.localScale = Vector3.zero;
                rect.localRotation = Quaternion.identity;
                
                // Yeni harf ve renk ata
                char yeniHarf = gridGen.RastgeleHarfAl();
                Color yeniRenk = gridGen.RastgeleRenkAl();
                kup.VeriAta(yeniHarf, yeniRenk);
                
                // Alpha'yı sıfırla
                if (kupImg != null)
                {
                    Color c = kupImg.color;
                    c.a = 1f;
                    kupImg.color = c;
                }
                if (harfText != null)
                {
                    Color c = harfText.color;
                    c.a = 1f;
                    harfText.color = c;
                }
                
                // Bounce animasyonu
                StartCoroutine(KupBelirmeAnimasyonu(kup));
                
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    System.Collections.IEnumerator KupBelirmeAnimasyonu(KupData kup)
    {
        RectTransform rect = kup.GetComponent<RectTransform>();
        
        float sure = 0.35f;
        float gecen = 0f;
        
        while (gecen < sure)
        {
            gecen += Time.deltaTime;
            float t = gecen / sure;
            
            // Bounce easing
            float scale;
            if (t < 0.6f)
            {
                scale = Mathf.Lerp(0f, 1.2f, t / 0.6f);
            }
            else
            {
                float bounceT = (t - 0.6f) / 0.4f;
                scale = Mathf.Lerp(1.2f, 1f, bounceT);
            }
            
            rect.localScale = Vector3.one * scale;
            yield return null;
        }
        
        rect.localScale = Vector3.one;
    }

    System.Collections.IEnumerator YanlisKelimeEfekti()
    {
        // Seçili küpleri kırmızı titret
        List<KupData> kupler = new List<KupData>(seciliKupler);
        
        for (int i = 0; i < 3; i++)
        {
            // Kırmızıya dön
            foreach (KupData kup in kupler)
            {
                if (kup != null && kup.kupGoruntusu != null)
                {
                    kup.kupGoruntusu.color = new Color(1f, 0.3f, 0.3f, 1f);
                }
            }
            yield return new WaitForSeconds(0.1f);
            
            // Normal renge dön
            foreach (KupData kup in kupler)
            {
                if (kup != null && kup.kupGoruntusu != null)
                {
                    kup.kupGoruntusu.color = Color.white;
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    System.Collections.IEnumerator CizgiyiAnimasyonluTemizle()
    {
        float sure = 0.3f;
        float gecenSure = 0f;

        List<Image> tumImages = new List<Image>();
        foreach (var parca in cizgiParcalari)
        {
            if (parca != null)
            {
                Image[] images = parca.GetComponentsInChildren<Image>();
                tumImages.AddRange(images);
            }
        }

        while (gecenSure < sure)
        {
            gecenSure += Time.deltaTime;
            float alpha = 1f - (gecenSure / sure);

            foreach (Image img in tumImages)
            {
                if (img != null)
                {
                    Color renk = img.color;
                    renk.a *= alpha;
                    img.color = renk;
                }
            }

            yield return null;
        }

        TemizleSecim();
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

    void KupuSec(KupData kup)
    {
        // Artık küpler arası kalıcı çizgi yok - sadece trail efekti var
        seciliKupler.Add(kup);
        
        // Küpü görsel olarak işaretle
        StartCoroutine(KupSecildiAnimasyonu(kup));
        
        GuncelleKelimeYazisi();
    }

    System.Collections.IEnumerator KupSecildiAnimasyonu(KupData kup)
    {
        Image kupImg = kup.GetComponent<Image>();
        if (kupImg == null) yield break;

        Color orijinalRenk = kupImg.color;
        
        // Parlama efekti
        kupImg.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        kupImg.color = orijinalRenk;
    }

    void GuncelleParmakCizgisi()
    {
        // Artık parmak çizgisi yok - sadece trail efekti var
        // Bu fonksiyon boş bırakıldı
        if (parmakCizgisi != null)
        {
            Destroy(parmakCizgisi);
            parmakCizgisi = null;
        }
    }

    GameObject SoftCizgiOlustur(Vector2 baslangic, Vector2 bitis, float alpha)
    {
        GameObject grupObj = new GameObject("SoftCizgi");
        grupObj.transform.SetParent(canvasRect, false);

        Vector2 yonu = bitis - baslangic;
        float mesafe = yonu.magnitude;
        float aci = Mathf.Atan2(yonu.y, yonu.x) * Mathf.Rad2Deg;

        // Glow katmanları
        for (int i = glowKatmanSayisi; i >= 1; i--)
        {
            float katmanGenisligi = cizgiKalinligi * (1 + (i * glowGenislemeCarpani * 0.3f));
            float katmanAlpha = glowRengi.a * (1f / (i + 1)) * alpha;
            
            GameObject katman = new GameObject($"Glow_{i}");
            katman.transform.SetParent(grupObj.transform, false);
            
            Image img = katman.AddComponent<Image>();
            img.color = new Color(glowRengi.r, glowRengi.g, glowRengi.b, katmanAlpha);
            img.raycastTarget = false;
            
            RectTransform rect = katman.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = baslangic;
            rect.sizeDelta = new Vector2(mesafe, katmanGenisligi);
            rect.localRotation = Quaternion.Euler(0, 0, aci);
        }

        // Ana çizgi
        GameObject anaCizgi = new GameObject("AnaCizgi");
        anaCizgi.transform.SetParent(grupObj.transform, false);
        
        Image anaImg = anaCizgi.AddComponent<Image>();
        anaImg.color = new Color(cizgiRengi.r, cizgiRengi.g, cizgiRengi.b, cizgiRengi.a * alpha);
        anaImg.raycastTarget = false;
        
        RectTransform anaRect = anaCizgi.GetComponent<RectTransform>();
        anaRect.anchorMin = new Vector2(0.5f, 0.5f);
        anaRect.anchorMax = new Vector2(0.5f, 0.5f);
        anaRect.pivot = new Vector2(0f, 0.5f);
        anaRect.anchoredPosition = baslangic;
        anaRect.sizeDelta = new Vector2(mesafe, cizgiKalinligi);
        anaRect.localRotation = Quaternion.Euler(0, 0, aci);

        // Parlak merkez
        GameObject merkezCizgi = new GameObject("MerkezCizgi");
        merkezCizgi.transform.SetParent(grupObj.transform, false);
        
        Image merkezImg = merkezCizgi.AddComponent<Image>();
        merkezImg.color = new Color(1f, 1f, 1f, 0.6f * alpha);
        merkezImg.raycastTarget = false;
        
        RectTransform merkezRect = merkezCizgi.GetComponent<RectTransform>();
        merkezRect.anchorMin = new Vector2(0.5f, 0.5f);
        merkezRect.anchorMax = new Vector2(0.5f, 0.5f);
        merkezRect.pivot = new Vector2(0f, 0.5f);
        merkezRect.anchoredPosition = baslangic;
        merkezRect.sizeDelta = new Vector2(mesafe, cizgiKalinligi * 0.3f);
        merkezRect.localRotation = Quaternion.Euler(0, 0, aci);

        return grupObj;
    }

    void GuncelleKelimeYazisi()
    {
        if (seciliKelimeYazisi != null)
        {
            seciliKelimeYazisi.text = SecilenKelimeyiAl();
        }
    }

    string SecilenKelimeyiAl()
    {
        string kelime = "";
        foreach (KupData kup in seciliKupler)
        {
            kelime += kup.mevcutHarf;
        }
        return kelime;
    }

    void TemizleSecim()
    {
        seciliKupler.Clear();
        
        foreach (var parca in cizgiParcalari)
        {
            if (parca != null)
            {
                Destroy(parca);
            }
        }
        cizgiParcalari.Clear();

        if (parmakCizgisi != null)
        {
            Destroy(parmakCizgisi);
            parmakCizgisi = null;
        }
        
        // Trail'i temizle
        TemizleTrail();

        if (seciliKelimeYazisi != null)
        {
            seciliKelimeYazisi.text = "";
        }
    }
    
    // ==================== TRAIL EFEKTİ ====================
    
    void GuncelleTrail()
    {
        Vector2 mevcutPoz = EkranPozisyonunuCanvasaÇevir(GetPointerPosition());
        
        // Yeni nokta ekle (yeterli mesafe varsa)
        if (trailNoktalari.Count == 0 || 
            Vector2.Distance(trailNoktalari[trailNoktalari.Count - 1], mevcutPoz) > minNoktaMesafe)
        {
            trailNoktalari.Add(mevcutPoz);
            
            // Maksimum nokta sayısını aşma
            if (trailNoktalari.Count > maxTrailNokta)
            {
                trailNoktalari.RemoveAt(0);
            }
        }
        
        // Trail çizgilerini yeniden oluştur
        TrailCiz();
    }
    
    void TrailCiz()
    {
        // Eski trail parçalarını temizle
        foreach (var parca in trailParcalari)
        {
            if (parca != null)
            {
                Destroy(parca);
            }
        }
        trailParcalari.Clear();
        
        // Yeni trail çiz
        for (int i = 0; i < trailNoktalari.Count - 1; i++)
        {
            // Alpha: başlangıçta şeffaf, sonda opak
            float alpha = (float)(i + 1) / trailNoktalari.Count;
            // Kalınlık: başlangıçta ince, sonda kalın
            float kalinlik = cizgiKalinligi * alpha;
            
            GameObject parca = TrailParcasiOlustur(
                trailNoktalari[i], 
                trailNoktalari[i + 1], 
                alpha * 0.8f,
                kalinlik
            );
            trailParcalari.Add(parca);
        }
    }
    
    GameObject TrailParcasiOlustur(Vector2 baslangic, Vector2 bitis, float alpha, float kalinlik)
    {
        GameObject grupObj = new GameObject("TrailParca");
        grupObj.transform.SetParent(canvasRect, false);

        Vector2 yonu = bitis - baslangic;
        float mesafe = yonu.magnitude;
        float aci = Mathf.Atan2(yonu.y, yonu.x) * Mathf.Rad2Deg;
        
        // Glow katmanı (dış)
        GameObject glowObj = new GameObject("TrailGlow");
        glowObj.transform.SetParent(grupObj.transform, false);
        
        Image glowImg = glowObj.AddComponent<Image>();
        glowImg.color = new Color(glowRengi.r, glowRengi.g, glowRengi.b, alpha * 0.3f);
        glowImg.raycastTarget = false;
        
        RectTransform glowRect = glowObj.GetComponent<RectTransform>();
        glowRect.anchorMin = new Vector2(0.5f, 0.5f);
        glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.pivot = new Vector2(0f, 0.5f);
        glowRect.anchoredPosition = baslangic;
        glowRect.sizeDelta = new Vector2(mesafe, kalinlik * 2.5f);
        glowRect.localRotation = Quaternion.Euler(0, 0, aci);

        // Ana çizgi (iç)
        GameObject anaObj = new GameObject("TrailAna");
        anaObj.transform.SetParent(grupObj.transform, false);
        
        Image anaImg = anaObj.AddComponent<Image>();
        anaImg.color = new Color(cizgiRengi.r, cizgiRengi.g, cizgiRengi.b, alpha);
        anaImg.raycastTarget = false;
        
        RectTransform anaRect = anaObj.GetComponent<RectTransform>();
        anaRect.anchorMin = new Vector2(0.5f, 0.5f);
        anaRect.anchorMax = new Vector2(0.5f, 0.5f);
        anaRect.pivot = new Vector2(0f, 0.5f);
        anaRect.anchoredPosition = baslangic;
        anaRect.sizeDelta = new Vector2(mesafe, kalinlik);
        anaRect.localRotation = Quaternion.Euler(0, 0, aci);
        
        // Parlak merkez
        GameObject merkezObj = new GameObject("TrailMerkez");
        merkezObj.transform.SetParent(grupObj.transform, false);
        
        Image merkezImg = merkezObj.AddComponent<Image>();
        merkezImg.color = new Color(1f, 1f, 1f, alpha * 0.7f);
        merkezImg.raycastTarget = false;
        
        RectTransform merkezRect = merkezObj.GetComponent<RectTransform>();
        merkezRect.anchorMin = new Vector2(0.5f, 0.5f);
        merkezRect.anchorMax = new Vector2(0.5f, 0.5f);
        merkezRect.pivot = new Vector2(0f, 0.5f);
        merkezRect.anchoredPosition = baslangic;
        merkezRect.sizeDelta = new Vector2(mesafe, kalinlik * 0.3f);
        merkezRect.localRotation = Quaternion.Euler(0, 0, aci);

        return grupObj;
    }
    
    void TemizleTrail()
    {
        trailNoktalari.Clear();
        
        foreach (var parca in trailParcalari)
        {
            if (parca != null)
            {
                Destroy(parca);
            }
        }
        trailParcalari.Clear();
    }
}
