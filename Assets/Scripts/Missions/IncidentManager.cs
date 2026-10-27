
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        new Dictionary<string, DeviceController>(
            StringComparer.OrdinalIgnoreCase
        );

    private readonly HashSet<string> physicalRepairs =
        new HashSet<string>(
            StringComparer.OrdinalIgnoreCase
        );

    private IncidentDefinition current;

    private int incidentIndex;
    private int reputation;
    private int wrongActions;

    private float incidentSeconds;
    private float completedIncidentSeconds;

    private bool completed;
    private bool gameOver;
    private bool campaignFinished;
    private bool suppressDeviceEvents;

    public bool CanInteract =>
        current != null &&
        !completed &&
        !gameOver;

    public int Reputation => reputation;
    public float IncidentSeconds => incidentSeconds;
    public int IncidentNumber => incidentIndex + 1;
    public int WrongActions => wrongActions;
    public float CompletedIncidentSeconds => completedIncidentSeconds;

    public int IncidentCount =>
        incidents != null ? incidents.Length : 0;

    public IncidentDefinition Current => current;

    private static string Key(
        string buildingId,
        string deviceId)
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
            if (string.IsNullOrWhiteSpace(device.BuildingId) ||
                string.IsNullOrWhiteSpace(device.DeviceId))
            {
                Debug.LogError(
                    "[LA CENTRAL] Dispositivo sin identificador.",
                    device
                );

                continue;
            }

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

    private void Start()
    {
        if (SaveService.TryLoad(out CampaignSaveData saved))
        {
            if (saved.stage == SaveService.StageGameplay &&
                saved.nextIncidentIndex < IncidentCount)
            {
                RestoreCampaign(saved);
                return;
            }

            if (saved.stage == SaveService.StageConcert)
            {
                if (Application.CanStreamedLevelBeLoaded(
                    "03_ConcertFinale"))
                {
                    SceneManager.LoadScene("03_ConcertFinale");
                    return;
                }
            }
        }

        RestartCampaign();
    }

    private void Update()
    {
        if (CanInteract)
            incidentSeconds += Time.deltaTime;

        if (hud != null && current != null)
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

    private void OnDestroy()
    {
        foreach (DeviceController device in devices.Values)
        {
            if (device != null)
                device.Changed -= HandleDeviceChanged;
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            SaveCurrentCheckpoint();
    }

    private void OnApplicationQuit()
    {
        SaveCurrentCheckpoint();
    }

    public void RestartCampaign()
    {
        SaveService.DeleteSave();

        reputation = Mathf.Max(1, startingReputation);
        wrongActions = 0;

        completedIncidentSeconds = 0f;

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

        PlayerPrefs.DeleteKey("LC_ConcertWon");
        PlayerPrefs.DeleteKey("LC_ConcertAssisted");
        PlayerPrefs.DeleteKey("LC_FinalCampaignReputation");
        PlayerPrefs.DeleteKey("LC_FinalCampaignCount");
        PlayerPrefs.DeleteKey("LC_FinalCampaignSeconds");
        PlayerPrefs.DeleteKey("LC_FinalCampaignMistakes");
        PlayerPrefs.Save();

        StartIncident(0);
    }

    private void RestoreCampaign(CampaignSaveData saved)
    {
        reputation = saved.reputation;
        wrongActions = saved.wrongActions;

        completedIncidentSeconds =
            saved.completedIncidentSeconds;

        completed = false;
        gameOver = false;
        campaignFinished = false;

        physicalRepairs.Clear();

        if (buildingAccess != null)
        {
            buildingAccess.RestoreProgress(
                saved.knownBuildingIds,
                saved.remoteUnlocked
            );
        }

        if (terminal != null)
            terminal.Close();

        if (hud != null)
            hud.HideOverlays();

        Debug.Log(
            "[LA CENTRAL] Continuando desde incidencia " +
            (saved.nextIncidentIndex + 1)
        );

        StartIncident(saved.nextIncidentIndex);
    }

    private void StartIncident(int index)
    {
        if (incidents == null ||
            index < 0 ||
            index >= incidents.Length ||
            incidents[index] == null)
        {
            Debug.LogError(
                "[LA CENTRAL] Incidencia no configurada: " +
                index
            );

            current = null;
            return;
        }

        incidentIndex = index;
        current = incidents[index];

        incidentSeconds = 0f;

        completed = false;
        gameOver = false;
        campaignFinished = false;

        physicalRepairs.Clear();

        if (terminal != null)
            terminal.Close();

        if (hud != null)
            hud.HideOverlays();

        suppressDeviceEvents = true;

        try
        {
            if (current.initialDeviceStates != null)
            {
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
                            "[LA CENTRAL] Dispositivo inicial ausente: " +
                            Key(state.buildingId, state.deviceId)
                        );
                    }
                }
            }
        }
        finally
        {
            suppressDeviceEvents = false;
        }

        if (current.objectives != null)
        {
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
        }

        SaveCurrentCheckpoint();

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

        int nextIndex = incidentIndex + 1;

        if (nextIndex >= IncidentCount)
            return;

        StartIncident(nextIndex);
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

    private void HandleDeviceChanged(DeviceController changedDevice)
    {
        if (suppressDeviceEvents || !CanInteract)
            return;

        bool belongsToIncident = false;

        foreach (DeviceObjective objective in current.objectives)
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

        if (terminal != null && terminal.IsOpen && !terminal.IsRemoteSession &&
            string.Equals( terminal.OpenBuildingId, changedDevice.BuildingId, StringComparison.OrdinalIgnoreCase))
        {
            physicalRepairs.Add(changedDevice.BuildingId);
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
        wrongActions++;

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

            return;
        }

        SaveCurrentCheckpoint();
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

        if (buildingAccess != null)
        {
            foreach (string buildingId in physicalRepairs)
            {
                buildingAccess.RegisterPhysicalRepair(
                    buildingId
                );
            }

            if (incidentIndex == 1)
                buildingAccess.UnlockRemote();
        }

        int bonus = CalculateTimeBonus();

        int earnedPoints = 5 + bonus;

        reputation += earnedPoints;
        completedIncidentSeconds += incidentSeconds;

        campaignFinished =
            incidentIndex + 1 >= IncidentCount;

        if (terminal != null)
            terminal.Close();

        SaveCurrentCheckpoint();

        if (campaignFinished)
        {
            PlayerPrefs.SetInt(
                "LC_FinalCampaignReputation",
                reputation
            );

            PlayerPrefs.SetInt(
                "LC_FinalCampaignCount",
                IncidentCount
            );

            PlayerPrefs.SetFloat(
                "LC_FinalCampaignSeconds",
                completedIncidentSeconds
            );

            PlayerPrefs.SetInt(
                "LC_FinalCampaignMistakes",
                wrongActions
            );

            PlayerPrefs.Save();
        }

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
            "[LA CENTRAL] Completada: " +
            current.incidentId +
            " | +" + earnedPoints +
            " | Reputación: " + reputation
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

    private void SaveCurrentCheckpoint()
    {
        if (current == null || reputation <= 0)
            return;

        int nextIndex = completed
            ? incidentIndex + 1
            : incidentIndex;

        string stage =
            campaignFinished && completed
                ? SaveService.StageConcert
                : SaveService.StageGameplay;

        CampaignSaveData data = new CampaignSaveData
        {
            stage = stage,
            nextIncidentIndex = nextIndex,
            reputation = reputation,
            wrongActions = wrongActions,

            completedIncidentSeconds =
                completedIncidentSeconds,

            remoteUnlocked =
                buildingAccess != null &&
                buildingAccess.RemoteUnlocked,

            knownBuildingIds =
                buildingAccess != null
                    ? buildingAccess.GetKnownBuildings()
                    : new string[0]
        };

        SaveService.Save(data);
    }
}
