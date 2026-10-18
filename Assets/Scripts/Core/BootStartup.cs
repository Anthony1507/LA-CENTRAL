
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BootStartup : MonoBehaviour
{
    [Header("Startup")]
    [SerializeField] private string mainMenuScene = "01_MainMenu";

    private static readonly Vector2Int[] SupportedResolutions =
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080)
    };

    private void Start()
    {
        Time.timeScale = 1f;

        ApplySavedSettings();
        OpenMainMenu();
    }

    private void ApplySavedSettings()
    {
        int resolutionIndex = Mathf.Clamp(
            PlayerPrefs.GetInt("LC_ResolutionIndex", 2),
            0,
            SupportedResolutions.Length - 1
        );

        bool fullscreen =
            PlayerPrefs.GetInt("LC_Fullscreen", 1) == 1;

        Vector2Int resolution =
            SupportedResolutions[resolutionIndex];

        if (!Application.isEditor)
        {
            Screen.SetResolution(
                resolution.x,
                resolution.y,
                fullscreen
            );
        }
    }

    private void OpenMainMenu()
    {
        if (!Application.CanStreamedLevelBeLoaded(mainMenuScene))
        {
            Debug.LogError(
                "[LA CENTRAL] No se encuentra MainMenu. " +
                "Comprueba las escenas de la build."
            );

            return;
        }

        SceneManager.LoadScene(mainMenuScene);
    }
}
