using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class IncidentManager : MonoBehaviour
{
    [Header("Campaign")]
    [SerializeField] private IncidentDefinition[] incidents;
    [SerializeField] private int startingReputation = 50;

    [Header("References")]
    [SerializeField] private GameplayHUD hud;
    [SerializeField] private RepairTerminalUI terminal;
    [SerializeField] private BuildingAccessManager buildingAccess;

    private readonly Dictionary<string, DeviceController> devices =
        new Dictionary<string, DeviceController>();

    private readonly HashSet<string> physicalRepairs =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private IncidentDefinition current;
    private int incidentIndex;
    private int reputation;
    private float incidentSeconds;

    private bool completed;
    private bool gameOver;
    private bool campaignFinished;

    public bool CanInteract =>
        current != null && !completed && !gameOver;

    public int Reputation => reputation;
    public float IncidentSeconds => incidentSeconds;
    public int IncidentNumber => incidentIndex + 1;

    public int IncidentCount =>
        incidents != null ? incidents.Length : 0;

    public IncidentDefinition Current => current;

    private static string Key(string buildingId, string deviceId)
    {
        return (
            buildingId.Trim() + "/" + deviceId.Trim()
        ).ToUpperInvariant();
    }

    private void Awake()
    {
        DeviceController[] foundDevices =
            FindObjectsByType<DeviceController>(
                FindObjectsSortMode.None
            );

        foreach (DeviceController device in foundDevices)
        {
            string key = Key(
                device.BuildingId,
                device.DeviceId
            );

            if (devices.ContainsKey(key))
            {
                Debug.LogError(
                    "[LA CENTRAL] Dispositivo duplicado: " + key,
                    device
                );

                continue;
            }

            devices.Add(key, device);

            device.Changed += HandleDeviceChanged;
        }
    }

    private void OnDestroy()
    {
        foreach (DeviceController device in devices.Values)
        {
            if (device != null)
                device.Changed -= HandleDeviceChanged;
        }
    }

    private void Start()
    {
        RestartCampaign();
    }

    private void Update()
    {
        if (CanInteract)
            incidentSeconds += Time.deltaTime;

        if (hud != null)
        {
            hud.RefreshStatus(
                current,
                incidentIndex + 1,
                IncidentCount,
                reputation,
                incidentSeconds
            );
        }
    }

    public void RestartCampaign()
    {
        reputation = Mathf.Max(1, startingReputation);

        completed = false;
        gameOver = false;
        campaignFinished = false;

        physicalRepairs.Clear();

        if (buildingAccess != null)
            buildingAccess.ResetProgress();

        if (terminal != null)
            terminal.Close();

        if (hud != null)
            hud.HideOverlays();

        StartIncident(0);
    }

    private void StartIncident(int index)
    {
        if (incidents == null ||
            index < 0 ||
            index >= incidents.Length ||
            incidents[index] == null)
        {
            Debug.LogError(
                "[LA CENTRAL] Incidencia no configurada: " + index
            );

            current = null;
            return;
        }

        incidentIndex = index;
        current = incidents[index];

        incidentSeconds = 0f;
        completed = false;
        gameOver = false;

        physicalRepairs.Clear();

        if (hud != null)
            hud.HideOverlays();

        foreach (InitialDeviceState state
                 in current.initialDeviceStates)
        {
            if (TryFind(
                    state.buildingId,
                    state.deviceId,
                    out DeviceController target))
            {
                target.ResetState(
                    state.poweredOn,
                    state.volume,
                    state.channelIndex
                );
            }
            else
            {
                Debug.LogError(
                    "[LA CENTRAL] Dispositivo inicial no encontrado: " +
                    Key(state.buildingId, state.deviceId)
                );
            }
        }

        foreach (DeviceObjective objective
                 in current.objectives)
        {
            if (!TryFind(
                    objective.buildingId,
                    objective.deviceId,
                    out _))
            {
                Debug.LogError(
                    "[LA CENTRAL] Objetivo sin dispositivo: " +
                    Key(objective.buildingId, objective.deviceId)
                );
            }
        }

        if (hud != null)
        {
            hud.ShowFeedback(
                "Nueva incidencia: " + current.title
            );
        }

        Debug.Log(
            "[LA CENTRAL] Iniciando " + current.incidentId
        );
    }

    public void NextIncident()
    {
        if (!completed || campaignFinished)
            return;

        StartIncident(incidentIndex + 1);
    }

    private bool TryFind(
        string buildingId,
        string deviceId,
        out DeviceController result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(buildingId) ||
            string.IsNullOrWhiteSpace(deviceId))
        {
            return false;
        }

        return devices.TryGetValue(
            Key(buildingId, deviceId),
            out result
        );
    }

    private void HandleDeviceChanged(
        DeviceController changedDevice)
    {
        if (!CanInteract)
            return;

        bool belongsToIncident = false;

        foreach (DeviceObjective objective
                 in current.objectives)
        {
            if (objective.Targets(changedDevice))
            {
                belongsToIncident = true;
                break;
            }
        }

        if (!belongsToIncident)
        {
            ApplyIncorrectAction();
            return;
        }

        // Registrar una reparación realizada físicamente.
        if (terminal != null &&
            terminal.IsOpen &&
            !terminal.IsRemoteSession &&
            string.Equals(
                terminal.OpenBuildingId,
                changedDevice.BuildingId,
                StringComparison.OrdinalIgnoreCase))
        {
            physicalRepairs.Add(
                changedDevice.BuildingId
            );
        }

        if (AllObjectivesSatisfied())
        {
            FinishIncident();
        }
        else if (hud != null)
        {
            hud.ShowFeedback(
                "Cambio aplicado. Sigue revisando el objetivo."
            );
        }
    }

    private void ApplyIncorrectAction()
    {
        reputation = Mathf.Max(0, reputation - 10);

        if (hud != null)
        {
            hud.ShowFeedback(
                "¡Equipo equivocado! -10 puntos."
            );
        }

        if (reputation <= 0)
        {
            gameOver = true;

            if (terminal != null)
                terminal.Close();

            if (hud != null)
                hud.ShowGameOver();
        }
    }

    private bool AllObjectivesSatisfied()
    {
        if (current == null ||
            current.objectives == null ||
            current.objectives.Count == 0)
        {
            return false;
        }

        foreach (DeviceObjective objective
                 in current.objectives)
        {
            if (!TryFind(
                    objective.buildingId,
                    objective.deviceId,
                    out DeviceController device))
            {
                return false;
            }

            if (!objective.IsSatisfied(device))
                return false;
        }

        return true;
    }

    private void FinishIncident()
    {
        completed = true;

        // Solo se consideran conocidos los edificios
        // reparados presencialmente durante esta misión.
        if (buildingAccess != null)
        {
            foreach (string buildingId in physicalRepairs)
            {
                buildingAccess.RegisterPhysicalRepair(
                    buildingId
                );
            }

            // El acceso remoto se desbloquea al
            // terminar el Caso 02.
            if (incidentIndex == 1)
            {
                buildingAccess.UnlockRemote();
            }
        }

        int bonus = CalculateTimeBonus();

        int earnedPoints = 5 + bonus;
        reputation += earnedPoints;

        campaignFinished =
            incidentIndex + 1 >= IncidentCount;

        if (terminal != null)
            terminal.Close();

        if (hud != null)
        {
            hud.ShowCompleted(
                current.title,
                earnedPoints,
                incidentSeconds,
                !campaignFinished
            );

            if (incidentIndex == 1)
            {
                hud.ShowFeedback(
                    "¡Acceso remoto desbloqueado! Pulsa TAB."
                );
            }
        }

        Debug.Log(
            "[LA CENTRAL] Incidencia completada: " +
            current.incidentId +
            " | Recompensa: +" + earnedPoints
        );
    }

    private int CalculateTimeBonus()
    {
        if (current.tutorialWithoutTimeBonus)
            return 0;

        float target = Mathf.Max(
            1f,
            current.targetTimeSeconds
        );

        if (incidentSeconds <= target)
            return 5;

        if (incidentSeconds <= target * 1.5f)
            return 3;

        if (incidentSeconds <= target * 2f)
            return 1;

        return 0;
    }
}