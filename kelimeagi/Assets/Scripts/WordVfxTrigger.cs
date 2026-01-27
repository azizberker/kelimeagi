using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 6 harfli dogru kelimeler icin UI VFX tetikleyici.
/// Canvas altindaki ComboPopupAnchor pozisyonunda VFX prefab spawn eder.
/// </summary>
public class WordVfxTrigger : MonoBehaviour
{
    public static WordVfxTrigger Instance { get; private set; }

    [Header("VFX Ayarlari")]
    [Tooltip("Spawn edilecek animasyonlu text VFX prefab")]
    public GameObject vfxPrefab;
    
    [Tooltip("VFX'in cikacagi pozisyon (ComboPopupAnchor)")]
    public RectTransform comboPopupAnchor;
    
    [Tooltip("VFX'in parent'i olacak transform (genellikle Canvas)")]
    public Transform spawnParent;
    
    [Tooltip("VFX'in ekranda kalma suresi")]
    public float lifeTime = 1.5f;

    [Header("Kelime Ayarlari")]
    [Tooltip("VFX tetiklemek icin gereken minimum harf sayisi")]
    public int requiredLength = 6;

    // Ayni frame'de birden fazla spawn onlemek icin
    private string lastSpawnedWord = "";
    private float lastSpawnTime = -999f;

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Kelime uzunlugunu kontrol eder, requiredLength harfli ise VFX spawn eder.
    /// </summary>
    /// <param name="word">Dogru bulunan kelime</param>
    public void TryShowVfx(string word)
    {
        Debug.Log($"[WordVfxTrigger] TryShowVfx cagirildi: '{word}', Uzunluk: {word?.Length ?? 0}, Gereken: {requiredLength}");
        
        // Null veya bos kontrol
        if (string.IsNullOrEmpty(word))
        {
            Debug.Log("[WordVfxTrigger] Kelime bos veya null!");
            return;
        }
        
        // Uzunluk kontrolu
        if (word.Length != requiredLength)
        {
            Debug.Log($"[WordVfxTrigger] Uzunluk eslesmiyor: {word.Length} != {requiredLength}");
            return;
        }
        
        // Ayni kelime tekrar spawn onleme (0.5 saniye icinde)
        if (word == lastSpawnedWord && Time.time - lastSpawnTime < 0.5f)
        {
            Debug.Log("[WordVfxTrigger] Ayni kelime tekrar spawn engellendi!");
            return;
        }
        
        Debug.Log($"[WordVfxTrigger] VFX SPAWN EDILIYOR: {word}");
        
        // VFX spawn et
        SpawnVfx(word);
        
        // Kaydet
        lastSpawnedWord = word;
        lastSpawnTime = Time.time;
    }

    private void SpawnVfx(string word)
    {
        // Prefab kontrolu
        if (vfxPrefab == null)
        {
            Debug.LogError("[WordVfxTrigger] vfxPrefab atanmamis!");
            return;
        }
        
        Debug.Log($"[WordVfxTrigger] Anchor world pos: {(comboPopupAnchor != null ? comboPopupAnchor.position.ToString() : "null")}");
        
        // 3D Particle mi yoksa UI prefab mi kontrol et
        bool is3DParticle = vfxPrefab.GetComponent<RectTransform>() == null;
        
        GameObject vfxInstance;
        
        if (is3DParticle && comboPopupAnchor != null)
        {
            // 3D Particle - World space'de anchor pozisyonunda olustur
            Vector3 worldPos = comboPopupAnchor.position;
            vfxInstance = Instantiate(vfxPrefab, worldPos, Quaternion.identity);
            Debug.Log($"[WordVfxTrigger] 3D Particle olusturuldu: {vfxInstance.name} pozisyon: {worldPos}");
        }
        else
        {
            // UI Prefab - Canvas altinda olustur
            Transform parent = spawnParent;
            if (parent == null && comboPopupAnchor != null)
            {
                parent = comboPopupAnchor.parent;
            }
            if (parent == null)
            {
                parent = transform;
            }
            
            vfxInstance = Instantiate(vfxPrefab, parent);
            
            // RectTransform ayarla
            RectTransform vfxRect = vfxInstance.GetComponent<RectTransform>();
            if (vfxRect != null && comboPopupAnchor != null)
            {
                vfxRect.anchoredPosition = comboPopupAnchor.anchoredPosition;
                vfxRect.localScale = Vector3.one;
                Debug.Log($"[WordVfxTrigger] UI VFX pozisyon: {vfxRect.anchoredPosition}");
            }
        }
        
        if (vfxInstance == null)
        {
            Debug.LogError("[WordVfxTrigger] Instantiate basarisiz!");
            return;
        }
        
        Debug.Log($"[WordVfxTrigger] VFX olusturuldu: {vfxInstance.name}");
        
        // Eger prefab'da text varsa kelimeyi yazdir (opsiyonel)
        SetTextIfExists(vfxInstance, word);
        
        // Otomatik yok et
        Destroy(vfxInstance, lifeTime);
        Debug.Log($"[WordVfxTrigger] {lifeTime} saniye sonra silinecek");
    }

    private void SetTextIfExists(GameObject obj, string word)
    {
        // TextMeshPro kontrolu
        TMP_Text tmpText = obj.GetComponentInChildren<TMP_Text>();
        if (tmpText != null)
        {
            tmpText.text = word.ToUpper();
            return;
        }
        
        // Legacy Text kontrolu
        Text legacyText = obj.GetComponentInChildren<Text>();
        if (legacyText != null)
        {
            legacyText.text = word.ToUpper();
        }
    }
}
