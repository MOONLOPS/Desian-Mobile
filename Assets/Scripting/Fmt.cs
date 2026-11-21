using System;
using System.Globalization;
using System.Text;

// Format angka dan tanggal. Tidak memakai CultureInfo Indonesia supaya aman di Android (IL2CPP).
public static class Fmt
{
    private const string FORMAT_ISO = "yyyy-MM-dd";
    private static readonly string[] BULAN =
        { "Jan", "Feb", "Mar", "Apr", "Mei", "Jun", "Jul", "Agu", "Sep", "Okt", "Nov", "Des" };

    public static string Angka(long n)
    {
        return n.ToString("N0", CultureInfo.InvariantCulture).Replace(",", ".");
    }

    public static string Rp(long n)
    {
        return "Rp " + Angka(n);
    }

    // ambil hanya digit dari teks (aman untuk "Rp 10.000")
    public static long ParseUang(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        var sb = new StringBuilder();
        foreach (char c in s) if (c >= '0' && c <= '9') sb.Append(c);
        long hasil;
        return long.TryParse(sb.ToString(), out hasil) ? hasil : 0;
    }

    public static string IsoHariIni()
    {
        return DateTime.Now.ToString(FORMAT_ISO, CultureInfo.InvariantCulture);
    }

    public static bool TryIso(string iso, out DateTime tanggal)
    {
        return DateTime.TryParseExact(iso, FORMAT_ISO, CultureInfo.InvariantCulture, DateTimeStyles.None, out tanggal);
    }

    // "2026-11-10" -> "10 Nov 2026"
    public static string TanggalIndo(string iso)
    {
        DateTime d;
        if (!TryIso(iso, out d)) return iso ?? "";
        return d.Day + " " + BULAN[d.Month - 1] + " " + d.Year;
    }

    public static int SelisihHari(string awalIso, string akhirIso)
    {
        DateTime a, b;
        if (!TryIso(awalIso, out a) || !TryIso(akhirIso, out b)) return 0;
        return (int)(b.Date - a.Date).TotalDays;
    }
}
