using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Kelime uzunluguna gore farkli VFX prefablar spawn eder.
/// Inspector'dan her uzunluk icin ayri prefab atanabilir.
/// </summary>
public class WordVfxTrigger : MonoBehaviour
{
    public static WordVfxTrigger Instance { get; private set; }

    [Serializable]
    public class VfxConfig
    {
        [Tooltip("Bu VFX icin gereken harf sayisi")]
        public int wordLength = 3;
        
        [Tooltip("Spawn edilecek VFX prefab")]
        public GameObject prefab;
    }

    [Header("VFX Ayarlari")]
    [Tooltip("Kelime uzunluguna gore VFX prefablari")]
    public VfxConfig[] vfxConfigs;
    
    [Tooltip("VFX'in cikacagi pozisyon (ComboPopupAnchor)")]
    public RectTransform comboPopupAnchor;
    
    [Tooltip("VFX'in parent'i olacak transform (genellikle Canvas)")]
    public Transform spawnParent;
    
    [Tooltip("VFX'in ekranda kalma suresi")]
    public float lifeTime = 1.5f;

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
    /// Kelime uzunluguna gore uygun VFX'i spawn eder.
    /// </summary>
    public void TryShowVfx(string word)
    {
        // Null veya bos kontrol
        if (string.IsNullOrEmpty(word)) return;
        
        // Config dizisi kontrolu
        if (vfxConfigs == null || vfxConfigs.Length == 0) return;
        
        // Ayni kelime tekrar spawn onleme (0.5 saniye icinde)
        if (word == lastSpawnedWord && Time.time - lastSpawnTime < 0.5f) return;
        
        // Kelime uzunluguna uygun config'i bul
        GameObject prefabToSpawn = null;
        foreach (var config in vfxConfigs)
        {
            if (config != null && config.prefab != null && word.Length == config.wordLength)
            {
                prefabToSpawn = config.prefab;
                break;
            }
        }
        
        // Uygun prefab bulunamadiysa cik
        if (prefabToSpawn == null) return;
        
        // VFX spawn et
        SpawnVfx(prefabToSpawn, word);
        
        // Kaydet
        lastSpawnedWord = word;
        lastSpawnTime = Time.time;
    }

    private void SpawnVfx(GameObject prefab, string word)
    {
        if (prefab == null) return;
        
        // 3D Particle mi yoksa UI prefab mi kontrol et
        bool is3DParticle = prefab.GetComponent<RectTransform>() == null;
        
        GameObject vfxInstance;
        
        if (is3DParticle && comboPopupAnchor != null)
        {
            // 3D Particle - World space'de anchor pozisyonunda olustur
            Vector3 worldPos = comboPopupAnchor.position;
            vfxInstance = Instantiate(prefab, worldPos, Quaternion.identity);
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
            
            vfxInstance = Instantiate(prefab, parent);
            
            // RectTransform ayarla
            RectTransform vfxRect = vfxInstance.GetComponent<RectTransform>();
            if (vfxRect != null && comboPopupAnchor != null)
            {
                vfxRect.anchoredPosition = comboPopupAnchor.anchoredPosition;
                vfxRect.localScale = Vector3.one;
            }
        }
        
        if (vfxInstance == null) return;
        
        // Eger prefab'da text varsa kelimeyi yazdir
        SetTextIfExists(vfxInstance, word);
        
        // Otomatik yok et
        Destroy(vfxInstance, lifeTime);
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
