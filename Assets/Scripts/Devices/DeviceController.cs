using System;
using UnityEngine;

public enum DeviceKind
{
    Television,
    Radio,
    Speaker
}

public sealed class DeviceController : MonoBehaviour
{
    private static readonly string[] TelevisionChannels = { "Local", "Cultura", "Cine" };
    private static readonly string[] RadioStations = { "89.5", "94.2", "101.7" };

    [Header("Identity")]
    [SerializeField] private string buildingId = "B01";
    [SerializeField] private string deviceId = "TV_MAIN";
    [SerializeField] private string deviceName = "Televisor principal";
    [SerializeField] private DeviceKind kind = DeviceKind.Television;

    [Header("Initial values (also editable by incident data)")]
    [SerializeField] private bool poweredOn = true;
    [Range(0, 100)] [SerializeField] private int volume = 90;
    [SerializeField] private int channelIndex = 2;

    public event Action<DeviceController> Changed;

    public string BuildingId => buildingId;
    public string DeviceId => deviceId;
    public string DisplayName => deviceName;
    public DeviceKind Kind => kind;
    public bool PoweredOn => poweredOn;
    public int Volume => volume;
    public int ChannelIndex => channelIndex;
    public bool HasChannels => kind != DeviceKind.Speaker;
    private string[] Channels => kind == DeviceKind.Radio ? RadioStations : TelevisionChannels;
    public string ChannelName => HasChannels ? Channels[Mathf.Clamp(channelIndex, 0, Channels.Length - 1)] : "No aplica";

    public void ResetState(bool isOn, int initialVolume, int initialChannelIndex)
    {
        poweredOn = isOn;
        volume = NormalizeVolume(initialVolume);
        channelIndex = HasChannels ? Mathf.Clamp(initialChannelIndex, 0, Channels.Length - 1) : 0;
    }

    public void SetPower(bool isOn)
    {
        if (poweredOn == isOn) return;
        poweredOn = isOn;
        Changed?.Invoke(this);
    }

    public void SetVolume(int value)
    {
        int newValue = NormalizeVolume(value);
        if (volume == newValue) return;
        volume = newValue;
        Changed?.Invoke(this);
    }

    public void ShiftChannel(int offset)
    {
        if (!HasChannels || offset == 0) return;
        int next = (channelIndex + offset % Channels.Length + Channels.Length) % Channels.Length;
        if (channelIndex == next) return;
        channelIndex = next;
        Changed?.Invoke(this);
    }

    private static int NormalizeVolume(int value)
    {
        return Mathf.Clamp(Mathf.RoundToInt(value / 5f) * 5, 0, 100);
    }
}
