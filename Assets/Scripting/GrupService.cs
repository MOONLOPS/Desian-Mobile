using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Auth;
using Firebase.Database;

// Semua operasi data Tabungan Bersama di Firebase Realtime Database:
// grup, kode undangan, peran admin/anggota, setoran, dan konfirmasi admin.
//
// Struktur database:
//   grup/{id}/                nama, kode, adminId, jumlahAnggota, target, tanggal
//   grup/{id}/anggota/{kunci} = email (admin ikut tercatat di sini)
//   grup/{id}/setoran/{key}   userId, jumlah, tanggal, catatan, status
//   kode/{KODE}               = id grup (untuk mencari grup dari kode undangan)
//   keanggotaan/{kunci}/{id}  = true (daftar grup milik satu pengguna)
// {kunci} = email dengan titik diganti koma, karena Firebase tidak mengizinkan "." di kunci.
public static class GrupService
{
    // Kosongkan untuk memakai URL dari google-services.json.
    // Isi (mis. "https://nama-project-default-rtdb.asia-southeast1.firebasedatabase.app") bila database
    // dibuat di region selain Amerika Serikat dan koneksi gagal.
    private const string DB_URL = "";

    private const int TIMEOUT_MS = 10000;
    private const string KODE_CHARS = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    // ---------- koneksi ----------
    private static DatabaseReference Root()
    {
        FirebaseDatabase db = string.IsNullOrEmpty(DB_URL)
            ? FirebaseDatabase.DefaultInstance
            : FirebaseDatabase.GetInstance(DB_URL);
        return db.RootReference;
    }

    // null = siap. Selain itu berisi pesan untuk ditampilkan ke pengguna.
    private static async Task<string> SiapAsync()
    {
        if (!await AuthService.SiapkanAsync()) return "Firebase belum siap. Periksa koneksi internet.";
        if (FirebaseAuth.DefaultInstance.CurrentUser == null) return "Sesi login berakhir. Silakan login ulang.";
        return null;
    }

    // Menunggu hasil Firebase paling lama TIMEOUT_MS. Tanpa ini, tugas bisa menggantung saat offline.
    private static async Task<T> Tunggu<T>(Task<T> tugas)
    {
        if (await Task.WhenAny(tugas, Task.Delay(TIMEOUT_MS)) != tugas) throw new TimeoutException();
        return await tugas;
    }

    private static async Task Tunggu(Task tugas)
    {
        if (await Task.WhenAny(tugas, Task.Delay(TIMEOUT_MS)) != tugas) throw new TimeoutException();
        await tugas;
    }

    private static string PesanError(Exception e)
    {
        Exception dasar = e.GetBaseException();
        Debug.LogException(dasar);

        if (dasar is TimeoutException) return "Koneksi lambat atau terputus. Coba lagi.";
        string m = dasar.Message ?? "";
        if (m.IndexOf("permission", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Tidak punya izin. Periksa Rules Realtime Database.";
        return "Terjadi kesalahan: " + m;
    }

    // ---------- helper data ----------
    // "budi.x@mail.com" -> "budi,x@mail,com"
    private static string Kunci(string email)
    {
        var sb = new StringBuilder((email ?? "").Trim().ToLowerInvariant());
        foreach (char c in new[] { '.', '#', '$', '[', ']', '/' }) sb.Replace(c, c == '.' ? ',' : '_');
        return sb.ToString();
    }

    private static string Teks(DataSnapshot s, string kunci)
    {
        object v = s.Child(kunci).Value;
        return v == null ? "" : v.ToString();
    }

    private static long Angka(DataSnapshot s, string kunci)
    {
        object v = s.Child(kunci).Value;
        if (v == null) return 0;
        try { return Convert.ToInt64(v); }
        catch { return 0; }
    }

    private static GrupData Baca(DataSnapshot s)
    {
        var g = new GrupData
        {
            id = s.Key,
            kode = Teks(s, "kode"),
            adminId = Teks(s, "adminId"),
            nama = Teks(s, "nama"),
            target = Angka(s, "target"),
            tanggal = Teks(s, "tanggal")
        };

        foreach (DataSnapshot a in s.Child("anggota").Children)
        {
            string email = a.Value as string;
            if (!string.IsNullOrEmpty(email) && email != g.adminId) g.anggotaIds.Add(email);
        }

        int nomor = 0;
        foreach (DataSnapshot x in s.Child("setoran").Children)
        {
            nomor++;
            g.setoran.Add(new SetoranData
            {
                id = nomor,
                key = x.Key,
                userId = Teks(x, "userId"),
                jumlah = Angka(x, "jumlah"),
                tanggal = Teks(x, "tanggal"),
                catatan = Teks(x, "catatan"),
                status = Teks(x, "status")
            });
        }

        g.terkumpul = g.setoran.Where(t => t.status == StatusSetoran.Dikonfirmasi).Sum(t => t.jumlah);
        g.jumlahAnggota = Math.Max((int)Angka(s, "jumlahAnggota"), 1 + g.anggotaIds.Count);
        return g;
    }

    // membaca satu grup; null kalau tidak ada atau tidak boleh dibaca
    private static async Task<GrupData> BacaSatuAsync(DatabaseReference root, string id)
    {
        try
        {
            DataSnapshot s = await Tunggu(root.Child("grup").Child(id).GetValueAsync());
            if (!s.Exists) return null;
            GrupData g = Baca(s);
            return BolehLihat(g) ? g : null;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Grup " + id + " tidak bisa dibaca: " + e.GetBaseException().Message);
            return null;
        }
    }

    private static async Task<string> KodeUnikAsync(DatabaseReference root)
    {
        for (int percobaan = 0; percobaan < 5; percobaan++)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 6; i++) sb.Append(KODE_CHARS[UnityEngine.Random.Range(0, KODE_CHARS.Length)]);
            string kode = sb.ToString();

            DataSnapshot ada = await Tunggu(root.Child("kode").Child(kode).GetValueAsync());
            if (!ada.Exists) return kode;
        }
        return null;
    }

    // ---------- peran ----------
    public static bool IsAdmin(GrupData g)
    {
        return g != null && g.adminId == Session.UserId;
    }

    public static bool IsAnggota(GrupData g)
    {
        return g != null && g.anggotaIds != null && g.anggotaIds.Contains(Session.UserId);
    }

    public static bool BolehLihat(GrupData g)
    {
        return IsAdmin(g) || IsAnggota(g);
    }

    // ---------- read ----------
    // Hanya grup yang diikuti pengguna (sebagai admin atau anggota). Urut dari yang paling lama dibuat.
    // Mengembalikan null kalau gagal; pesannya ada di Console.
    public static async Task<List<GrupData>> MilikAsync()
    {
        string tidakSiap = await SiapAsync();
        if (tidakSiap != null) { Debug.LogWarning(tidakSiap); return null; }

        try
        {
            DatabaseReference root = Root();
            DataSnapshot daftar = await Tunggu(root.Child("keanggotaan").Child(Kunci(Session.UserId)).GetValueAsync());

            List<Task<GrupData>> tugas = daftar.Children.Select(c => BacaSatuAsync(root, c.Key)).ToList();
            GrupData[] hasil = await Task.WhenAll(tugas);

            return hasil.Where(g => g != null).OrderBy(g => g.id, StringComparer.Ordinal).ToList();
        }
        catch (Exception e)
        {
            Debug.LogWarning(PesanError(e));
            return null;
        }
    }

    // null kalau grup tidak ada, bukan milik pengguna, atau gagal dibaca
    public static async Task<GrupData> AmbilAsync(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (await SiapAsync() != null) return null;
        return await BacaSatuAsync(Root(), id);
    }

    // ---------- create / update / delete grup ----------
    // Pembuat otomatis menjadi admin dan langsung mendapat kode undangan.
    public static async Task<(GrupData grup, string error)> BuatAsync(string nama, int jumlahAnggota, long target, string tanggal)
    {
        string tidakSiap = await SiapAsync();
        if (tidakSiap != null) return (null, tidakSiap);

        try
        {
            DatabaseReference root = Root();
            string id = root.Child("grup").Push().Key;
            string kode = await KodeUnikAsync(root);
            if (kode == null) return (null, "Gagal membuat kode undangan. Coba lagi.");

            string saya = Session.UserId;
            string kunci = Kunci(saya);

            var data = new Dictionary<string, object>
            {
                { "nama", nama },
                { "kode", kode },
                { "adminId", saya },
                { "jumlahAnggota", jumlahAnggota },
                { "target", target },
                { "tanggal", tanggal },
                { "anggota", new Dictionary<string, object> { { kunci, saya } } }
            };

            // satu pembaruan atomik: grup, indeks kode, dan keanggotaan admin
            var perubahan = new Dictionary<string, object>
            {
                { "grup/" + id, data },
                { "kode/" + kode, id },
                { "keanggotaan/" + kunci + "/" + id, true }
            };
            await Tunggu(root.UpdateChildrenAsync(perubahan));

            return (new GrupData
            {
                id = id, kode = kode, adminId = saya, nama = nama,
                jumlahAnggota = Math.Max(jumlahAnggota, 1), target = target, tanggal = tanggal
            }, null);
        }
        catch (Exception e) { return (null, PesanError(e)); }
    }

    // null = berhasil
    public static async Task<string> PerbaruiAsync(string id, string nama, int jumlahAnggota, long target, string tanggal)
    {
        string tidakSiap = await SiapAsync();
        if (tidakSiap != null) return tidakSiap;

        try
        {
            DatabaseReference root = Root();
            GrupData g = await BacaSatuAsync(root, id);
            if (g == null) return "Grup tidak ditemukan.";
            if (!IsAdmin(g)) return "Hanya admin yang boleh mengedit grup.";

            var perubahan = new Dictionary<string, object>
            {
                { "grup/" + id + "/nama", nama },
                { "grup/" + id + "/jumlahAnggota", jumlahAnggota },
                { "grup/" + id + "/target", target },
                { "grup/" + id + "/tanggal", tanggal }
            };
            await Tunggu(root.UpdateChildrenAsync(perubahan));
            return null;
        }
        catch (Exception e) { return PesanError(e); }
    }

    public static async Task<string> HapusAsync(string id)
    {
        string tidakSiap = await SiapAsync();
        if (tidakSiap != null) return tidakSiap;

        try
        {
            DatabaseReference root = Root();
            GrupData g = await BacaSatuAsync(root, id);
            if (g == null) return "Grup tidak ditemukan.";
            if (!IsAdmin(g)) return "Hanya admin yang boleh menghapus grup.";

            // hapus grup, kode, dan daftar keanggotaan semua orang dalam satu pembaruan atomik
            var perubahan = new Dictionary<string, object>
            {
                { "grup/" + id, null },
                { "kode/" + g.kode, null }
            };
            foreach (string email in SemuaAnggota(g)) perubahan["keanggotaan/" + Kunci(email) + "/" + id] = null;

            await Tunggu(root.UpdateChildrenAsync(perubahan));
            return null;
        }
        catch (Exception e) { return PesanError(e); }
    }

    // ---------- gabung dengan kode ----------
    // null = berhasil, selain itu pesan untuk ditampilkan
    public static async Task<string> GabungAsync(string kode)
    {
        kode = (kode ?? "").Trim().ToUpperInvariant();
        if (kode.Length == 0) return "Masukkan kode undangan.";

        string tidakSiap = await SiapAsync();
        if (tidakSiap != null) return tidakSiap;

        try
        {
            DatabaseReference root = Root();

            DataSnapshot cari = await Tunggu(root.Child("kode").Child(kode).GetValueAsync());
            if (!cari.Exists || cari.Value == null) return "Kode tidak ditemukan.";
            string id = cari.Value.ToString();

            string saya = Session.UserId;
            string kunci = Kunci(saya);

            DataSnapshot sudah = await Tunggu(root.Child("keanggotaan").Child(kunci).Child(id).GetValueAsync());
            if (sudah.Exists) return "Anda sudah tergabung di grup ini.";

            var perubahan = new Dictionary<string, object>
            {
                { "grup/" + id + "/anggota/" + kunci, saya },
                { "keanggotaan/" + kunci + "/" + id, true }
            };
            await Tunggu(root.UpdateChildrenAsync(perubahan));
            return null;
        }
        catch (Exception e) { return PesanError(e); }
    }

    // ---------- setoran ----------
    // Anggota: status Menunggu sampai dikonfirmasi admin. Admin: langsung Dikonfirmasi.
    public static async Task<string> SetorAsync(string grupId, long jumlah, string catatan)
    {
        if (jumlah <= 0) return "Masukkan jumlah setoran.";

        string tidakSiap = await SiapAsync();
        if (tidakSiap != null) return tidakSiap;

        try
        {
            DatabaseReference root = Root();
            GrupData g = await BacaSatuAsync(root, grupId);
            if (g == null) return "Grup tidak ditemukan.";

            DatabaseReference baru = root.Child("grup").Child(grupId).Child("setoran").Push();
            var data = new Dictionary<string, object>
            {
                { "userId", Session.UserId },
                { "jumlah", jumlah },
                { "tanggal", Fmt.IsoHariIni() },
                { "catatan", catatan ?? "" },
                { "status", IsAdmin(g) ? StatusSetoran.Dikonfirmasi : StatusSetoran.Menunggu }
            };
            await Tunggu(baru.SetValueAsync(data));
            return null;
        }
        catch (Exception e) { return PesanError(e); }
    }

    // Admin menerima (konfirmasi) atau menolak setoran yang Menunggu.
    public static async Task<string> PutuskanAsync(string grupId, string setoranKey, bool terima)
    {
        string tidakSiap = await SiapAsync();
        if (tidakSiap != null) return tidakSiap;

        try
        {
            DatabaseReference root = Root();
            GrupData g = await BacaSatuAsync(root, grupId);
            if (g == null) return "Grup tidak ditemukan.";
            if (!IsAdmin(g)) return "Hanya admin yang boleh mengonfirmasi setoran.";

            SetoranData s = g.setoran.FirstOrDefault(x => x.key == setoranKey && x.status == StatusSetoran.Menunggu);
            if (s == null) return "Setoran sudah diproses.";

            await Tunggu(root.Child("grup").Child(grupId).Child("setoran").Child(setoranKey).Child("status")
                .SetValueAsync(terima ? StatusSetoran.Dikonfirmasi : StatusSetoran.Ditolak));
            return null;
        }
        catch (Exception e) { return PesanError(e); }
    }

    // ---------- turunan untuk tampilan ----------
    public static List<SetoranData> Menunggu(GrupData g)
    {
        return g.setoran.Where(s => s.status == StatusSetoran.Menunggu).ToList();
    }

    // admin di urutan pertama, lalu anggota
    public static List<string> SemuaAnggota(GrupData g)
    {
        var daftar = new List<string> { g.adminId };
        daftar.AddRange(g.anggotaIds.Where(x => x != g.adminId));
        return daftar;
    }

    // total setoran anggota yang sudah dikonfirmasi
    public static long TotalAnggota(GrupData g, string userId)
    {
        return g.setoran
            .Where(s => s.userId == userId && s.status == StatusSetoran.Dikonfirmasi)
            .Sum(s => s.jumlah);
    }

    public static string StatusAnggota(GrupData g, string userId)
    {
        List<SetoranData> miliknya = g.setoran.Where(s => s.userId == userId).ToList();
        if (miliknya.Any(s => s.status == StatusSetoran.Menunggu)) return StatusSetoran.Menunggu;
        if (miliknya.Any(s => s.status == StatusSetoran.Dikonfirmasi)) return StatusSetoran.Dikonfirmasi;
        return StatusSetoran.BelumSetor;
    }
}
