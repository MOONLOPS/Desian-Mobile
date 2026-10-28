using System.Linq;
using System.Text.RegularExpressions;
using Unity.VisualScripting.ReorderableList.Internal;
using UnityEngine;
using UnityEngine.UI;

public class CRUDManager : MonoBehaviour
{
    private const string KEY_DATA = "grup_data";
    private const string KEY_NEXT_ID = "grup_next_id";

    [Header("List")]
    public Transform content;            // Scroll View > Viewport > Content
    public GameObject grupCardPrefab;    // prefab Grupcard (drag dari Project window)
    public Button btnBuatGrup;           // "btn Utama" (Buat/Gabung Grup)

    [Header("Panel Form (EditGrup) - drag GameObject-nya")]
    public GameObject panelEdit;
    public GameObject txtJudulForm;      // teks "Edit Grup"
    public GameObject inputNama;
    public GameObject inputTarget;
    public GameObject inputTanggal;
    public GameObject inputAnggota;
    public Button btnTambah;             // +
    public Button btnKurang;             // -
    public Button btnClose;              // tombol "<"
    public Button btnSimpan;

    private GrupList db = new GrupList();
    private int editId = -1;             // -1 = mode tambah, selain itu = mode edit

    // ---------- LIFECYCLE ----------
    void Start()
    {
        btnBuatGrup.onClick.AddListener(BukaFormTambah);
        btnClose.onClick.AddListener(TutupForm);
        btnSimpan.onClick.AddListener(Simpan);
        btnTambah.onClick.AddListener(() => UbahAnggota(+1));
        btnKurang.onClick.AddListener(() => UbahAnggota(-1));

        UIHelper.SetNumberOnly(inputAnggota);
        UIHelper.SetNumberOnly(inputTarget);

        panelEdit.SetActive(false);
        Load();
        Render();
    }

    // ---------- STORAGE (PlayerPrefs + JSON) ----------
    void Load()
    {
        string json = PlayerPrefs.GetString(KEY_DATA, "");
        db = string.IsNullOrEmpty(json) ? new GrupList() : JsonUtility.FromJson<GrupList>(json);
        if (db == null || db.items == null) db = new GrupList();
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

    // ---------- READ ----------
    void Render()
    {
        // hapus card lama, lalu bangun ulang dari data
        foreach (Transform child in content) Destroy(child.gameObject);

        foreach (GrupData g in db.items)
        {
            GameObject go = Instantiate(grupCardPrefab, content);
            go.GetComponent<GrupCardItem>().Setup(g, this);
        }
    }

    // ---------- FORM ----------
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
        editId = -1; // reset supaya tidak mengedit data yang salah
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