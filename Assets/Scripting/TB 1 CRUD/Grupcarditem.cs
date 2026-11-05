using UnityEngine;
using UnityEngine.UI;

public class GrupCardItem : MonoBehaviour
{
    [Header("Isi Card (drag GameObject-nya)")]
    public GameObject txtNama;
    public GameObject txtDetail;
    public GameObject txtTerkumpul;
    public GameObject txtPersentase;
    public GameObject txtPosisi;       // menampilkan ADMIN atau ANGGOTA
    public GameObject txtKode;         // opsional: kalau kosong, kode ditulis di baris detail
    public Slider slider;

    [Header("Tombol (hanya tampil untuk admin)")]
    public Button btnEdit;
    public Button btnHapus;

    private GrupData data;
    private CRUDManager manager;

    public void Setup(GrupData d, CRUDManager mgr, bool isAdmin)
    {
        data = d;
        manager = mgr;

        float persen = d.target > 0 ? (float)d.terkumpul / d.target * 100f : 0f;
        persen = Mathf.Clamp(persen, 0f, 100f);

        string detail = $"{d.jumlahAnggota} anggota - tenggat {d.tanggal}";

        // kode undangan hanya terlihat oleh admin, supaya bisa dibagikan
        if (isAdmin)
        {
            if (txtKode != null) UIHelper.SetText(txtKode, $"Kode: {d.kode}");
            else detail += $"  |  Kode: {d.kode}";
        }
        else
        {
            UIHelper.SetText(txtKode, "");
        }

        UIHelper.SetText(txtNama, d.nama);
        UIHelper.SetText(txtDetail, detail);
        UIHelper.SetText(txtTerkumpul, $"Rp {d.terkumpul:N0} dari Rp {d.target:N0}");
        UIHelper.SetText(txtPersentase, $"{persen:0}%");
        UIHelper.SetText(txtPosisi, isAdmin ? "ADMIN" : "ANGGOTA");

        if (slider)
        {
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.interactable = false;
            slider.value = persen;
        }

        // anggota tidak melihat tombol edit dan hapus
        btnEdit.gameObject.SetActive(isAdmin);
        btnHapus.gameObject.SetActive(isAdmin);

        btnEdit.onClick.RemoveAllListeners();
        btnHapus.onClick.RemoveAllListeners();
        btnEdit.onClick.AddListener(() => manager.BukaFormEdit(data.id));
        btnHapus.onClick.AddListener(() => manager.HapusGrup(data.id));
    }
}