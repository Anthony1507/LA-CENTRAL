
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameplayNavigationUI : MonoBehaviour
{
    [Header("Gameplay")]
    public IncidentManager incidentManager;
    public Transform player;
    public SpriteRenderer groundRenderer;

    [Header("Objective HUD")]
    public TMP_Text missionTitleText;
    public TMP_Text progressText;
    public TMP_Text objectivesText;

    [Header("Minimap")]
    public RectTransform mapArea;
    public RectTransform playerMarker;

    public Transform[] buildingTransforms = new Transform[12];
    public TMP_Text[] buildingLabels = new TMP_Text[12];

    private readonly Dictionary<string, DeviceController> devices =
        new Dictionary<string, DeviceController>(
            StringComparer.OrdinalIgnoreCase
        );

    private readonly string[] buildingIds =
    {
        "B01", "B02", "B03", "B04",
        "B05", "B06", "B07", "B08",
        "B09", "B10", "B11", "B12"
    };

    private void Awake()
    {
        DeviceController[] found =
            FindObjectsByType<DeviceController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (DeviceController device in found)
        {
            string key = MakeKey(
                device.BuildingId,
                device.DeviceId
            );

            if (!devices.ContainsKey(key))
                devices.Add(key, device);
        }
    }

    private void LateUpdate()
    {
        RefreshObjectives();
        RefreshMinimap();
    }

    private static string MakeKey(
        string buildingId,
        string deviceId)
    {
        return buildingId + "/" + deviceId;
    }

    private void RefreshObjectives()
    {
        if (incidentManager == null ||
            incidentManager.Current == null)
        {
            return;
        }

        IncidentDefinition incident =
            incidentManager.Current;

        if (missionTitleText != null)
        {
            missionTitleText.text =
                "CASO " +
                incidentManager.IncidentNumber +
                " / " +
                incidentManager.IncidentCount +
                " - " +
                incident.title;
        }

        if (incident.objectives == null)
            return;

        int completed = 0;
        int total = incident.objectives.Count;

        HashSet<string> targetBuildings =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

        StringBuilder builder = new StringBuilder();

        foreach (DeviceObjective objective in incident.objectives)
        {
            targetBuildings.Add(objective.buildingId);

            string key = MakeKey(
                objective.buildingId,
                objective.deviceId
            );

            bool foundDevice =
                devices.TryGetValue(
                    key,
                    out DeviceController device
                );

            bool success =
                foundDevice &&
                objective.IsSatisfied(device);

            if (success)
                completed++;

            builder.Append(success ? "[OK] " : "[  ] ");
            builder.Append(objective.buildingId);
            builder.Append(" - ");
            builder.Append(GetObjectiveDescription(objective));

            if (foundDevice && !success)
            {
                builder.Append("\n       Actual: ");

                if (objective.checkVolume)
                {
                    builder.Append("Vol ");
                    builder.Append(device.Volume);
                    builder.Append(" ");
                }

                if (objective.checkChannel)
                {
                    builder.Append(device.ChannelName);
                    builder.Append(" ");
                }

                if (objective.checkPower)
                {
                    builder.Append(
                        device.PoweredOn ? "ON" : "OFF"
                    );
                }
            }

            builder.AppendLine();
        }

        if (progressText != null)
        {
            progressText.text =
                "OBJETIVOS: " + completed + " / " + total;

            progressText.color = completed == total
                ? new Color(0.45f, 1f, 0.65f)
                : Color.white;
        }

        if (objectivesText != null)
        {
            if (completed == total && total > 0)
            {
                objectivesText.text =
                    builder.ToString() +
                    "\n¡OBJETIVOS COMPLETADOS!";
            }
            else
            {
                objectivesText.text =
                    builder.ToString();
            }
        }

        for (int i = 0; i < buildingLabels.Length; i++)
        {
            if (buildingLabels[i] == null)
                continue;

            bool target =
                i < buildingIds.Length &&
                targetBuildings.Contains(buildingIds[i]);

            buildingLabels[i].color = target
                ? new Color(1f, 0.85f, 0.25f)
                : Color.white;
        }
    }

    private static string GetObjectiveDescription(
        DeviceObjective objective)
    {
        StringBuilder result = new StringBuilder();

        bool isRadio =
            objective.deviceId.IndexOf(
                "RADIO",
                StringComparison.OrdinalIgnoreCase
            ) >= 0;

        bool isSpeaker =
            objective.deviceId.IndexOf(
                "SPEAKER",
                StringComparison.OrdinalIgnoreCase
            ) >= 0;

        result.Append(
            isRadio ? "Radio" :
            isSpeaker ? "Altavoz" : "Televisor"
        );

        if (objective.checkPower)
        {
            result.Append(
                objective.requiredPoweredOn
                    ? " ON"
                    : " OFF"
            );
        }

        if (objective.checkVolume)
        {
            result.Append(" | Vol ");

            if (objective.minVolume ==
                objective.maxVolume)
            {
                result.Append(objective.minVolume);
            }
            else
            {
                result.Append(objective.minVolume);
                result.Append("-");
                result.Append(objective.maxVolume);
            }
        }

        if (objective.checkChannel)
        {
            result.Append(" | ");

            string[] radioChannels =
            {
                "89.5", "94.2", "101.7"
            };

            string[] tvChannels =
            {
                "Local", "Cultura", "Cine"
            };

            string[] channels =
                isRadio ? radioChannels : tvChannels;

            int index = objective.requiredChannelIndex;

            result.Append(
                index >= 0 && index < channels.Length
                    ? channels[index]
                    : index.ToString()
            );
        }

        return result.ToString();
    }

    private void RefreshMinimap()
    {
        if (mapArea == null ||
            groundRenderer == null ||
            player == null)
        {
            return;
        }

        Bounds bounds = groundRenderer.bounds;

        if (bounds.size.x <= 0f ||
            bounds.size.y <= 0f)
        {
            return;
        }

        if (playerMarker != null)
        {
            playerMarker.anchoredPosition =
                WorldToMap(player.position, bounds);
        }

        int count = Mathf.Min(
            buildingTransforms.Length,
            buildingLabels.Length
        );

        for (int i = 0; i < count; i++)
        {
            if (buildingTransforms[i] == null ||
                buildingLabels[i] == null)
            {
                continue;
            }

            RectTransform labelRect =
                buildingLabels[i].rectTransform;

            labelRect.anchoredPosition =
                WorldToMap(
                    buildingTransforms[i].position,
                    bounds
                );
        }
    }

    private Vector2 WorldToMap(
        Vector3 worldPosition,
        Bounds bounds)
    {
        float x = Mathf.InverseLerp(
            bounds.min.x,
            bounds.max.x,
            worldPosition.x
        );

        float y = Mathf.InverseLerp(
            bounds.min.y,
            bounds.max.y,
            worldPosition.y
        );

        return new Vector2(
            (x - 0.5f) * mapArea.rect.width,
            (y - 0.5f) * mapArea.rect.height
        );
    }
}
