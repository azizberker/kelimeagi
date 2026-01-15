using UnityEngine;
using System.Collections.Generic; // Listeler için gerekli

public class GridGenerator : MonoBehaviour
{
    [Header("Ayarlar")]
    public GameObject kupPrefabi; // Oluşturacağımız küp kalıbı
    public Transform gridKutusu;  // Küplerin dizileceği Grid_Container
    
    [Header("Renk Paleti")]
    public Color[] renkListesi;   // Editörden seçeceğin renkler buraya gelecek

    // Türkçe karakterleri de içeren alfabe
    private string alfabe = "ABCÇDEFGĞHIİJKLMNOÖPRSŞTUÜVYZ"; 

    void Start()
    {
        TahtayiOlustur();
    }

    void TahtayiOlustur()
    {
        // Renk listesi kontrolü
        Debug.Log($"Renk listesi boyutu: {renkListesi.Length}");
        
        // 25 Tane Küp Üret (5x5 = 25)
        for (int i = 0; i < 25; i++)
        {
            // 1. Küpü sahnede oluştur
            GameObject yeniKup = Instantiate(kupPrefabi);
            
            // 2. Grid Container'ın içine koy (Hizalanması için)
            yeniKup.transform.SetParent(gridKutusu, false);

            // 3. Rastgele Veri Seç
            char rastgeleHarf = alfabe[Random.Range(0, alfabe.Length)];
            Color rastgeleRenk = Color.white; // Varsayılan beyaz

            // Eğer renk listesi boş değilse rastgele renk seç
            if (renkListesi != null && renkListesi.Length > 0)
            {
                rastgeleRenk = renkListesi[Random.Range(0, renkListesi.Length)];
                
                // Alpha değeri 0 ise 1 yap (şeffaflık sorununu önle)
                if (rastgeleRenk.a < 0.01f)
                {
                    rastgeleRenk.a = 1f;
                }
                
                Debug.Log($"Küp {i}: Seçilen renk = {rastgeleRenk}");
            }
            else
            {
                Debug.LogWarning("Renk listesi boş! Lütfen Inspector'dan renk ekleyin.");
            }

            // 4. Küpün scriptine ulaşıp verileri gönder
            KupData veriScripti = yeniKup.GetComponent<KupData>();
            if (veriScripti != null)
            {
                veriScripti.VeriAta(rastgeleHarf, rastgeleRenk);
            }
        }
    }
}