using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;


public class PlaneController : MonoBehaviour
{
    // similar to Vector3.Scale, but has separate factor negative values on each axis
    // allows us to specify a different value for positive and negative inputs
    public static Vector3 Scale6(
        Vector3 value,
        float posX, float negX,
        float posY, float negY,
        float posZ, float negZ
    )
    {
        Vector3 result = value;

        if (result.x > 0)
        {
            result.x *= posX;
        }
        else if (result.x < 0)
        {
            result.x = negX;
        }

        if (result.y > 0)
        {
            result.y *= posY;
        }
        else if (result.y < 0)
        {
            result.y *= negY;
        }

        if (result.z > 0)
        {
            result.z *= posZ;
        }
        else if (result.z < 0)
        {
            result.z = negZ;
        }

        return result;
    }

    [SerializeField] public Rigidbody rb;
    public Vector3 Velocity;
    public Vector3 LocalVelocity;
    public Vector3 LocalAngularVelocity;
    public float AngleOfAttackYaw;
    public float AngleOfAttack;
    public Vector3 LastVelocity;
    public Vector3 LocalGForce;
    public float Throttle = 0.0F;
    public float MaxThrust = 295F;
    public float IncrementSpeed = 100F;

    public AnimationCurve DragRight;
    public AnimationCurve DragLeft;
    public AnimationCurve DragTop;
    public AnimationCurve DragBottom;
    public AnimationCurve DragForward;
    public AnimationCurve DragBack;
    public AnimationCurve AoaCurve;

    public bool AirbrakeDeployed;
    public float AirbrakeDrag = 50F;
    public bool FlapsDeployed;
    public float FlapsDrag;
    public float FlapsLiftPower;
    public float FlapsAOABias;

    //tuning parameter for dragForce
    public float InducedDrag = 15;

    public float LiftPower = 150F;
    public AnimationCurve LiftAOACurve;
    public float RudderPower;
    public AnimationCurve RudderAOACurve;

    void OnDrawGizmosSelected()
    {
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(rb.centerOfMass, 0.2f);
        }
        rb.centerOfMass = new Vector3(0, -.5f, 2f);
    }


    // IMPORTANT NOTE: For some reason for the plane to move forwards, the throttle must be in the negative values. 
    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.UpArrow)) // if a player holds the up arrow, the throttle gradually 'increases' (in reality the throttle value is negative, but the plane picks up speed)
        {
            Throttle -= IncrementSpeed * Time.deltaTime;
            Debug.Log("Current Throttle Value: " + Throttle);
        }
        else if (Throttle < 0)
        {
            Throttle += IncrementSpeed * Time.deltaTime;
            Debug.Log("Current Throttle Value: " + Throttle);
        }

        float dt = Time.fixedDeltaTime;

        CalculateState(dt);
        CalculateAngleOfAttack();
        CalculateGForce(dt);
        UpdateThrust();
        UpdateDrag();
        UpdateLift();
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
        //rb.AddForceAtPosition(Throttle * MaxThrust * Vector3.right, rb.centerOfMass);

    }

    void UpdateDrag ()
    {
        var lv = LocalVelocity;
        var lv2 = lv.sqrMagnitude;  // velocity squared

        float AirbrakeDrag = AirbrakeDeployed ? this.AirbrakeDrag : 0;
        float FlapsDrag = FlapsDeployed ? this.FlapsDrag : 0;

        //calculate coefficient of drag depending on direction on velocity
        //six drag coefficents are defined by the unity AnimationCurve class, the input to the curve is speed and the output is coefficient of drag
        //allowing us to fine tune drag behavior at different speeds
        var coefficient = PlaneController.Scale6(
            lv.normalized,
            DragRight.Evaluate(Mathf.Abs(lv.x)), DragLeft.Evaluate(Mathf.Abs(lv.x)),
            DragTop.Evaluate(Mathf.Abs(lv.y)), DragBottom.Evaluate(Mathf.Abs(lv.y)),
            DragForward.Evaluate(Mathf.Abs(lv.z)) + AirbrakeDrag + FlapsDrag, DragBack.Evaluate(Mathf.Abs(lv.z))
        );

        var drag = coefficient.magnitude * lv2 * -lv.normalized;    // drag is the opposite direction of velocity

        rb.AddRelativeForce(drag);
    }

    void UpdateLift()
    {
        if (LocalVelocity.sqrMagnitude < 1f) return;

        //extra lift from flaps is handtuned by adjusting FlapsLiftPower and FlapsAOABias by handtuned amounts
        //what's a good value for lift power? 0 lift power results in 0 lift
        float FlapsLiftPower = FlapsDeployed ? this.FlapsLiftPower : 0;
        float FlapsAOABias = FlapsDeployed ? this.FlapsAOABias : 0;

        var liftForce = CalculateLift(
            AngleOfAttack + (FlapsAOABias * Mathf.Deg2Rad), Vector3.right,
            LiftPower + FlapsLiftPower,
            LiftAOACurve
            );

        //sideways lift generated by vertical stabilizers is applied here
        //this lift is only used to change the planes velocity, we calculate torque generated by the rudder seperately
        var yawForce = CalculateLift(AngleOfAttackYaw, Vector3.up, RudderPower, RudderAOACurve);

        rb.AddRelativeForce(liftForce);
        rb.AddRelativeForce(yawForce);
    }

    Vector3 CalculateLift(float angleOfAttack, Vector3 rightAxis, float LiftPower, AnimationCurve AoaCurve)
    {
        //calculate lift
        var liftVelocity = Vector3.ProjectOnPlane(LocalVelocity, rightAxis);
        var v2 = liftVelocity.sqrMagnitude;

        //lift = velocity^2 * coefficient * liftPower
        //coefficient varies with AOA
        var liftCoefficient = AoaCurve.Evaluate(angleOfAttack * Mathf.Rad2Deg);
        var liftForce = v2 * liftCoefficient * LiftPower;

        //lift is perpendicular to velocity
        var liftDirection = Vector3.Cross(liftVelocity.normalized, rightAxis);
        var lift = liftDirection * liftForce;

        //calculate induced drag

        //induced drag varies with square of lift coefficient
        var dragForce = liftCoefficient * liftCoefficient * this.InducedDrag;
        var dragDirection = -liftVelocity.normalized;
        var inducedDrag = dragDirection * v2 * dragForce;

        return lift + inducedDrag;
    }

   

}
