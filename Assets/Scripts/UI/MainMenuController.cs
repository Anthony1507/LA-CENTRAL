
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MainMenuController : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField] public Button newGameButton;
    [SerializeField] public Button continueButton;
    [SerializeField] public Button settingsButton;
    [SerializeField] public Button controlsButton;
    [SerializeField] public Button creditsButton;
    [SerializeField] public Button exitButton;
    [SerializeField] public TMP_Text statusText;

    [Header("Panels")]
    [SerializeField] public GameObject settingsPanel;
    [SerializeField] public GameObject controlsPanel;
    [SerializeField] public GameObject confirmationPanel;

    [Header("Settings")]
    [SerializeField] public TMP_Text resolutionText;
    [SerializeField] public TMP_Text fullscreenText;
    [SerializeField] public Button previousResolutionButton;
    [SerializeField] public Button nextResolutionButton;
    [SerializeField] public Button fullscreenButton;
    [SerializeField] public Button settingsBackButton;

    [Header("Controls")]
    [SerializeField] public Button controlsBackButton;

    [Header("New Game Confirmation")]
    [SerializeField] public Button confirmNewGameButton;
    [SerializeField] public Button cancelNewGameButton;

    [Header("Scene Names")]
    [SerializeField] private string gameplayScene = "02_Gameplay";
    [SerializeField] private string concertScene = "03_ConcertFinale";
    [SerializeField] private string creditsScene = "05_Credits";

    private static readonly Vector2Int[] Resolutions =
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080)
    };

    private int resolutionIndex;
    private bool fullscreen;

    private void Awake()
    {
        RegisterButtons();
    }

    private void Start()
    {
        Time.timeScale = 1f;

        resolutionIndex = Mathf.Clamp(
            PlayerPrefs.GetInt("LC_ResolutionIndex", 2),
            0,
            Resolutions.Length - 1
        );

        fullscreen =
            PlayerPrefs.GetInt("LC_Fullscreen", 1) == 1;

        HideAllPanels();
        RefreshSettings();
        RefreshContinueButton();

        SelectButton(newGameButton);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null ||
            !keyboard.escapeKey.wasPressedThisFrame)
            return;

        if (confirmationPanel != null &&
            confirmationPanel.activeSelf)
        {
            CloseConfirmation();
            return;
        }

        if (settingsPanel != null &&
            settingsPanel.activeSelf)
        {
            CloseSettings();
            return;
        }

        if (controlsPanel != null &&
            controlsPanel.activeSelf)
        {
            CloseControls();
        }
    }

    private void RegisterButtons()
    {
        if (newGameButton != null)
        {
            newGameButton.onClick.AddListener(
                OpenNewGameConfirmation
            );
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(
                ContinueGame
            );
        }

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        if (controlsButton != null)
            controlsButton.onClick.AddListener(OpenControls);

        if (creditsButton != null)
            creditsButton.onClick.AddListener(OpenCredits);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);

        if (previousResolutionButton != null)
        {
            previousResolutionButton.onClick.AddListener(
                () => ChangeResolution(-1)
            );
        }

        if (nextResolutionButton != null)
        {
            nextResolutionButton.onClick.AddListener(
                () => ChangeResolution(1)
            );
        }

        if (fullscreenButton != null)
            fullscreenButton.onClick.AddListener(ToggleFullscreen);

        if (settingsBackButton != null)
            settingsBackButton.onClick.AddListener(CloseSettings);

        if (controlsBackButton != null)
            controlsBackButton.onClick.AddListener(CloseControls);

        if (confirmNewGameButton != null)
            confirmNewGameButton.onClick.AddListener(StartNewGame);

        if (cancelNewGameButton != null)
            cancelNewGameButton.onClick.AddListener(CloseConfirmation);
    }

    private void HideAllPanels()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (controlsPanel != null)
            controlsPanel.SetActive(false);

        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }

    private void SelectButton(Button button)
    {
        if (button == null || EventSystem.current == null)
            return;

        if (!button.gameObject.activeInHierarchy ||
            !button.interactable)
            return;

        EventSystem.current.SetSelectedGameObject(
            button.gameObject
        );
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void RefreshContinueButton()
    {
        bool hasSave = SaveService.HasResumableSave();

        if (continueButton != null)
            continueButton.interactable = hasSave;

        if (!hasSave)
        {
            SetStatus(
                "CENTRAL VECINAL - SIN PARTIDA PENDIENTE"
            );

            return;
        }

        if (SaveService.TryLoad(out CampaignSaveData data))
        {
            if (data.stage == SaveService.StageConcert)
            {
                SetStatus(
                    "CONCIERTO FINAL PENDIENTE - CONTINUAR"
                );
            }
            else
            {
                SetStatus(
                    "PARTIDA GUARDADA - CASO " +
                    (data.nextIncidentIndex + 1) +
                    " - REPUTACION " +
                    data.reputation
                );
            }
        }
    }

    public void ContinueGame()
    {
        if (!SaveService.TryLoad(out CampaignSaveData data))
        {
            SetStatus(
                "NO SE ENCONTRO UNA PARTIDA GUARDADA."
            );

            RefreshContinueButton();
            return;
        }

        if (!SaveService.HasResumableSave())
        {
            RefreshContinueButton();
            return;
        }

        string destination =
            data.stage == SaveService.StageConcert
                ? concertScene
                : gameplayScene;

        if (!Application.CanStreamedLevelBeLoaded(destination))
        {
            SetStatus(
                "ERROR: No se encuentra " + destination
            );

            Debug.LogError(
                "[LA CENTRAL] Escena no disponible: " +
                destination
            );

            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(destination);
    }

    public void OpenNewGameConfirmation()
    {
        HideAllPanels();

        if (confirmationPanel != null)
            confirmationPanel.SetActive(true);

        SelectButton(confirmNewGameButton);
    }

    public void CloseConfirmation()
    {
        HideAllPanels();
        SelectButton(newGameButton);
    }

    public void StartNewGame()
    {
        if (!Application.CanStreamedLevelBeLoaded(gameplayScene))
        {
            HideAllPanels();

            SetStatus(
                "ERROR: No se encuentra Gameplay."
            );

            return;
        }

        SaveService.DeleteSave();

        PlayerPrefs.DeleteKey("LC_FinalCampaignReputation");
        PlayerPrefs.DeleteKey("LC_FinalCampaignCount");
        PlayerPrefs.DeleteKey("LC_FinalCampaignSeconds");
        PlayerPrefs.DeleteKey("LC_FinalCampaignMistakes");
        PlayerPrefs.DeleteKey("LC_ConcertWon");
        PlayerPrefs.DeleteKey("LC_ConcertAssisted");
        PlayerPrefs.DeleteKey("LC_CreditsReturnScene");

        PlayerPrefs.Save();

        Time.timeScale = 1f;

        SceneManager.LoadScene(gameplayScene);
    }

    public void OpenSettings()
    {
        HideAllPanels();

        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        RefreshSettings();
        SelectButton(nextResolutionButton);
    }

    public void CloseSettings()
    {
        HideAllPanels();
        SelectButton(settingsButton);
    }

    private void ChangeResolution(int direction)
    {
        resolutionIndex += direction;

        if (resolutionIndex < 0)
            resolutionIndex = Resolutions.Length - 1;

        if (resolutionIndex >= Resolutions.Length)
            resolutionIndex = 0;

        SaveSettings();
        RefreshSettings();
        ApplyDisplaySettings();
    }

    private void ToggleFullscreen()
    {
        fullscreen = !fullscreen;

        SaveSettings();
        RefreshSettings();
        ApplyDisplaySettings();
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetInt(
            "LC_ResolutionIndex",
            resolutionIndex
        );

        PlayerPrefs.SetInt(
            "LC_Fullscreen",
            fullscreen ? 1 : 0
        );

        PlayerPrefs.Save();
    }

    private void ApplyDisplaySettings()
    {
        Vector2Int resolution = Resolutions[resolutionIndex];

        if (!Application.isEditor)
        {
            Screen.SetResolution(
                resolution.x,
                resolution.y,
                fullscreen
            );
        }
    }

    private void RefreshSettings()
    {
        Vector2Int resolution = Resolutions[resolutionIndex];

        if (resolutionText != null)
        {
            resolutionText.text =
                "RESOLUCION: " +
                resolution.x + " x " + resolution.y;
        }

        if (fullscreenText != null)
        {
            fullscreenText.text =
                "PANTALLA COMPLETA: " +
                (fullscreen ? "ACTIVADA" : "DESACTIVADA");
        }
    }

    public void OpenControls()
    {
        HideAllPanels();

        if (controlsPanel != null)
            controlsPanel.SetActive(true);

        SelectButton(controlsBackButton);
    }

    public void CloseControls()
    {
        HideAllPanels();
        SelectButton(controlsButton);
    }

    public void OpenCredits()
    {
        if (!Application.CanStreamedLevelBeLoaded(creditsScene))
        {
            SetStatus(
                "ERROR: No se encuentra Credits."
            );

            return;
        }

        PlayerPrefs.SetString(
            "LC_CreditsReturnScene",
            "01_MainMenu"
        );

        PlayerPrefs.Save();

        Time.timeScale = 1f;
        SceneManager.LoadScene(creditsScene);
    }

    public void ExitGame()
    {
        Debug.Log("[LA CENTRAL] Cerrando videojuego.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
