
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

        if (titleText != null)
            titleText.text = "LA CENTRAL\nCREDITOS";

        if (creditsText != null)
        {
            creditsText.text =
                "GAME SCHOLARS GAMEJAM 8\n\n" +
                "TEMA: DANDO LA NOTA\n\n" +
                "CHIFITA STUDIOS\n\n" +
                "DESARROLLO\n" +
                "Anthony\n" +
                "ByNiteYT\n\n" +
                "Diseno, arte, musica y colaboraciones:\n" +
                "creditos definitivos al integrar los assets.\n\n" +
                "GRACIAS POR JUGAR";
        }

        if (footerText != null)
        {
            footerText.text =
                "Una ciudad. Un tecnico. Un ultimo concierto.\n" +
                "Porque todos merecemos dar la nota.";
        }

        if (menuButton != null)
        {
            menuButton.interactable =
                Application.CanStreamedLevelBeLoaded(MenuScene);
        }

        if (backButton != null)
        {
            backButton.interactable =
                GetReturnScene() != "";
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
        }

        if (keyboard.mKey.wasPressedThisFrame)
        {
            OpenMenu();
        }
    }

    private string GetReturnScene()
    {
        string requestedScene = PlayerPrefs.GetString(
            "LC_CreditsReturnScene",
            ResultsScene
        );

        if (Application.CanStreamedLevelBeLoaded(requestedScene))
            return requestedScene;

        if (Application.CanStreamedLevelBeLoaded(ResultsScene))
            return ResultsScene;

        if (Application.CanStreamedLevelBeLoaded(MenuScene))
            return MenuScene;

        return "";
    }

    public void GoBack()
    {
        string sceneName = GetReturnScene();

        if (string.IsNullOrEmpty(sceneName))
            return;

        SceneManager.LoadScene(sceneName);
    }

    public void OpenMenu()
    {
        if (!Application.CanStreamedLevelBeLoaded(MenuScene))
            return;

        SceneManager.LoadScene(MenuScene);
    }
}
