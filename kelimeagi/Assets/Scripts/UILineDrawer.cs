using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;

public class UILineDrawer : MonoBehaviour
{
    [Header("UI Referansları")]
    public TMP_Text seciliKelimeYazisi;
    public Canvas anaCanvas;

    // Seçilen küpler
    private List<KupData> seciliKupler = new List<KupData>();
    private RectTransform canvasRect;
    private GameObject hataPaneli;

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
        bool tiklandiMi = false;

        // Touch kontrolü
        if (Touchscreen.current != null)
        {
            tiklandiMi = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        }
        // Mouse kontrolü
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
        
        if (tiklananKup != null)
        {
            // Zaten seçiliyse, seçimi kaldır
            if (seciliKupler.Contains(tiklananKup))
            {
                SecimdenKaldir(tiklananKup);
                Debug.Log($"Küp seçimden çıkarıldı: {tiklananKup.mevcutHarf}");
            }
            else
            {
                // Yeni küp seç
                KupuSec(tiklananKup);
                Debug.Log($"Küp seçildi: {tiklananKup.mevcutHarf}");
            }
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

    void KupuSec(KupData kup)
    {
        seciliKupler.Add(kup);
        
        // Küpü görsel olarak işaretle (sarı/turuncu tona çevir)
        if (kup.kupGoruntusu != null)
        {
            kup.kupGoruntusu.color = new Color(1f, 0.85f, 0.4f, 1f);
        }
        
        GuncelleKelimeYazisi();
    }

    void SecimdenKaldir(KupData kup)
    {
        seciliKupler.Remove(kup);
        
        // Görsel olarak seçimi kaldır
        if (kup.kupGoruntusu != null)
        {
            kup.kupGoruntusu.color = Color.white;
        }
        
        GuncelleKelimeYazisi();
    }

    // Kelimeyi onayla (butondan çağrılabilir)
    public void KelimeyiOnayla()
    {
        if (seciliKupler.Count < 2)
        {
            Debug.Log("Yeterli harf seçilmedi (minimum 2)");
            return;
        }

        string seciliKelime = SecilenKelimeyiAl();
        string kelimeUpper = seciliKelime.ToUpper();
        Debug.Log($"Kontrol edilen kelime: '{kelimeUpper}'");

        if (KelimeVeritabani.Instance != null)
        {
            bool gecerliMi = KelimeVeritabani.Instance.KelimeGecerliMi(kelimeUpper);
            
            if (gecerliMi)
            {
                Debug.Log($"✓ Geçerli kelime: {kelimeUpper}");
                StartCoroutine(EkraniRenklendir(new Color(0f, 1f, 0f, 1f)));
                StartCoroutine(KupleriPatlat(new List<KupData>(seciliKupler)));
            }
            else
            {
                Debug.Log($"✗ Geçersiz kelime: {kelimeUpper}");
                StartCoroutine(YanlisKelimeEfekti());
            }
        }
        
        // Seçimi temizle
        TemizleSecim();
    }

    // Seçimi temizle (butondan çağrılabilir)
    public void SecimiBosalt()
    {
        TemizleSecim();
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
        // Tüm küplerin rengini sıfırla
        foreach (KupData kup in seciliKupler)
        {
            if (kup != null && kup.kupGoruntusu != null)
            {
                kup.kupGoruntusu.color = Color.white;
            }
        }
        
        seciliKupler.Clear();

        if (seciliKelimeYazisi != null)
        {
            seciliKelimeYazisi.text = "";
        }
    }

    // ==================== PATLAMA EFEKTLERİ ====================

    System.Collections.IEnumerator KupleriPatlat(List<KupData> patlayanKupler)
    {
        GridGenerator gridGen = FindAnyObjectByType<GridGenerator>();
        
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
        
        // 2. Patlayan küpleri doğrudan yenile (aynı pozisyonda yeni harf)
        foreach (KupData kup in patlayanKupler)
        {
            if (kup != null)
            {
                // Küpü yeniden etkinleştir
                kup.gameObject.SetActive(true);
                
                RectTransform rect = kup.GetComponent<RectTransform>();
                Image kupImg = kup.kupGoruntusu;
                TMP_Text harfText = kup.harfYazisi;
                
                // Ölçeği ve rotasyonu sıfırla
                rect.localScale = Vector3.zero;
                rect.localRotation = Quaternion.identity;
                
                // Yeni harf ve renk ata
                char yeniHarf = gridGen != null ? gridGen.RastgeleHarfAl() : 'A';
                Color yeniRenk = gridGen != null ? gridGen.RastgeleRenkAl() : Color.white;
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
            }
        }
        
        yield return new WaitForSeconds(0.4f);
        
        // 3. Gridde geçerli kelime olup olmadığını kontrol et
        if (gridGen != null && KelimeVeritabani.Instance != null)
        {
            char[] mevcutHarfler = MevcutGridHarfleriniAl(gridGen);
            bool gecerliKelimeVar = KelimeVeritabani.Instance.GridGecerliMi(mevcutHarfler, 1);
            
            if (!gecerliKelimeVar)
            {
                Debug.Log("⚠️ Gridde geçerli kelime yok! Grid yenileniyor...");
                gridGen.GridiYenile();
            }
            else
            {
                Debug.Log("✓ Gridde en az 1 geçerli kelime var.");
            }
        }
    }

    char[] MevcutGridHarfleriniAl(GridGenerator gridGen)
    {
        List<KupData> tumKupler = gridGen.TumKuplerAl();
        char[] harfler = new char[tumKupler.Count];
        
        for (int i = 0; i < tumKupler.Count; i++)
        {
            if (tumKupler[i] != null && tumKupler[i].gameObject.activeSelf)
            {
                harfler[i] = tumKupler[i].mevcutHarf;
            }
        }
        
        return harfler;
    }

    System.Collections.IEnumerator KupPatlamaAnimasyonu(KupData kup)
    {
        RectTransform rect = kup.GetComponent<RectTransform>();
        Image kupImg = kup.kupGoruntusu;
        TMP_Text harfText = kup.harfYazisi;
        
        float sure = 0.35f;
        float gecen = 0f;
        
        Vector3 baslangicOlcek = rect.localScale;
        
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

    // ==================== YANLIŞ KELİME EFEKTİ ====================

    System.Collections.IEnumerator YanlisKelimeEfekti()
    {
        // Ekranı kızart
        StartCoroutine(EkraniRenklendir(new Color(1f, 0f, 0f, 1f)));

        // Seçili küpleri kırmızı titret
        List<KupData> kupler = new List<KupData>(seciliKupler);
        
        for (int i = 0; i < 2; i++)
        {
            // Kırmızıya dön
            foreach (KupData kup in kupler)
            {
                if (kup != null && kup.kupGoruntusu != null)
                {
                    kup.kupGoruntusu.color = new Color(1f, 0.3f, 0.3f, 1f);
                }
            }
            yield return new WaitForSeconds(0.06f);
            
            // Normal renge dön
            foreach (KupData kup in kupler)
            {
                if (kup != null && kup.kupGoruntusu != null)
                {
                    kup.kupGoruntusu.color = Color.white;
                }
            }
            yield return new WaitForSeconds(0.06f);
        }
    }

    System.Collections.IEnumerator EkraniRenklendir(Color hedefRenk)
    {
        if (hataPaneli == null)
        {
            HataPaneliOlustur();
        }

        Image img = hataPaneli.GetComponent<Image>();
        if (img == null) yield break;

        // Görünür yap
        hataPaneli.SetActive(true);

        // Parlat
        float sure = 0.1f;
        float gecen = 0f;

        while (gecen < sure)
        {
            gecen += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 0.3f, gecen / sure);
            img.color = new Color(hedefRenk.r, hedefRenk.g, hedefRenk.b, alpha);
            yield return null;
        }

        // Söndür
        sure = 0.15f;
        gecen = 0f;
        while (gecen < sure)
        {
            gecen += Time.deltaTime;
            float alpha = Mathf.Lerp(0.3f, 0f, gecen / sure);
            img.color = new Color(hedefRenk.r, hedefRenk.g, hedefRenk.b, alpha);
            yield return null;
        }

        img.color = new Color(hedefRenk.r, hedefRenk.g, hedefRenk.b, 0f);
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
}
