using System;

// Satu catatan setoran. Dipakai untuk riwayat Tabunganku (pribadi) dan setoran grup.
[Serializable]
public class SetoranData
{
    public int id;            // nomor urut (Tabunganku)
    public string key;        // kunci di Firebase (setoran grup)
    public string userId;
    public long jumlah;
    public string tanggal;    // yyyy-MM-dd
    public string catatan;
    public string status;     // lihat StatusSetoran
}

public static class StatusSetoran
{
    public const string Menunggu = "Menunggu";
    public const string Dikonfirmasi = "Dikonfirmasi";
    public const string Ditolak = "Ditolak";
    public const string BelumSetor = "Belum setor";   // hanya untuk tampilan daftar anggota
}
