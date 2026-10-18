
using System;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class CampaignSaveData
{
    public int version = 1;

    public string stage = "Gameplay";

    public int nextIncidentIndex = 0;
    public int reputation = 50;

    public int wrongActions = 0;
    public float completedIncidentSeconds = 0f;

    public bool remoteUnlocked = false;
    public string[] knownBuildingIds = new string[0];
}

public static class SaveService
{
    public const string StageGameplay = "Gameplay";
    public const string StageConcert = "Concert";

    private const int CurrentVersion = 1;
    private const string FileName = "la-central-save.json";

    private const string CampaignAssistKey =
        "LC_CampaignAssistUsed";

    public static string SavePath
    {
        get
        {
            return Path.Combine(
                Application.persistentDataPath,
                FileName
            );
        }
    }

    public static bool HasSave()
    {
        return TryLoad(out _);
    }

    public static bool HasResumableSave()
    {
        if (!TryLoad(out CampaignSaveData data))
            return false;

        if (data.stage == StageGameplay)
        {
            return data.nextIncidentIndex >= 0 &&
                   data.nextIncidentIndex < 10;
        }

        if (data.stage == StageConcert)
        {
            return PlayerPrefs.GetInt(
                "LC_ConcertWon", 0
            ) == 0;
        }

        return false;
    }

    public static void Save(CampaignSaveData data)
    {
        if (data == null)
        {
            Debug.LogError(
                "[LA CENTRAL] Se intento guardar " +
                "una partida nula."
            );
            return;
        }

        try
        {
            data.version = CurrentVersion;

            if (data.knownBuildingIds == null)
            {
                data.knownBuildingIds =
                    new string[0];
            }

            string directory =
                Application.persistentDataPath;

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string json = JsonUtility.ToJson(data, true);

            string temporaryPath = SavePath + ".tmp";

            File.WriteAllText(temporaryPath, json);

            File.Copy(
                temporaryPath,
                SavePath,
                true
            );

            File.Delete(temporaryPath);

            Debug.Log(
                "[LA CENTRAL] Progreso guardado: " +
                data.stage +
                " | Incidencia siguiente: " +
                (data.nextIncidentIndex + 1)
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[LA CENTRAL] Error al guardar: " +
                exception
            );
        }
    }

    public static bool TryLoad(
        out CampaignSaveData data)
    {
        data = null;

        if (!File.Exists(SavePath))
            return false;

        try
        {
            string json = File.ReadAllText(SavePath);

            CampaignSaveData loaded =
                JsonUtility.FromJson<CampaignSaveData>(json);

            if (loaded == null)
                return false;

            if (loaded.version != CurrentVersion)
            {
                Debug.LogWarning(
                    "[LA CENTRAL] Version de " +
                    "guardado incompatible."
                );
                return false;
            }

            if (loaded.reputation <= 0 ||
                loaded.nextIncidentIndex < 0 ||
                loaded.nextIncidentIndex > 10)
            {
                Debug.LogWarning(
                    "[LA CENTRAL] Datos de " +
                    "guardado invalidos."
                );
                return false;
            }

            if (loaded.stage != StageGameplay &&
                loaded.stage != StageConcert)
            {
                return false;
            }

            if (loaded.knownBuildingIds == null)
            {
                loaded.knownBuildingIds =
                    new string[0];
            }

            data = loaded;
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[LA CENTRAL] No se pudo cargar: " +
                exception
            );

            return false;
        }
    }

    public static void DeleteSave()
    {
        try
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);

            string temporaryPath = SavePath + ".tmp";

            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);

            PlayerPrefs.DeleteKey(CampaignAssistKey);
            PlayerPrefs.Save();

            Debug.Log(
                "[LA CENTRAL] Nueva campaña: " +
                "checkpoint y asistencia reiniciados."
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[LA CENTRAL] Error al reiniciar guardado: " +
                exception
            );
        }
    }

    public static void PrintSaveLocation()
    {
        Debug.Log(
            "[LA CENTRAL] Archivo de guardado: " +
            SavePath
        );
    }
}
