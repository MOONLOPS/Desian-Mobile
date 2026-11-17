using System;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Auth;

public static class AuthService
{
    private static FirebaseApp app;
    private static FirebaseAuth auth;
    private static bool siap;

    // Firebase dimatikan saat Stop, Reset otomatis setiap Play
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        app = null;
        auth = null;
        siap = false;
    }

    // Manggil otomatis LoginController saat scene Login dimulai
    public static async Task<bool> SiapkanAsync()
    {
        if (siap && auth != null) return true;

        try
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dependencyStatus == DependencyStatus.Available)
            {
                app = FirebaseApp.DefaultInstance;
                auth = FirebaseAuth.DefaultInstance;
                siap = true;

                Debug.Log("Firebase is ready to use.");
                return true;
            }

            Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
        }
        catch (Exception ex)
        {
            Debug.LogError("Firebase gagal diinisialisasi: " + ex.Message);
        }

        siap = false;
        return false;
    }

    // Ngecek apakah pengguna masih punya sesi login aktif di Firebase
    public static bool PulihkanSesi()
    {
        if (!siap) return false;
        auth = FirebaseAuth.DefaultInstance;
        if (auth == null || auth.CurrentUser == null)
            return false;

        string email = auth.CurrentUser.Email ?? "";
        if (!string.IsNullOrEmpty(email))
        {
            Session.Masuk(BuatUser(email));
            return true;
        }

        return false;
    }

    // Proses Login ke Firebase Authentication
    public static async Task<string> MasukAsync(string email, string password)
    {
        if (!await SiapkanAsync())
            return "Firebase belum siap. Periksa koneksi internet.";

        auth = FirebaseAuth.DefaultInstance;

        try
        {
            AuthResult hasil = await auth.SignInWithEmailAndPasswordAsync(email, password);
            Session.Masuk(BuatUser(EmailDariHasil(hasil, email)));
            Debug.Log((hasil.User ?? auth.CurrentUser).UserId);   // UserId untuk laporan
            return null; // null = berhasil
        }
        catch (Exception ex)
        {
            return TerjemahkanError(ex);
        }
    }

    // Proses Register akun baru ke Firebase Authentication
    public static async Task<string> DaftarAsync(string email, string password)
    {
        if (!await SiapkanAsync())
            return "Firebase belum siap. Periksa koneksi internet.";

        auth = FirebaseAuth.DefaultInstance; 

        try
        {
            AuthResult hasil = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            Session.Masuk(BuatUser(EmailDariHasil(hasil, email)));
            return null; // null = berhasil
        }
        catch (Exception ex)
        {
            return TerjemahkanError(ex);
        }
    }


    private static string EmailDariHasil(AuthResult hasil, string emailKetik)
    {
        FirebaseUser user = (hasil != null && hasil.User != null) ? hasil.User : auth.CurrentUser;
        return (user != null && !string.IsNullOrEmpty(user.Email)) ? user.Email : emailKetik;
    }

    // Logout dari Firebase Authentication
    public static void Keluar()
    {
        try { FirebaseAuth.DefaultInstance.SignOut(); }
        catch (Exception ex) { Debug.LogWarning("SignOut gagal: " + ex.Message); }
    }

    // Membentuk objek UserData dari email
    public static UserData BuatUser(string id)
    {
        string bersih = (id ?? "").Trim().ToLower();

        return new UserData
        {
            id = bersih,
            email = bersih,
            nama = NamaDari(bersih)
        };
    }

    public static string NamaDari(string id)
    {
        if (string.IsNullOrEmpty(id)) return "";
        int at = id.IndexOf('@');
        string depan = at > 0 ? id.Substring(0, at) : id;
        return depan.Length == 0 ? depan : char.ToUpper(depan[0]) + depan.Substring(1);
    }

    // Mengubah kode error Firebase menjadi pesan yang mudah dibaca
    private static string TerjemahkanError(Exception ex)
    {
        Debug.LogException(ex.GetBaseException());   // tampilkan stack trace lengkap di Console

        FirebaseException firebaseEx = ex.GetBaseException() as FirebaseException;
        if (firebaseEx != null)
        {
            AuthError kode = (AuthError)firebaseEx.ErrorCode;
            switch (kode)
            {
                case AuthError.InvalidEmail:
                    return "Format email tidak valid.";
                case AuthError.WrongPassword:
                case AuthError.InvalidCredential:
                    return "Email atau password salah.";
                case AuthError.UserNotFound:
                    return "Akun tidak ditemukan. Silakan daftar terlebih dahulu.";
                case AuthError.EmailAlreadyInUse:
                    return "Email sudah terdaftar. Silakan login.";
                case AuthError.NetworkRequestFailed:
                    return "Koneksi internet bermasalah.";
                case AuthError.TooManyRequests:
                    return "Terlalu banyak percobaan. Coba lagi nanti.";
                case AuthError.UserDisabled:
                    return "Akun ini dinonaktifkan.";
            }
        }

        return "Terjadi kesalahan autentikasi: " + ex.GetBaseException().Message;
    }
}