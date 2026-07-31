using UnityEngine;

public class PlaneController : MonoBehaviour
{
    [SerializeField] public Rigidbody rb;
    public Vector3 Velocity;
    public Vector3 LocalVelocity;
    public Vector3 LocalAngularVelocity;
    public float AngleOfAttackYaw;
    public float AngleOfAttack;
    public Vector3 LastVelocity;
    public Vector3 LocalGForce;
    public float Throttle = 0.7F;
    public float MaxThrust = 5F;
    public float IncrementSpeed = 5F;
    // IMPORTANT NOTE: For some reason for the plane to move forwards, the throttle must be in the negative values. 
    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.UpArrow)) // if a player holds the up arrow, the throttle gradually 'increases' (in reality the throttle value is negative, but the plane picks up speed)
        {
            Throttle -= IncrementSpeed * Time.deltaTime;
            Debug.Log("Current Value: " + Throttle);
        }

        float dt = Time.fixedDeltaTime;

        CalculateState(dt);
        CalculateAngleOfAttack();
        CalculateGForce(dt);
        UpdateThrust();
    }

    void CalculateState(float dt)
    {
        var invRotation = Quaternion.Inverse(rb.rotation);
        Velocity = rb.linearVelocity;
        LocalVelocity = invRotation * Velocity; //transform world velocity into local space
        LocalAngularVelocity = invRotation * rb.angularVelocity; //transform into local space
    }

    void CalculateAngleOfAttack()
    {
        if (LocalVelocity.sqrMagnitude < 0.1f)
        {
            AngleOfAttack = 0;
            AngleOfAttackYaw = 0;
            return;
        }

        AngleOfAttackYaw = Mathf.Atan2(-LocalVelocity.y, LocalVelocity.z);
        AngleOfAttackYaw = Mathf.Atan2(LocalVelocity.x, LocalVelocity.z);
    }

    void CalculateGForce(float dt)
    {
        var invRotation = Quaternion.Inverse(rb.rotation);
        var acceleration = (Velocity - LastVelocity) / dt;
        LocalGForce = invRotation * acceleration;
        LastVelocity = Velocity;
    }

    void UpdateThrust()
    {
        rb.AddRelativeForce(Throttle * MaxThrust * Vector3.forward);

    }
}
