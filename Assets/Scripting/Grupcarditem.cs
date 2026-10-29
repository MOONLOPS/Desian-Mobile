using UnityEngine;
using UnityEngine.UI;

public class GrupCardItem : MonoBehaviour
{
    [Header("Isi Card (drag GameObject-nya)")]
    public GameObject txtNama;
    public GameObject txtDetail;
    public GameObject txtTerkumpul;
    public GameObject txtPersentase;
    public GameObject txtPosisi;
    public Slider slider;

    [Header("Tombol")]
    public Button btnEdit;
    public Button btnHapus;

    private GrupData data;
    private CRUDManager manager;

    public void Setup(GrupData d, CRUDManager mgr)
    {
        data = d;
        manager = mgr;

        float persen = d.target > 0 ? (float)d.terkumpul / d.target * 100f : 0f;
        persen = Mathf.Clamp(persen, 0f, 100f);

        UIHelper.SetText(txtNama, d.nama);
        UIHelper.SetText(txtDetail, $"{d.jumlahAnggota} anggota - tenggat {d.tanggal}");
        UIHelper.SetText(txtTerkumpul, $"Rp {d.terkumpul:N0} dari Rp {d.target:N0}");
        UIHelper.SetText(txtPersentase, $"{persen:0}%");
        UIHelper.SetText(txtPosisi, "ADMIN");

        if (slider)
        {
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.interactable = false;
            slider.value = persen;
        }

        btnEdit.onClick.RemoveAllListeners();
        btnHapus.onClick.RemoveAllListeners();
        btnEdit.onClick.AddListener(() => manager.BukaFormEdit(data.id));
        btnHapus.onClick.AddListener(() => manager.HapusGrup(data.id));
    }
}