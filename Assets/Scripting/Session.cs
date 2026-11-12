using UnityEngine;

// Menyimpan siapa yang sedang login dan "parameter" yang dibawa antar scene
// (impian atau grup mana yang sedang dibuka). Proses login-nya sendiri ada di AuthService (Firebase).
public static class Session
{
    private const string KEY_LOGIN = "session_user_id";

    public static UserData User { get; private set; }
    public static bool SudahLogin { get { return User != null; } }
    public static string UserId { get { return User != null ? User.id : "tamu"; } }
    public static string Nama { get { return User != null ? User.nama : "Tamu"; } }

    // dibawa antar scene
    public static int ImpianTerpilihId = -1;
    public static int GrupTerpilihId = -1;

    public static void Masuk(UserData user)
    {
        User = user;
        PlayerPrefs.SetString(KEY_LOGIN, user.id);
        PlayerPrefs.Save();
    }

    public static void Keluar()
    {
        User = null;
        ImpianTerpilihId = -1;
        GrupTerpilihId = -1;
        PlayerPrefs.DeleteKey(KEY_LOGIN);
        PlayerPrefs.Save();
        AuthService.Keluar();      // sign out dari Firebase juga
    }

    // Memulihkan pengguna dari penyimpanan lokal (tanpa menunggu Firebase),
    // supaya scene mana pun bisa langsung dibuka setelah aplikasi dinyalakan ulang.
    public static void Pulihkan()
    {
        if (User != null) return;
        string id = PlayerPrefs.GetString(KEY_LOGIN, "");
        if (!string.IsNullOrEmpty(id)) User = AuthService.BuatUser(id);
    }

    // Dipanggil di awal setiap scene selain Login.
    // Kalau belum login, otomatis dikirim ke scene Login.
    public static bool PastikanLogin()
    {
        if (SudahLogin) return true;
        Pulihkan();
        if (SudahLogin) return true;
        SceneNavigator.Buka(SceneNames.Login);
        return false;
    }
}