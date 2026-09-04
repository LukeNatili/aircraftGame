using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;


public class HologramConsole : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public MonoBehaviour PlayerMovementScript;
    public MonoBehaviour CameraLookScript;
    public CinemachineCamera PlayerCamera;
    public CinemachineCamera GlobeCamera;
    public HologramRotator HoloRotator;
    public PodiumButtonFeedback ButtonFeedback;
    public GameObject InteractPrompt;

    private PlayerInputActions PlayerInput;
    private bool PlayerInRange = false;
    private bool IsControlling = false;



    void Awake()
    {
        PlayerInput = new PlayerInputActions();
    }

    void OnEnable()
    {
        Debug.Log("HologramConsole OnEnable Called");

        PlayerInput.Player.Interact.Enable();
        PlayerInput.Player.Interact.performed += OnInteractPressed;
    }

    void OnDisable()
    {
        PlayerInput.Player.Interact.performed -= OnInteractPressed;
        PlayerInput.Player.Interact.Disable();
    }

    void OnInteractPressed(InputAction.CallbackContext ctx)
    {
        Debug.Log("Interact Pressed");

        if (!PlayerInRange) return;

        if (!IsControlling)
            EnterControlMode();
        else
            ExitControlMode();
    }

    void EnterControlMode()
    {
        IsControlling = true;

        PlayerMovementScript.enabled = false;
        if (CameraLookScript) CameraLookScript.enabled = false;

        //HoloRotator.enabled = true;
        HoloRotator.SetControlling(true);
        ButtonFeedback.enabled = true;

        if (InteractPrompt) InteractPrompt.SetActive(false);

        GlobeCamera.Priority = 20; // Higher than PlayerCam
    }

    void ExitControlMode()
    {
        IsControlling = false;

        //HoloRotator.enabled = false;
        HoloRotator.SetControlling(false);
        ButtonFeedback.enabled = false;

        GlobeCamera.Priority = 0; // Lower than PlayerCam

        PlayerMovementScript.enabled = true;
        if (CameraLookScript) CameraLookScript.enabled = true;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Entered Range");

        if (other.transform == player)
        {
            PlayerInRange = true;
            if (InteractPrompt && !IsControlling) InteractPrompt.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log("Exited Range");

        if (other.transform == player)
        {
            PlayerInRange = false;
            if (IsControlling) ExitControlMode();
            if (InteractPrompt) InteractPrompt.SetActive(false);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

}
