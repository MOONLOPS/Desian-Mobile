using UnityEngine;
using TMPro;

public class RegisterManager : MonoBehaviour
{
    public TMP_InputField inputnama;
    public TMP_InputField inputemail;
    public TMP_InputField inputPassword;
    public TMP_InputField inputKonfirmasi;


    public GameObject popUpGagalDaftar;

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

        if (popUpGagalDaftar != null)
        {
            popUpGagalDaftar.SetActive(true);
        }
    }
}
