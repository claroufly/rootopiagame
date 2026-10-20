using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("Rootopia"); // your gameplay scene name
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}