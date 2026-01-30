using UnityEngine;

/// <summary>
/// Mobil performans optimizasyonlari
/// </summary>
public class PerformanceManager : MonoBehaviour
{
    [Header("Frame Rate Ayarlari")]
    [Tooltip("Hedef FPS (60 veya 30 onerilir)")]
    public int targetFPS = 60;
    
    [Header("Kalite Ayarlari")]
    [Tooltip("VSync kapali daha iyi performans verir")]
    public bool vSyncKapali = true;
    
    [Tooltip("Dusuk kalite seviyesi daha iyi performans verir (0-5)")]
    [Range(0, 5)]
    public int kaliteSeviyesi = 2;

    void Awake()
    {
        // Hedef FPS ayarla
        Application.targetFrameRate = targetFPS;
        
        // VSync
        QualitySettings.vSyncCount = vSyncKapali ? 0 : 1;
        
        // Kalite seviyesi
        QualitySettings.SetQualityLevel(kaliteSeviyesi, true);
        
        // Ekran uyanik kalsin
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        
        // Multi-touch limit (performans icin)
        Input.multiTouchEnabled = false;
    }

    void Start()
    {
        // Garbage collection optimize
        OptimizeGC();
    }

    void OptimizeGC()
    {
        // GC'yi daha az calistir
        System.GC.Collect();
        
        // Incremental GC aktif (Unity 2019.1+)
        #if UNITY_2019_1_OR_NEWER
        UnityEngine.Scripting.GarbageCollector.GCMode = UnityEngine.Scripting.GarbageCollector.Mode.Enabled;
        #endif
    }
    
    // Inspector'dan ayarlari degistirince uygula
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            Application.targetFrameRate = targetFPS;
            QualitySettings.vSyncCount = vSyncKapali ? 0 : 1;
            QualitySettings.SetQualityLevel(kaliteSeviyesi, true);
        }
    }
}
