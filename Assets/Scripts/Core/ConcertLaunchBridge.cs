
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class ConcertLaunchBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private IncidentManager incidentManager;

    [SerializeField] private GameObject completedPanel;

    [Header("Transition")]
    [SerializeField] private string concertSceneName =
        "03_ConcertFinale";

    [SerializeField, Min(0.5f)]
    private float completionDisplaySeconds = 2.5f;

    private bool completionDetected;
    private bool waitingForPlayer;
    private bool transitionStarted;

    private float completionDetectedAt;

    private void Update()
    {
        if (incidentManager == null || transitionStarted)
            return;

        bool finalIncidentCompleted =
            incidentManager.IncidentCount == 10 &&
            incidentManager.IncidentNumber == 10 &&
            !incidentManager.CanInteract &&
            incidentManager.Reputation > 0;

        if (!finalIncidentCompleted)
        {
            completionDetected = false;
            waitingForPlayer = false;
            return;
        }

        if (!completionDetected)
        {
            completionDetected = true;
            completionDetectedAt = Time.unscaledTime;

            ReplacePrototypeMessage();
            return;
        }

        if (!waitingForPlayer)
        {
            float elapsed =
                Time.unscaledTime - completionDetectedAt;

            if (elapsed < completionDisplaySeconds)
                return;

            if (completedPanel != null)
            {
                completedPanel.SetActive(false);
            }
            else
            {
                Debug.LogWarning(
                    "[LA CENTRAL] CompletedPanel sin asignar " +
                    "en ConcertLaunchBridge."
                );
            }

            waitingForPlayer = true;
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        bool confirm =
            keyboard.enterKey.wasPressedThisFrame ||
            keyboard.numpadEnterKey.wasPressedThisFrame;

        if (confirm)
            LaunchConcert();
    }

    private void ReplacePrototypeMessage()
    {
        if (completedPanel == null)
            return;

        Transform labelTransform =
            completedPanel.transform.Find("CompletedText");

        if (labelTransform == null)
            return;

        TMP_Text label =
            labelTransform.GetComponent<TMP_Text>();

        if (label == null)
            return;

        label.text = label.text.Replace(
            "¡Primer prototipo completado!",
            "¡Preparativos del concierto listos!"
        );
    }

    private void LaunchConcert()
    {
        if (transitionStarted)
            return;

        if (!Application.CanStreamedLevelBeLoaded(
                concertSceneName))
        {
            Debug.LogError(
                "[LA CENTRAL] Escena no disponible: " +
                concertSceneName
            );
            return;
        }

        transitionStarted = true;
        waitingForPlayer = false;

        PlayerPrefs.SetInt(
            "LC_FinalCampaignReputation",
            incidentManager.Reputation
        );

        PlayerPrefs.SetInt(
            "LC_FinalCampaignCount",
            incidentManager.IncidentNumber
        );

        PlayerPrefs.SetInt("LC_ConcertWon", 0);
        PlayerPrefs.SetInt("LC_ConcertAssisted", 0);

        PlayerPrefs.Save();

        Time.timeScale = 1f;
        SceneManager.LoadScene(concertSceneName);
    }

    private void OnGUI()
    {

        if (!waitingForPlayer || transitionStarted)
            return;

        float width =
            Mathf.Min(Screen.width - 30f, 680f);

        float height = 135f;

        Rect area = new Rect(
            (Screen.width - width) * 0.5f,
            Screen.height - height - 25f,
            width,
            height
        );

        GUI.Box(area, "");

        GUILayout.BeginArea(area);

        GUILayout.Space(12);

        GUILayout.Label(
            "¡LOS PREPARATIVOS DEL CONCIERTO ESTAN LISTOS!",
            GUI.skin.box
        );

        GUILayout.Space(8);

        GUILayout.Label(
            "Pulsa ENTER para comenzar el espectaculo.",
            GUI.skin.box
        );

        GUILayout.EndArea();
    }
}
