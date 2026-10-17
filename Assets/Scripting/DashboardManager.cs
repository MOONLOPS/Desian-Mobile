using UnityEngine;
using UnityEngine.SceneManagement;

public class DashboardManager : MonoBehaviour
{
    public void BukaTabunganku()
    {
        SceneManager.LoadScene("T1");
    }

    public void BukaTabunganBersama()
    {
        SceneManager.LoadScene("TB1");
    }
}