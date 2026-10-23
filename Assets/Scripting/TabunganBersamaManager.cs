using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TabunganBersamaManager : MonoBehaviour
{
    [Header("Grup 1")]
    public Text nama1;
    public Text detail1;
    public Text terkumpul1;
    public Text persen1;
    public Slider slider1;

    [Header("Grup 2")]
    public Text nama2;
    public Text detail2;
    public Text terkumpul2;
    public Text persen2;
    public Slider slider2;

    [Header("Grup 3")]
    public Text nama3;
    public Text detail3;
    public Text terkumpul3;
    public Text persen3;
    public Slider slider3;

    private void Start()
    {
        IsiDataAwal();
    }

    void IsiDataAwal()
    {
        SetupGrup(
            nama1, detail1, terkumpul1, persen1, slider1,
            "Liburan Bali", 5, "30/12/2026",
            2500000, 10000000, "ADMIN"
        );

        SetupGrup(
            nama2, detail2, terkumpul2, persen2, slider2,
            "Beli Laptop", 3, "15/11/2026",
            4000000, 8000000, "ADMIN"
        );

        SetupGrup(
            nama3, detail3, terkumpul3, persen3, slider3,
            "Motor Baru", 8, "01/01/2027",
            6000000, 15000000, "ANGGOTA"
        );
    }

    void SetupGrup(
        Text nama,
        Text detail,
        Text terkumpul,
        Text persen,
        Slider slider,
        string namaGrup,
        int anggota,
        string tanggal,
        float current,
        float target,
        string posisi)
    {
        float progress = current / target;

        nama.text = namaGrup;

        detail.text =
            anggota + " anggota - tenggat " +
            tanggal + " | " + posisi;

        terkumpul.text =
            "Rp " + current.ToString("N0") +
            " dari Rp " + target.ToString("N0");

        persen.text =
            (progress * 100).ToString("F0") + "%";

        slider.value = progress;
    }

    public void BuatGrup()
    {
        SceneManager.LoadScene("TB2");
    }

    public void GabungGrup()
    {
        SceneManager.LoadScene("TB3");
    }
}