using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class OrbitCamIdleSwitcher : MonoBehaviour
{
    public CinemachineCamera FollowCam;
    public CinemachineCamera OrbitCam;
    public InputActionReference LookAction;

    public float IdleRevertDelay = 3f;
    public float InputDeadzone = 0.01f;

    private float IdleTimer;

    private void OnEnable() => LookAction.action.Enable();
    private void OnDisable() => LookAction.action.Disable();

    private void Update()
    {
        Vector2 look = LookAction.action.ReadValue<Vector2>();

        if (look.sqrMagnitude > InputDeadzone * InputDeadzone)
        {
            IdleTimer = 0f;
            OrbitCam.Priority = 20;
            FollowCam.Priority = 10;
        }
        else
        {
            IdleTimer += Time.deltaTime;
            if (IdleTimer >= IdleRevertDelay)
            {
                OrbitCam.Priority = 10;
                FollowCam.Priority = 20;
            }
        }
    }
}