
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-800)]
[DisallowMultipleComponent]
public sealed class PhoneNarrativeController : MonoBehaviour
{
    [Header("Gameplay")]
    public IncidentManager incidentManager;
    public PlayerMovement2D playerMovement;
    public PlayerInteractor2D playerInteractor;
    public RemoteAccessUI remoteAccess;

    [Header("Dialogue Assets")]
    public PhoneDialogueDefinition[] dialogues;

    [Header("Phone Panel")]
    public GameObject panelRoot;
    public TMP_Text stateText;
    public TMP_Text callerText;
    public TMP_Text speakerText;
    public TMP_Text dialogueText;
    public TMP_Text progressText;

    [Header("Phone Buttons")]
    public Button answerButton;
    public Button nextButton;
    public Button skipButton;

    [Header("Incoming Call Notification - NEW")]
    public Button incomingCallButton;

    public static bool IsAnyOpen { get; private set; }
    private static PhoneNarrativeController activeInstance;

    public static bool HasPendingCall =>
        activeInstance != null &&
        activeInstance.pendingDialogue != null;

    private enum PhoneState
    {
        Closed,
        Incoming,
        Conversation
    }

    private PhoneState state = PhoneState.Closed;

    private IncidentDefinition lastObservedIncident;
    private PhoneDialogueDefinition pendingDialogue;
    private PhoneDialogueDefinition activeDialogue;

    private int lineIndex;
    private int openedFrame;

    private float previousTimeScale = 1f;

    private bool movementWasEnabled;
    private bool interactionWasEnabled;
    private bool remoteWasEnabled;

    private TMP_Text nextButtonLabel;

    private RectTransform notificationRect;
    private Image notificationImage;

    private Quaternion originalNotificationRotation;
    private Color originalNotificationColor;

    private RepairTerminalUI repairTerminal;

    private GameObject remotePanel;
    private GameObject pausePanel;
    private GameObject completedPanel;
    private GameObject gameOverPanel;

    private void Awake()
    {
        activeInstance = this;
        IsAnyOpen = false;

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (answerButton != null)
            answerButton.onClick.AddListener(AnswerCall);

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(NextLine);

            nextButtonLabel =
                nextButton.GetComponentInChildren<TMP_Text>(true);
        }

        if (skipButton != null)
            skipButton.onClick.AddListener(SkipCall);

        if (incomingCallButton != null)
        {
            incomingCallButton.onClick.AddListener(OpenPendingCall);

            notificationRect =
                incomingCallButton.GetComponent<RectTransform>();

            notificationImage =
                incomingCallButton.GetComponent<Image>();

            if (notificationRect != null)
            {
                originalNotificationRotation =
                    notificationRect.localRotation;
            }

            if (notificationImage != null)
            {
                originalNotificationColor =
                    notificationImage.color;
            }

            incomingCallButton.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning(
                "[LA CENTRAL] Falta asignar Incoming Call Button.");
        }

        FindExistingGameplayPanels();
    }

    private void FindExistingGameplayPanels()
    {
        if (panelRoot == null ||
            panelRoot.transform.parent == null)
        {
            return;
        }

        Transform canvas = panelRoot.transform.parent;

        repairTerminal =
            canvas.GetComponent<RepairTerminalUI>();

        remotePanel = FindPanel(canvas, "RemotePanel");
        pausePanel = FindPanel(canvas, "PausePanel");
        completedPanel = FindPanel(canvas, "CompletedPanel");
        gameOverPanel = FindPanel(canvas, "GameOverPanel");
    }

    private static GameObject FindPanel(
        Transform parent,
        string name)
    {
        Transform child = parent.Find(name);

        return child != null ? child.gameObject : null;
    }

    private void Update()
    {
        WatchIncidentChanges();

        if (state == PhoneState.Closed)
        {
            UpdateIncomingNotification();
            return;
        }

        if (Keyboard.current == null)
            return;

        if (Time.frameCount == openedFrame)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            SkipCall();
            return;
        }

        if (state == PhoneState.Incoming)
        {
            if (keyboard.enterKey.wasPressedThisFrame ||
                keyboard.numpadEnterKey.wasPressedThisFrame ||
                keyboard.eKey.wasPressedThisFrame)
            {
                AnswerCall();
            }

            return;
        }

        if (state == PhoneState.Conversation)
        {
            if (keyboard.enterKey.wasPressedThisFrame ||
                keyboard.numpadEnterKey.wasPressedThisFrame ||
                keyboard.spaceKey.wasPressedThisFrame)
            {
                NextLine();
            }
        }
    }

    private void WatchIncidentChanges()
    {
        if (incidentManager == null)
            return;

        IncidentDefinition current =
            incidentManager.Current;

        if (current == null ||
            current == lastObservedIncident)
        {
            return;
        }

        lastObservedIncident = current;

        pendingDialogue = FindDialogue(current.incidentId);

        if (pendingDialogue == null)
        {
            Debug.LogWarning(
                "[LA CENTRAL] Sin diálogo para " +
                current.incidentId);
        }
    }

    private PhoneDialogueDefinition FindDialogue(string id)
    {
        if (dialogues == null)
            return null;

        foreach (PhoneDialogueDefinition definition in dialogues)
        {
            if (definition == null)
                continue;

            if (string.Equals(
                definition.incidentId,
                id,
                StringComparison.OrdinalIgnoreCase))
            {
                return definition;
            }
        }

        return null;
    }

    private void UpdateIncomingNotification()
    {
        if (incomingCallButton == null)
            return;

        bool show =
            pendingDialogue != null &&
            incidentManager != null &&
            incidentManager.CanInteract &&
            !IsOtherInterfaceOpen();

        GameObject notification =
            incomingCallButton.gameObject;

        if (notification.activeSelf != show)
            notification.SetActive(show);

        if (!show)
        {
            ResetNotificationAppearance();
            return;
        }

        float angle =
            Mathf.Sin(Time.unscaledTime * 26f) * 5f;

        if (notificationRect != null)
        {
            notificationRect.localRotation =
                originalNotificationRotation *
                Quaternion.Euler(0f, 0f, angle);
        }

        if (notificationImage != null)
        {
            float pulse =
                (Mathf.Sin(Time.unscaledTime * 5f) + 1f) * 0.5f;

            notificationImage.color = Color.Lerp(
                originalNotificationColor,
                new Color(0.25f, 0.65f, 0.95f),
                pulse * 0.55f);
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null &&
            keyboard.cKey.wasPressedThisFrame)
        {
            OpenPendingCall();
        }
    }

    private bool IsOtherInterfaceOpen()
    {
        return IsActive(remotePanel) ||
               IsActive(pausePanel) ||
               IsActive(completedPanel) ||
               IsActive(gameOverPanel) ||
               (repairTerminal != null &&
                repairTerminal.IsOpen);
    }

    private static bool IsActive(GameObject panel)
    {
        return panel != null && panel.activeInHierarchy;
    }

    private void ResetNotificationAppearance()
    {
        if (notificationRect != null)
        {
            notificationRect.localRotation =
                originalNotificationRotation;
        }

        if (notificationImage != null)
        {
            notificationImage.color =
                originalNotificationColor;
        }
    }

    public void OpenPendingCall()
    {
        if (state != PhoneState.Closed ||
            pendingDialogue == null ||
            panelRoot == null ||
            IsOtherInterfaceOpen())
        {
            return;
        }

        activeDialogue = pendingDialogue;
        pendingDialogue = null;

        lineIndex = 0;
        openedFrame = Time.frameCount;

        previousTimeScale = Time.timeScale;

        movementWasEnabled =
            playerMovement != null &&
            playerMovement.enabled;

        interactionWasEnabled =
            playerInteractor != null &&
            playerInteractor.enabled;

        remoteWasEnabled =
            remoteAccess != null &&
            remoteAccess.enabled;

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (playerInteractor != null)
            playerInteractor.enabled = false;

        if (remoteAccess != null)
            remoteAccess.enabled = false;

        if (incomingCallButton != null)
            incomingCallButton.gameObject.SetActive(false);

        ResetNotificationAppearance();

        state = PhoneState.Incoming;
        IsAnyOpen = true;

        Time.timeScale = 0f;

        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();

        RefreshPhone();
        SelectButton(answerButton);
    }

    public void AnswerCall()
    {
        if (state != PhoneState.Incoming)
            return;

        if (activeDialogue == null ||
            activeDialogue.lines == null ||
            activeDialogue.lines.Count == 0)
        {
            EndCall();
            return;
        }

        state = PhoneState.Conversation;
        lineIndex = 0;

        RefreshPhone();
        SelectButton(nextButton);
    }

    public void NextLine()
    {
        if (state != PhoneState.Conversation ||
            activeDialogue == null)
        {
            return;
        }

        lineIndex++;

        if (lineIndex >= activeDialogue.lines.Count)
        {
            EndCall();
            return;
        }

        RefreshPhone();
    }

    public void SkipCall()
    {
        if (state == PhoneState.Closed)
            return;

        EndCall();
    }

    private void RefreshPhone()
    {
        if (activeDialogue == null)
            return;

        bool incoming = state == PhoneState.Incoming;

        if (callerText != null)
            callerText.text = activeDialogue.callerName;

        if (stateText != null)
        {
            stateText.text = incoming
                ? "LLAMADA ENTRANTE"
                : "LLAMADA EN CURSO";
        }

        if (answerButton != null)
            answerButton.gameObject.SetActive(incoming);

        if (nextButton != null)
            nextButton.gameObject.SetActive(!incoming);

        if (speakerText != null)
        {
            speakerText.text = incoming
                ? "CENTRAL VECINAL"
                : GetCurrentLine().speaker;
        }

        if (dialogueText != null)
        {
            dialogueText.text = incoming
                ? "Has recibido una llamada de servicio.\n" +
                  "Contesta para escuchar el encargo."
                : GetCurrentLine().text;
        }

        if (progressText != null)
        {
            progressText.text = incoming
                ? "ENTER / E — CONTESTAR"
                : (lineIndex + 1) +
                  " / " +
                  activeDialogue.lines.Count;
        }

        if (nextButtonLabel != null && !incoming)
        {
            nextButtonLabel.text =
                lineIndex == activeDialogue.lines.Count - 1
                    ? "COLGAR"
                    : "SIGUIENTE";
        }
    }

    private PhoneDialogueLine GetCurrentLine()
    {
        return activeDialogue.lines[
            Mathf.Clamp(
                lineIndex,
                0,
                activeDialogue.lines.Count - 1)
        ];
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

    private void EndCall()
    {
        if (state == PhoneState.Closed)
            return;

        state = PhoneState.Closed;
        IsAnyOpen = false;

        if (panelRoot != null)
            panelRoot.SetActive(false);

        Time.timeScale = previousTimeScale;

        if (playerMovement != null)
            playerMovement.enabled = movementWasEnabled;

        if (playerInteractor != null)
            playerInteractor.enabled = interactionWasEnabled;

        if (remoteAccess != null)
            remoteAccess.enabled = remoteWasEnabled;

        activeDialogue = null;
    }

    private void OnDisable()
    {
        if (state != PhoneState.Closed)
            EndCall();

        IsAnyOpen = false;
    }

    private void OnDestroy()
    {
        if (state != PhoneState.Closed)
            EndCall();

        if (activeInstance == this)
            activeInstance = null;

        IsAnyOpen = false;
    }
}
