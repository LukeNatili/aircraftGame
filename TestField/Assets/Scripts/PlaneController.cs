using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;


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
    public Vector3 MaxAngularVelocity;
    public float AngleOfAttackYaw;
    public float AngleOfAttack;
    public Vector3 LastVelocity;
    public Vector3 LocalGForce;
    public float Throttle = 0.0F;
    public float MaxThrust = 129047f;
    public float IncrementSpeed = 35f;

    public AnimationCurve DragRight;
    public AnimationCurve DragLeft;
    public AnimationCurve DragTop;
    public AnimationCurve DragBottom;
    public AnimationCurve DragForward;
    public AnimationCurve DragBack;
    public AnimationCurve AoaCurve;

    public bool AirbrakeDeployed;
    public float AirbrakeDrag = 15F;
    public bool FlapsDeployed;
    public float FlapsDrag;
    public float FlapsLiftPower;
    public float FlapsAOABias;
    public bool LandingGearDeployed;
    public float LandingGearDrag;
    public bool CollapseTurtleDeck;

    //tuning parameter for dragForce
    public float InducedDrag = 15;

    public float LiftPower = 150f;
    public AnimationCurve LiftAOACurve;
    public float RudderPower;
    public AnimationCurve RudderAOACurve;

    //tuning parameter for steering
    public AnimationCurve SteeringCurve;

    public Vector3 TurnSpeed;
    public Vector3 TurnAcceleration;
    public Vector3 controlInput;

    //forward speed (m/s) at which thrust output tapers to zero
    public float TopSpeed = 132f;

    //X-Axis: current speed / TopSpeed. Y-Axis: thrust multiplier.
    //should stay at 1 for most of the range, then fall to 0 by X=1 so thrust can't push the plane past TopSpeed
    //but doesn't limit dive speed too
    public AnimationCurve ThrottleResponseCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    //new way of controlling player input
    public PlayerInputActions PlayerInput;

    [Header("Aerodynamic Stability (Weathervaning)")]
    [Tooltip("How strongly the nose is pulled toward the velocity vector in yaw." + "(what makes the nose tip down in a dive and up in a climb)")]
    public AnimationCurve PitchStabilityCurve;

    [Tooltip("How strongly the nose is pulled toward the velocity vector. " + "(what turns a bank into an actual coordinated turn instead of a sideways slide)")]
    public AnimationCurve YawStabilityCurve;

    [Tooltip("Minimum seconds between accepted button inputs")]
    public float LandingGearCooldownDuration = 1f;
    public float TurtleDeckCooldownDuration = 1f;
    private float LandingGearNextAllowedTime = 0f;
    private float TurtleDeckNextAllowedTime = 0f;

    void OnDrawGizmosSelected()
    {
        if (rb != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.TransformPoint(rb.centerOfMass), 0.2f);
        }
    }

    void Awake()
    {
        //centering the mass to the plane
        //rb.centerOfMass = new Vector3(-6.5f, 4.5f, -9f);

        Debug.Log($"Inertia Tensor: {rb.inertiaTensor}, Mass: {rb.mass}");

        //instantiating the player input
        PlayerInput = new PlayerInputActions();
        

    }

    void Start()
    {

    }

    void OnEnable()
    {
        //enabling the action map
        PlayerInput.Plane.Enable();

        //generates a context event on key press
        //PlayerInput.Plane.IncreaseThrottle.performed += ThrottleStatus;

    }

    void OnDisable()
    {
        //PlayerInput.Plane.IncreaseThrottle.performed -= ThrottleStatus;
        PlayerInput.Plane.Disable();
    }

    //supposed to debug, i dont think it works
    //void ThrottleStatus(InputAction.CallbackContext context)
    //{
    //    Debug.Log("Throttled!");
    //}

    void Update()
    {
        //these input readers are hear because they are a one-time button press to toggle a bool. WasPressedThisFrame() is a one-frame edge rather than IsPressed(), a held state
        //adds a cooldown to prevent spam toggling the bool
        if (PlayerInput.Plane.MoveLandingGear.WasPressedThisFrame() && Time.time >= LandingGearNextAllowedTime)
        {
            LandingGearNextAllowedTime = Time.time + LandingGearCooldownDuration;
            LandingGearDeployed = !LandingGearDeployed;
            //if (PlayerInput.Plane.MoveLandingGear.IsPressed()) LandingGearDeployed = !LandingGearDeployed;
        }

        if (PlayerInput.Plane.MoveTurtleDeck.WasPressedThisFrame() && Time.time >= TurtleDeckNextAllowedTime)
        {
            TurtleDeckNextAllowedTime = Time.time + TurtleDeckCooldownDuration;
            CollapseTurtleDeck = !CollapseTurtleDeck;
            //if (PlayerInput.Plane.MoveTurtleDeck.IsPressed()) CollapseTurtleDeck = !CollapseTurtleDeck;

        }

    }

    // FixedUpdate doesn't run once per rendered frame, depending on your framerate vs the fixed timestep it can run zero, one, or multiple times within a single rendered frame
    void FixedUpdate()
    {
        //these input readers are here because IsPressed is a held state rather than WasPressedThisFrame(), which is a one-frame edge
        if (PlayerInput.Plane.IncreaseThrottle.IsPressed())
        {
            Throttle = Mathf.Min(Throttle + (IncrementSpeed / 500f) * Time.deltaTime, 1f);
            Debug.Log("Current Throttle Value: " + Throttle);
        }
        else if (Throttle > 0)
        {
            Throttle = Mathf.Min(Throttle - (IncrementSpeed / 250f) * Time.deltaTime, 1f);
            Debug.Log("Current Throttle Value: " + Throttle);
        }

        //Roll = z axis, Pitch = x axis, Yaw = y axis
        float roll = 0f;
        if (PlayerInput.Plane.RollRight.IsPressed()) roll = -1f;
        if (PlayerInput.Plane.RollLeft.IsPressed()) roll = 1f;

        float pitch = 0f;
        if (PlayerInput.Plane.PitchDown.IsPressed()) pitch = 1f;
        if (PlayerInput.Plane.PitchUp.IsPressed()) pitch = -1f;

        float yaw = 0f;
        if (PlayerInput.Plane.BankLeft.IsPressed()) yaw = -1f;
        if (PlayerInput.Plane.BankRight.IsPressed()) yaw = 1f;

        if (PlayerInput.Plane.DeployAirbreak.IsPressed()) AirbrakeDeployed = true;
        if (!PlayerInput.Plane.DeployAirbreak.IsPressed()) AirbrakeDeployed = false;

        controlInput = new Vector3(pitch, yaw, roll);


        float dt = Time.fixedDeltaTime;

        Debug.Log($"Throttle: {Throttle}, Thrust Force: {Throttle * MaxThrust}");
        //Debug.Log($"[Cooldown] frame:{Time.frameCount} pressed:{PlayerInput.Plane.MoveLandingGear.WasPressedThisFrame()} time:{Time.time:F3} nextAllowed:{NextAllowedTime:F3} ready:{Time.time >= NextAllowedTime}");


        CalculateState(dt);
        CalculateAngleOfAttack();
        CalculateGForce(LocalAngularVelocity, Velocity);
        UpdateThrust();
        UpdateDrag();
        UpdateLift();
        UpdateAeroTorque();
        UpdateSteering(dt);
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
        if (LocalVelocity.sqrMagnitude < 0.01f)
        {
            AngleOfAttack = 0;
            AngleOfAttackYaw = 0;
            return;
        }
        AngleOfAttack = Mathf.Atan2(-LocalVelocity.y, LocalVelocity.z);
        AngleOfAttackYaw = Mathf.Atan2(LocalVelocity.x, LocalVelocity.z);
    }

    Vector3 CalculateGForce(Vector3 AngularVelocity, Vector3 Velocity)
    {
        return Vector3.Cross(AngularVelocity, Velocity);
    }

    Vector3 CalculateGForceLimit(Vector3 input)
    {
        return PlaneController.Scale6(input,
            4f, 8f,  //pitch down gLimit, pitch up gLimit
            7f, 7f,   //yaw
            5f, 5f    //roll
            ) * 9.81f;
    }

    float CalculateGLimiter(Vector3 controlInput, Vector3 MaxAngularVelocity)
    {
        //if the player gives input with magnitude less than 1, scale up there input so that magnitude == 1
        var maxInput = controlInput.normalized;

        var limit = CalculateGForceLimit(maxInput);
        var maxGForce = CalculateGForce(Vector3.Scale(maxInput, MaxAngularVelocity), LocalVelocity);

        if (maxGForce.magnitude > limit.magnitude)
        {
            //example:
            //maxGForce = 16G, limit = 8G
            //so this is 8 / 16 or 0.5
            return limit.magnitude / maxGForce.magnitude;
        }

        return 1;
    }

    //tuner variable for the simulated climb/dive forces
    public float Weight = 1f;
    void UpdateThrust()
    {
        float forwardSpeed = Mathf.Max(0f, LocalVelocity.z);
        float speedRatio = forwardSpeed / Mathf.Max(TopSpeed, 0.01f);
        float thrustMultiplier = ThrottleResponseCurve.Evaluate(speedRatio);

        rb.AddRelativeForce(Throttle * MaxThrust * thrustMultiplier * Vector3.forward);

        //apply a tunable gravity-like force along the plane's forward axis
        //climbing loses speed diving gains speed independent of actual gravity
        //pitch up (forwards points skyward) gives a negative dot product, resulting in speed loss, and vice versa
        //using ForceMode.Acceleration here specifically bypasses mass scaling so Weight acts consistently despite mass
        float forwardGravityForce = Vector3.Dot(Physics.gravity.normalized, transform.forward);
        rb.AddRelativeForce(Vector3.forward * forwardGravityForce * Weight, ForceMode.Acceleration);

    }

    //tuner variable stuff to see if fix
    public float DragScale = 0.16f;

    void UpdateDrag ()
    {
        var lv = LocalVelocity;
        var lv2 = lv.sqrMagnitude;  // velocity squared

        float AirbrakeDrag = AirbrakeDeployed ? this.AirbrakeDrag : 0;
        float FlapsDrag = FlapsDeployed ? this.FlapsDrag : 0;
        float LandingGearDrag = LandingGearDeployed ? this.LandingGearDrag : 0;

        //calculate coefficient of drag depending on direction on velocity
        //six drag coefficents are defined by the unity AnimationCurve class, the input to the curve is speed and the output is coefficient of drag
        //allowing us to fine tune drag behavior at different speeds
        var coefficient = PlaneController.Scale6(
            lv.normalized,
            DragRight.Evaluate(Mathf.Abs(lv.x)), DragLeft.Evaluate(Mathf.Abs(lv.x)),
            DragTop.Evaluate(Mathf.Abs(lv.y)), DragBottom.Evaluate(Mathf.Abs(lv.y)),
            DragForward.Evaluate(Mathf.Abs(lv.z)) + AirbrakeDrag + FlapsDrag + LandingGearDrag, DragBack.Evaluate(Mathf.Abs(lv.z))
        );

        var drag = coefficient.magnitude * lv2 * -lv.normalized * DragScale;    // drag is the opposite direction of velocity

        rb.AddRelativeForce(drag);
    }

    //temp scale to try to fix
    public float LiftScale = 0.13f;

    void UpdateLift()
    {
        //if (LocalVelocity.sqrMagnitude < 1f) return;
        if (LocalVelocity.sqrMagnitude < 0.01f) return; // was 1f, much too aggressive a cutoff

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

        Debug.Log($"liftForce:{liftForce} scaledLift:{liftForce * LiftScale} impliedLateralAccel:{(liftForce * LiftScale).magnitude / rb.mass}");

        rb.AddRelativeForce(liftForce * LiftScale);
        rb.AddRelativeForce(yawForce * LiftScale);
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

    float CalculateSteering(float dt, float angularVelocity, float targetVelocity, float acceleration)
    {
        var error = targetVelocity - angularVelocity;
        var accel = acceleration * dt;
        return Mathf.Clamp(error, -accel, accel);
    }

    void UpdateSteering(float dt)
    {
        //var speed = Mathf.Max(0, LocalVelocity.z);
        var speed = Mathf.Abs(LocalVelocity.z);
        var steeringPower = SteeringCurve.Evaluate(speed);

        var gForceScaling = CalculateGLimiter(controlInput, TurnSpeed * Mathf.Deg2Rad * steeringPower);

        //control input is the combination of inputs from the player (pitch, roll, and yaw). turnSpeed is the turn rate of each axis. TargetAV is limited by steeringPower, an AnimationCurve that reduces the strength of turn at low speed.
        var targetAV = Vector3.Scale(controlInput, TurnSpeed * steeringPower * gForceScaling);
        var av = LocalAngularVelocity * Mathf.Rad2Deg;

        var correction = new Vector3(
            CalculateSteering(dt, av.x, targetAV.x, TurnAcceleration.x * steeringPower),
            CalculateSteering(dt, av.y, targetAV.y, TurnAcceleration.y * steeringPower),
            CalculateSteering(dt, av.z, targetAV.z, TurnAcceleration.z * steeringPower)
        );

        Debug.Log($"input:{controlInput} speed:{speed} steeringPower:{steeringPower} targetAV:{targetAV} correction:{correction}");

        rb.AddRelativeTorque(correction * Mathf.Deg2Rad, ForceMode.VelocityChange);
        //var torque =
        //rb.AddRelativeTorque(torque * Mathf.Deg2Rad);
    }



    //Reset() only runs in editor when the component is added or manually reset

    void Reset()
    {
        MaxThrust = 129047;
        IncrementSpeed = 35;
        DragRight = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(1f, 1f)
        );
        DragLeft = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(1f, 1f)
        );
        DragTop = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(1f, 1f)
        );
        DragBottom = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(1f, 1f)
        );
        DragForward = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(1f, 1f)
        );
        DragBack = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(1f, 1f)
        );
        AoaCurve = new AnimationCurve(
            new Keyframe(-90f, 0f),
            new Keyframe(-30f, -1f),
            new Keyframe(0f, 0f),
            new Keyframe(30f, 1f),
            new Keyframe(90f, 0f)
        );
        AirbrakeDrag = 15;
        FlapsDrag = 0;
        FlapsLiftPower = 150;
        FlapsAOABias = 0;
        LandingGearDeployed = true;
        LandingGearDrag = 15;
        CollapseTurtleDeck = false;
        InducedDrag = 15;
        LiftPower = 150;
        LiftAOACurve = new AnimationCurve(
            new Keyframe(-90f, 0f),
            new Keyframe(-30f, -1f),
            new Keyframe(0f, 0f),
            new Keyframe(30f, 1f),
            new Keyframe(90f, 0f)
        );
        RudderPower = 50;
        RudderAOACurve = new AnimationCurve(
            new Keyframe(-90f, 0f),
            new Keyframe(-30f, -1f),
            new Keyframe(0f, 0f),
            new Keyframe(30f, 1f),
            new Keyframe(90f, 0f)
        );
        SteeringCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(1f, 1f)
        );
        TurnSpeed = new Vector3(27, 30, 100);
        TurnAcceleration = new Vector3(60, 15, 250);
        TopSpeed = 132;
        ThrottleResponseCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.90f, 1f),
            new Keyframe(1f, 0f)
        );
        Weight = 10;
        DragScale = 0.16f;
        LiftScale = 0.16f;
        PitchStabilityCurve = new AnimationCurve(
            new Keyframe(0f, 0f),   //no restoring torque near stall
            new Keyframe(15f, 0f),  //ramping up through normal flight speed
            new Keyframe(40f, 20000f), //strong by cruise/dive speed
            new Keyframe(150f, 60000f) //flattened off at high speed, not still climbing
        );
        YawStabilityCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(15f, 0f),
            new Keyframe(40f, 150000f),
            new Keyframe(150f, 250000f)
        );


    }

    void UpdateAeroTorque()
    {

        float speed = LocalVelocity.magnitude;

        float pitchTorque = AngleOfAttack * PitchStabilityCurve.Evaluate(speed);
        float yawTorque = AngleOfAttackYaw * YawStabilityCurve.Evaluate(speed);

        Debug.Log($"AoAYaw:{AngleOfAttackYaw} yawCurveVal:{YawStabilityCurve.Evaluate(speed)} yawTorque:{yawTorque} predictedYawAccelDeg:{(yawTorque / rb.inertiaTensor.y) * Mathf.Rad2Deg}");

        //safety clamp to prevent any single-frame spike (noisy AoA or an extreme angle)
        //from producing a violent, uncontrollable snap
        float maxTorque = 20000f;
        pitchTorque = Mathf.Clamp(pitchTorque, -maxTorque, maxTorque);
        yawTorque = Mathf.Clamp(yawTorque, -maxTorque, maxTorque);

        rb.AddRelativeTorque(new Vector3(pitchTorque, yawTorque, 0f));
        Debug.Log($"speed:{speed} AoA:{AngleOfAttack} pitchCurveVal:{PitchStabilityCurve.Evaluate(speed)} pitchTorque:{pitchTorque}");
    }

   

}
