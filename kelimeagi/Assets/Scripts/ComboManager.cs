using UnityEngine;
using System.Collections.Generic;

public class ComboManager : MonoBehaviour
{
    public static ComboManager Instance { get; private set; }

    [Header("Görsel Referanslar")]
    [Tooltip("Harflerin toplanacağı çerçeve alanı")]
    public Transform cerceveAlani; // Harflerin gideceği Frame
    
    [Tooltip("Alev Efekti (Combo Modu)")]
    public ParticleSystem alevEfekti;
    
    [Tooltip("Buz Efekti (Hata Modu)")]
    public ParticleSystem buzEfekti;

    [Header("Combo Ayarları")]
    public float comboSuresi = 10f; // 10 saniye içinde 3 kelime
    public int gerekenKelimeSayisi = 3;

    private int ardisikKelimeSayisi = 0;
    private float sonKelimeZamani = -999f;
    private bool comboModuAktif = false;
    private bool buzModuAktif = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Combo süresi doldu mu kontrol et (Sadece combo modunda değilken sayaç işliyor)
        if (!comboModuAktif && ardisikKelimeSayisi > 0)
        {
            if (Time.time - sonKelimeZamani > comboSuresi)
            {
                Sifirla(false); // Sessizce sıfırla
            }
        }
    }

    public void DogruKelime(int puan)
    {
        if (buzModuAktif)
        {
            // Buz modundan çıkış
            BuzModunuKapat();
            ardisikKelimeSayisi = 1; // Yeni seriye başla
        }
        else
        {
            ardisikKelimeSayisi++;
        }

        sonKelimeZamani = Time.time;

        // Combo kontrolü
        if (!comboModuAktif && ardisikKelimeSayisi >= gerekenKelimeSayisi)
        {
            ComboModunuAc();
        }

        Debug.Log($"Doğru Kelime! Seri: {ardisikKelimeSayisi} - Combo Modu: {comboModuAktif}");
    }

    public void YanlisKelime()
    {
        if (comboModuAktif)
        {
            // Alevden Buza geçiş
            ComboModunuKapat();
            BuzModunuAc();
        }
        
        Sifirla(true); // Seriyi sıfırla
    }

    public int PuanHesapla(int hamPuan)
    {
        if (comboModuAktif)
        {
            return hamPuan * 2; // Combodayken 2 kat puan!
        }
        return hamPuan;
    }

    private void ComboModunuAc()
    {
        comboModuAktif = true;
        
        if (alevEfekti != null)
        {
            alevEfekti.Play();
        }
        
        Debug.Log("🔥 COMBO MODU AKTİF! ÇERÇEVE ALEV ALDI! 🔥");
    }

    private void ComboModunuKapat()
    {
        comboModuAktif = false;
        
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
        
        Debug.Log("❄️ BUZ MODU! DONDUN! ❄️");
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
        // Eğer hata yapılmadıysa (süre dolduysa) ve combo açık değilse sadece sayacı sıfırladık
        // Eğer hata yapıldıysa yukarıda YanlisKelime içinde buz modunu zaten açtık
    }
}
