
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement2D : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 movement;

    [Header("Player Items")]
    [SerializeField] private GameObject mobile;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference movementInput;
    [SerializeField] private InputActionReference interactionInput;

    [Header("Movement Settings")]
    [SerializeField] private float speed = 5f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        movementInput.action.Enable();

        if (interactionInput != null)
            interactionInput.action.Enable();
    }

    private void OnDisable()
    {
        movementInput.action.Disable();

        if (interactionInput != null)
            interactionInput.action.Disable();
    }

    private void Update()
    {
        movement = movementInput.action.ReadValue<Vector2>().normalized;
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = movement * speed;
    }
}
