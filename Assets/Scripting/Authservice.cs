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

    // Dipanggil otomatis oleh LoginController saat scene Login dimulai
    public static async Task<bool> SiapkanAsync()
    {
        if (siap && auth != null) return true;

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
        siap = false;
        return false;
    }

    // Mengecek apakah pengguna masih punya sesi login aktif di Firebase
    public static bool PulihkanSesi()
    {
        if (!siap || auth == null || auth.CurrentUser == null)
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
        if (!siap || auth == null)
            return "Firebase belum siap.";

        try
        {
            AuthResult hasil = await auth.SignInWithEmailAndPasswordAsync(email, password);
            string userEmail = hasil.User.Email ?? email;
            Session.Masuk(BuatUser(userEmail));
            return null; // null menandakan berhasil
        }
        catch (Exception ex)
        {
            return TerjemahkanError(ex);
        }
    }

    // Proses Register akun baru ke Firebase Authentication
    public static async Task<string> DaftarAsync(string email, string password)
    {
        if (!siap || auth == null)
            return "Firebase belum siap.";

        try
        {
            AuthResult hasil = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            string userEmail = hasil.User.Email ?? email;
            Session.Masuk(BuatUser(userEmail));
            return null; // null menandakan berhasil
        }
        catch (Exception ex)
        {
            return TerjemahkanError(ex);
        }
    }

    // Logout dari Firebase Authentication
    public static void Keluar()
    {
        if (auth != null)
            auth.SignOut();
    }

    // Membentuk objek UserData dari email
    public static UserData BuatUser(string id)
    {
        string bersih = id.Trim().ToLower();
        string namaDepan = bersih.Contains("@") ? bersih.Split('@')[0] : bersih;

        return new UserData
        {
            id = bersih,
            email = bersih,
            nama = namaDepan
        };
    }

    // Mengubah kode error Firebase menjadi pesan yang mudah dibaca
    private static string TerjemahkanError(Exception ex)
    {
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
                case AuthError.WeakPassword:
                    return "Password terlalu lemah (minimal 6 karakter).";
                case AuthError.NetworkRequestFailed:
                    return "Koneksi internet bermasalah.";
            }
        }

        return "Terjadi kesalahan autentikasi: " + ex.GetBaseException().Message;
    }
}