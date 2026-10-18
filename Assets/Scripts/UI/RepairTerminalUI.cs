using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class RepairTerminalUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text buildingText;
    [SerializeField] private TMP_Text deviceText;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private TMP_Text volumeText;
    [SerializeField] private TMP_Text channelText;

    [Header("Controls")]
    [SerializeField] private Button togglePowerButton;
    [SerializeField] private Button volumeMinusButton;
    [SerializeField] private Button volumePlusButton;
    [SerializeField] private Button channelPrevButton;
    [SerializeField] private Button channelNextButton;
    [SerializeField] private Button nextDeviceButton;
    [SerializeField] private Button closeButton;

    private BuildingEntrance entrance;
    private DeviceController selectedDevice;
    private int selectedDeviceIndex;

    public bool IsOpen { get; private set; }

    public bool IsRemoteSession { get; private set; }

    public string OpenBuildingId =>
        entrance != null ? entrance.BuildingId : "";

    public event Action Closed;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (togglePowerButton != null)
            togglePowerButton.onClick.AddListener(
                () => Act(d => d.SetPower(!d.PoweredOn))
            );

        if (volumeMinusButton != null)
            volumeMinusButton.onClick.AddListener(
                () => Act(d => d.SetVolume(d.Volume - 5))
            );

        if (volumePlusButton != null)
            volumePlusButton.onClick.AddListener(
                () => Act(d => d.SetVolume(d.Volume + 5))
            );

        if (channelPrevButton != null)
            channelPrevButton.onClick.AddListener(
                () => Act(d => d.ShiftChannel(-1))
            );

        if (channelNextButton != null)
            channelNextButton.onClick.AddListener(
                () => Act(d => d.ShiftChannel(1))
            );

        if (nextDeviceButton != null)
            nextDeviceButton.onClick.AddListener(NextDevice);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void Update()
    {
        if (!IsOpen || Keyboard.current == null)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }

        if (keyboard.minusKey.wasPressedThisFrame ||
            keyboard.numpadMinusKey.wasPressedThisFrame)
        {
            Act(d => d.SetVolume(d.Volume - 5));
        }

        if (keyboard.equalsKey.wasPressedThisFrame ||
            keyboard.numpadPlusKey.wasPressedThisFrame)
        {
            Act(d => d.SetVolume(d.Volume + 5));
        }
    }

    public bool Open(
        BuildingEntrance target,
        bool remotely = false
    )
    {
        if (IsOpen || target == null || panelRoot == null)
            return false;
        if (PhoneNarrativeController.HasPendingCall)
        {
            GameplayHUD hud =
                FindFirstObjectByType<GameplayHUD>();

            if (hud != null)
            {
                hud.ShowFeedback(
                    "¡Tienes una llamada pendiente! " +
                    "Pulsa C o haz clic en el celular antes de reparar."
                );
            }

            return false;
        }

        DeviceController[] devices = target.Devices;

        if (devices == null || devices.Length == 0)
        {
            Debug.LogWarning(
                "[LA CENTRAL] Sin dispositivos: " +
                target.DisplayName
            );

            return false;
        }

        entrance = target;
        selectedDeviceIndex = 0;
        selectedDevice = devices[0];

        IsRemoteSession = remotely;
        IsOpen = true;

        panelRoot.SetActive(true);

        Refresh();

        if (EventSystem.current != null &&
            volumeMinusButton != null)
        {
            EventSystem.current.SetSelectedGameObject(
                volumeMinusButton.gameObject
            );
        }

        return true;
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        IsOpen = false;

        if (panelRoot != null)
            panelRoot.SetActive(false);

        entrance = null;
        selectedDevice = null;
        IsRemoteSession = false;

        Closed?.Invoke();
    }

    private void Act(Action<DeviceController> action)
    {
        if (!IsOpen || selectedDevice == null)
            return;

        action(selectedDevice);

        if (IsOpen)
            Refresh();
    }

    private void NextDevice()
    {
        if (!IsOpen || entrance == null)
            return;

        DeviceController[] devices = entrance.Devices;

        if (devices == null || devices.Length == 0)
            return;

        selectedDeviceIndex =
            (selectedDeviceIndex + 1) % devices.Length;

        selectedDevice = devices[selectedDeviceIndex];

        Refresh();
    }

    private void Refresh()
    {
        if (entrance == null || selectedDevice == null)
            return;

        if (buildingText != null)
            buildingText.text =
                (IsRemoteSession ? "[REMOTO] " : "[LOCAL] ") +
                entrance.DisplayName;

        if (deviceText != null)
            deviceText.text = selectedDevice.DisplayName;

        if (powerText != null)
            powerText.text =
                "Estado: " +
                (selectedDevice.PoweredOn
                    ? "Encendido"
                    : "Apagado");

        if (volumeText != null)
            volumeText.text =
                "Volumen: " + selectedDevice.Volume;

        if (channelText != null)
            channelText.text =
                "Canal / emisora: " +
                selectedDevice.ChannelName;

        if (channelPrevButton != null)
            channelPrevButton.interactable =
                selectedDevice.HasChannels;

        if (channelNextButton != null)
            channelNextButton.interactable =
                selectedDevice.HasChannels;

        if (nextDeviceButton != null)
        {
            nextDeviceButton.gameObject.SetActive(
                entrance.Devices.Length > 1
            );
        }
    }
}