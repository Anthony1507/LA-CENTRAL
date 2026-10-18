
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ConcertFinaleFlowController : MonoBehaviour
{
    [Header("Finale")]
    [SerializeField] private ConcertFinaleController finale;

    [Header("Pause")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private TMP_Text pauseStatusText;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button menuButton;

    [Header("Epilogue")]
    [SerializeField] private GameObject epiloguePanel;
    [SerializeField] private TMP_Text epilogueTitleText;
    [SerializeField] private TMP_Text epilogueSpeakerText;
    [SerializeField] private TMP_Text epilogueBodyText;
    [SerializeField] private TMP_Text epilogueProgressText;
    [SerializeField] private Button epilogueNextButton;
    [SerializeField] private Button epilogueSkipButton;

    [Header("Scene")]
    [SerializeField]
    private string menuSceneName =
        "01_MainMenu";

    private readonly string[] speakers =
    {
        "COORDINADORA",
        "OPERADORA DE LA CENTRAL",
        "VECINOS DEL BARRIO",
        "PROTAGONISTA",
        "PROTAGONISTA",
        "COORDINADORA",
        "PROTAGONISTA"
    };

    private readonly string[] dialogueLines =
    {
        "¿Hola? ¿La Central me escucha? " +
        "¡La señal ha vuelto! ¡Estamos en directo!",

        "Confirmado. Todos los equipos responden. " +
        "La transmisión se ha recuperado por completo.",

        "¡SE ESCUCHA! ¡POR FIN! " +
        "¡Que empiece el concierto!",

        "Bueno... eso estuvo cerca. " +
        "Mucho más cerca de lo que pienso admitir.",

        "Y tú, sí, tú, el que está al otro lado " +
        "de la pantalla... lo conseguiste.",

        "De parte de todo el barrio: gracias. " +
        "Hoy no solo arreglaste aparatos. " +
        "Hiciste posible algo que todos queríamos vivir.",

        "Diez avisos, doce edificios y un concierto. " +
        "Creo que por hoy ya hemos dado suficiente la nota. " +
        "Nos vemos en el siguiente turno."
    };

    private bool paused;
    private bool epilogueActive;
    private bool epilogueShown;

    private int epilogueIndex;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (epiloguePanel != null)
            epiloguePanel.SetActive(false);

        BindButtons();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (epilogueActive)
        {
            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    SkipEpilogue();
                }
                else if (
                    keyboard.enterKey.wasPressedThisFrame ||
                    keyboard.numpadEnterKey.wasPressedThisFrame ||
                    keyboard.spaceKey.wasPressedThisFrame)
                {
                    NextEpilogueLine();
                }
            }

            return;
        }

        if (paused)
        {
            if (keyboard != null &&
                keyboard.escapeKey.wasPressedThisFrame)
            {
                ResumeFinale();
            }

            return;
        }

        if (!epilogueShown && IsVictoryReady())
        {
            BeginEpilogue();
            return;
        }

        if (keyboard != null &&
            keyboard.escapeKey.wasPressedThisFrame)
        {
            OpenPause();
        }
    }

    private bool IsVictoryReady()
    {
        return finale != null &&
               finale.resultPanel != null &&
               finale.resultPanel.activeInHierarchy &&
               finale.continueButton != null &&
               finale.continueButton.gameObject.activeInHierarchy &&
               PlayerPrefs.GetInt("LC_ConcertWon", 0) == 1;
    }

    private void BindButtons()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeFinale);

        if (menuButton != null)
            menuButton.onClick.AddListener(ReturnToMenu);

        if (epilogueNextButton != null)
        {
            epilogueNextButton.onClick.AddListener(
                NextEpilogueLine);
        }

        if (epilogueSkipButton != null)
        {
            epilogueSkipButton.onClick.AddListener(
                SkipEpilogue);
        }
    }


    public void OpenPause()
    {
        if (paused || epilogueActive || finale == null)
            return;

        if (pausePanel == null)
            return;

        if (finale.resultPanel != null &&
            finale.resultPanel.activeInHierarchy)
        {
            return;
        }

        previousTimeScale = Time.timeScale;

        paused = true;

        finale.enabled = false;

        Time.timeScale = 0f;

        pausePanel.SetActive(true);
        pausePanel.transform.SetAsLastSibling();

        if (pauseStatusText != null)
        {
            string phase = finale.phaseText != null
                ? finale.phaseText.text
                : "CONCIERTO FINAL";

            string timer = finale.timerText != null
                ? finale.timerText.text
                : "";

            pauseStatusText.text =
                phase + "\n" +
                timer + "\n\n" +
                "El reloj del concierto está detenido.";
        }

        SelectButton(resumeButton);
    }

    public void ResumeFinale()
    {
        if (!paused)
            return;

        paused = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        Time.timeScale = previousTimeScale;

        if (finale != null)
            finale.enabled = true;
    }

    public void ReturnToMenu()
    {
        if (!paused)
            return;

        if (!Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            Debug.LogError(
                "[LA CENTRAL] MainMenu no está disponible.");
            return;
        }

        paused = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        Time.timeScale = 1f;

        if (finale != null)
            finale.enabled = true;

        SceneManager.LoadScene(menuSceneName);
    }

    private void BeginEpilogue()
    {
        if (epilogueShown || epiloguePanel == null)
            return;

        epilogueShown = true;
        epilogueActive = true;
        epilogueIndex = 0;

        previousTimeScale = Time.timeScale;

        Time.timeScale = 0f;

        if (finale != null)
            finale.enabled = false;

        epiloguePanel.SetActive(true);
        epiloguePanel.transform.SetAsLastSibling();

        if (epilogueTitleText != null)
        {
            epilogueTitleText.text =
                "LA CENTRAL — LA ÚLTIMA NOTA";
        }

        RefreshEpilogue();
        SelectButton(epilogueNextButton);
    }

    public void NextEpilogueLine()
    {
        if (!epilogueActive)
            return;

        epilogueIndex++;

        if (epilogueIndex >= dialogueLines.Length)
        {
            EndEpilogue();
            return;
        }

        RefreshEpilogue();
    }

    public void SkipEpilogue()
    {
        if (!epilogueActive)
            return;

        EndEpilogue();
    }

    private void RefreshEpilogue()
    {
        if (!epilogueActive)
            return;

        int index = Mathf.Clamp(
            epilogueIndex,
            0,
            dialogueLines.Length - 1);

        if (epilogueSpeakerText != null)
            epilogueSpeakerText.text = speakers[index];

        if (epilogueBodyText != null)
        {
            string text = dialogueLines[index];

            if (index == 4)
            {
                bool assisted =
                    PlayerPrefs.GetInt(
                        "LC_ConcertAssisted", 0) == 1 ||
                    PlayerPrefs.GetInt(
                        "LC_CampaignAssistUsed", 0) == 1;

                if (assisted)
                {
                    text =
                        "Y tú, el que está al otro lado " +
                        "de la pantalla... incluso con ayuda, " +
                        "lograste que el barrio siguiera adelante.";
                }
            }

            epilogueBodyText.text = text;
        }

        if (epilogueProgressText != null)
        {
            epilogueProgressText.text =
                (index + 1) + " / " +
                dialogueLines.Length;
        }

        if (epilogueNextButton != null)
        {
            TMP_Text label =
                epilogueNextButton.GetComponentInChildren<TMP_Text>(
                    true);

            if (label != null)
            {
                label.text =
                    index == dialogueLines.Length - 1
                        ? "FINALIZAR"
                        : "SIGUIENTE";
            }
        }
    }

    private void EndEpilogue()
    {
        epilogueActive = false;

        if (epiloguePanel != null)
            epiloguePanel.SetActive(false);

        Time.timeScale = previousTimeScale;

        if (finale != null)
            finale.enabled = true;

        if (finale != null)
            SelectButton(finale.continueButton);
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

    private void OnDisable()
    {
        if (!paused && !epilogueActive)
            return;

        paused = false;
        epilogueActive = false;

        Time.timeScale = 1f;

        if (finale != null)
            finale.enabled = true;
    }
}
