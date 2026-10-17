using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BuildingAccessManager : MonoBehaviour
{
    private readonly HashSet<string> knownBuildings =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

        if (knownBuildings.Add(buildingId.Trim()))
        {
            Debug.Log(
                "[LA CENTRAL] Edificio conocido: " + buildingId
            );
        }
    }

    public void UnlockRemote()
    {
        if (RemoteUnlocked)
            return;

        RemoteUnlocked = true;

        Debug.Log(
            "[LA CENTRAL] Acceso remoto desbloqueado."
        );
    }

    public bool IsKnown(string buildingId)
    {
        return !string.IsNullOrWhiteSpace(buildingId)
               && knownBuildings.Contains(buildingId.Trim());
    }

    public bool CanAccessRemotely(string buildingId)
    {
        return RemoteUnlocked && IsKnown(buildingId);
    }
}