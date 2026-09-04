using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class HologramRotator : MonoBehaviour
{
    [Header("How fast the globe spins when the player is controlling it")]
    public float PlayerSpinSpeed = 30f;
    [Header("How fast the globe spins vertically")]
    public float VerticalRotationSpeed = 60f;
    [Header("How fast the globe spins when the player is not controlling it")]
    public float IdleSpinSpeed = 5f;
    [Header("How long the transition back to the original rotation takes")]
    public float ReturnDuration = 1f; // how long to transition back to original rotation
    [Header("Determines if the globe is being controlled by the player")]
    public bool IsControlling { get; private set; } = false;

    private PlayerInputActions PlayerInput;

    // the original XYZ rotation of the mesh
    private Quaternion OriginalRotation;
    // determines if the globe is returning to its original rotation
    private bool IsReturning = false;
    // Coroutines are specialized methods that can suspend its execution (yield) and resume at a later time
    private Coroutine ReturnCoroutine;

    void Awake()
    {
        PlayerInput = new PlayerInputActions();
        OriginalRotation = transform.rotation;
    }

    void OnEnable()
    {
        PlayerInput.Globe.Enable();
    }

    void OnDisable()
    {
        PlayerInput.Globe.Disable();
    }

    // Allows HologramConsole to change the IsControlling state
    public void SetControlling(bool controlling)
    {
        IsControlling = controlling;

        if (!controlling)
        {
            if (ReturnCoroutine != null) StopCoroutine(ReturnCoroutine);
            ReturnCoroutine = StartCoroutine(ReturnToOriginalRotation());
        }
    }

    IEnumerator ReturnToOriginalRotation()
    {
        IsReturning = true;

        Quaternion startRot = transform.rotation;
        float t = 0f;

        while (t < ReturnDuration)
        {
            t += Time.deltaTime;
            float lerpT = Mathf.SmoothStep(0f, 1f, t / ReturnDuration);
            transform.rotation = Quaternion.Slerp(startRot, OriginalRotation, lerpT);
            yield return null;
        }

        transform.rotation = OriginalRotation;
        IsReturning = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (IsControlling)
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (PlayerInput.Globe.RotateRight.IsPressed()) horizontal += 1f;
            if (PlayerInput.Globe.RotateLeft.IsPressed()) horizontal -= 1f;
            if (PlayerInput.Globe.RotateUp.IsPressed()) vertical -= 1f;
            if (PlayerInput.Globe.RotateDown.IsPressed()) vertical += 1f;

            transform.Rotate(Vector3.up, horizontal * PlayerSpinSpeed * Time.deltaTime, Space.World);
            transform.Rotate(Vector3.right, vertical * VerticalRotationSpeed * Time.deltaTime, Space.World);
        }
        else if (!IsReturning)
        {
            transform.Rotate(Vector3.up, IdleSpinSpeed * Time.deltaTime, Space.World);
        }
    }
}
