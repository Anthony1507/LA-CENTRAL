using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class RemoteAccessUI : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private BuildingAccessManager accessManager;
    [SerializeField] private IncidentManager incidentManager;
    [SerializeField] private RepairTerminalUI repairTerminal;
    [SerializeField] private PlayerMovement2D playerMovement;
    [SerializeField] private GameplayHUD hud;

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform listContainer;
    [SerializeField] private Button buildingButtonPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text informationText;

    private readonly List<Button> generatedButtons =
        new List<Button>();

    private bool isOpen;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseMenu);
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        Keyboard keyboard = Keyboard.current;

        if (isOpen)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
                CloseMenu();

            return;
        }

        if (!keyboard.tabKey.wasPressedThisFrame)
            return;

        if (repairTerminal != null &&
            repairTerminal.IsOpen)
        {
            return;
        }

        if (incidentManager == null ||
            !incidentManager.CanInteract)
        {
            return;
        }

        if (accessManager == null ||
            !accessManager.RemoteUnlocked)
        {
            if (hud != null)
            {
                hud.ShowFeedback(
                    "Completa el Caso 02 para desbloquear TAB."
                );
            }

            return;
        }

        OpenMenu();
    }

    public void OpenMenu()
    {
        if (isOpen ||
            panelRoot == null ||
            accessManager == null ||
            listContainer == null ||
            buildingButtonPrefab == null)
        {
            return;
        }

        if (!accessManager.RemoteUnlocked ||
            incidentManager == null ||
            !incidentManager.CanInteract)
        {
            return;
        }

        BuildBuildingList();

        if (generatedButtons.Count == 0)
        {
            if (hud != null)
            {
                hud.ShowFeedback(
                    "Aún no tienes edificios disponibles en remoto."
                );
            }

            return;
        }

        isOpen = true;

        if (playerMovement != null)
            playerMovement.enabled = false;

        panelRoot.SetActive(true);

        if (informationText != null)
        {
            informationText.text =
                "Selecciona un edificio conectado.";
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(
                generatedButtons[0].gameObject
            );
        }
    }

    public void CloseMenu()
    {
        CloseInternal(true);
    }

    private void CloseInternal(bool restoreMovement)
    {
        if (!isOpen)
            return;

        isOpen = false;

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (restoreMovement && playerMovement != null)
            playerMovement.enabled = true;
    }

    private void BuildBuildingList()
    {
        foreach (Button button in generatedButtons)
        {
            if (button != null)
            {
                button.gameObject.SetActive(false);
                Destroy(button.gameObject);
            }
        }

        generatedButtons.Clear();

        BuildingEntrance[] buildings =
            FindObjectsByType<BuildingEntrance>(
                FindObjectsSortMode.None
            );

        Array.Sort(
            buildings,
            (a, b) => string.Compare(
                a.BuildingId,
                b.BuildingId,
                StringComparison.OrdinalIgnoreCase
            )
        );

        foreach (BuildingEntrance building in buildings)
        {
            if (!accessManager.CanAccessRemotely(
                    building.BuildingId))
            {
                continue;
            }

            BuildingEntrance selectedBuilding = building;

            Button newButton = Instantiate(
                buildingButtonPrefab,
                listContainer
            );

            newButton.gameObject.name =
                "Remote_" + selectedBuilding.BuildingId;

            newButton.gameObject.SetActive(true);

            TMP_Text label =
                newButton.GetComponentInChildren<TMP_Text>(true);

            if (label != null)
            {
                label.text =
                    selectedBuilding.BuildingId +
                    " — " +
                    selectedBuilding.DisplayName;
            }

            newButton.onClick.RemoveAllListeners();

            newButton.onClick.AddListener(
                () => SelectBuilding(selectedBuilding)
            );

            generatedButtons.Add(newButton);
        }
    }

    private void SelectBuilding(BuildingEntrance building)
    {
        if (building == null ||
            repairTerminal == null ||
            accessManager == null)
        {
            return;
        }

        if (!accessManager.CanAccessRemotely(
                building.BuildingId))
        {
            return;
        }

        CloseInternal(false);

        bool opened = repairTerminal.Open(
            building,
            true
        );

        if (!opened && playerMovement != null)
            playerMovement.enabled = true;
    }
}