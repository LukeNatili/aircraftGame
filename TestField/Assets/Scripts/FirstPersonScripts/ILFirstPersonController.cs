using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ILFirstPersonController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] private float WalkSpeed = 4f;
    [SerializeField] private float SprintSpeed = 7f;
    [SerializeField] private float JumpHeight = 1.2f;
    [SerializeField] private float Gravity = -9.81f;

    [Header("Look")]
    [SerializeField] private float LookSensitivity = 0.1f;
    [SerializeField] private float MaxLookAngle = 85f;

    private PlayerInputActions PlayerInput;
    private CharacterController Controller;

    private Vector2 MoveInput;
    private Vector2 LookInput;
    private bool SprintHeld;
    private bool JumpPressed;

    private Vector3 Velocity;
    private float Pitch;

    void Awake()
    {
        Controller = GetComponent<CharacterController>();
        PlayerInput = new PlayerInputActions();
    }

    void OnEnable()
    {
        PlayerInput.Player.Enable();

        // ctx refers to an instance of InputAction.CallbackContext which provides state info when handling user input through unity's Input System
        // essentially a snapshot of the inputs happening in that moment
        // += attaches the code function to the event, ctx, containing the input data
        // => tells the code to run the expression after it when the event before it triggers
        PlayerInput.Player.Move.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        PlayerInput.Player.Move.canceled += ctx => MoveInput = Vector2.zero;

        PlayerInput.Player.Look.performed += ctx => LookInput = ctx.ReadValue<Vector2>();
        PlayerInput.Player.Look.canceled += ctx => LookInput = Vector2.zero;

        PlayerInput.Player.Sprint.performed += ctx => SprintHeld = true;
        PlayerInput.Player.Sprint.canceled += ctx => SprintHeld = false;

        PlayerInput.Player.Jump.performed += ctx => JumpPressed = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

    }

    void OnDisable()
    {
        PlayerInput.Player.Disable();

        // unsubscribing is best practice to prevent memory leaks in unity
        PlayerInput.Player.Move.performed -= ctx => MoveInput = ctx.ReadValue<Vector2>();
        PlayerInput.Player.Move.canceled -= ctx => MoveInput = Vector2.zero;

        PlayerInput.Player.Look.performed -= ctx => LookInput = ctx.ReadValue<Vector2>();
        PlayerInput.Player.Look.canceled -= ctx => LookInput = Vector2.zero;

        PlayerInput.Player.Sprint.performed -= ctx => SprintHeld = true;
        PlayerInput.Player.Sprint.canceled -= ctx => SprintHeld = false;

        PlayerInput.Player.Jump.performed -= ctx => JumpPressed = true;
    }

    // Update is called once per frame
    void Update()
    {
        HandleLook();
        HandleMove();
    }

    void HandleLook()
    {
        float mouseX = LookInput.x * LookSensitivity;
        float mouseY = LookInput.y * LookSensitivity;

        // yaw on the body
        transform.Rotate(Vector3.up * mouseX);

        // pitch on the camera only
        Pitch -= mouseY;
        Pitch = Mathf.Clamp(Pitch, -MaxLookAngle, MaxLookAngle);
        cameraTransform.localRotation = Quaternion.Euler(Pitch, 0f, 0f);
    }

    void HandleMove()
    {
        bool grounded = Controller.isGrounded;
        if (grounded && Velocity.y <0)
        {
            Velocity.y = -2f; //small stick to ground value
        }

        Vector3 move = transform.right * MoveInput.x + transform.forward * MoveInput.y;
        float speed = SprintHeld ? SprintSpeed : WalkSpeed;
        Controller.Move(move * speed * Time.deltaTime);

        if (JumpPressed && grounded)
        {
            Velocity.y = Mathf.Sqrt(JumpHeight * -2f * Gravity);
        }
        JumpPressed = false;

        Velocity.y += Gravity * Time.deltaTime;
        Controller.Move(Velocity * Time.deltaTime);
    }
}
