
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class CreditsScreenController : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text titleText;
    public TMP_Text creditsText;
    public TMP_Text footerText;

    [Header("Buttons")]
    public Button backButton;
    public Button menuButton;

    private const string ResultsScene = "04_Results";
    private const string MenuScene = "01_MainMenu";

    private void Awake()
    {
        if (backButton != null)
            backButton.onClick.AddListener(GoBack);

        if (menuButton != null)
            menuButton.onClick.AddListener(OpenMenu);
    }

    private void Start()
    {
        Time.timeScale = 1f;

        ConfigureCredits();
        FitCreditsToContent();
        ConfigureButtons();
    }

    private void ConfigureCredits()
    {
        if (titleText != null)
        {
            titleText.text =
                "LA CENTRAL\nCRÉDITOS";
        }

        if (creditsText != null)
        {
            creditsText.text =
                "GAME SCHOLARS GAME JAM 8\n\n" +
                "DANDO LA NOTA\n\n" +

                "CHIFITA STUDIOS\n\n" +

                "UN JUEGO CREADO POR\n\n" +

                "MrTh0ny & ByNiteYT\n\n" +

                "DISEÑO · PROGRAMACIÓN · ARTE\n" +
                "MÚSICA · NARRATIVA · DESARROLLO\n\n" +

                "¡GRACIAS POR JUGAR!";
        }

        if (footerText != null)
        {
            footerText.text =
                "Una ciudad. Un técnico. Un concierto.\n" +
                "¡Todos merecemos dar la nota!";
        }
    }

    private void FitCreditsToContent()
    {
        if (creditsText == null)
            return;

        ScrollRect scrollRect =
            creditsText.GetComponentInParent<ScrollRect>();

        if (scrollRect == null)
            return;

        RectTransform textRect =
            creditsText.rectTransform;

        RectTransform contentRect =
            scrollRect.content;

        if (contentRect == null)
            return;

        Canvas.ForceUpdateCanvases();

        creditsText.ForceMeshUpdate();

        float preferredHeight =
            creditsText.GetPreferredValues(
                creditsText.text,
                textRect.rect.width,
                Mathf.Infinity
            ).y;

        float viewportHeight =
            scrollRect.viewport != null
                ? scrollRect.viewport.rect.height
                : 480f;

        float finalHeight = Mathf.Max(
            preferredHeight + 60f,
            viewportHeight
        );

        contentRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            finalHeight
        );

        textRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            finalHeight
        );

        Canvas.ForceUpdateCanvases();

        scrollRect.verticalNormalizedPosition = 1f;
    }

    private void ConfigureButtons()
    {
        if (menuButton != null)
        {
            menuButton.interactable =
                Application.CanStreamedLevelBeLoaded(
                    MenuScene
                );
        }

        if (backButton != null)
        {
            backButton.interactable =
                !string.IsNullOrEmpty(GetReturnScene());
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.escapeKey.wasPressedThisFrame ||
            keyboard.backspaceKey.wasPressedThisFrame)
        {
            GoBack();
            return;
        }

        if (keyboard.mKey.wasPressedThisFrame)
        {
            OpenMenu();
        }
    }

    private string GetReturnScene()
    {
        string requestedScene =
            PlayerPrefs.GetString(
                "LC_CreditsReturnScene",
                ResultsScene
            );

        if (!string.IsNullOrEmpty(requestedScene) &&
            Application.CanStreamedLevelBeLoaded(
                requestedScene))
        {
            return requestedScene;
        }

        if (Application.CanStreamedLevelBeLoaded(
            ResultsScene))
        {
            return ResultsScene;
        }

        if (Application.CanStreamedLevelBeLoaded(
            MenuScene))
        {
            return MenuScene;
        }

        return string.Empty;
    }

    public void GoBack()
    {
        string sceneName = GetReturnScene();

        if (string.IsNullOrEmpty(sceneName))
            return;

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public void OpenMenu()
    {
        if (!Application.CanStreamedLevelBeLoaded(
            MenuScene))
        {
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(MenuScene);
    }

    private void OnDestroy()
    {
        if (backButton != null)
            backButton.onClick.RemoveListener(GoBack);

        if (menuButton != null)
            menuButton.onClick.RemoveListener(OpenMenu);
    }
}
