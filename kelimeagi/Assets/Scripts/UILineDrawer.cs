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
    public TMP_Text seciliKelimeYazisi;
    public Canvas anaCanvas;

    [Header("Oyun Ayarları")]
    public int minKelimeUzunluk = 2;
    public float patlamaSuresi = 0.3f;

    [Header("Efekt Renkleri")]
    public Color seciliRenk = new Color(1f, 0.7f, 0.2f, 1f);
    public Color parlamaRengi = new Color(1f, 1f, 0.5f, 1f);

    [Header("Çizgi Ayarları")]
    public Color cizgiRengi = new Color(1f, 0.8f, 0.2f, 0.8f);
    public float cizgiKalinligi = 12f;

    // Seçili küpler
    private List<KupData> seciliKupler = new List<KupData>();
    private RectTransform canvasRect;
    private GridGenerator gridGen;
    private GameObject hataPaneli;
    
    // Sürükleme durumu
    private bool suruklemeAktif = false;
    
    // Puan
    private int toplamPuan = 0;
    
    // Animasyon kilidi
    private bool animasyonDevam = false;
    
    // Trail sistemi
    private List<Vector2> trailNoktalari = new List<Vector2>();
    private List<GameObject> trailParcalari = new List<GameObject>();
    private const int maxTrailNokta = 30;
    private const float minNoktaMesafe = 8f;
    
    // Harf seçim bekleme sistemi
    private KupData bekleyenKup = null;
    private float beklemeSuresi = 0f;
    private const float SECIM_BEKLEME = 0.15f; // 0.15 saniye bekleme

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
        
        bool dokunmaBasladi = false;
        bool dokunmaDevam = false;
        bool dokunmaBitti = false;

        // Touch kontrolü
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            dokunmaBasladi = touch.press.wasPressedThisFrame;
            dokunmaDevam = touch.press.isPressed;
            dokunmaBitti = touch.press.wasReleasedThisFrame;
        }
        // Mouse kontrolü
        else if (Mouse.current != null)
        {
            dokunmaBasladi = Mouse.current.leftButton.wasPressedThisFrame;
            dokunmaDevam = Mouse.current.leftButton.isPressed;
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
        else if (dokunmaBitti && suruklemeAktif)
        {
            SuruklemeySonlandir();
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

    // ==================== SÜRÜKLEME MEKANİĞİ ====================

    void SuruklemeyeBasla()
    {
        suruklemeAktif = true;
        TemizleSecim();
        TemizleTrail();
        
        // İlk trail noktası
        Vector2 poz = EkranPozisyonunaCanvas(GetPointerPosition());
        trailNoktalari.Add(poz);
        
        // İlk dokunulan küpü seç
        KupData ilkKup = TiklananKupuBul();
        if (ilkKup != null)
        {
            KupuSec(ilkKup);
        }
    }

    void SuruklemeDevam()
    {
        // Trail güncelle
        GuncelleTrail();
        
        KupData uzerindekiKup = TiklananKupuBul();
        
        if (uzerindekiKup != null && !seciliKupler.Contains(uzerindekiKup))
        {
            // Aynı küp üzerinde mi?
            if (bekleyenKup == uzerindekiKup)
            {
                // Bekleme süresini artır
                beklemeSuresi += Time.deltaTime;
                
                // Süre doldu mu?
                if (beklemeSuresi >= SECIM_BEKLEME)
                {
                    KupuSec(uzerindekiKup);
                    bekleyenKup = null;
                    beklemeSuresi = 0f;
                }
            }
            else
            {
                // Yeni küp, beklemeyi başlat
                bekleyenKup = uzerindekiKup;
                beklemeSuresi = 0f;
            }
        }
        else
        {
            // Küp yok veya zaten seçili, beklemeyi sıfırla
            bekleyenKup = null;
            beklemeSuresi = 0f;
        }
    }

    void SuruklemeySonlandir()
    {
        suruklemeAktif = false;
        TemizleTrail();
        
        if (seciliKupler.Count >= minKelimeUzunluk)
        {
            string kelime = SecilenKelimeyiAl();
            string kelimeUpper = kelime.ToUpper();
            
            if (KelimeVeritabani.Instance != null && KelimeVeritabani.Instance.KelimeGecerliMi(kelimeUpper))
            {
                Debug.Log($"✓ Geçerli kelime: {kelimeUpper}");
                StartCoroutine(KupleriPatlat(new List<KupData>(seciliKupler)));
            }
            else
            {
                Debug.Log($"✗ Geçersiz kelime: {kelimeUpper}");
                StartCoroutine(YanlisKelimeEfekti());
            }
        }
        else
        {
            TemizleSecim();
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

    // ==================== HARF SEÇİMİ VE YANMA EFEKTİ ====================

    void KupuSec(KupData kup)
    {
        seciliKupler.Add(kup);
        
        // Yanma/Parlama efekti başlat
        StartCoroutine(HarfYanmaEfekti(kup));
        
        GuncelleKelimeYazisi();
    }

    System.Collections.IEnumerator HarfYanmaEfekti(KupData kup)
    {
        if (kup.kupGoruntusu == null) yield break;
        
        Image img = kup.kupGoruntusu;
        RectTransform rect = kup.GetComponent<RectTransform>();
        
        // Orijinal değerleri kaydet
        Color orijinalRenk = img.color;
        Vector3 orijinalScale = rect.localScale;
        
        // Yanma animasyonu - sürekli nabız atar
        float sure = 0f;
        while (seciliKupler.Contains(kup) && suruklemeAktif)
        {
            sure += Time.deltaTime;
            
            // Renk titremesi (turuncu-sarı arası)
            float t = (Mathf.Sin(sure * 10f) + 1f) / 2f;
            img.color = Color.Lerp(seciliRenk, parlamaRengi, t);
            
            // Hafif büyüme-küçülme
            float scale = 1f + Mathf.Sin(sure * 8f) * 0.05f;
            rect.localScale = orijinalScale * scale;
            
            yield return null;
        }
        
        // Eğer hala seçiliyse sabit turuncu renkte kal
        if (seciliKupler.Contains(kup))
        {
            img.color = seciliRenk;
            rect.localScale = orijinalScale * 1.05f;
        }
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
            if (kup != null)
                kelime += kup.mevcutHarf;
        }
        return kelime;
    }

    void TemizleSecim()
    {
        // Tüm küplerin rengini ve boyutunu sıfırla
        foreach (KupData kup in seciliKupler)
        {
            if (kup != null && kup.kupGoruntusu != null)
            {
                kup.kupGoruntusu.color = Color.white;
                RectTransform rect = kup.GetComponent<RectTransform>();
                if (rect != null) rect.localScale = Vector3.one;
            }
        }
        
        seciliKupler.Clear();

        if (seciliKelimeYazisi != null)
        {
            seciliKelimeYazisi.text = "";
        }
    }

    // ==================== PATLAMA ====================

    System.Collections.IEnumerator KupleriPatlat(List<KupData> patlayanKupler)
    {
        animasyonDevam = true;
        
        // Kelimeyi oluştur
        string kelime = "";
        foreach (KupData kup in patlayanKupler)
        {
            if (kup != null) kelime += kup.mevcutHarf;
        }
        
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
        int kelimeBonus = patlayanKupler.Count >= 4 ? patlayanKupler.Count * 3 : 0;
        int kazanilanPuan = harfPuanlari + kelimeBonus;
        
        // Harfleri ekranın altına topla
        yield return StartCoroutine(HarfleriTopla(patlayanKupler));
        
        // Kelimeyi göster
        yield return StartCoroutine(KelimeyiGoster(kelime, kazanilanPuan));
        
        // Patlama animasyonu
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
        
        // Küpleri eski yerlerine döndür ve yeni harfler ver
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
        
        TemizleSecim();
        animasyonDevam = false;
    }

    // Harfleri ekranın altına topla
    System.Collections.IEnumerator HarfleriTopla(List<KupData> kupler)
    {
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
        
        float hedefY = minY - 400f;
        
        float sure = 0.35f;
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
                    
                    float xOffset = (index - (kupler.Count - 1) / 2f) * 85;
                    Vector2 kupHedef = new Vector2(ortalamaX + xOffset, hedefY);
                    
                    rect.anchoredPosition = Vector2.Lerp(orijinalPozisyonlar[kup], kupHedef, t);
                    rect.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.15f, t);
                    
                    index++;
                }
            }
            
            yield return null;
        }
        
        foreach (var kvp in orijinalPozisyonlar)
        {
            if (kvp.Key != null)
            {
                var comp = kvp.Key.gameObject.GetComponent<OrijinalPozisyon>();
                if (comp == null)
                    comp = kvp.Key.gameObject.AddComponent<OrijinalPozisyon>();
                comp.pozisyon = kvp.Value;
            }
        }
    }

    System.Collections.IEnumerator KelimeyiGoster(string kelime, int puan)
    {
        GameObject kelimeObj = new GameObject("KelimeGosterge");
        kelimeObj.transform.SetParent(canvasRect, false);
        
        TMP_Text kelimeText = kelimeObj.AddComponent<TMP_Text>();
        kelimeText.text = $"{kelime}\n+{puan}";
        kelimeText.fontSize = 56;
        kelimeText.fontStyle = FontStyles.Bold;
        kelimeText.color = new Color(1f, 0.9f, 0.3f, 1f);
        kelimeText.alignment = TextAlignmentOptions.Center;
        kelimeText.enableAutoSizing = false;
        
        RectTransform rect = kelimeObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -150);
        rect.sizeDelta = new Vector2(500, 200);
        
        StartCoroutine(EkraniRenklendir(new Color(0f, 1f, 0f, 1f)));
        
        float sure = 0.7f;
        float gecen = 0f;
        
        while (gecen < sure)
        {
            gecen += Time.deltaTime;
            float t = gecen / sure;
            
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.25f;
            rect.localScale = Vector3.one * scale;
            
            if (t > 0.5f)
            {
                float alpha = 1f - (t - 0.5f) / 0.5f;
                kelimeText.color = new Color(1f, 0.9f, 0.3f, alpha);
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
            
            float olcek = Mathf.Lerp(1f, 1.6f, t);
            rect.localScale = baslangicOlcek * olcek;
            rect.localRotation = Quaternion.Euler(0, 0, t * 120f);
            
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
            
            float scale = t < 0.65f 
                ? Mathf.Lerp(0f, 1.2f, t / 0.65f)
                : Mathf.Lerp(1.2f, 1f, (t - 0.65f) / 0.35f);
            
            rect.localScale = Vector3.one * scale;
            yield return null;
        }
        
        rect.localScale = Vector3.one;
    }

    // ==================== YANLIŞ KELİME EFEKTİ ====================

    System.Collections.IEnumerator YanlisKelimeEfekti()
    {
        StartCoroutine(EkraniRenklendir(new Color(1f, 0f, 0f, 1f)));

        List<KupData> kupler = new List<KupData>(seciliKupler);
        
        for (int i = 0; i < 3; i++)
        {
            foreach (KupData kup in kupler)
            {
                if (kup != null && kup.kupGoruntusu != null)
                {
                    kup.kupGoruntusu.color = new Color(1f, 0.3f, 0.3f, 1f);
                }
            }
            yield return new WaitForSeconds(0.07f);
            
            foreach (KupData kup in kupler)
            {
                if (kup != null && kup.kupGoruntusu != null)
                {
                    kup.kupGoruntusu.color = Color.white;
                }
            }
            yield return new WaitForSeconds(0.07f);
        }
        
        TemizleSecim();
    }

    System.Collections.IEnumerator EkraniRenklendir(Color hedefRenk)
    {
        if (hataPaneli == null) HataPaneliOlustur();
        
        Image img = hataPaneli.GetComponent<Image>();
        if (img == null) yield break;

        hataPaneli.SetActive(true);

        float sure = 0.12f;
        float gecen = 0f;

        while (gecen < sure)
        {
            gecen += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 0.3f, gecen / sure);
            img.color = new Color(hedefRenk.r, hedefRenk.g, hedefRenk.b, alpha);
            yield return null;
        }

        sure = 0.18f;
        gecen = 0f;
        while (gecen < sure)
        {
            gecen += Time.deltaTime;
            float alpha = Mathf.Lerp(0.3f, 0f, gecen / sure);
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

    // ==================== TRAIL ÇİZİM SİSTEMİ ====================

    Vector2 EkranPozisyonunaCanvas(Vector2 ekranPoz)
    {
        Vector2 lokalPoz;
        Camera kamera = anaCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : anaCanvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, ekranPoz, kamera, out lokalPoz);
        return lokalPoz;
    }

    void GuncelleTrail()
    {
        Vector2 mevcutPoz = EkranPozisyonunaCanvas(GetPointerPosition());
        
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
        // Eski parçaları temizle
        foreach (var parca in trailParcalari)
        {
            if (parca != null) Destroy(parca);
        }
        trailParcalari.Clear();
        
        // Yeni trail çiz
        for (int i = 0; i < trailNoktalari.Count - 1; i++)
        {
            // Alpha: başlangıçta şeffaf, sonda opak
            float alpha = (float)(i + 1) / trailNoktalari.Count;
            // Kalınlık: başlangıçta ince, sonda kalın
            float kalinlik = cizgiKalinligi * (0.3f + alpha * 0.7f);
            
            GameObject parca = CizgiParcasiOlustur(
                trailNoktalari[i], 
                trailNoktalari[i + 1], 
                alpha * 0.9f,
                kalinlik
            );
            trailParcalari.Add(parca);
        }
    }

    GameObject CizgiParcasiOlustur(Vector2 baslangic, Vector2 bitis, float alpha, float kalinlik)
    {
        GameObject cizgiObj = new GameObject("TrailParca");
        cizgiObj.transform.SetParent(canvasRect, false);

        Vector2 yonu = bitis - baslangic;
        float mesafe = yonu.magnitude;
        float aci = Mathf.Atan2(yonu.y, yonu.x) * Mathf.Rad2Deg;

        // Glow efekti (dış)
        GameObject glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(cizgiObj.transform, false);
        
        Image glowImg = glowObj.AddComponent<Image>();
        glowImg.color = new Color(cizgiRengi.r, cizgiRengi.g, cizgiRengi.b, alpha * 0.3f);
        glowImg.raycastTarget = false;
        
        RectTransform glowRect = glowObj.GetComponent<RectTransform>();
        glowRect.anchorMin = new Vector2(0.5f, 0.5f);
        glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.pivot = new Vector2(0f, 0.5f);
        glowRect.anchoredPosition = baslangic;
        glowRect.sizeDelta = new Vector2(mesafe, kalinlik * 2.5f);
        glowRect.localRotation = Quaternion.Euler(0, 0, aci);

        // Ana çizgi (iç)
        GameObject anaObj = new GameObject("Ana");
        anaObj.transform.SetParent(cizgiObj.transform, false);
        
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
        GameObject merkezObj = new GameObject("Merkez");
        merkezObj.transform.SetParent(cizgiObj.transform, false);
        
        Image merkezImg = merkezObj.AddComponent<Image>();
        merkezImg.color = new Color(1f, 1f, 1f, alpha * 0.6f);
        merkezImg.raycastTarget = false;
        
        RectTransform merkezRect = merkezObj.GetComponent<RectTransform>();
        merkezRect.anchorMin = new Vector2(0.5f, 0.5f);
        merkezRect.anchorMax = new Vector2(0.5f, 0.5f);
        merkezRect.pivot = new Vector2(0f, 0.5f);
        merkezRect.anchoredPosition = baslangic;
        merkezRect.sizeDelta = new Vector2(mesafe, kalinlik * 0.4f);
        merkezRect.localRotation = Quaternion.Euler(0, 0, aci);

        return cizgiObj;
    }

    void TemizleTrail()
    {
        trailNoktalari.Clear();
        
        foreach (var parca in trailParcalari)
        {
            if (parca != null) Destroy(parca);
        }
        trailParcalari.Clear();
    }
}
