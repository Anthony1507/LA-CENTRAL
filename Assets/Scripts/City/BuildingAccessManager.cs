
using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BuildingAccessManager : MonoBehaviour
{
    private readonly HashSet<string> knownBuildings =
        new HashSet<string>(
            StringComparer.OrdinalIgnoreCase
        );

    public bool RemoteUnlocked { get; private set; }

    public int KnownBuildingCount => knownBuildings.Count;

    public void ResetProgress()
    {
        knownBuildings.Clear();
        RemoteUnlocked = false;
    }

    public void RegisterPhysicalRepair(string buildingId)
    {
        if (string.IsNullOrWhiteSpace(buildingId))
            return;

        string normalizedId = buildingId.Trim();

        if (knownBuildings.Add(normalizedId))
        {
            Debug.Log(
                "[LA CENTRAL] Edificio registrado: " +
                normalizedId
            );
        }
    }

    public void UnlockRemote()
    {
        if (RemoteUnlocked)
            return;

        RemoteUnlocked = true;

        Debug.Log(
            "[LA CENTRAL] Terminal remota desbloqueada."
        );
    }

    public bool IsKnown(string buildingId)
    {
        return !string.IsNullOrWhiteSpace(buildingId)
               && knownBuildings.Contains(
                   buildingId.Trim()
               );
    }

    public bool CanAccessRemotely(string buildingId)
    {
        return RemoteUnlocked && IsKnown(buildingId);
    }

    public string[] GetKnownBuildings()
    {
        string[] result =
            new string[knownBuildings.Count];

        knownBuildings.CopyTo(result);

        Array.Sort(
            result,
            StringComparer.OrdinalIgnoreCase
        );

        return result;
    }

    public void RestoreProgress(
        string[] buildingIds,
        bool remoteUnlocked)
    {
        knownBuildings.Clear();

        if (buildingIds != null)
        {
            foreach (string buildingId in buildingIds)
            {
                if (!string.IsNullOrWhiteSpace(buildingId))
                {
                    knownBuildings.Add(
                        buildingId.Trim()
                    );
                }
            }
        }

        RemoteUnlocked = remoteUnlocked;

        Debug.Log(
            "[LA CENTRAL] Progreso restaurado: " +
            KnownBuildingCount + " edificios | " +
            "Remoto: " +
            (RemoteUnlocked ? "Activo" : "Bloqueado")
        );
    }
}
