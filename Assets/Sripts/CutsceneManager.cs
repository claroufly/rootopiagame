
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class CutsceneManager : MonoBehaviour
{
    [Header("Cutscene Frames")]
    public Sprite[] frames;
    public Image displayImage;
    public Button continueButton;

    [Header("Fade")]
    public Image fadeImage;
    public float fadeDuration = 0.5f;

    [Header("Animation")]
    public float animationDuration = 5f;
    public float zoomAmount = 1.1f;
    public float panAmount = 30f;

    private int currentFrame = 0;
    private bool isTransitioning = false;

    private RectTransform imageTransform;

    private Vector2 startingPosition;
    private Vector3 startingScale;

    private float animationTimer = 0f;

    void Start()
    {
        // Check if frames exist
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning("No cutscene frames assigned.");
            return;
        }

        // Check required references
        if (displayImage == null)
        {
            Debug.LogError("Display Image is not assigned!");
            return;
        }

        if (continueButton == null)
        {
            Debug.LogError("Continue Button is not assigned!");
            return;
        }

        if (fadeImage == null)
        {
            Debug.LogError("Fade Image is not assigned!");
            return;
        }

        imageTransform = displayImage.GetComponent<RectTransform>();

        // Make sure fade starts invisible
        SetFadeAlpha(0f);

        // Show first frame
        ShowFrame(0);

        // Listen for button click
        continueButton.onClick.AddListener(OnContinueClicked);
    }

    void ShowFrame(int index)
    {
        displayImage.sprite = frames[index];

        // Reset animation
        animationTimer = 0f;

        startingPosition = imageTransform.anchoredPosition;
        startingScale = imageTransform.localScale;

        imageTransform.anchoredPosition = startingPosition;
        imageTransform.localScale = startingScale;
    }

    void OnContinueClicked()
    {
        if (isTransitioning)
            return;

        StartCoroutine(TransitionToNextFrame());
    }

    IEnumerator TransitionToNextFrame()
    {
        isTransitioning = true;

        // --------------------------------
        // FADE TO BLACK
        // --------------------------------

        yield return StartCoroutine(Fade(1f));

        // --------------------------------
        // CHANGE FRAME
        // --------------------------------

        currentFrame++;

        if (currentFrame < frames.Length)
        {
            ShowFrame(currentFrame);

            // --------------------------------
            // FADE FROM BLACK
            // --------------------------------

            yield return StartCoroutine(Fade(0f));

            isTransitioning = false;
        }
        else
        {
            // --------------------------------
            // CUTSCENE FINISHED
            // --------------------------------

            SceneManager.LoadScene("Rootopia");
        }
    }

    IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = fadeImage.color.a;
        float elapsed = 0f;

        Color color = fadeImage.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float progress = elapsed / fadeDuration;

            color.a = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                progress
            );

            fadeImage.color = color;

            yield return null;
        }

        color.a = targetAlpha;
        fadeImage.color = color;
    }

    void SetFadeAlpha(float alpha)
    {
        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }

   
void Update()
{
    if (isTransitioning)
        return;

    if (currentFrame >= frames.Length)
        return;

    // --------------------------------
    // ANIMATION TIMER
    // --------------------------------

    animationTimer += Time.deltaTime;

    float progress = Mathf.Clamp01(
        animationTimer / animationDuration
    );

    // Smooth Ease In + Ease Out
    // Starts slow → speeds up → slows down
    float smoothProgress =
        progress * progress * (3f - 2f * progress);

    // --------------------------------
    // ZOOM
    // --------------------------------

    float zoom = Mathf.Lerp(
        1f,
        zoomAmount,
        smoothProgress
    );

    imageTransform.localScale =
        startingScale * zoom;

    // --------------------------------
    // PAN
    // --------------------------------

    float panX =
        Mathf.Sin(smoothProgress * Mathf.PI) * panAmount;

    float panY =
        Mathf.Cos(smoothProgress * Mathf.PI) * panAmount;

    imageTransform.anchoredPosition =
        startingPosition + new Vector2(panX, panY);
}


}

