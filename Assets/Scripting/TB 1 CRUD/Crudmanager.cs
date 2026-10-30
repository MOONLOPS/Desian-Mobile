using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CRUDManager : MonoBehaviour
{
    private const string KEY_DATA = "grup_data";
    private const string KEY_NEXT_ID = "grup_next_id";
    private const string KODE_CHARS = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    [Header("List")]
    public Transform content;
    public GameObject grupCardPrefab;
    public Button btnBuatGrup;            // "btn Utama" (Buat/Gabung Grup)

    [Header("Popup Pilihan (opsional)")]
    public GameObject panelPilih;         // popup kecil berisi 2 tombol
    public Button btnPilihBuat;
    public Button btnPilihGabung;
    public Button btnClosePilih;

    [Header("Popup Form Buat/Edit (EditGrup)")]
    public GameObject panelEdit;
    public GameObject txtJudulForm;       // teks "Edit Grup" DI DALAM EditGrup
    public GameObject inputNama;
    public GameObject inputTarget;
    public GameObject inputTanggal;
    public GameObject inputAnggota;
    public Button btnTambah;
    public Button btnKurang;
    public Button btnClose;
    public Button btnSimpan;

    [Header("Popup Gabung Grup (opsional)")]
    public GameObject panelGabung;
    public GameObject inputKode;
    public GameObject txtPesanGabung;     // teks kecil untuk pesan hasil
    public Button btnGabung;
    public Button btnCloseGabung;

    private GrupList db = new GrupList();
    private int editId = -1;              // -1 = mode tambah, selain itu = mode edit

    // ---------- LIFECYCLE ----------
    void Start()
    {
        btnBuatGrup.onClick.AddListener(BukaPilihan);
        btnClose.onClick.AddListener(TutupForm);
        btnSimpan.onClick.AddListener(Simpan);
        btnTambah.onClick.AddListener(() => UbahAnggota(+1));
        btnKurang.onClick.AddListener(() => UbahAnggota(-1));

        // tombol opsional: aman kalau belum dibuat di scene
        Hook(btnPilihBuat, () => { Aktif(panelPilih, false); BukaFormTambah(); });
        Hook(btnPilihGabung, () => { Aktif(panelPilih, false); BukaFormGabung(); });
        Hook(btnClosePilih, () => Aktif(panelPilih, false));
        Hook(btnGabung, Gabung);
        Hook(btnCloseGabung, () => Aktif(panelGabung, false));

        UIHelper.SetNumberOnly(inputAnggota);
        UIHelper.SetNumberOnly(inputTarget);

        panelEdit.SetActive(false);
        Aktif(panelPilih, false);
        Aktif(panelGabung, false);

        Load();
        Render();
    }

    static void Hook(Button b, UnityAction a) { if (b != null) b.onClick.AddListener(a); }
    static void Aktif(GameObject go, bool v) { if (go != null) go.SetActive(v); }

    // ---------- STORAGE ----------
    void Load()
    {
        string json = PlayerPrefs.GetString(KEY_DATA, "");
        db = string.IsNullOrEmpty(json) ? new GrupList() : JsonUtility.FromJson<GrupList>(json);
        if (db == null || db.items == null) db = new GrupList();

        // data lama belum punya kode: isi sekarang
        bool berubah = false;
        foreach (GrupData g in db.items)
        {
            if (string.IsNullOrEmpty(g.kode)) { g.kode = BuatKodeUnik(); berubah = true; }
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

        foreach (GrupData g in db.items)
        {
            GameObject go = Instantiate(grupCardPrefab, content);
            go.GetComponent<GrupCardItem>().Setup(g, this);
        }
    }

    // ---------- PILIHAN BUAT / GABUNG ----------
    void BukaPilihan()
    {
        if (panelPilih != null) panelPilih.SetActive(true);   // ada popup pilihan
        else BukaFormTambah();                                 // belum ada: langsung buat grup
    }

    // ---------- FORM BUAT / EDIT ----------
    public void BukaFormTambah()
    {
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
        editId = -1;
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
            // CREATE
            db.items.Add(new GrupData
            {
                id = NextId(),
                kode = BuatKodeUnik(),
                nama = nama,
                jumlahAnggota = anggota,
                target = target,
                terkumpul = 0,
                tanggal = tanggal
            });
        }
        else
        {
            // UPDATE
            GrupData g = db.items.FirstOrDefault(x => x.id == editId);
            if (g != null)
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
    void BukaFormGabung()
    {
        UIHelper.SetText(inputKode, "");
        UIHelper.SetText(txtPesanGabung, "");
        Aktif(panelGabung, true);
    }

    void Gabung()
    {
        string kode = UIHelper.GetText(inputKode).Trim().ToUpper();
        if (string.IsNullOrEmpty(kode))
        {
            UIHelper.SetText(txtPesanGabung, "Masukkan kode grup.");
            return;
        }

        GrupData g = db.items.FirstOrDefault(x => x.kode == kode);
        if (g == null)
        {
            UIHelper.SetText(txtPesanGabung, "Kode tidak ditemukan.");
            return;
        }

        // Versi lokal: hanya memverifikasi kode di data perangkat ini.
        g.jumlahAnggota++;
        SaveToPrefs();
        Render();
        UIHelper.SetText(txtPesanGabung, $"Berhasil bergabung ke {g.nama}.");
    }

    // ---------- DELETE ----------
    public void HapusGrup(int id)
    {
        db.items.RemoveAll(x => x.id == id);
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