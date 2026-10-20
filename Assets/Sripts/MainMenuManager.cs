using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button startGameButton;
    public Button quitButton;

    [Header("Fade")]
    [Tooltip("A full-screen black CanvasGroup placed above all menu UI.")]
    public CanvasGroup fader;
    public float fadeDuration = 0.6f;

    [Header("Scene to Load")]
    public string cutsceneSceneName = "Rootopia";

    private bool sequenceStarted;

    private void Start()
    {
        if (fader != null)
            fader.alpha = 0f;

        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartGameClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
    }

    public void OnStartGameClicked()
    {
        Debug.Log("Start clicked, sequenceStarted: " + sequenceStarted);
        if (sequenceStarted) return;
        sequenceStarted = true;

        if (startGameButton != null)
            startGameButton.interactable = false;

        StartCoroutine(FadeAndLoad());
    }

    public void OnQuitClicked()
    {
        Debug.Log("Quit requested");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator FadeAndLoad()
    {
        yield return StartCoroutine(Fade(0f, 1f));
        SceneManager.LoadScene(cutsceneSceneName);
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fader == null) yield break;

        float elapsed = 0f;
        fader.alpha = from;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fader.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        fader.alpha = to;
    }
}