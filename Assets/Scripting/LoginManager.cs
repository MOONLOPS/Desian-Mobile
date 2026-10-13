using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; 

public class LoginManager : MonoBehaviour
{
    public TMP_InputField inputEmail;
    public TMP_InputField inputPassword;


    [Header("Pengaturan Scene")]
    public string namaSceneTujuan = "Page 1";

   
    public void ProsesLogin()
    {
        string email = inputEmail.text;
        string password = inputPassword.text;

        
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("Login gagal: Email dan Password harus diisi!");
            return;
        }

       
        if (email == "admin" && password == "123456")
        {
            Debug.Log("Login Berhasil sebagai ADMIN!");

           
            SceneManager.LoadScene(namaSceneTujuan);
        }
        else
        {
            
            Debug.LogError("Login gagal: Email atau Password salah!");
        }
    }
}