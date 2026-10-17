using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerInteractor2D : MonoBehaviour
{
    [SerializeField] private PlayerMovement2D playerMovement;
    [SerializeField] private RepairTerminalUI terminal;
    [SerializeField] private GameplayHUD hud;
    [SerializeField] private IncidentManager incidentManager;

    private BuildingEntrance nearbyEntrance;

    private void Awake()
    {
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement2D>();
    }

    private void OnEnable()
    {
        if (terminal != null) terminal.Closed += AllowMovement;
    }

    private void OnDisable()
    {
        if (terminal != null) terminal.Closed -= AllowMovement;
    }

    private void Update()
    {
        if (nearbyEntrance == null || terminal == null) return;
        if (terminal.IsOpen || (incidentManager != null && !incidentManager.CanInteract)) return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (terminal.Open(nearbyEntrance) && playerMovement != null)
                playerMovement.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out BuildingEntrance entrance)) return;
        nearbyEntrance = entrance;
        if (hud != null) hud.SetInteractionHint("E — Reparar en " + entrance.DisplayName);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.TryGetComponent(out BuildingEntrance entrance) || nearbyEntrance != entrance) return;
        nearbyEntrance = null;
        if (hud != null) hud.SetInteractionHint("");
    }

    private void AllowMovement()
    {
        if (playerMovement != null) playerMovement.enabled = true;
    }
}
