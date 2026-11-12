using UnityEngine;
using UnityEngine.UI;

// Scene: Login. Login dan Register berada di SATU scene, dua panel yang saling berganti.
// Pasang script ini di objek GameManager.
public class LoginController : MonoBehaviour
{
    [Header("Dua Panel")]
    public GameObject panelLogin;
    public GameObject panelRegister;

    [Header("Panel Login")]
    public GameObject inputEmailLogin;        // field_email di Panel Login
    public GameObject inputPasswordLogin;     // field_passwrd di Panel Login
    public Button btnLogin;                   // tombol utama "Login"
    public Button btnKeRegister;              // link "Belum punya akun? Daftar" (pindah ke panel Register)
    public GameObject txtPesanLogin;          // opsional: teks kecil untuk pesan error

    [Header("Panel Register")]
    public GameObject inputEmailRegister;     // field_email di Panel Register
    public GameObject inputPasswordRegister;  // field_passwrd di Panel Register
    public Button btnRegister;                // tombol utama "Register"
    public Button btnKeLogin;                 // link "Sudah memiliki akun? Login" (pindah ke panel Login)
    public GameObject txtPesanRegister;       // opsional

    [Header("Opsional")]
    public GameObject loading;                // teks/overlay "Mohon tunggu..." yang tampil saat proses berjalan

    private const int MIN_PASSWORD = 6;       // batas minimum Firebase

    private bool modeLogin = true;
    private bool sibuk;

    // ---------- LIFECYCLE ----------
    async void Start()
    {
        UIHelper.SetPassword(inputPasswordLogin);       // password tampil sebagai titik
        UIHelper.SetPassword(inputPasswordRegister);

        btnLogin.onClick.AddListener(OnLoginClick);
        btnRegister.onClick.AddListener(OnRegisterClick);
        btnKeRegister.onClick.AddListener(() => TampilkanPanel(false));
        btnKeLogin.onClick.AddListener(() => TampilkanPanel(true));

        TampilkanPanel(true);
        SetSibuk(true);                                 // tombol dikunci sampai Firebase siap

        bool siap = await AuthService.SiapkanAsync();
        if (!siap)
        {
            Pesan("Firebase belum siap. Periksa koneksi internet.");
            SetSibuk(false);
            return;
        }

        // sudah pernah login -> langsung ke Menu
        if (AuthService.PulihkanSesi())
        {
            SceneNavigator.Buka(SceneNames.Menu);
            return;
        }

        SetSibuk(false);
    }

    // ---------- GANTI PANEL ----------
    void TampilkanPanel(bool login)
    {
        // email yang sudah diketik ikut pindah supaya tidak mengetik ulang
        string email = UIHelper.GetText(login ? inputEmailRegister : inputEmailLogin);
        if (!string.IsNullOrEmpty(email))
            UIHelper.SetText(login ? inputEmailLogin : inputEmailRegister, email);

        modeLogin = login;
        panelLogin.SetActive(login);
        panelRegister.SetActive(!login);

        UIHelper.SetText(txtPesanLogin, "");
        UIHelper.SetText(txtPesanRegister, "");
    }

    // ---------- LOGIN ----------
    async void OnLoginClick()
    {
        if (sibuk) return;

        string email = UIHelper.GetText(inputEmailLogin).Trim();
        string password = UIHelper.GetText(inputPasswordLogin);

        string salah = Validasi(email, password, false);
        if (salah != null) { Pesan(salah); return; }

        SetSibuk(true);
        Pesan("Sedang masuk...");

        string error = await AuthService.MasukAsync(email, password);
        if (error == null)
        {
            SceneNavigator.Buka(SceneNames.Menu);
            return;
        }

        SetSibuk(false);
        Pesan(error);
    }

    // ---------- REGISTER (setelah berhasil langsung login) ----------
    async void OnRegisterClick()
    {
        if (sibuk) return;

        string email = UIHelper.GetText(inputEmailRegister).Trim();
        string password = UIHelper.GetText(inputPasswordRegister);

        string salah = Validasi(email, password, true);
        if (salah != null) { Pesan(salah); return; }

        SetSibuk(true);
        Pesan("Membuat akun...");

        string error = await AuthService.DaftarAsync(email, password);
        if (error == null)
        {
            SceneNavigator.Buka(SceneNames.Menu);
            return;
        }

        SetSibuk(false);
        Pesan(error);
    }

    // ---------- HELPER ----------
    // null = data sudah layak dikirim ke Firebase
    static string Validasi(string email, string password, bool cekPanjang)
    {
        if (string.IsNullOrEmpty(email)) return "Isi email dulu.";
        if (!email.Contains("@") || !email.Contains(".")) return "Format email tidak valid.";
        if (string.IsNullOrEmpty(password)) return "Isi password dulu.";
        if (cekPanjang && password.Length < MIN_PASSWORD) return "Password minimal " + MIN_PASSWORD + " karakter.";
        return null;
    }

    // pesan tampil di panel yang sedang aktif
    void Pesan(string teks)
    {
        UIHelper.Pesan(modeLogin ? txtPesanLogin : txtPesanRegister, teks);
    }

    // kunci semua tombol selama proses berjalan supaya tidak terkirim dua kali
    void SetSibuk(bool nilai)
    {
        sibuk = nilai;
        btnLogin.interactable = !nilai;
        btnRegister.interactable = !nilai;
        btnKeRegister.interactable = !nilai;
        btnKeLogin.interactable = !nilai;
        if (loading != null) loading.SetActive(nilai);
    }
}