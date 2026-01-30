using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System;

/// <summary>
/// Oyun zamanlayici sistemi.
/// Hierarchy'de TimerManager adli bir GameObject'e eklenebilir.
/// </summary>
public class TimerManager : MonoBehaviour
{
    public static TimerManager Instance { get; private set; }

    [Header("Zaman Ayarlari")]
    [Tooltip("Baslangic suresi (saniye)")]
    public float StartSeconds = 180f;
    
    [Tooltip("Dusuk sure esigi - bu degerin altinda sure eklenebilir")]
    public float LowTimeThreshold = 30f;
    
    [Tooltip("True ise sure baslangic degerini asamaz")]
    public bool ClampToStartTime = true;

    [Header("UI Referanslari")]
    [Tooltip("TextMeshPro kullaniyorsan buraya ata")]
    public TMP_Text timeTextTMP;
    
    [Tooltip("Legacy Text kullaniyorsan buraya ata")]
    public Text timeTextLegacy;

    [Header("Events")]
    [Tooltip("Sure bittiginde tetiklenir (Inspector'dan atanabilir)")]
    public UnityEvent OnTimeOverUnityEvent;

    // C# event - kod ile dinlenebilir
    public event Action OnTimeOver;

    // Mevcut kalan sure
    private float currentTime;
    private bool isRunning = false;
    private bool hasTriggeredTimeOver = false;

    /// <summary>
    /// Kalan sureyi okumak icin
    /// </summary>
    public float CurrentTime => currentTime;

    void Awake()
    {
        // Singleton pattern
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

    void Start()
    {
        // Oyun basladiginda timer'i baslat
        ResetTimer();
        StartTimer();
    }

    // Performans icin - son gosterilen saniye
    private int lastDisplayedSecond = -1;
    
    void Update()
    {
        if (!isRunning) return;

        // Sure azalt
        currentTime -= Time.deltaTime;

        // UI sadece saniye degistiginde guncelle (performans icin)
        int currentSecond = Mathf.FloorToInt(currentTime);
        if (currentSecond != lastDisplayedSecond)
        {
            lastDisplayedSecond = currentSecond;
            UpdateTimeDisplay();
        }

        // Sure bitti mi?
        if (currentTime <= 0f)
        {
            currentTime = 0f;
            isRunning = false;
            
            // Sadece bir kere tetikle
            if (!hasTriggeredTimeOver)
            {
                hasTriggeredTimeOver = true;
                TriggerTimeOver();
            }
        }
    }

    /// <summary>
    /// Timer'i baslangic degerine sifirlar
    /// </summary>
    public void ResetTimer()
    {
        currentTime = StartSeconds;
        hasTriggeredTimeOver = false;
        UpdateTimeDisplay();
    }

    /// <summary>
    /// Timer'i baslatir
    /// </summary>
    public void StartTimer()
    {
        isRunning = true;
        hasTriggeredTimeOver = false;
    }

    /// <summary>
    /// Timer'i durdurur
    /// </summary>
    public void StopTimer()
    {
        isRunning = false;
    }

    /// <summary>
    /// Kalan sure dusuk mu? (LowTimeThreshold altinda mi)
    /// </summary>
    public bool IsLowTime()
    {
        return currentTime <= LowTimeThreshold;
    }

    /// <summary>
    /// Eger sure dusukse (LowTimeThreshold altinda) sure ekler.
    /// ClampToStartTime true ise baslangic degerini asmaz.
    /// </summary>
    /// <param name="secondsToAdd">Eklenecek saniye</param>
    public void AddTimeIfLow(float secondsToAdd)
    {
        // Sadece dusuk suredeyken ekle
        if (!IsLowTime()) return;
        
        currentTime += secondsToAdd;
        
        // Clamp kontrolu
        if (ClampToStartTime && currentTime > StartSeconds)
        {
            currentTime = StartSeconds;
        }
        
        UpdateTimeDisplay();
    }

    /// <summary>
    /// Kosula bakmadan sure ekler (ozel durumlar icin)
    /// </summary>
    public void AddTime(float secondsToAdd)
    {
        currentTime += secondsToAdd;
        
        if (ClampToStartTime && currentTime > StartSeconds)
        {
            currentTime = StartSeconds;
        }
        
        UpdateTimeDisplay();
    }

    private void UpdateTimeDisplay()
    {
        string timeString = FormatTime(currentTime);
        
        // TextMeshPro varsa onu kullan
        if (timeTextTMP != null)
        {
            timeTextTMP.text = timeString;
        }
        
        // Legacy Text varsa onu kullan
        if (timeTextLegacy != null)
        {
            timeTextLegacy.text = timeString;
        }
    }

    private string FormatTime(float time)
    {
        if (time < 0f) time = 0f;
        
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void TriggerTimeOver()
    {
        // C# event
        OnTimeOver?.Invoke();
        
        // UnityEvent (Inspector'dan atanabilen)
        OnTimeOverUnityEvent?.Invoke();
    }
}
