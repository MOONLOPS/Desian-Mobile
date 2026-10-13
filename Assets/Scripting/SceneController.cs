using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public void KeHalamanDaftar()
    {
        SceneManager.LoadScene("Daftar Akun");
    }

    public void KeHalamanLogin()
    {
        SceneManager.LoadScene("LOGIN");
    } 
}