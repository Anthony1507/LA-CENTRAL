using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class InitialDeviceState
{
    public string buildingId = "B01";
    public string deviceId = "TV_MAIN";
    public bool poweredOn = true;
    [Range(0, 100)] public int volume = 90;
    public int channelIndex = 2;
}

[Serializable]
public sealed class DeviceObjective
{
    public string buildingId = "B01";
    public string deviceId = "TV_MAIN";

    public bool checkVolume = true;
    [Range(0, 100)] public int minVolume = 0;
    [Range(0, 100)] public int maxVolume = 30;

    public bool checkPower = false;
    public bool requiredPoweredOn = true;

    public bool checkChannel = false;
    public int requiredChannelIndex = 0;

    public bool Targets(DeviceController device)
    {
        return device != null &&
               string.Equals(buildingId, device.BuildingId, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(deviceId, device.DeviceId, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsSatisfied(DeviceController device)
    {
        if (!Targets(device)) return false;
        if (checkVolume && (device.Volume < minVolume || device.Volume > maxVolume)) return false;
        if (checkPower && device.PoweredOn != requiredPoweredOn) return false;
        if (checkChannel && device.ChannelIndex != requiredChannelIndex) return false;
        return true;
    }
}

[CreateAssetMenu(fileName = "Incident_01", menuName = "LA CENTRAL/Incident Definition")]
public sealed class IncidentDefinition : ScriptableObject
{
    public string incidentId = "CASE01";
    public string title = "La tele de enfrente";
    [TextArea(2, 5)] public string description = "La tele junto a la parada no deja dormir. Baja el volumen a 30 o menos.";
    public bool tutorialWithoutTimeBonus = true;
    [Min(1)] public float targetTimeSeconds = 100f;
    public List<InitialDeviceState> initialDeviceStates = new List<InitialDeviceState>();
    public List<DeviceObjective> objectives = new List<DeviceObjective>();
}
