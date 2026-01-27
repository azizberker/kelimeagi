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

    [Header("Efekt Renkleri")]
    public Color seciliRenk = new Color(1f, 0.7f, 0.2f, 1f);
    public Color parlamaRengi = new Color(1f, 1f, 0.5f, 1f);

    [Header("Çizgi Ayarları")]
    public Color cizgiRengi = new Color(1f, 0.8f, 0.2f, 0.8f);
    public float cizgiKalinligi = 12f;

    // Secili kupler
    private List<KupData> seciliKupler = new List<KupData>();
    private RectTransform canvasRect;
    private GridGenerator gridGen;
    private GameObject hataPaneli;
    private GlassShatterSpawner glassShatterSpawner;
    
    // Mobil performans icin cached raycast nesneleri
    private PointerEventData cachedPointerData;
    private List<RaycastResult> cachedRaycastResults = new List<RaycastResult>();
    
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
        
        // Havuzu baslangicta doldur (Prewarm)
        BaslangicHavuzunuOlustur();
        
        // Cached raycast nesneleri olustur
        if (EventSystem.current != null)
        {
            cachedPointerData = new PointerEventData(EventSystem.current);
        }
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
                // ComboManager bilgilendir
                if (ComboManager.Instance != null)
                {
                    ComboManager.Instance.DogruKelime();
                }
                
                // 6 harfli kelime VFX tetikle
                if (WordVfxTrigger.Instance != null)
                {
                    WordVfxTrigger.Instance.TryShowVfx(kelimeUpper);
                }
                
                StartCoroutine(KupleriPatlat(new List<KupData>(seciliKupler)));
            }
            else
            {
                // ComboManager bilgilendir
                if (ComboManager.Instance != null)
                {
                    ComboManager.Instance.YanlisKelime();
                }
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
        // EventSystem null kontrolu
        if (EventSystem.current == null) return null;
        
        // Cached pointer data olustur/guncelle
        if (cachedPointerData == null)
        {
            cachedPointerData = new PointerEventData(EventSystem.current);
        }
        cachedPointerData.position = GetPointerPosition();
        
        // Cached listeyi temizle ve kullan
        cachedRaycastResults.Clear();
        EventSystem.current.RaycastAll(cachedPointerData, cachedRaycastResults);

        for (int i = 0; i < cachedRaycastResults.Count; i++)
        {
            RaycastResult sonuc = cachedRaycastResults[i];
            if (sonuc.gameObject == null) continue;
            
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
        
        // 1. YEŞİL EKRAN
        StartCoroutine(EkraniRenklendir(new Color(0.1f, 1f, 0.1f, 1f)));
        
        // 2. PUAN HESAPLA
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
        
        // 3. CAM KIRILMA EFEKTİ
        foreach (KupData kup in patlayanKupler)
        {
            if (kup != null)
            {
                CamKirilmaEfektiOlustur(kup.transform.position, kup.GetKupRengi());
                kup.SetSeciliDurum(false);
            }
        }
        
        // 4. KÜÇÜLME ANİMASYONU - Scale 0'a düşür (SetActive KULLANMA!)
        float shrinkTime = 0.2f;
        float elapsed = 0f;
        
        while (elapsed < shrinkTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shrinkTime;
            float scale = 1f - t;
            
            foreach (KupData kup in patlayanKupler)
            {
                if (kup != null)
                {
                    kup.GetComponent<RectTransform>().localScale = Vector3.one * scale;
                }
            }
            yield return null;
        }
        
        // 5. SCALE = 0 (GİZLEME YOK - Grid Layout bozulmasın!)
        foreach (KupData kup in patlayanKupler)
        {
            if (kup != null)
            {
                kup.GetComponent<RectTransform>().localScale = Vector3.zero;
                
                // Yazıyı da gizle
                if (kup.harfYazisi != null)
                {
                    Color c = kup.harfYazisi.color;
                    c.a = 0f;
                    kup.harfYazisi.color = c;
                }
            }
        }
        
        // 6. PUAN EKLE - ComboManager carpani uygula
        int finalPuan = kazanilanPuan;
        if (ComboManager.Instance != null)
        {
            finalPuan = ComboManager.Instance.PuanHesapla(kazanilanPuan);
        }
        toplamPuan += finalPuan;
        GuncellePuanYazisi();
        
        // 7. TIMER - Dusuk suredeyse puan kadar saniye ekle
        if (TimerManager.Instance != null)
        {
            TimerManager.Instance.AddTimeIfLow(kazanilanPuan);
        }
        
        // 8. KISA BEKLEME
        yield return new WaitForSeconds(0.15f);
        
        // 9. YENİ HARF ATA
        foreach (KupData kup in patlayanKupler)
        {
            if (kup != null)
            {
                char yeniHarf = gridGen.RastgeleHarfAl();
                Color yeniRenk = gridGen.RastgeleRenkAl();
                kup.VeriAta(yeniHarf, yeniRenk);
            }
        }
        
        // 10. SIRAYLA POP-IN ANİMASYONU
        for (int i = 0; i < patlayanKupler.Count; i++)
        {
            KupData kup = patlayanKupler[i];
            if (kup != null)
            {
                StartCoroutine(PopInAnimasyonu(kup, i * 0.06f));
            }
        }
        
        // 11. ANİMASYONLARIN BİTMESİNİ BEKLE
        float toplamAnimSuresi = 0.4f + (patlayanKupler.Count * 0.06f);
        yield return new WaitForSeconds(toplamAnimSuresi);
        
        // 12. TEMİZLİK
        TemizleSecim();
        animasyonDevam = false;
    }
    
    /// <summary>
    /// Basit pop-in animasyonu - sadece scale değiştirir
    /// </summary>
    System.Collections.IEnumerator PopInAnimasyonu(KupData kup, float gecikme)
    {
        // Gecikme
        if (gecikme > 0f)
            yield return new WaitForSeconds(gecikme);
        
        RectTransform rect = kup.GetComponent<RectTransform>();
        
        // Yazı alpha'sını geri getir
        if (kup.harfYazisi != null)
        {
            Color c = kup.harfYazisi.color;
            c.a = 1f;
            kup.harfYazisi.color = c;
        }
        
        float sure = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < sure)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / sure;
            
            // Elastic-out benzeri: hızlı büyü, biraz aş, geri gel
            float scale;
            if (t < 0.6f)
            {
                // 0 -> 1.15 arası hızlı büyüme
                scale = Mathf.Lerp(0f, 1.15f, t / 0.6f);
            }
            else
            {
                // 1.15 -> 1.0 arası yavaş küçülme (bounce)
                float bounceT = (t - 0.6f) / 0.4f;
                scale = Mathf.Lerp(1.15f, 1f, bounceT);
            }
            
            rect.localScale = Vector3.one * scale;
            yield return null;
        }
        
        // Final scale
        rect.localScale = Vector3.one;
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


    // ==================== YANLIŞ KELİME EFEKTİ ====================

    System.Collections.IEnumerator YanlisKelimeEfekti()
    {
        // Seçili küplerin duman VFX'ini tetikle
        List<KupData> kupler = new List<KupData>(seciliKupler);
        foreach (KupData kup in kupler)
        {
            if (kup != null)
            {
                kup.YanlisKelimeVfxBaslat();
            }
        }

        // Ekranı kırmızı yaparak hata bildirimi
        StartCoroutine(EkraniRenklendir(new Color(1f, 0f, 0f, 1f)));
        
        // VFX'in görünmesi için kısa bekle
        yield return new WaitForSeconds(0.3f);
        
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
        // Önizleme sistemi devre dışı - ileride yeniden tasarlanacak
        return;
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
