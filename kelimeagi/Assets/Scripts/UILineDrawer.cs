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
    
    // Seçili harf önizleme sistemi
    [Header("Önizleme Panel Ayarları")]
    public float harfOnizlemeBoyutu = 85f;
    public float harfOnizlemeAraligi = 12f;
    public float panelYuksekligi = 110f;
    public float panelYPozisyonu = 120f;
    
    private GameObject onizlemeKonteyner;
    private List<GameObject> onizlemeHarfler = new List<GameObject>();

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
        OnizlemePaneliOlustur();
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

    // ==================== HARF SEÇİMİ VE TİTREME EFEKTİ ====================

    void KupuSec(KupData kup)
    {
        seciliKupler.Add(kup);
        
        // Görünüm Değişimi: Opaklaştır
        kup.SetSeciliDurum(true);
        
        // Titreme efekti başlat (Stress)
        StartCoroutine(HarfTitremeEfekti(kup));
        
        // Önizlemeye harf ekle
        OnizlemeyeHarfEkle(kup);
        
        GuncelleKelimeYazisi();
    }

    System.Collections.IEnumerator HarfTitremeEfekti(KupData kup)
    {
        RectTransform rect = kup.GetComponent<RectTransform>();
        Vector3 orijinalPos = rect.anchoredPosition;
        Vector3 orijinalScale = rect.localScale;
        
        float sure = 0f;
        while (seciliKupler.Contains(kup) && suruklemeAktif)
        {
            sure += Time.deltaTime;
            
            // TİTREME (Shake): Sağa sola, yukarı aşağı çok hızlı oyna
            float xOffset = Mathf.Sin(sure * 50f) * 4f; 
            float yOffset = Mathf.Cos(sure * 45f) * 4f;
            
            rect.anchoredPosition = orijinalPos + new Vector3(xOffset, yOffset, 0);
            
            // Hafif Büyüme (Nefes alma)
            float scale = 1.1f + Mathf.Sin(sure * 10f) * 0.05f;
            rect.localScale = orijinalScale * scale;
            
            yield return null;
        }
        
        // Bırakılınca veya seçim bitince
        if (kup != null && rect != null)
        {
            rect.anchoredPosition = orijinalPos;
            if (!seciliKupler.Contains(kup))
            {
                rect.localScale = orijinalScale;
                kup.SetSeciliDurum(false); // Eski haline dön
            }
        }
    }

    // ==================== PATLAMA MEKANİĞİ ====================

    System.Collections.IEnumerator KupleriPatlat(List<KupData> patlayanKupler)
    {
        animasyonDevam = true;
        
        // Puan hesapla vs... (Önceki kodlar aynı)
        string kelime = "";
        int harfPuanlari = 0;
        foreach (KupData kup in patlayanKupler)
        {
            if (kup != null) 
            {
                kelime += kup.mevcutHarf;
                harfPuanlari += kup.mevcutPuan;
            }
        }
        int kelimeBonus = patlayanKupler.Count >= 4 ? patlayanKupler.Count * 3 : 0;
        int kazanilanPuan = harfPuanlari + kelimeBonus;
        
        // Harfleri topla animasyonu
        yield return StartCoroutine(HarfleriTopla(patlayanKupler));
        
        // Puan Göstergesi
        yield return StartCoroutine(KelimeyiGoster(kelime, kazanilanPuan));
        
        // GERÇEK PATLAMA
        foreach (KupData kup in patlayanKupler)
        {
            if (kup != null)
            {
                // Partikül efektini oluştur
                CamKirilmaEfektiOlustur(kup.transform.position, kup.GetKupRengi());
                
                // Küpü gizle ve resetle (hemen yok olsun ki partiküller görünsün)
                kup.gameObject.SetActive(false);
                kup.SetSeciliDurum(false); 
            }
        }
        
        toplamPuan += kazanilanPuan;
        GuncellePuanYazisi();
        
        yield return new WaitForSeconds(patlamaSuresi + 0.1f);
        
        // Küpleri geri yükle
        foreach (KupData kup in patlayanKupler)
        {
            if (kup != null)
            {
                kup.gameObject.SetActive(true);
                RectTransform rect = kup.GetComponent<RectTransform>();
                
                OrijinalPozisyon orijPoz = kup.GetComponent<OrijinalPozisyon>();
                // Image rengini ellemeyelim, sadece yazıyı düzeltelim
                
                if (kup.harfYazisi != null)
                {
                    Color c = kup.harfYazisi.color;
                    c.a = 1f;
                    kup.harfYazisi.color = c;
                }
                
                // Şeffaf cam haline geri döndür (Beyaz kalmasını engeller)
                kup.SetSeciliDurum(false); 
                
                rect.localScale = Vector3.zero;
                rect.localRotation = Quaternion.identity;
                
                char yeniHarf = gridGen.RastgeleHarfAl();
                Color yeniRenk = gridGen.RastgeleRenkAl();
                kup.VeriAta(yeniHarf, yeniRenk);
                
                StartCoroutine(KupBelirmeAnimasyonu(kup));
            }
        }
        
        TemizleSecim();
        animasyonDevam = false;
    }

    void CamKirilmaEfektiOlustur(Vector3 pozisyon, Color renk)
    {
        GameObject efektObj = new GameObject("KirilmaEfekti");
        efektObj.transform.position = pozisyon;
        efektObj.transform.SetParent(anaCanvas.transform); 
        efektObj.transform.localScale = Vector3.one;
        
        // Z konusunu çöz: Öne al
        Vector3 yerelPoz = efektObj.transform.localPosition;
        yerelPoz.z = -100f; // Daha da öne al (Canvas plane'inden kurtul)
        efektObj.transform.localPosition = yerelPoz;

        ParticleSystem ps = efektObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = efektObj.GetComponent<ParticleSystemRenderer>();
        
        // Custom Shader Materyali
        Material particleMat = new Material(Shader.Find("Custom/BasitKristal"));
        if (particleMat.HasProperty("_MainColor"))
        {
            // Shader'ın düzgün çalışması için tam opak renk ver, sonra vertex color ile kısılır
            Color matRenk = renk;
            matRenk.a = 1.0f; 
            particleMat.SetColor("_MainColor", matRenk);
        }
        
        psr.material = particleMat;
        psr.renderMode = ParticleSystemRenderMode.Billboard;
        
        // AYARLAR - KAOS VE ŞİDDET
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 1.0f); // Rastgele ömür
        main.startSpeed = new ParticleSystem.MinMaxCurve(300f, 900f); // Çok hızlı fırlasın (Patlama hissi)
        main.startSize = new ParticleSystem.MinMaxCurve(5f, 45f); // Kimisi toz, kimisi koca parça
        main.startColor = Color.white; 
        main.gravityModifier = 5f; // Hızla yere çakılsın
        main.maxParticles = 30;
        main.loop = false;
        main.playOnAwake = true;

        // EMISSION - Tek seferde patlasın
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 25) });

        // SHAPE - Her yöne saçılsın (Sphere)
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 20f; // Biraz genişten çıksın

        // ROTATION - Parçalar dönsün (En önemlisi bu!)
        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-360f, 360f); // Çılgınca dönsünler
        
        Destroy(efektObj, 1.2f);
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
            if (kup != null)
            {
                // Artık Image rengini ellemiyoruz çünkü o sadece görünmez bir Hitbox
                // kup.kupGoruntusu.color = Color.white; 
                
                RectTransform rect = kup.GetComponent<RectTransform>();
                if (rect != null) rect.localScale = Vector3.one;
                
                // Şeffaflığı geri getir
                kup.SetSeciliDurum(false);
            }
        }
        
        seciliKupler.Clear();
        
        // Önizleme harflerini temizle
        TemizleOnizleme();

        if (seciliKelimeYazisi != null)
        {
            seciliKelimeYazisi.text = "";
        }
    }

    // ==================== PATLAMA ====================

    // (Eski animasyon metodları silindi - Yeni sistem KupleriPatlat içinde entegre)

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
                    // Hata efekti bitince tekrar görünmez yap (Hitbox)
                    kup.kupGoruntusu.color = new Color(0, 0, 0, 0);
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

    // ==================== SEÇİLİ HARF ÖNİZLEME SİSTEMİ ====================

    void OnizlemePaneliOlustur()
    {
        if (canvasRect == null) return;

        // Ana konteyner
        onizlemeKonteyner = new GameObject("OnizlemeKonteyner");
        onizlemeKonteyner.transform.SetParent(canvasRect, false);

        RectTransform konteynerRect = onizlemeKonteyner.AddComponent<RectTransform>();
        konteynerRect.anchorMin = new Vector2(0.5f, 0f);
        konteynerRect.anchorMax = new Vector2(0.5f, 0f);
        konteynerRect.pivot = new Vector2(0.5f, 0f);
        konteynerRect.anchoredPosition = new Vector2(0, panelYPozisyonu);
        konteynerRect.sizeDelta = new Vector2(600, panelYuksekligi);

        // Arka plan paneli (kırmızı çerçeve)
        GameObject arkaPlanObj = new GameObject("OnizlemeArkaPlan");
        arkaPlanObj.transform.SetParent(onizlemeKonteyner.transform, false);

        Image arkaPlanImg = arkaPlanObj.AddComponent<Image>();
        arkaPlanImg.color = new Color(0.1f, 0.1f, 0.15f, 0.85f); // Koyu arka plan
        arkaPlanImg.raycastTarget = false;

        // Yuvarlak köşeli görünüm için
        RectTransform arkaPlanRect = arkaPlanObj.GetComponent<RectTransform>();
        arkaPlanRect.anchorMin = Vector2.zero;
        arkaPlanRect.anchorMax = Vector2.one;
        arkaPlanRect.sizeDelta = Vector2.zero;
        arkaPlanRect.anchoredPosition = Vector2.zero;

        // Kırmızı çerçeve (Outline efekti)
        Outline outline = arkaPlanObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.9f, 0.2f, 0.2f, 1f); // Kırmızı
        outline.effectDistance = new Vector2(3, 3);

        // Harfler için konteyner
        GameObject harflerKonteyner = new GameObject("HarflerKonteyner");
        harflerKonteyner.transform.SetParent(onizlemeKonteyner.transform, false);
        harflerKonteyner.tag = "Untagged";
        harflerKonteyner.name = "HarflerKonteyner";

        RectTransform harflerRect = harflerKonteyner.AddComponent<RectTransform>();
        harflerRect.anchorMin = new Vector2(0.5f, 0.5f);
        harflerRect.anchorMax = new Vector2(0.5f, 0.5f);
        harflerRect.pivot = new Vector2(0.5f, 0.5f);
        harflerRect.anchoredPosition = Vector2.zero;
        harflerRect.sizeDelta = new Vector2(380, 80);

        // HorizontalLayoutGroup ekle
        HorizontalLayoutGroup layout = harflerKonteyner.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = harfOnizlemeAraligi;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    void OnizlemeyeHarfEkle(KupData kup)
    {
        if (onizlemeKonteyner == null) return;

        Transform harflerKonteyner = onizlemeKonteyner.transform.Find("HarflerKonteyner");
        if (harflerKonteyner == null) return;

        // Yeni harf objesi oluştur
        GameObject harfObj = new GameObject($"OnizlemeHarf_{onizlemeHarfler.Count}");
        harfObj.transform.SetParent(harflerKonteyner, false);

        // Arka plan (küp görüntüsü)
        Image harfArkaPlan = harfObj.AddComponent<Image>();
        harfArkaPlan.color = new Color(0.25f, 0.25f, 0.35f, 1f); // Koyu gri-mavi
        harfArkaPlan.raycastTarget = false;

        RectTransform harfRect = harfObj.GetComponent<RectTransform>();
        harfRect.sizeDelta = new Vector2(harfOnizlemeBoyutu, harfOnizlemeBoyutu);

        // Parlak kenar efekti
        Outline harfOutline = harfObj.AddComponent<Outline>();
        harfOutline.effectColor = new Color(1f, 0.8f, 0.3f, 0.8f); // Turuncu-sarı
        harfOutline.effectDistance = new Vector2(2, 2);

        // Harf yazısı
        GameObject yaziObj = new GameObject("Yazi");
        yaziObj.transform.SetParent(harfObj.transform, false);

        TMP_Text yaziText = yaziObj.AddComponent<TextMeshProUGUI>();
        yaziText.text = kup.mevcutHarf.ToString();
        yaziText.fontSize = 36;
        yaziText.fontStyle = FontStyles.Bold;
        yaziText.color = Color.white;
        yaziText.alignment = TextAlignmentOptions.Center;
        yaziText.raycastTarget = false;

        RectTransform yaziRect = yaziObj.GetComponent<RectTransform>();
        yaziRect.anchorMin = Vector2.zero;
        yaziRect.anchorMax = Vector2.one;
        yaziRect.sizeDelta = Vector2.zero;
        yaziRect.anchoredPosition = Vector2.zero;

        // Listeye ekle
        onizlemeHarfler.Add(harfObj);

        // Panel boyutunu güncelle
        GuncellePanelBoyutu();

        // Belirme animasyonu
        StartCoroutine(OnizlemeHarfAnimasyonu(harfRect));
    }

    void GuncellePanelBoyutu()
    {
        if (onizlemeKonteyner == null) return;

        RectTransform konteynerRect = onizlemeKonteyner.GetComponent<RectTransform>();
        if (konteynerRect == null) return;

        // Harf sayısına göre genişlik hesapla
        int harfSayisi = onizlemeHarfler.Count;
        float genislik = (harfSayisi * harfOnizlemeBoyutu) + ((harfSayisi - 1) * harfOnizlemeAraligi) + 40f; // 40 padding
        genislik = Mathf.Max(genislik, 100f); // Minimum genişlik

        konteynerRect.sizeDelta = new Vector2(genislik, panelYuksekligi);
    }

    System.Collections.IEnumerator OnizlemeHarfAnimasyonu(RectTransform rect)
    {
        float sure = 0.2f;
        float gecen = 0f;

        rect.localScale = Vector3.zero;

        while (gecen < sure)
        {
            gecen += Time.deltaTime;
            float t = gecen / sure;

            // Elastic easing
            float scale;
            if (t < 0.6f)
            {
                scale = Mathf.Lerp(0f, 1.2f, t / 0.6f);
            }
            else
            {
                scale = Mathf.Lerp(1.2f, 1f, (t - 0.6f) / 0.4f);
            }

            rect.localScale = Vector3.one * scale;
            yield return null;
        }

        rect.localScale = Vector3.one;
    }

    void TemizleOnizleme()
    {
        foreach (var harf in onizlemeHarfler)
        {
            if (harf != null) Destroy(harf);
        }
        onizlemeHarfler.Clear();
    }
}
