using UnityEngine;
using UnityEngine.UI;

public class ComboAutoSetup : MonoBehaviour
{
    [ContextMenu("Setup Combo System")]
    public void SetupSystem()
    {
        SetupComboManager();
    }

    void Awake()
    {
        // Oyun başladığında yoksa otomatik kur
        if (FindAnyObjectByType<ComboManager>() == null)
        {
            SetupComboManager();
        }
    }

    void SetupComboManager()
    {
        Debug.Log("🔧 Combo Sistemi Otomatik Kurulum Başlatılıyor...");

        // 1. ComboManager'ı Bul veya Yarat
        ComboManager manager = FindAnyObjectByType<ComboManager>();
        if (manager == null)
        {
            GameObject managerObj = new GameObject("ComboManager");
            manager = managerObj.AddComponent<ComboManager>();
            Debug.Log("Created ComboManager object.");
        }

        // 2. Canvas'ı Bul
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Sahne'de Canvas bulunamadı! Lütfen bir Canvas olduğundan emin olun.");
            return;
        }

        // 3. Combo Frame'i Yarat (Eğer yoksa)
        if (manager.cerceveAlani == null)
        {
            // Frame objesi yarat
            GameObject frameObj = new GameObject("ComboFrame");
            frameObj.transform.SetParent(canvas.transform, false);
            
            // Image bileşeni ekle
            Image img = frameObj.AddComponent<Image>();
            
            // Görseli Resources'tan yükle
            Sprite frameSprite = Resources.Load<Sprite>("combo_frame");
            if (frameSprite != null)
            {
                img.sprite = frameSprite;
                img.type = Image.Type.Sliced; // Sliced olması için sprite ayarlarının yapılmış olması önerilir
                Debug.Log("Combo Frame görseli yüklendi.");
            }
            else
            {
                Debug.LogWarning("Resources/combo_frame bulunamadı! Varsayılan bir renk kullanılacak.");
                img.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            }

            // Pozisyonlama (Ekranın üst kısmı)
            RectTransform rect = frameObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f); // Üst Orta
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -150); // Biraz aşağıda
            rect.sizeDelta = new Vector2(500, 150); // Geniş bir çerçeve

            // Manager'a ata
            manager.cerceveAlani = frameObj.transform;
            Debug.Log("Combo Frame oluşturuldu ve atandı.");
        }

        // 4. Parçacık Efektleri (Hazır yoksa geçici oluştur)
        if (manager.alevEfekti == null)
        {
            manager.alevEfekti = CreatePlaceholderParticle("FireEffect_Placeholder", Color.red, manager.cerceveAlani);
            Debug.Log("Alev efekti (geçici) oluşturuldu.");
        }

        if (manager.buzEfekti == null)
        {
            manager.buzEfekti = CreatePlaceholderParticle("IceEffect_Placeholder", Color.cyan, manager.cerceveAlani);
            Debug.Log("Buz efekti (geçici) oluşturuldu.");
        }

        Debug.Log("✅ Combo Sistemi Kurulumu Tamamlandı!");
    }

    ParticleSystem CreatePlaceholderParticle(string name, Color color, Transform parent)
    {
        GameObject pObj = new GameObject(name);
        pObj.transform.SetParent(parent, false);
        pObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = pObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = color;
        main.startSize = 1f;
        main.startLifetime = 1f;
        main.loop = true;
        
        var emission = ps.emission;
        emission.rateOverTime = 20f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        
        ps.Stop(); // Başlangıçta durdur
        return ps;
    }
}
