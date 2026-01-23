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
    public int maxHarfSayisi = 6; // Maksimum seçilebilecek harf sayısı
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
    private GlassShatterSpawner glassShatterSpawner; // Cached spawner reference
    
    // Sürükleme durumu
    private bool suruklemeAktif = false;
    
    // Puan
    private int toplamPuan = 0;
    
    // Animasyon kilidi
    private bool animasyonDevam = false;
    
    // Trail sistemi
    private List<Vector2> trailNoktalari = new List<Vector2>();
    private List<GameObject> trailParcalari = new List<GameObject>(); // Aktif kullanılan parçalar
    private List<GameObject> pooledSegments = new List<GameObject>(); // Havuzdaki parçalar
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
    public float panelYPozisyonu = 220f;
    
    [Header("Önizleme Arka Plan Ayarları")]
    [SerializeField] private Sprite onizlemeArkaplanSprite; // Custom themed background image
    [SerializeField] private bool useSliced = true; // Use 9-slice if sprite has borders
    [SerializeField] private Vector2 arkaplanPadding = new Vector2(0, 0); // Safe padding
    [SerializeField] private Color arkaplanRengi = new Color(0.1f, 0.1f, 0.15f, 0.85f); // Fallback color if no sprite
    
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
        // OnizlemePaneliOlustur(); // Artık ComboManager'ın Frame'ini kullanacağız, buradaki oluşturma iptal
        
        // Havuzu başlangıçta doldur (Prewarm)
        BaslangicHavuzunuOlustur();
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
        // Maksimum harf sayısına ulaşıldıysa yeni harf seçme
        if (seciliKupler.Count >= maxHarfSayisi) return;
        
        seciliKupler.Add(kup);
        
        // Görünüm Değişimi: Opaklaştır
        kup.SetSeciliDurum(true);
        
        // Titreme efekti başlat (Stress)
        StartCoroutine(HarfTitremeEfekti(kup));
        
        // Harfleri Frame'e gönder (Eski önizleme sistemi, şimdi Frame Toplama sistemi)
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
        
        // ANINDA YEŞİL EKRAN (Doğru bildin!)
        StartCoroutine(EkraniRenklendir(new Color(0.1f, 1f, 0.1f, 1f)));
        
        // Orijinal pozisyonları ÖNCE kaydet (harfler henüz yerinde)
        Dictionary<KupData, Vector3> orijinalPozisyonlar = new Dictionary<KupData, Vector3>();
        foreach (KupData kup in patlayanKupler)
        {
            if (kup != null)
            {
                orijinalPozisyonlar[kup] = kup.transform.position;
            }
        }
        
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
        int hamPuan = harfPuanlari + kelimeBonus;
        
        // COMBO MANAGER PUAN HESAPLAMA
        int kazanilanPuan = ComboManager.Instance != null ? ComboManager.Instance.PuanHesapla(hamPuan) : hamPuan;
        
        // COMBO MANAGER DOĞRU KELİME BİLDİRİMİ
        if (ComboManager.Instance != null)
        {
            ComboManager.Instance.DogruKelime(kazanilanPuan);
        }
        
        // GERÇEK PATLAMA - Orijinal pozisyonlarda patlat (harfler hareket etmeden)
        foreach (KupData kup in patlayanKupler)
        {
            if (kup != null && orijinalPozisyonlar.ContainsKey(kup))
            {
                // Partikül efektini ORİJİNAL pozisyonda oluştur
                CamKirilmaEfektiOlustur(orijinalPozisyonlar[kup], kup.GetKupRengi());
                
                // Küpü gizle ve resetle (hemen yok olsun ki partiküller görünsün)
                kup.gameObject.SetActive(false);
                kup.SetSeciliDurum(false); 
            }
        }
        
        // Puan Göstergesi
        yield return StartCoroutine(KelimeyiGoster(kelime, kazanilanPuan));
        
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
        // Find and cache the spawner if not already cached
        if (glassShatterSpawner == null)
        {
            glassShatterSpawner = FindAnyObjectByType<GlassShatterSpawner>();
        }

        if (glassShatterSpawner != null)
        {
            glassShatterSpawner.Spawn(pozisyon, renk);
        }
        else
        {
            Debug.LogWarning("CamKirilmaEfektiOlustur: GlassShatterSpawner not found in scene!");
        }
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
        
        TMP_Text kelimeText = kelimeObj.AddComponent<TextMeshProUGUI>();
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
        
        // StartCoroutine(EkraniRenklendir(new Color(0f, 1f, 0f, 1f))); // Artık KupleriPatlat içinde başta yapıyoruz
        
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
        
        // Frame'deki harfleri temizle
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
        // COMBO MANAGER YANLIŞ KELİME BİLDİRİMİ
        if (ComboManager.Instance != null)
        {
            ComboManager.Instance.YanlisKelime();
        }

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

    // POOL SİSTEMİ BAŞLANGIÇ
    void BaslangicHavuzunuOlustur()
    {
        // Yeterli sayıda trail parçasını baştan oluşturup havuza atıyoruz
        for (int i = 0; i < maxTrailNokta + 5; i++)
        {
            GameObject segment = CreateTrailSegment();
            ReleaseSegment(segment);
        }
    }

    GameObject GetSegment()
    {
        GameObject segment = null;
        if (pooledSegments.Count > 0)
        {
            segment = pooledSegments[pooledSegments.Count - 1];
            pooledSegments.RemoveAt(pooledSegments.Count - 1);
        }
        else
        {
            segment = CreateTrailSegment();
        }

        if (segment != null)
        {
            segment.SetActive(true);
            segment.transform.SetAsLastSibling(); // En önde çizilsin
        }
        return segment;
    }

    void ReleaseSegment(GameObject segment)
    {
        if (segment != null)
        {
            segment.SetActive(false);
            pooledSegments.Add(segment);
        }
    }
    // POOL SİSTEMİ BİTİŞ

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
        // İhtiyaç duyulan parça sayısı
        int gerekliParca = Mathf.Max(0, trailNoktalari.Count - 1);

        // Fazlalıkları havuza gönder
        while (trailParcalari.Count > gerekliParca)
        {
            int sonIndex = trailParcalari.Count - 1;
            ReleaseSegment(trailParcalari[sonIndex]);
            trailParcalari.RemoveAt(sonIndex);
        }

        // Eksikleri havuzdan tamamla
        while (trailParcalari.Count < gerekliParca)
        {
            GameObject seg = GetSegment();
            trailParcalari.Add(seg);
        }
        
        // Mevcut parçaları güncelle
        for (int i = 0; i < gerekliParca; i++)
        {
            // Alpha: başlangıçta şeffaf, sonda opak
            float alpha = (float)(i + 1) / trailNoktalari.Count;
            // Kalınlık: başlangıçta ince, sonda kalın
            float kalinlik = cizgiKalinligi * (0.3f + alpha * 0.7f);
            
            UpdateTrailSegment(
                trailParcalari[i],
                trailNoktalari[i], 
                trailNoktalari[i + 1], 
                alpha * 0.9f,
                kalinlik
            );
        }
    }

    // Sadece nesne oluşturur, değer atamaz (Değerler Update'de atanır)
    GameObject CreateTrailSegment()
    {
        GameObject cizgiObj = new GameObject("TrailParca");
        if (canvasRect != null) cizgiObj.transform.SetParent(canvasRect, false);

        // Glow efekti (dış) -> Child 0
        GameObject glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(cizgiObj.transform, false);
        Image glowImg = glowObj.AddComponent<Image>();
        glowImg.raycastTarget = false;
        RectTransform glowRect = glowObj.GetComponent<RectTransform>();
        glowRect.anchorMin = new Vector2(0.5f, 0.5f);
        glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.pivot = new Vector2(0f, 0.5f);

        // Ana çizgi (iç) -> Child 1
        GameObject anaObj = new GameObject("Ana");
        anaObj.transform.SetParent(cizgiObj.transform, false);
        Image anaImg = anaObj.AddComponent<Image>();
        anaImg.raycastTarget = false;
        RectTransform anaRect = anaObj.GetComponent<RectTransform>();
        anaRect.anchorMin = new Vector2(0.5f, 0.5f);
        anaRect.anchorMax = new Vector2(0.5f, 0.5f);
        anaRect.pivot = new Vector2(0f, 0.5f);

        // Parlak merkez -> Child 2
        GameObject merkezObj = new GameObject("Merkez");
        merkezObj.transform.SetParent(cizgiObj.transform, false);
        Image merkezImg = merkezObj.AddComponent<Image>();
        merkezImg.raycastTarget = false;
        RectTransform merkezRect = merkezObj.GetComponent<RectTransform>();
        merkezRect.anchorMin = new Vector2(0.5f, 0.5f);
        merkezRect.anchorMax = new Vector2(0.5f, 0.5f);
        merkezRect.pivot = new Vector2(0f, 0.5f);

        return cizgiObj;
    }

    // Mevcut segmenti yeni verilere göre günceller (Allocator-Free)
    void UpdateTrailSegment(GameObject cizgiObj, Vector2 baslangic, Vector2 bitis, float alpha, float kalinlik)
    {
        Vector2 yonu = bitis - baslangic;
        float mesafe = yonu.magnitude;
        float aci = Mathf.Atan2(yonu.y, yonu.x) * Mathf.Rad2Deg;
        Quaternion rotasyon = Quaternion.Euler(0, 0, aci);

        // Child 0: Glow
        Transform glowTr = cizgiObj.transform.GetChild(0);
        Image glowImg = glowTr.GetComponent<Image>();
        RectTransform glowRect = glowTr.GetComponent<RectTransform>();
        
        glowImg.color = new Color(cizgiRengi.r, cizgiRengi.g, cizgiRengi.b, alpha * 0.3f);
        glowRect.anchoredPosition = baslangic;
        glowRect.sizeDelta = new Vector2(mesafe, kalinlik * 2.5f);
        glowRect.localRotation = rotasyon;

        // Child 1: Ana
        Transform anaTr = cizgiObj.transform.GetChild(1);
        Image anaImg = anaTr.GetComponent<Image>();
        RectTransform anaRect = anaTr.GetComponent<RectTransform>();

        anaImg.color = new Color(cizgiRengi.r, cizgiRengi.g, cizgiRengi.b, alpha);
        anaRect.anchoredPosition = baslangic;
        anaRect.sizeDelta = new Vector2(mesafe, kalinlik);
        anaRect.localRotation = rotasyon;

        // Child 2: Merkez
        Transform merkezTr = cizgiObj.transform.GetChild(2);
        Image merkezImg = merkezTr.GetComponent<Image>();
        RectTransform merkezRect = merkezTr.GetComponent<RectTransform>();

        merkezImg.color = new Color(1f, 1f, 1f, alpha * 0.6f);
        merkezRect.anchoredPosition = baslangic;
        merkezRect.sizeDelta = new Vector2(mesafe, kalinlik * 0.4f);
        merkezRect.localRotation = rotasyon;
    }

    void TemizleTrail()
    {
        trailNoktalari.Clear();
        
        // Hepsini havuza gönder
        foreach (var parca in trailParcalari)
        {
            ReleaseSegment(parca);
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

        // Arka plan paneli
        GameObject arkaPlanObj = new GameObject("BackgroundImage");
        arkaPlanObj.transform.SetParent(onizlemeKonteyner.transform, false);
        arkaPlanObj.transform.SetAsFirstSibling(); // Ensure it's behind letters

        Image arkaPlanImg = arkaPlanObj.AddComponent<Image>();
        arkaPlanImg.raycastTarget = false;
        
        // Use custom sprite if assigned, otherwise use fallback color
        if (onizlemeArkaplanSprite != null)
        {
            arkaPlanImg.sprite = onizlemeArkaplanSprite;
            arkaPlanImg.color = Color.white;
            
            // Use sliced mode if enabled and sprite has borders
            if (useSliced && onizlemeArkaplanSprite.border != Vector4.zero)
            {
                arkaPlanImg.type = Image.Type.Sliced;
            }
            else
            {
                arkaPlanImg.type = Image.Type.Simple;
            }
        }
        else
        {
            // Fallback: use solid color
            arkaPlanImg.color = arkaplanRengi;
        }

        // Stretch to fill panel with optional padding
        RectTransform arkaPlanRect = arkaPlanObj.GetComponent<RectTransform>();
        arkaPlanRect.anchorMin = Vector2.zero;
        arkaPlanRect.anchorMax = Vector2.one;
        arkaPlanRect.offsetMin = arkaplanPadding; // Left, Bottom padding
        arkaPlanRect.offsetMax = -arkaplanPadding; // Right, Top padding (negative)
        arkaPlanRect.anchoredPosition = Vector2.zero;

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

    void OnizlemeyeHarfEkle(KupData veriKup)
    {
        // YENİ SİSTEM: ComboManager Frame Kullanımı
        if (ComboManager.Instance != null && ComboManager.Instance.cerceveAlani != null)
        {
            Transform frameTransform = ComboManager.Instance.cerceveAlani;

            // Frame içinde harf objesi oluştur
            GameObject kupObj = new GameObject($"FrameKup_{onizlemeHarfler.Count}");
            kupObj.transform.SetParent(frameTransform, false);

            // Temel bileşenler
            RectTransform kupRect = kupObj.AddComponent<RectTransform>();
            kupRect.sizeDelta = new Vector2(harfOnizlemeBoyutu, harfOnizlemeBoyutu);

            Image hitbox = kupObj.AddComponent<Image>();
            hitbox.color = new Color(0, 0, 0, 0); 
            hitbox.raycastTarget = false;

            // 3D Model Kopyalama
            if (gridGen != null && gridGen.ucBoyutluModelPrefabi != null)
            {
                GameObject model3D = Instantiate(gridGen.ucBoyutluModelPrefabi, kupObj.transform);
                model3D.transform.localPosition = new Vector3(0, 0, -50f); 
                model3D.transform.localRotation = Quaternion.Euler(-15f, 180f, 0);
                model3D.transform.localScale = Vector3.one * (harfOnizlemeBoyutu * 0.6f);

                SetLayerRecursively(model3D, 5); // UI Layer
                
                Renderer[] renderers = model3D.GetComponentsInChildren<Renderer>();
                MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
                foreach (Renderer r in renderers)
                {
                    r.GetPropertyBlock(propBlock);
                    if (veriKup != null)
                    {
                        propBlock.SetColor("_BaseColor", veriKup.GetKupRengi());
                        propBlock.SetColor("_Color", veriKup.GetKupRengi());
                        r.SetPropertyBlock(propBlock);
                    }
                }
            }

            // Harf Yazısı
            GameObject harfYaziObj = new GameObject("HarfYazisi");
            harfYaziObj.transform.SetParent(kupObj.transform, false);

            TMP_Text harfText = harfYaziObj.AddComponent<TextMeshProUGUI>();
            if (veriKup != null) harfText.text = veriKup.mevcutHarf.ToString();
            harfText.fontSize = 42;
            harfText.fontStyle = FontStyles.Bold;
            harfText.color = Color.white;
            harfText.alignment = TextAlignmentOptions.Center;
            harfText.raycastTarget = false;

            RectTransform harfRect = harfYaziObj.GetComponent<RectTransform>();
            harfRect.anchorMin = Vector2.zero;
            harfRect.anchorMax = Vector2.one;
            harfRect.sizeDelta = Vector2.zero;
            harfRect.anchoredPosition = new Vector2(0, 5);

            // Listeye ekle ve animasyon
            onizlemeHarfler.Add(kupObj);
            StartCoroutine(OnizlemeHarfAnimasyonu(kupObj, onizlemeHarfler.Count - 1));
            
            // Pozisyonları güncelle
            GuncelleFrameHarfPozisyonlari();

            // Fonksiyondan çık, eski kodu çalıştırma
            return;
        }

        /* ESKİ KOD BAŞLANGICI (Backup olarak kalsın)
        if (onizlemeKonteyner == null) return;

        Transform harflerKonteyner = onizlemeKonteyner.transform.Find("HarflerKonteyner");
        if (harflerKonteyner == null) return;

        // Ana konteyner (UI için)
        GameObject kupObj = new GameObject($"OnizlemeKup_{onizlemeHarfler.Count}");
        kupObj.transform.SetParent(harflerKonteyner, false);

        // RectTransform ekle (HorizontalLayoutGroup için gerekli)
        RectTransform kupRect = kupObj.AddComponent<RectTransform>();
        kupRect.sizeDelta = new Vector2(harfOnizlemeBoyutu, harfOnizlemeBoyutu);

        // Görünmez hitbox (raycast için)
        Image hitbox = kupObj.AddComponent<Image>();
        hitbox.color = new Color(0, 0, 0, 0); // Tamamen şeffaf
        hitbox.raycastTarget = false;

        // 3D Küp modelini GridGenerator'dan al ve ekle
        if (gridGen != null && gridGen.ucBoyutluModelPrefabi != null)
        {
            GameObject model3D = Instantiate(gridGen.ucBoyutluModelPrefabi, kupObj.transform);
            model3D.transform.localPosition = new Vector3(0, 0, 5f);
            model3D.transform.localRotation = Quaternion.Euler(-15f, 180f, 0);
            
            // Önizleme için ölçekle
            float modelOlcek = harfOnizlemeBoyutu * 0.6f;
            model3D.transform.localScale = Vector3.one * modelOlcek;

            // UI Layer'a al
            SetLayerRecursively(model3D, 5); // 5 = UI Layer
        }

        // Harf yazısı (ortada, büyük)
        GameObject harfYaziObj = new GameObject("HarfYazisi");
        harfYaziObj.transform.SetParent(kupObj.transform, false);

        TMP_Text harfText = harfYaziObj.AddComponent<TextMeshProUGUI>();
        harfText.text = kup.mevcutHarf.ToString();
        harfText.fontSize = 42;
        harfText.fontStyle = FontStyles.Bold;
        harfText.color = Color.white;
        harfText.alignment = TextAlignmentOptions.Center;
        harfText.raycastTarget = false;

        RectTransform harfRect = harfYaziObj.GetComponent<RectTransform>();
        harfRect.anchorMin = Vector2.zero;
        harfRect.anchorMax = Vector2.one;
        harfRect.sizeDelta = Vector2.zero;
        harfRect.anchoredPosition = new Vector2(0, 5);

        // Puan yazısı (sağ alt köşe)
        GameObject puanYaziObj = new GameObject("PuanYazisi");
        puanYaziObj.transform.SetParent(kupObj.transform, false);

        TMP_Text puanText = puanYaziObj.AddComponent<TextMeshProUGUI>();
        puanText.text = kup.mevcutPuan.ToString();
        puanText.fontSize = 16;
        puanText.fontStyle = FontStyles.Bold;
        puanText.color = new Color(0.5f, 1f, 0.5f, 1f);
        puanText.alignment = TextAlignmentOptions.BottomRight;
        puanText.raycastTarget = false;

        RectTransform puanRect = puanYaziObj.GetComponent<RectTransform>();
        puanRect.anchorMin = Vector2.zero;
        puanRect.anchorMax = Vector2.one;
        puanRect.offsetMin = new Vector2(4, 4);
        puanRect.offsetMax = new Vector2(-4, -4);

        // Listeye ekle
        onizlemeHarfler.Add(kupObj);

        // Panel boyutunu güncelle
        GuncellePanelBoyutu();

        // Belirme animasyonu
        StartCoroutine(OnizlemeHarfAnimasyonu(kupRect));
        */
    }

    // YENİ FONKSİYON: Frame içindeki harfleri sırala
    void GuncelleFrameHarfPozisyonlari()
    {
        if (ComboManager.Instance == null || ComboManager.Instance.cerceveAlani == null) return;
        
        float toplamGenislik = onizlemeHarfler.Count * harfOnizlemeBoyutu + (onizlemeHarfler.Count - 1) * 5f; 
        float baslangicX = -toplamGenislik / 2f + harfOnizlemeBoyutu / 2f;
        
        for (int i = 0; i < onizlemeHarfler.Count; i++)
        {
            GameObject obj = onizlemeHarfler[i];
            if (obj != null)
            {
                 RectTransform rect = obj.GetComponent<RectTransform>();
                 rect.anchoredPosition = new Vector2(baslangicX + i * (harfOnizlemeBoyutu + 5f), 0);
            }
        }
    }
    
    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
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

        if (rect == null) yield break;
        rect.localScale = Vector3.zero;

        while (gecen < sure)
        {
            // Null check - object may have been destroyed during animation
            if (rect == null) yield break;
            
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

        if (rect != null)
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
