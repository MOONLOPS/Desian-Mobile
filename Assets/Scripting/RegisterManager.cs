using UnityEngine;
using TMPro;

public class RegisterManager : MonoBehaviour
{
    public TMP_InputField inputnama;
    public TMP_InputField inputemail;
    public TMP_InputField inputPassword;
    public TMP_InputField inputKonfirmasi;

    public void ProsesDaftar()
    {
        string nama = inputnama.text;
        string email = inputemail.text;
        string password = inputPassword.text;
        string konfirmasi = inputKonfirmasi.text;

        if (string.IsNullOrEmpty(nama) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("Pendaftaran gagal : Harap isi semua data!!");
            return;
        }

        if (password != konfirmasi)
        {
            Debug.LogWarning("Pendaftaran gagal : Password dan Konfimasi Password tidak cocok!");
            return;
        }

        int otp = Random.Range(100000, 999999);

        KirimEmailOTP(nama, email, otp);
    }

    private void KirimEmailOTP(string nama, string email, int otp)
    {

        Debug.Log("================================");
        Debug.Log($"Halo {nama}, memproses pendaftaran....");
        Debug.Log($"MENGIRIM OTP: {otp} ke email: {email}");
        Debug.Log("Status: Membutuhkan koneksi internet");
        Debug.Log("================================");
    }
}
