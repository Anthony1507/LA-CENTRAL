
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ConcertFinaleController : MonoBehaviour
{
    [Header("Texts")]
    public TMP_Text titleText;
    public TMP_Text phaseText;
    public TMP_Text timerText;
    public TMP_Text feedbackText;
    public TMP_Text instructionsText;

    [Header("Device Cards - 3 Elements Each")]
    public GameObject[] deviceCards = new GameObject[3];
    public TMP_Text[] deviceTexts = new TMP_Text[3];
    public Button[] minusButtons = new Button[3];
    public Button[] plusButtons = new Button[3];
    public Button[] powerButtons = new Button[3];
    public Button[] channelButtons = new Button[3];

    [Header("Actions")]
    public Button stabilizeButton;

    [Header("Emergency")]
    public GameObject emergencyPanel;
    public TMP_Text emergencyText;
    public Button[] emergencyButtons = new Button[4];

    [Header("Results")]
    public GameObject resultPanel;
    public TMP_Text resultText;
    public Button retryButton;
    public Button assistButton;
    public Button continueButton;

    private enum FinaleMode
    {
        Playing,
        Emergency,
        Failed,
        Won
    }

    private sealed class Device
    {
        public string name;
        public int volume;
        public int minimum;
        public int maximum;
        public bool powered;
        public bool requiredPower;
        public int channel;
        public int requiredChannel;
        public bool hasChannel;

        public bool IsStable()
        {
            bool volumeCorrect =
                volume >= minimum && volume <= maximum;

            bool powerCorrect = powered == requiredPower;

            bool channelCorrect =
                !hasChannel || channel == requiredChannel;

            return volumeCorrect && powerCorrect && channelCorrect;
        }
    }

    private readonly Device[] devices = new Device[3];

    private readonly float[] roundDurations =
    {
        38f, 30f, 24f
    };

    private readonly string[] roundNames =
    {
        "RONDA 1 - PRUEBA DE SONIDO",
        "RONDA 2 - INTERFERENCIA",
        "RONDA 3 - SOBRECARGA"
    };

    private FinaleMode mode;
    private int roundIndex;
    private int selectedDevice;
    private int emergencyProgress;
    private float remainingSeconds;
    private bool assisted;

    private void Start()
    {
        Time.timeScale = 1f;

        BindButtons();
        StartRound(0);
    }

    private void BindButtons()
    {
        for (int i = 0; i < 3; i++)
        {
            int index = i;

            if (minusButtons[i] != null)
                minusButtons[i].onClick.AddListener(
                    () => ChangeVolume(index, -10));

            if (plusButtons[i] != null)
                plusButtons[i].onClick.AddListener(
                    () => ChangeVolume(index, 10));

            if (powerButtons[i] != null)
                powerButtons[i].onClick.AddListener(
                    () => TogglePower(index));

            if (channelButtons[i] != null)
                channelButtons[i].onClick.AddListener(
                    () => ChangeChannel(index));
        }

        for (int i = 0; i < emergencyButtons.Length; i++)
        {
            int index = i;

            if (emergencyButtons[i] != null)
                emergencyButtons[i].onClick.AddListener(
                    () => EmergencyInput(index));
        }

        if (stabilizeButton != null)
            stabilizeButton.onClick.AddListener(ConfirmRound);

        if (retryButton != null)
            retryButton.onClick.AddListener(RetryRound);

        if (assistButton != null)
            assistButton.onClick.AddListener(UseAssistance);

        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueAfterVictory);
    }

    private void Update()
    {
        if (mode != FinaleMode.Playing &&
            mode != FinaleMode.Emergency)
            return;

        remainingSeconds -= Time.deltaTime;

        if (remainingSeconds <= 0f)
        {
            remainingSeconds = 0f;
            FailRound();
            RefreshUI();
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (mode == FinaleMode.Playing)
            {
                if (keyboard.digit1Key.wasPressedThisFrame)
                    selectedDevice = 0;

                if (keyboard.digit2Key.wasPressedThisFrame)
                    selectedDevice = 1;

                if (keyboard.digit3Key.wasPressedThisFrame)
                    selectedDevice = 2;

                if (keyboard.leftArrowKey.wasPressedThisFrame)
                    ChangeVolume(selectedDevice, -10);

                if (keyboard.rightArrowKey.wasPressedThisFrame)
                    ChangeVolume(selectedDevice, 10);

                if (keyboard.spaceKey.wasPressedThisFrame)
                    TogglePower(selectedDevice);

                if (keyboard.eKey.wasPressedThisFrame)
                    ChangeChannel(selectedDevice);

                if (keyboard.enterKey.wasPressedThisFrame ||
                    keyboard.numpadEnterKey.wasPressedThisFrame)
                    ConfirmRound();
            }
            else
            {
                if (keyboard.digit1Key.wasPressedThisFrame)
                    EmergencyInput(0);

                if (keyboard.digit2Key.wasPressedThisFrame)
                    EmergencyInput(1);

                if (keyboard.digit3Key.wasPressedThisFrame)
                    EmergencyInput(2);

                if (keyboard.rKey.wasPressedThisFrame)
                    EmergencyInput(3);
            }
        }

        RefreshUI();
    }

    private Device MakeDevice(
        string name,
        int volume,
        int min,
        int max,
        bool powered = true,
        bool requiredPower = true,
        int channel = 2,
        int requiredChannel = 2,
        bool hasChannel = false)
    {
        return new Device
        {
            name = name,
            volume = volume,
            minimum = min,
            maximum = max,
            powered = powered,
            requiredPower = requiredPower,
            channel = channel,
            requiredChannel = requiredChannel,
            hasChannel = hasChannel
        };
    }

    private void StartRound(int index)
    {
        roundIndex = Mathf.Clamp(index, 0, 2);
        selectedDevice = 0;
        emergencyProgress = 0;
        mode = FinaleMode.Playing;
        remainingSeconds = roundDurations[roundIndex];

        if (roundIndex == 0)
        {
            devices[0] = MakeDevice(
                "ALTAVOCES DE PLAZA", 90, 40, 50);

            devices[1] = MakeDevice(
                "MICROFONOS", 20, 50, 60);

            devices[2] = MakeDevice(
                "ENLACE DE RADIO", 30, 30, 30,
                hasChannel: true);
        }
        else if (roundIndex == 1)
        {
            devices[0] = MakeDevice(
                "ALTAVOCES DE PLAZA", 70, 40, 50);

            devices[1] = MakeDevice(
                "MICROFONOS", 50, 50, 60,
                powered: false);

            devices[2] = MakeDevice(
                "ENLACE DE RADIO", 30, 30, 30,
                channel: 0,
                hasChannel: true);
        }
        else
        {
            devices[0] = MakeDevice(
                "ALTAVOCES DE PLAZA", 100, 40, 50);

            devices[1] = MakeDevice(
                "MICROFONOS", 10, 50, 60);

            devices[2] = MakeDevice(
                "ENLACE DE RADIO", 30, 30, 30,
                powered: false,
                channel: 0,
                hasChannel: true);
        }

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (emergencyPanel != null)
            emergencyPanel.SetActive(false);

        if (stabilizeButton != null)
            stabilizeButton.gameObject.SetActive(true);

        SetCardsVisible(true);

        feedbackText.text = roundIndex switch
        {
            0 => "La banda esta lista. Equilibra el sonido.",
            1 => "Interferencia detectada. Recupera la senal.",
            _ => "SOBRECARGA CRITICA. ¡NO DEJES CAER EL CONCIERTO!"
        };

        RefreshUI();
    }

    private void SetCardsVisible(bool visible)
    {
        foreach (GameObject card in deviceCards)
        {
            if (card != null)
                card.SetActive(visible);
        }
    }

    private void ChangeVolume(int index, int change)
    {
        if (mode != FinaleMode.Playing)
            return;

        Device device = devices[index];

        device.volume = Mathf.Clamp(
            device.volume + change, 0, 100);

        RefreshUI();
    }

    private void TogglePower(int index)
    {
        if (mode != FinaleMode.Playing)
            return;

        devices[index].powered = !devices[index].powered;
        RefreshUI();
    }

    private void ChangeChannel(int index)
    {
        if (mode != FinaleMode.Playing)
            return;

        Device device = devices[index];

        if (!device.hasChannel)
            return;

        device.channel = (device.channel + 1) % 3;
        RefreshUI();
    }

    private void ConfirmRound()
    {
        if (mode != FinaleMode.Playing)
            return;

        foreach (Device device in devices)
        {
            if (!device.IsStable())
            {
                feedbackText.text =
                    "Aun hay equipos inestables. ¡Revisa los objetivos!";

                RefreshUI();
                return;
            }
        }

        if (roundIndex == 2)
        {
            StartEmergency();
            return;
        }

        StartRound(roundIndex + 1);
    }

    private void StartEmergency()
    {
        mode = FinaleMode.Emergency;
        remainingSeconds = 12f;
        emergencyProgress = 0;

        SetCardsVisible(false);

        if (stabilizeButton != null)
            stabilizeButton.gameObject.SetActive(false);

        if (emergencyPanel != null)
            emergencyPanel.SetActive(true);

        feedbackText.text =
            "¡APAGON! EL RELE CENTRAL HA CAIDO.";

        RefreshUI();
    }

    private void EmergencyInput(int index)
    {
        if (mode != FinaleMode.Emergency)
            return;

        if (index == emergencyProgress)
        {
            emergencyProgress++;

            if (emergencyProgress >= 4)
            {
                WinFinale();
                return;
            }

            feedbackText.text = "Conexion parcial restaurada.";
        }
        else
        {
            emergencyProgress = 0;
            feedbackText.text =
                "¡SECUENCIA INCORRECTA! Vuelve a comenzar.";
        }

        RefreshUI();
    }

    private void FailRound()
    {
        mode = FinaleMode.Failed;

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (retryButton != null)
            retryButton.gameObject.SetActive(true);

        if (assistButton != null)
            assistButton.gameObject.SetActive(true);

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        if (resultText != null)
        {
            resultText.text =
                "EL CONCIERTO SE HA DETENIDO\n\n" +
                "El tiempo se agoto.\n" +
                "Puedes reintentar esta ronda.";
        }
    }

    private void RetryRound()
    {
        StartRound(roundIndex);
    }

    private void UseAssistance()
    {
        if (mode != FinaleMode.Failed)
            return;

        assisted = true;

        if (roundIndex < 2)
        {
            StartRound(roundIndex + 1);
        }
        else
        {
            WinFinale();
        }
    }

    private void WinFinale()
    {
        mode = FinaleMode.Won;

        if (emergencyPanel != null)
            emergencyPanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (retryButton != null)
            retryButton.gameObject.SetActive(false);

        if (assistButton != null)
            assistButton.gameObject.SetActive(false);

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);

            continueButton.interactable =
                Application.CanStreamedLevelBeLoaded("04_Results");
        }

        if (resultText != null)
        {
            resultText.text =
                "¡CONCIERTO SALVADO!\n\n" +
                "La ciudad entera vuelve a escuchar.\n" +
                "Has conseguido dar la nota.\n\n" +
                (assisted
                    ? "PARTIDA ASISTIDA"
                    : "VICTORIA COMPLETA") +
                "\n\nContinuara en la pantalla de resultados.";
        }

        PlayerPrefs.SetInt("LC_ConcertWon", 1);
        PlayerPrefs.SetInt("LC_ConcertAssisted", assisted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ContinueAfterVictory()
    {
        if (mode != FinaleMode.Won)
            return;

        if (Application.CanStreamedLevelBeLoaded("04_Results"))
        {
            SceneManager.LoadScene("04_Results");
        }
    }

    private void RefreshUI()
    {
        if (titleText != null)
            titleText.text = "LA CENTRAL - DANDO LA NOTA";

        if (timerText != null)
        {
            timerText.text =
                "TIEMPO: " +
                Mathf.CeilToInt(remainingSeconds) + "s";

            timerText.color = remainingSeconds <= 7f
                ? Color.red
                : Color.white;
        }

        if (phaseText != null)
        {
            phaseText.text = mode == FinaleMode.Emergency
                ? "FASE FINAL - LA ULTIMA NOTA"
                : roundNames[roundIndex];
        }

        if (mode == FinaleMode.Emergency)
        {
            if (emergencyText != null)
            {
                emergencyText.text =
                    "REINICIA EL RELE CENTRAL\n\n" +
                    "SECUENCIA: 1 > 2 > 3 > R\n\n" +
                    "PASOS COMPLETADOS: " +
                    emergencyProgress + " / 4";
            }

            if (instructionsText != null)
                instructionsText.text =
                    "Pulsa la secuencia indicada antes de que el reloj llegue a cero.";

            return;
        }

        if (mode != FinaleMode.Playing)
            return;

        if (instructionsText != null)
        {
            instructionsText.text =
                "1/2/3 Seleccionar | Flechas Izq/Der Volumen | " +
                "Espacio Encender | E Canal | Enter Confirmar";
        }

        for (int i = 0; i < devices.Length; i++)
        {
            Device device = devices[i];

            if (deviceTexts[i] != null)
            {
                string header = i == selectedDevice ? "> " : "";

                string channelLine = device.hasChannel
                    ? "\nEMISORA: " +
                      (device.channel == 0 ? "89.5" :
                       device.channel == 1 ? "94.2" : "101.7") +
                      " -> 101.7"
                    : "";

                deviceTexts[i].text =
                    header + device.name + "\n\n" +
                    "VOLUMEN: " + device.volume +
                    " -> " + device.minimum +
                    "-" + device.maximum + "\n" +
                    "ENCENDIDO: " +
                    (device.powered ? "SI" : "NO") +
                    " -> " +
                    (device.requiredPower ? "SI" : "NO") +
                    channelLine + "\n\n" +
                    (device.IsStable()
                        ? "[ ESTABLE ]"
                        : "[ INESTABLE ]");

                deviceTexts[i].color =
                    device.IsStable()
                        ? new Color(0.45f, 1f, 0.65f)
                        : Color.white;
            }

            if (channelButtons[i] != null)
                channelButtons[i].interactable =
                    device.hasChannel;
        }
    }
}
