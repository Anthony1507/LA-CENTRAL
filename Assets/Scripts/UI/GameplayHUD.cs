using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameplayHUD : MonoBehaviour
{
    [SerializeField] private IncidentManager incidentManager;

    [Header("HUD labels")]
    [SerializeField] private TMP_Text incidentTitleText;
    [SerializeField] private TMP_Text incidentDescriptionText;
    [SerializeField] private TMP_Text reputationText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text interactionHintText;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Results overlays")]
    [SerializeField] private GameObject completedPanel;
    [SerializeField] private TMP_Text completedText;
    [SerializeField] private Button nextIncidentButton;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;

    private void Awake()
    {
        if (nextIncidentButton != null) nextIncidentButton.onClick.AddListener(() => incidentManager?.NextIncident());
        if (restartButton != null) restartButton.onClick.AddListener(() => incidentManager?.RestartCampaign());
        HideOverlays();
        SetInteractionHint("");
    }

    public void RefreshStatus(IncidentDefinition incident, int number, int total, int reputation, float seconds)
    {
        if (incidentTitleText != null)
            incidentTitleText.text = incident == null ? "Sin incidencia" : $"CASO {number:00}/{total:00} · {incident.title}";
        if (incidentDescriptionText != null) incidentDescriptionText.text = incident == null ? "" : incident.description;
        if (reputationText != null) reputationText.text = "Reputación: " + reputation;
        if (timerText != null)
        {
            int whole = Mathf.FloorToInt(seconds);
            timerText.text = $"Tiempo: {whole / 60:00}:{whole % 60:00}";
        }
    }

    public void SetInteractionHint(string hint)
    {
        if (interactionHintText != null) interactionHintText.text = hint;
    }

    public void ShowFeedback(string message)
    {
        if (feedbackText != null) feedbackText.text = message;
    }

    public void ShowCompleted(string title, int points, float elapsed, bool hasNext)
    {
        if (completedPanel != null) completedPanel.SetActive(true);
        if (completedText != null)
            completedText.text = $"¡Incidencia resuelta!\n{title}\n+{points} puntos\nTiempo: {elapsed:0.0} s" +
                                 (hasNext ? "" : "\n\n¡Primer prototipo completado!");
        if (nextIncidentButton != null) nextIncidentButton.gameObject.SetActive(hasNext);
    }

    public void ShowGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    public void HideOverlays()
    {
        if (completedPanel != null) completedPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (nextIncidentButton != null) nextIncidentButton.gameObject.SetActive(true);
    }
}
