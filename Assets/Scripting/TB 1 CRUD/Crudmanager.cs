using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static Unity.Burst.Intrinsics.X86;

// Scene: TB1 ("Tabungan Bersama": daftar grup + popup Buat/Edit + panel Gabung dengan kode)
// Create + Update : popup EditGrup     Read : daftar card     Delete : tombol hapus di card
public class CRUDManager : MonoBehaviour
{
    [Header("List")]
    public Transform content;             // Scroll View > Viewport > Content
    public GameObject grupCardPrefab;     // prefab GrupCard

    [Header("Tombol Utama")]
    public Button btnBuatGrup;            // "btn buat"
    public Button btnGabungGrup;          // "btn gabung"
    public Button btnKembali;             // opsional: kembali ke Menu

    [Header("Popup Buat/Edit (EditGrup)")]
    public GameObject panelEdit;
    public GameObject txtJudulForm;       // teks judul DI DALAM EditGrup
    public GameObject inputNama;
    public GameObject inputTarget;
    public GameObject inputTanggal;
    public GameObject inputAnggota;
    public Button btnTambah;
    public Button btnKurang;
    public Button btnClose;
    public Button btnSimpan;

    [Header("Panel Gabung Grup")]
    public GameObject panelGabung;        // panel "Gabung Grup"
    public GameObject inputKode;          // "input kode"
    public Button btnGabung;              // "btn_gabung"
    public Button btnCloseGabung;         // opsional: tombol tutup panel
    public GameObject txtPesanGabung;     // opsional: teks kecil untuk pesan hasil

    [Header("Pesan (opsional)")]
    public GameObject txtStatus;          // di bawah daftar: "Memuat...", "Grup dibuat. Kode: XXXXXX", pesan error
    public GameObject txtPesanForm;       // di dalam popup EditGrup: pesan error saat simpan

    private string editId = null;         // null = mode tambah, selain itu = mode edit (kunci grup di Firebase)
    private string pesanInfo = "";       // pesan yang tampil setelah daftar selesai dimuat (mis. kode grup baru)
    private bool sibuk;                   // mencegah tombol terkirim dua kali selama proses ke Firebase
    private readonly Dictionary<string, GrupData> cache = new Dictionary<string, GrupData>();

    // ---------- LIFECYCLE ----------
    void Start()
    {
        if (!Session.PastikanLogin()) return;

        // pengaman: kalau dua tombol utama tertukar di Inspector, tukar otomatis
        if (btnBuatGrup != null && btnGabungGrup != null &&
            btnBuatGrup.name.ToLower().Contains("gabung") &&
            btnGabungGrup.name.ToLower().Contains("buat"))
        {
            Button tmp = btnBuatGrup;
            btnBuatGrup = btnGabungGrup;
            btnGabungGrup = tmp;
        }
        Debug.Log("Buat = " + btnBuatGrup.name + " | Gabung = " + (btnGabungGrup != null ? btnGabungGrup.name : "KOSONG"));

        btnBuatGrup.onClick.AddListener(BukaFormTambah);
        Hook(btnGabungGrup, BukaPanelGabung);
        Hook(btnKembali, SceneNavigator.Kembali);

        btnClose.onClick.AddListener(TutupForm);
        btnSimpan.onClick.AddListener(Simpan);
        btnTambah.onClick.AddListener(() => UbahAnggota(+1));
        btnKurang.onClick.AddListener(() => UbahAnggota(-1));

        Hook(btnGabung, Gabung);
        Hook(btnCloseGabung, TutupPanelGabung);

        UIHelper.SetNumberOnly(inputAnggota);
        UIHelper.SetNumberOnly(inputTarget);

        panelEdit.SetActive(false);
        Aktif(panelGabung, false);

        Render();
    }

    static void Hook(Button b, UnityAction a) { if (b != null) b.onClick.AddListener(a); }
    static void Aktif(GameObject go, bool v) { if (go != null) go.SetActive(v); }

    // ---------- READ ----------
    async void Render()
    {
        UIHelper.Pesan(txtStatus, "Memuat grup...");

        List<GrupData> daftar = await GrupService.MilikAsync();   // hanya grup milik pengguna ini
        if (this == null || content == null) return;               // scene sudah berganti selama menunggu

        if (daftar == null)
        {
            UIHelper.Pesan(txtStatus, "Gagal memuat grup. Periksa koneksi internet.");
            return;
        }

        foreach (Transform child in content)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        cache.Clear();
        foreach (GrupData g in daftar)
        {
            cache[g.id] = g;
            GameObject go = Instantiate(grupCardPrefab, content);
            go.GetComponent<GrupCardItem>().Setup(g, this, GrupService.IsAdmin(g));
        }

        if (pesanInfo != "") UIHelper.Pesan(txtStatus, pesanInfo);
        else UIHelper.Pesan(txtStatus, daftar.Count == 0 ? "Belum ada grup. Buat grup atau gabung dengan kode." : "");
        pesanInfo = "";
    }

    // klik card -> Detail Grup
    public void BukaDetail(string id)
    {
        Session.GrupTerpilihId = id;
        SceneNavigator.Buka(SceneNames.DetailGrup);
    }

    // ---------- FORM BUAT / EDIT ----------
    public void BukaFormTambah()
    {
        Aktif(panelGabung, false);

        editId = null;
        UIHelper.Pesan(txtPesanForm, "");
        UIHelper.SetText(txtJudulForm, "Buat Grup");
        UIHelper.SetText(inputNama, "");
        UIHelper.SetText(inputTarget, "");
        UIHelper.SetText(inputTanggal, "");
        UIHelper.SetText(inputAnggota, "1");
        panelEdit.SetActive(true);
        panelEdit.transform.SetAsLastSibling();
    }

    public void BukaFormEdit(string id)
    {
        GrupData g;
        if (!cache.TryGetValue(id, out g)) return;

        if (!GrupService.IsAdmin(g))   // pengaman: hanya admin
        {
            Debug.LogWarning("Hanya admin yang boleh mengedit grup.");
            return;
        }

        Aktif(panelGabung, false);

        editId = id;
        UIHelper.Pesan(txtPesanForm, "");
        UIHelper.SetText(txtJudulForm, "Edit Grup");
        UIHelper.SetText(inputNama, g.nama);
        UIHelper.SetText(inputTarget, g.target.ToString());
        UIHelper.SetText(inputTanggal, g.tanggal);
        UIHelper.SetText(inputAnggota, g.jumlahAnggota.ToString());
        panelEdit.SetActive(true);
        panelEdit.transform.SetAsLastSibling();
    }

    public void TutupForm()
    {
        editId = null;   // reset supaya tidak mengedit data yang salah
        panelEdit.SetActive(false);
    }

    void UbahAnggota(int delta)
    {
        int n = ParseAnggota(UIHelper.GetText(inputAnggota)) + delta;
        UIHelper.SetText(inputAnggota, Mathf.Max(1, n).ToString());
    }

    // ---------- CREATE + UPDATE ----------
    async void Simpan()
    {
        if (sibuk) return;

        string nama = UIHelper.GetText(inputNama).Trim();
        if (string.IsNullOrEmpty(nama))
        {
            UIHelper.Pesan(txtPesanForm, "Nama grup tidak boleh kosong.");
            return;
        }

        int anggota = ParseAnggota(UIHelper.GetText(inputAnggota));
        long target = Fmt.ParseUang(UIHelper.GetText(inputTarget));
        string tanggal = UIHelper.GetText(inputTanggal).Trim();

        sibuk = true;
        btnSimpan.interactable = false;
        UIHelper.Pesan(txtPesanForm, "Menyimpan...");

        string error;
        string info = "";
        if (editId == null)
        {
            var hasil = await GrupService.BuatAsync(nama, anggota, target, tanggal);   // pembuat = admin
            error = hasil.error;
            if (error == null) info = "Grup dibuat. Kode undangan: " + hasil.grup.kode;
        }
        else
        {
            error = await GrupService.PerbaruiAsync(editId, nama, anggota, target, tanggal);   // hanya admin
        }

        if (this == null) return;
        sibuk = false;
        btnSimpan.interactable = true;

        if (error != null)
        {
            UIHelper.Pesan(txtPesanForm, error);
            return;
        }

        TutupForm();
        pesanInfo = info;
        Render();
    }

    // ---------- GABUNG DENGAN KODE ----------
    void BukaPanelGabung()
    {
        TutupForm();   // pastikan popup buat/edit tertutup

        UIHelper.SetText(inputKode, "");
        UIHelper.Pesan(txtPesanGabung, "");
        Aktif(panelGabung, true);
        if (panelGabung != null) panelGabung.transform.SetAsLastSibling();
    }

    void TutupPanelGabung()
    {
        Aktif(panelGabung, false);
    }

    async void Gabung()
    {
        if (sibuk) return;
        sibuk = true;
        if (btnGabung != null) btnGabung.interactable = false;
        UIHelper.Pesan(txtPesanGabung, "Mencari grup...");

        string error = await GrupService.GabungAsync(UIHelper.GetText(inputKode));

        if (this == null) return;
        sibuk = false;
        if (btnGabung != null) btnGabung.interactable = true;

        if (error != null)
        {
            UIHelper.Pesan(txtPesanGabung, error);
            return;
        }

        TutupPanelGabung();
        Render();      // grup baru muncul sebagai Anggota
    }

    // ---------- DELETE ----------
    public async void HapusGrup(string id)
    {
        if (sibuk) return;
        sibuk = true;

        string error = await GrupService.HapusAsync(id);    // gagal kalau bukan admin

        if (this == null) return;
        sibuk = false;

        if (error != null)
        {
            UIHelper.Pesan(txtStatus, error);
            return;
        }
        Render();
    }

    // ---------- HELPER ----------
    static int ParseAnggota(string s)
    {
        long n = Fmt.ParseUang(s);
        return n < 1 ? 1 : (int)System.Math.Min(n, 9999);
    }
}
