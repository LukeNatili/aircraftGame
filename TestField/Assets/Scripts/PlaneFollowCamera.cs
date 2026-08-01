using UnityEngine;

/// <summary>
/// Smooth chase/follow camera for a plane (or any vehicle).
/// Attach to the camera, assign the plane's transform as "target".
/// </summary>
[DisallowMultipleComponent]
public class PlaneFollowCamera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The plane transform to follow.")]
    public Transform target;

    [Header("Position")]
    [Tooltip("Offset from the target in the target's LOCAL space (behind and above the plane).")]
    public Vector3 offset = new Vector3(0f, 3f, -12f);

    [Tooltip("How quickly the camera position catches up to the desired position. Higher = snappier.")]
    public float positionDamping = 6f;

    [Header("Rotation")]
    [Tooltip("If true, the camera fully matches the plane's rotation (rolls with it). If false, camera stays more level.")]
    public bool matchFullRotation = true;

    [Tooltip("How quickly the camera rotation catches up. Higher = snappier.")]
    public float rotationDamping = 8f;

    [Tooltip("How much of the plane's roll to apply when matchFullRotation is false (0 = none, 1 = full).")]
    [Range(0f, 1f)]
    public float rollInfluence = 0.15f;

    [Header("Look Ahead (optional)")]
    [Tooltip("Adds a slight look-ahead based on target velocity for a more dynamic feel.")]
    public bool useLookAhead = false;
    public float lookAheadAmount = 5f;
    public float lookAheadDamping = 3f;

    [Header("Collision Avoidance (optional)")]
    [Tooltip("Prevents the camera from clipping through geometry between it and the target.")]
    public bool avoidCollisions = false;
    public LayerMask collisionMask = ~0;
    public float collisionBuffer = 0.3f;

    private Vector3 _velocity = Vector3.zero;
    private Vector3 _currentLookAheadPos;
    private Vector3 _lookAheadVelocity;
    private Vector3 _lastTargetPos;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("PlaneFollowCamera: No target assigned.");
            return;
        }

        _lastTargetPos = target.position;

        // Snap into place on start so there's no initial swoop-in.
        transform.position = DesiredPosition();
        transform.rotation = DesiredRotation();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // --- Position ---
        Vector3 desiredPos = DesiredPosition();

        if (useLookAhead)
        {
            Vector3 targetVelocity = (target.position - _lastTargetPos) / Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 lookAheadTarget = targetVelocity.normalized * lookAheadAmount;
            _currentLookAheadPos = Vector3.SmoothDamp(
                _currentLookAheadPos, lookAheadTarget, ref _lookAheadVelocity, 1f / Mathf.Max(lookAheadDamping, 0.01f));
            desiredPos += _currentLookAheadPos;
        }

        if (avoidCollisions)
        {
            desiredPos = HandleCollisions(desiredPos);
        }

        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPos, ref _velocity, 1f / Mathf.Max(positionDamping, 0.01f));

        // --- Rotation ---
        Quaternion desiredRot = DesiredRotation();
        transform.rotation = Quaternion.Slerp(
            transform.rotation, desiredRot, 1f - Mathf.Exp(-rotationDamping * Time.deltaTime));

        _lastTargetPos = target.position;
    }

    private Vector3 DesiredPosition()
    {
        return target.position + target.rotation * offset;
    }

    private Quaternion DesiredRotation()
    {
        if (matchFullRotation)
        {
            // Look at the plane, oriented using the plane's up vector so roll follows naturally.
            return Quaternion.LookRotation(target.position - transform.position, target.up);
        }
        else
        {
            // Stay mostly level, but blend in a bit of the plane's roll for feel.
            Vector3 lookDir = target.position - transform.position;
            Quaternion levelLook = Quaternion.LookRotation(lookDir, Vector3.up);
            Quaternion rolledLook = Quaternion.LookRotation(lookDir, target.up);
            return Quaternion.Slerp(levelLook, rolledLook, rollInfluence);
        }
    }

    private Vector3 HandleCollisions(Vector3 desiredPos)
    {
        Vector3 dir = desiredPos - target.position;
        float distance = dir.magnitude;

        if (Physics.Raycast(target.position, dir.normalized, out RaycastHit hit, distance, collisionMask))
        {
            return hit.point - dir.normalized * collisionBuffer;
        }

        return desiredPos;
    }
}