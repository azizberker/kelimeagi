using UnityEngine;
using TMPro; // Yazı işlemleri için gerekli
using UnityEngine.UI; // Renk değişimi için gerekli

public class KupData : MonoBehaviour
{
    [Header("Bileşenler")]
    public TMP_Text harfYazisi; // Harfin yazacağı yer
    public Image kupGoruntusu;  // Rengini değiştireceğimiz görsel (Image)

    [HideInInspector]
    public char mevcutHarf; // Hafızada tuttuğumuz harf

    void Awake()
    {
        // Eğer Inspector'dan atanmadıysa, otomatik bul
        if (harfYazisi == null)
        {
            harfYazisi = GetComponentInChildren<TMP_Text>();
            if (harfYazisi == null)
            {
                Debug.LogError($"{gameObject.name}: TMP_Text bileşeni bulunamadı!");
            }
        }

        if (kupGoruntusu == null)
        {
            // Önce kendi üzerinde ara, sonra child'larda
            kupGoruntusu = GetComponent<Image>();
            if (kupGoruntusu == null)
            {
                kupGoruntusu = GetComponentInChildren<Image>();
            }
            
            if (kupGoruntusu == null)
            {
                Debug.LogError($"{gameObject.name}: Image bileşeni bulunamadı!");
            }
            else
            {
                Debug.Log($"{gameObject.name}: Image bulundu -> {kupGoruntusu.gameObject.name}");
            }
        }
    }

    // Bu fonksiyonu Yönetici çağıracak ve "Şu harfi al, şu renge bürün" diyecek
    public void VeriAta(char gelenHarf, Color gelenRenk)
    {
        // 1. Harfi ayarla
        mevcutHarf = gelenHarf;
        if (harfYazisi != null)
        {
            harfYazisi.text = gelenHarf.ToString();
        }

        // 2. Rengi ayarla
        if (kupGoruntusu != null)
        {
            Debug.Log($"{gameObject.name}: Renk atanıyor -> {gelenRenk}");
            kupGoruntusu.color = gelenRenk;
            Debug.Log($"{gameObject.name}: Yeni renk -> {kupGoruntusu.color}");
        }
        else
        {
            Debug.LogError($"{gameObject.name}: kupGoruntusu NULL, renk atanamadı!");
        }
    }
}