
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ResultsScreenController : MonoBehaviour
{
    [Header("Results")]
    public TMP_Text titleText;
    public TMP_Text subtitleText;
    public TMP_Text reputationText;
    public TMP_Text incidentsText;
    public TMP_Text evaluationText;
    public TMP_Text summaryText;
    public TMP_Text helpText;

    [Header("Buttons")]
    public Button creditsButton;
    public Button replayButton;
    public Button menuButton;

    private const string CreditsScene = "05_Credits";
    private const string ConcertScene = "03_ConcertFinale";
    private const string MenuScene = "01_MainMenu";

    private void Awake()
    {
        if (creditsButton != null)
            creditsButton.onClick.AddListener(OpenCredits);

        if (replayButton != null)
            replayButton.onClick.AddListener(ReplayConcert);

        if (menuButton != null)
            menuButton.onClick.AddListener(OpenMenu);
    }

    private void Start()
    {
        Time.timeScale = 1f;

        RefreshResults();
        RefreshButtons();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.cKey.wasPressedThisFrame)
            OpenCredits();

        if (keyboard.rKey.wasPressedThisFrame)
            ReplayConcert();

        if (keyboard.mKey.wasPressedThisFrame)
            OpenMenu();
    }

    private void RefreshResults()
    {
        bool won =
            PlayerPrefs.GetInt("LC_ConcertWon", 0) == 1;

        bool assistedConcert =
            PlayerPrefs.GetInt("LC_ConcertAssisted", 0) == 1;

        bool assistedCampaign =
            PlayerPrefs.GetInt("LC_CampaignAssistUsed", 0) == 1;

        bool assisted =
            assistedConcert || assistedCampaign;

        bool hasReputation =
            PlayerPrefs.HasKey("LC_FinalCampaignReputation");

        bool hasIncidents =
            PlayerPrefs.HasKey("LC_FinalCampaignCount");

        bool hasTime =
            PlayerPrefs.HasKey("LC_FinalCampaignSeconds");

        bool hasMistakes =
            PlayerPrefs.HasKey("LC_FinalCampaignMistakes");

        int reputation = PlayerPrefs.GetInt(
            "LC_FinalCampaignReputation", 0);

        int incidents = PlayerPrefs.GetInt(
            "LC_FinalCampaignCount", 0);

        float campaignSeconds = PlayerPrefs.GetFloat(
            "LC_FinalCampaignSeconds", 0f);

        int mistakes = PlayerPrefs.GetInt(
            "LC_FinalCampaignMistakes", 0);

        if (titleText != null)
        {
            titleText.text = won
                ? "¡CONCIERTO SALVADO!"
                : "LA CENTRAL - RESULTADOS";
        }

        if (subtitleText != null)
        {
            subtitleText.text = won
                ? "Has conseguido que el barrio de la nota."
                : "Todavia no hay una victoria registrada.";
        }

        if (reputationText != null)
        {
            reputationText.text =
                "REPUTACION\n\n" +
                (hasReputation ? reputation.ToString() : "--") +
                "\nPUNTOS";
        }

        if (incidentsText != null)
        {
            incidentsText.text =
                "INCIDENCIAS\n\n" +
                (hasIncidents ? incidents.ToString() : "--") +
                " / 10";
        }

        if (evaluationText != null)
        {
            string evaluation = "--";

            if (won && assisted)
            {
                evaluation = "ASISTIDA";
            }
            else if (won && hasReputation)
            {
                evaluation = CalculateGrade(reputation);
            }
            else if (won)
            {
                evaluation = "VICTORIA";
            }

            evaluationText.text =
                "EVALUACION\n\n" + evaluation;
        }

        if (summaryText != null)
        {
            if (!won)
            {
                summaryText.text =
                    "Completa la campaña y supera " +
                    "el concierto final para obtener " +
                    "tu evaluacion.";

                return;
            }

            string resultMessage = assisted
                ? "El concierto fue salvado con asistencia."
                : "¡Salvaste el concierto sin asistencia!";

            string timeMessage = hasTime
                ? FormatTime(campaignSeconds)
                : "--:--:--";

            string mistakesMessage = hasMistakes
                ? mistakes.ToString()
                : "--";

            summaryText.text =
                resultMessage + "\n" +
                "TIEMPO DE INCIDENCIAS: " + timeMessage +
                "    ERRORES: " + mistakesMessage + "\n" +
                (assisted
                    ? "Resultado no competitivo."
                    : "Resultado completo.");
        }

        if (helpText != null)
        {
            helpText.text =
                "C - Creditos     R - Repetir concierto" +
                "     M - Menu principal";
        }
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.Max(
            0,
            Mathf.FloorToInt(seconds)
        );

        int hours = total / 3600;
        int minutes = (total % 3600) / 60;
        int remaining = total % 60;

        return hours.ToString("00") + ":" +
               minutes.ToString("00") + ":" +
               remaining.ToString("00");
    }

    private static string CalculateGrade(int reputation)
    {
        if (reputation >= 120)
            return "S";

        if (reputation >= 100)
            return "A";

        if (reputation >= 80)
            return "B";

        if (reputation >= 60)
            return "C";

        return "D";
    }

    private void RefreshButtons()
    {
        if (creditsButton != null)
        {
            creditsButton.interactable =
                Application.CanStreamedLevelBeLoaded(CreditsScene);
        }

        if (replayButton != null)
        {
            replayButton.interactable =
                Application.CanStreamedLevelBeLoaded(ConcertScene);
        }

        if (menuButton != null)
        {
            menuButton.interactable =
                Application.CanStreamedLevelBeLoaded(MenuScene);
        }
    }

    public void OpenCredits()
    {
        if (!Application.CanStreamedLevelBeLoaded(CreditsScene))
            return;

        PlayerPrefs.SetString(
            "LC_CreditsReturnScene",
            "04_Results"
        );

        PlayerPrefs.Save();

        SceneManager.LoadScene(CreditsScene);
    }

    public void ReplayConcert()
    {
        if (!Application.CanStreamedLevelBeLoaded(ConcertScene))
            return;

        PlayerPrefs.SetInt("LC_ConcertWon", 0);
        PlayerPrefs.SetInt("LC_ConcertAssisted", 0);
        PlayerPrefs.Save();

        SceneManager.LoadScene(ConcertScene);
    }

    public void OpenMenu()
    {
        if (!Application.CanStreamedLevelBeLoaded(MenuScene))
            return;

        SceneManager.LoadScene(MenuScene);
    }
}
