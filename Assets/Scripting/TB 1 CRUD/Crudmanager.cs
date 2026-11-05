using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Satu scene: daftar grup (Read), popup EditGrup (Create + Update), Delete di card,
// dan panel Gabung Grup (bergabung memakai kode undangan).
public class CRUDManager : MonoBehaviour
{
    private const string KEY_DATA = "grup_data";
    private const string KEY_NEXT_ID = "grup_next_id";
    private const string KEY_USER = "user_id";
    private const string KODE_CHARS = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    [Header("Pengguna Saat Ini")]
    [Tooltip("Kosongkan untuk memakai ID perangkat. Isi nama (mis. budi) di Editor untuk menguji peran anggota.")]
    public string idPenggunaUji = "";

    [Header("List")]
    public Transform content;             // Scroll View > Viewport > Content
    public GameObject grupCardPrefab;     // prefab GrupCard (drag dari Project window)

    [Header("Tombol Utama")]
    public Button btnBuatGrup;            // "btn buat"
    public Button btnGabungGrup;          // "btn gabung"

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

    private GrupList db = new GrupList();
    private int editId = -1;              // -1 = mode tambah, selain itu = mode edit
    private string userId;

    // ---------- LIFECYCLE ----------
    void Start()
    {
        userId = AmbilUserId();

        btnBuatGrup.onClick.AddListener(BukaFormTambah);
        Hook(btnGabungGrup, BukaPanelGabung);

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

        Load();
        Render();
    }

    static void Hook(Button b, UnityAction a) { if (b != null) b.onClick.AddListener(a); }
    static void Aktif(GameObject go, bool v) { if (go != null) go.SetActive(v); }

    // ---------- PENGGUNA & PERAN ----------
    // Kalau project punya login, ganti isi fungsi ini dengan username hasil login
    // (misalnya PlayerPrefs.GetString("username")).
    string AmbilUserId()
    {
        if (!string.IsNullOrEmpty(idPenggunaUji)) return idPenggunaUji.Trim();

        string id = PlayerPrefs.GetString(KEY_USER, "");
        if (string.IsNullOrEmpty(id))
        {
            id = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            PlayerPrefs.SetString(KEY_USER, id);
            PlayerPrefs.Save();
        }
        return id;
    }

    bool IsAdmin(GrupData g) { return g.adminId == userId; }
    bool IsAnggota(GrupData g) { return g.anggotaIds != null && g.anggotaIds.Contains(userId); }
    bool BolehLihat(GrupData g) { return IsAdmin(g) || IsAnggota(g); }

    // ---------- STORAGE ----------
    void Load()
    {
        string json = PlayerPrefs.GetString(KEY_DATA, "");
        db = string.IsNullOrEmpty(json) ? new GrupList() : JsonUtility.FromJson<GrupList>(json);
        if (db == null || db.items == null) db = new GrupList();

        // melengkapi data lama (belum punya kode atau admin)
        bool berubah = false;
        foreach (GrupData g in db.items)
        {
            if (string.IsNullOrEmpty(g.kode)) { g.kode = BuatKodeUnik(); berubah = true; }
            if (string.IsNullOrEmpty(g.adminId)) { g.adminId = userId; berubah = true; }
            if (g.anggotaIds == null) { g.anggotaIds = new List<string>(); berubah = true; }
        }
        if (berubah) SaveToPrefs();
    }

    void SaveToPrefs()
    {
        PlayerPrefs.SetString(KEY_DATA, JsonUtility.ToJson(db));
        PlayerPrefs.Save();
    }

    int NextId()
    {
        int id = PlayerPrefs.GetInt(KEY_NEXT_ID, 1);
        PlayerPrefs.SetInt(KEY_NEXT_ID, id + 1);
        return id;
    }

    string BuatKodeUnik()
    {
        string kode;
        do
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 6; i++) sb.Append(KODE_CHARS[Random.Range(0, KODE_CHARS.Length)]);
            kode = sb.ToString();
        } while (db.items.Any(x => x.kode == kode));
        return kode;
    }

    // ---------- READ ----------
    void Render()
    {
        foreach (Transform child in content) Destroy(child.gameObject);

        // hanya grup milik pengguna ini (sebagai admin atau anggota)
        foreach (GrupData g in db.items.Where(BolehLihat))
        {
            GameObject go = Instantiate(grupCardPrefab, content);
            go.GetComponent<GrupCardItem>().Setup(g, this, IsAdmin(g));
        }
    }

    // ---------- FORM BUAT / EDIT ----------
    public void BukaFormTambah()
    {
        Aktif(panelGabung, false);

        editId = -1;
        UIHelper.SetText(txtJudulForm, "Buat Grup");
        UIHelper.SetText(inputNama, "");
        UIHelper.SetText(inputTarget, "");
        UIHelper.SetText(inputTanggal, "");
        UIHelper.SetText(inputAnggota, "1");
        panelEdit.SetActive(true);
    }

    public void BukaFormEdit(int id)
    {
        GrupData g = db.items.FirstOrDefault(x => x.id == id);
        if (g == null) return;

        if (!IsAdmin(g))   // pengaman: hanya admin
        {
            Debug.LogWarning("Hanya admin yang boleh mengedit grup.");
            return;
        }

        Aktif(panelGabung, false);

        editId = id;
        UIHelper.SetText(txtJudulForm, "Edit Grup");
        UIHelper.SetText(inputNama, g.nama);
        UIHelper.SetText(inputTarget, g.target.ToString());
        UIHelper.SetText(inputTanggal, g.tanggal);
        UIHelper.SetText(inputAnggota, g.jumlahAnggota.ToString());
        panelEdit.SetActive(true);
    }

    public void TutupForm()
    {
        editId = -1;   // reset supaya tidak mengedit data yang salah
        panelEdit.SetActive(false);
    }

    void UbahAnggota(int delta)
    {
        int n = ParseInt(UIHelper.GetText(inputAnggota), 1) + delta;
        UIHelper.SetText(inputAnggota, Mathf.Max(1, n).ToString());
    }

    // ---------- CREATE + UPDATE ----------
    void Simpan()
    {
        string nama = UIHelper.GetText(inputNama).Trim();
        if (string.IsNullOrEmpty(nama))
        {
            Debug.LogWarning("Nama grup tidak boleh kosong.");
            return;
        }

        int anggota = Mathf.Max(1, ParseInt(UIHelper.GetText(inputAnggota), 1));
        long target = ParseLong(UIHelper.GetText(inputTarget));
        string tanggal = UIHelper.GetText(inputTanggal).Trim();

        if (editId < 0)
        {
            // CREATE: pembuat otomatis menjadi admin
            db.items.Add(new GrupData
            {
                id = NextId(),
                kode = BuatKodeUnik(),
                adminId = userId,
                nama = nama,
                jumlahAnggota = anggota,
                target = target,
                terkumpul = 0,
                tanggal = tanggal
            });
        }
        else
        {
            // UPDATE: hanya admin
            GrupData g = db.items.FirstOrDefault(x => x.id == editId);
            if (g != null && IsAdmin(g))
            {
                g.nama = nama;
                g.jumlahAnggota = anggota;
                g.target = target;
                g.tanggal = tanggal;
            }
        }

        SaveToPrefs();
        TutupForm();
        Render();
    }

    // ---------- GABUNG DENGAN KODE ----------
    void BukaPanelGabung()
    {
        TutupForm();   // pastikan popup buat/edit tertutup

        UIHelper.SetText(inputKode, "");
        Pesan("");
        Aktif(panelGabung, true);
    }

    void TutupPanelGabung()
    {
        Aktif(panelGabung, false);
    }

    void Gabung()
    {
        string kode = UIHelper.GetText(inputKode).Trim().ToUpper();
        if (string.IsNullOrEmpty(kode))
        {
            Pesan("Masukkan kode undangan.");
            return;
        }

        GrupData g = db.items.FirstOrDefault(x => x.kode == kode);
        if (g == null)
        {
            Pesan("Kode tidak ditemukan.");
            return;
        }

        if (BolehLihat(g))
        {
            Pesan("Anda sudah tergabung di grup ini.");
            return;
        }

        // pengguna masuk sebagai ANGGOTA (tanpa hak edit dan hapus)
        g.anggotaIds.Add(userId);
        g.jumlahAnggota++;
        SaveToPrefs();

        TutupPanelGabung();
        Render();
    }

    // pesan hasil: tampil di teks kecil kalau ada, kalau tidak ke Console
    void Pesan(string teks)
    {
        if (txtPesanGabung != null) UIHelper.SetText(txtPesanGabung, teks);
        else if (!string.IsNullOrEmpty(teks)) Debug.Log(teks);
    }

    // ---------- DELETE ----------
    public void HapusGrup(int id)
    {
        GrupData g = db.items.FirstOrDefault(x => x.id == id);
        if (g == null) return;

        if (!IsAdmin(g))   // pengaman: hanya admin
        {
            Debug.LogWarning("Hanya admin yang boleh menghapus grup.");
            return;
        }

        db.items.Remove(g);
        SaveToPrefs();
        Render();
    }

    // ---------- HELPER ----------
    static int ParseInt(string s, int fallback)
    {
        return int.TryParse(Regex.Replace(s ?? "", "[^0-9]", ""), out int r) ? r : fallback;
    }

    static long ParseLong(string s)
    {
        return long.TryParse(Regex.Replace(s ?? "", "[^0-9]", ""), out long r) ? r : 0;
    }
}