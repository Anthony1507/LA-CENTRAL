
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class GameplayPauseAssistController : MonoBehaviour
{
    [Header("Gameplay")]
    public IncidentManager incidentManager;
    public BuildingAccessManager buildingAccess;
    public PlayerMovement2D playerMovement;
    public RepairTerminalUI repairTerminal;

    [Header("Existing Panels")]
    public GameObject remotePanel;
    public GameObject gameOverPanel;

    [Header("Pause UI")]
    public GameObject pausePanel;
    public TMP_Text pauseStatusText;
    public Button resumeButton;
    public Button restartIncidentButton;
    public Button skipIncidentButton;
    public Button mainMenuButton;

    [Header("Skip Confirmation")]
    public GameObject skipConfirmationPanel;
    public Button confirmSkipButton;
    public Button cancelSkipButton;

    [Header("Game Over Assistance")]
    public Button gameOverAssistButton;

    private const string GameplayScene = "02_Gameplay";
    private const string ConcertScene = "03_ConcertFinale";
    private const string MainMenuScene = "01_MainMenu";
    private const string AssistanceKey = "LC_CampaignAssistUsed";

    private bool isPaused;
    private bool movementWasEnabled;

    private void Awake()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (skipConfirmationPanel != null)
            skipConfirmationPanel.SetActive(false);

        BindButtons();
    }

    private void Start()
    {

        if (!SaveService.HasSave())
        {
            PlayerPrefs.DeleteKey(AssistanceKey);
            PlayerPrefs.Save();
        }
    }

    private void Update()
    {
        if (PhoneNarrativeController.IsAnyOpen)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (!keyboard.escapeKey.wasPressedThisFrame)
            return;

        if (isPaused)
        {
            if (skipConfirmationPanel != null &&
                skipConfirmationPanel.activeSelf)
            {
                CancelSkip();
            }
            else
            {
                ResumeGame();
            }

            return;
        }

        if (repairTerminal != null && repairTerminal.IsOpen)
            return;

        if (remotePanel != null &&
            remotePanel.activeInHierarchy)
            return;

        if (gameOverPanel != null &&
            gameOverPanel.activeInHierarchy)
            return;

        OpenPause();
    }

    private void BindButtons()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (restartIncidentButton != null)
        {
            restartIncidentButton.onClick.AddListener(
                RestartCurrentIncident);
        }

        if (skipIncidentButton != null)
        {
            skipIncidentButton.onClick.AddListener(
                RequestSkip);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(
                ReturnToMainMenu);
        }

        if (confirmSkipButton != null)
        {
            confirmSkipButton.onClick.AddListener(
                ConfirmSkip);
        }

        if (cancelSkipButton != null)
        {
            cancelSkipButton.onClick.AddListener(
                CancelSkip);
        }

        if (gameOverAssistButton != null)
        {
            gameOverAssistButton.onClick.AddListener(
                ContinueAfterGameOverAssisted);
        }
    }

    private void SelectButton(Button button)
    {
        if (button == null ||
            EventSystem.current == null ||
            !button.gameObject.activeInHierarchy ||
            !button.interactable)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(
            button.gameObject);
    }

    public void OpenPause()
    {
        if (PhoneNarrativeController.IsAnyOpen)
            return;

        if (isPaused ||
            incidentManager == null ||
            incidentManager.Current == null ||
            pausePanel == null)
        {
            return;
        }

        if (repairTerminal != null && repairTerminal.IsOpen)
            return;

        if (remotePanel != null &&
            remotePanel.activeInHierarchy)
            return;

        if (gameOverPanel != null &&
            gameOverPanel.activeInHierarchy)
            return;

        movementWasEnabled =
            playerMovement != null && playerMovement.enabled;

        if (playerMovement != null)
            playerMovement.enabled = false;

        isPaused = true;
        Time.timeScale = 0f;

        pausePanel.SetActive(true);

        if (skipConfirmationPanel != null)
            skipConfirmationPanel.SetActive(false);

        bool canModifyIncident = incidentManager.CanInteract;

        if (restartIncidentButton != null)
            restartIncidentButton.interactable = canModifyIncident;

        if (skipIncidentButton != null)
            skipIncidentButton.interactable = canModifyIncident;

        if (pauseStatusText != null)
        {
            pauseStatusText.text =
                "CASO " +
                incidentManager.IncidentNumber +
                " / " +
                incidentManager.IncidentCount +
                "\n" +
                incidentManager.Current.title +
                "\nREPUTACION: " +
                incidentManager.Reputation +
                "\n\nEl tiempo de la incidencia esta detenido.";
        }

        SelectButton(resumeButton);
    }

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        if (skipConfirmationPanel != null)
            skipConfirmationPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        isPaused = false;
        Time.timeScale = 1f;

        if (playerMovement != null)
            playerMovement.enabled = movementWasEnabled;
    }

    private CampaignSaveData BuildCheckpoint(
        int nextIncidentIndex,
        bool concertStage)
    {
        string[] knownBuildings =
            buildingAccess != null
                ? buildingAccess.GetKnownBuildings()
                : new string[0];

        return new CampaignSaveData
        {
            stage = concertStage
                ? SaveService.StageConcert
                : SaveService.StageGameplay,

            nextIncidentIndex = nextIncidentIndex,

            reputation = Mathf.Max(
                1,
                incidentManager.Reputation),

            wrongActions = incidentManager.WrongActions,

            completedIncidentSeconds =
                incidentManager.CompletedIncidentSeconds,

            remoteUnlocked =
                buildingAccess != null &&
                buildingAccess.RemoteUnlocked,

            knownBuildingIds = knownBuildings
        };
    }

    private void MarkAssistanceUsed()
    {
        PlayerPrefs.SetInt(AssistanceKey, 1);
        PlayerPrefs.Save();
    }

    private void LoadScene(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                "[LA CENTRAL] Escena no registrada: " +
                sceneName);
            return;
        }

        Time.timeScale = 1f;
        isPaused = false;

        SceneManager.LoadScene(sceneName);
    }

    public void RestartCurrentIncident()
    {
        if (!isPaused ||
            incidentManager == null ||
            !incidentManager.CanInteract)
        {
            return;
        }

        int index = incidentManager.IncidentNumber - 1;

        CampaignSaveData checkpoint =
            BuildCheckpoint(index, false);

        SaveService.Save(checkpoint);

        LoadScene(GameplayScene);
    }

    public void RequestSkip()
    {
        if (!isPaused ||
            incidentManager == null ||
            !incidentManager.CanInteract ||
            skipConfirmationPanel == null)
        {
            return;
        }

        skipConfirmationPanel.SetActive(true);
        SelectButton(confirmSkipButton);
    }

    public void CancelSkip()
    {
        if (skipConfirmationPanel != null)
            skipConfirmationPanel.SetActive(false);

        SelectButton(skipIncidentButton);
    }

    public void ConfirmSkip()
    {
        if (!isPaused ||
            incidentManager == null ||
            !incidentManager.CanInteract)
        {
            return;
        }

        IncidentDefinition current = incidentManager.Current;

        if (current == null)
            return;

        if (buildingAccess != null && current.objectives != null)
        {
            foreach (DeviceObjective objective in current.objectives)
            {
                buildingAccess.RegisterPhysicalRepair(
                    objective.buildingId);
            }

            if (incidentManager.IncidentNumber >= 2)
            {
                buildingAccess.UnlockRemote();
            }
        }

        MarkAssistanceUsed();

        int nextIndex = incidentManager.IncidentNumber;

        bool lastIncident =
            nextIndex >= incidentManager.IncidentCount;

        CampaignSaveData checkpoint =
            BuildCheckpoint(nextIndex, lastIncident);

        SaveService.Save(checkpoint);

        if (lastIncident)
        {
            PlayerPrefs.SetInt(
                "LC_FinalCampaignReputation",
                incidentManager.Reputation);

            PlayerPrefs.SetInt(
                "LC_FinalCampaignCount",
                incidentManager.IncidentCount);

            PlayerPrefs.SetFloat(
                "LC_FinalCampaignSeconds",
                incidentManager.CompletedIncidentSeconds);

            PlayerPrefs.SetInt(
                "LC_FinalCampaignMistakes",
                incidentManager.WrongActions);

            PlayerPrefs.SetInt("LC_ConcertWon", 0);
            PlayerPrefs.SetInt("LC_ConcertAssisted", 0);
            PlayerPrefs.Save();

            LoadScene(ConcertScene);
        }
        else
        {
            LoadScene(GameplayScene);
        }
    }

    public void ReturnToMainMenu()
    {
        if (!isPaused)
            return;
        LoadScene(MainMenuScene);
    }

    public void ContinueAfterGameOverAssisted()
    {
        if (incidentManager == null ||
            incidentManager.Current == null ||
            gameOverPanel == null ||
            !gameOverPanel.activeInHierarchy ||
            incidentManager.Reputation > 0)
        {
            return;
        }

        MarkAssistanceUsed();

        int currentIndex =
            incidentManager.IncidentNumber - 1;

        CampaignSaveData checkpoint =
            BuildCheckpoint(currentIndex, false);

        checkpoint.reputation = 20;

        SaveService.Save(checkpoint);

        LoadScene(GameplayScene);
    }

    private void OnDisable()
    {
        if (isPaused)
        {
            Time.timeScale = 1f;
            isPaused = false;
        }
    }
}
