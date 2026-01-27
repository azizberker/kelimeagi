using UnityEngine;

public class ComboManager : MonoBehaviour
{
    public static ComboManager Instance { get; private set; }

    [Header("Gorsel Referanslar")]
    [Tooltip("Harflerin toplanacagi cerceve alani")]
    public Transform cerceveAlani;
    
    [Tooltip("Alev Efekti (Combo Modu)")]
    public ParticleSystem alevEfekti;
    
    [Tooltip("Buz Efekti (Hata Modu)")]
    public ParticleSystem buzEfekti;

    [Header("Combo Ayarlari")]
    public float comboSuresi = 10f;
    public int gerekenKelimeSayisi = 3;

    private int ardisikKelimeSayisi = 0;
    private float sonKelimeZamani = -999f;
    private bool comboModuAktif = false;
    private bool buzModuAktif = false;

    // Mevcut carpan
    private float currentMultiplier = 1f;

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

    void Update()
    {
        // Combo suresi doldu mu kontrol et
        if (!comboModuAktif && ardisikKelimeSayisi > 0)
        {
            if (Time.time - sonKelimeZamani > comboSuresi)
            {
                Sifirla(false);
            }
        }
    }

    /// <summary>
    /// Dogru kelime bulundugunda cagrilir
    /// </summary>
    public void DogruKelime()
    {
        DogruKelime(0);
    }

    public void DogruKelime(int puan)
    {
        if (buzModuAktif)
        {
            BuzModunuKapat();
            ardisikKelimeSayisi = 1;
        }
        else
        {
            ardisikKelimeSayisi++;
        }

        sonKelimeZamani = Time.time;

        // Combo kontrolu
        if (!comboModuAktif && ardisikKelimeSayisi >= gerekenKelimeSayisi)
        {
            ComboModunuAc();
        }
    }

    /// <summary>
    /// Yanlis kelime veya iptal durumunda cagrilir
    /// </summary>
    public void YanlisKelime()
    {
        if (comboModuAktif)
        {
            ComboModunuKapat();
            BuzModunuAc();
        }
        
        Sifirla(true);
    }

    /// <summary>
    /// Ham puani combo carpani ile hesaplar
    /// </summary>
    public int PuanHesapla(int hamPuan)
    {
        if (comboModuAktif)
        {
            return hamPuan * 2; // Combodayken 2 kat puan
        }
        return Mathf.RoundToInt(hamPuan * currentMultiplier);
    }

    private void ComboModunuAc()
    {
        comboModuAktif = true;
        currentMultiplier = 2f;
        
        if (alevEfekti != null)
        {
            alevEfekti.Play();
        }
    }

    private void ComboModunuKapat()
    {
        comboModuAktif = false;
        currentMultiplier = 1f;
        
        if (alevEfekti != null)
        {
            alevEfekti.Stop();
            alevEfekti.Clear();
        }
    }

    private void BuzModunuAc()
    {
        buzModuAktif = true;
        
        if (buzEfekti != null)
        {
            buzEfekti.Play();
        }
    }

    private void BuzModunuKapat()
    {
        buzModuAktif = false;
        
        if (buzEfekti != null)
        {
            buzEfekti.Stop();
            buzEfekti.Clear();
        }
    }

    private void Sifirla(bool hataYapildi)
    {
        ardisikKelimeSayisi = 0;
    }
}
