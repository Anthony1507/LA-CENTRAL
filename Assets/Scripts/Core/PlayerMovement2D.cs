using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement2D : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 movement;

    [Header("Player Items")]
    [SerializeField] private GameObject mobile;


   [Header("Input Actions")]   // Nos ayudarán a tener un mejor maneclo de las teclas de entrada, y nos permitirá cambiar las teclas de entrada sin tener que modificar el código.
    [SerializeField] private InputActionReference movementInput;
    [SerializeField] private InputActionReference interactionInput;

    [Header("Movement Settings")]
    [SerializeField] private float speed = 5f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }


    private void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            mobile.SetActive(!mobile.activeSelf);
        }
        movement = movementInput.action.ReadValue<Vector2>().normalized;
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = movement * speed;
    }
}