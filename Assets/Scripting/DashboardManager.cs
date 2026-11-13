using UnityEngine;
using UnityEngine.UI;

// Scene: Dashboard ("Selamat Datang [user]")
public class DashboardController : MonoBehaviour
{
    public GameObject txtSapaan;          // teks "Selamat Datang [user]"
    public Button btnTabunganku;
    public Button btnTabunganBersama;
    public Button btnKeluar;              // opsional

    void Start()
    {
        if (!Session.PastikanLogin()) return;                         // <- tambahkan
        UIHelper.SetText(txtSapaan, "Selamat Datang " + Session.Nama); // <- tambahkan

        btnTabunganku.onClick.AddListener(() => SceneNavigator.Buka(SceneNames.DaftarTabungan));
        btnTabunganBersama.onClick.AddListener(() => SceneNavigator.Buka(SceneNames.TabunganBersama));

        if (btnKeluar != null)
            btnKeluar.onClick.AddListener(() =>
            {
                Session.Keluar();
                SceneNavigator.Buka(SceneNames.Login);
            });
    }
}