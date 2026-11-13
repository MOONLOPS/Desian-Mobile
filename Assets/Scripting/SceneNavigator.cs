using System.Collections.Generic;
using UnityEngine.SceneManagement;

// Navigasi antar scene. Tombol kembali selalu menuju scene "induk" yang tetap,
// jadi alurnya tidak bisa nyasar walaupun scene dibuka dari mana saja.
public static class SceneNavigator
{
    private static readonly Dictionary<string, string> induk = new Dictionary<string, string>
    {
        { SceneNames.DaftarTabungan,   SceneNames.Dashboard },
        { SceneNames.ApaImpianmu,      SceneNames.DaftarTabungan },
        { SceneNames.HomeTabunganku,   SceneNames.DaftarTabungan },
        { SceneNames.CatatTabungan,    SceneNames.HomeTabunganku },
        { SceneNames.TargetTercapai,   SceneNames.DaftarTabungan },

        { SceneNames.TabunganBersama,  SceneNames.Dashboard },
        { SceneNames.DetailGrup,       SceneNames.TabunganBersama },
        { SceneNames.CatatSetoran,     SceneNames.DetailGrup },
        { SceneNames.KonfirmasiSetoran, SceneNames.DetailGrup },
    };

    public static void Buka(string scene)
    {
        SceneManager.LoadScene(scene);
    }

    // Dipakai tombol panah kembali di semua scene.
    public static void Kembali()
    {
        string sekarang = SceneManager.GetActiveScene().name;
        string tujuan;
        if (induk.TryGetValue(sekarang, out tujuan)) Buka(tujuan);
    }
}