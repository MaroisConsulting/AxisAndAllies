using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simplified aerodynamic flight using Rigidbody forces. Local +Z is the nose,
/// +Y is up and +X is the right wing. Distances are metres and mass is kilograms.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class FighterPlane : MonoBehaviour
{
    [Header("Engine")]
    [Tooltip("Forward thrust in newtons while the engine is on. Press Space to toggle it.")]
    [Min(0f)] public float engineThrust = 18000f;
    [Tooltip("Maximum plane speed in metres per second, including during dives.")]
    [Min(1f)] public float TopSpeed = 80f;
    [Tooltip("Front propeller object, rotated around its local Z axis.")]
    public GameObject propeller;
    [Tooltip("Clockwise rotation in degrees per second for each metre per second of plane speed.")]
    [Min(0f)] public float propellerSpinPerSpeed = 60f;
    [Tooltip("Seconds for the propeller to gradually stop after the engine turns off. Zero stops it immediately.")]
    [Min(0f)] public float propellerStopDuration = 3f;

    [Header("Aerodynamics")]
    [Min(0.01f)] public float wingArea = 16f;
    [Min(0.01f)] public float airDensity = 1.225f;
    [Tooltip("Positive lift at level attitude lets the plane lift off a flat runway.")]
    [Range(0f, 1.5f)] public float levelLiftCoefficient = 0.8f;
    [Tooltip("Change in lift coefficient per radian of angle of attack.")]
    [Min(0f)] public float liftSlope = 4f;
    [Min(0.01f)] public float maximumLiftCoefficient = 1.5f;
    [Range(5f, 40f)] public float stallAngle = 18f;
    [Min(0.001f)] public float parasiticDrag = 0.025f;
    [Min(0f)] public float inducedDrag = 0.045f;
    [Min(0f)] public float stalledDrag = 0.8f;
    [Tooltip("Resistance to sideways motion, creating coordinated banked turns.")]
    [Min(0f)] public float sidewaysDrag = 1f;
    [Tooltip("World-space wind velocity in metres per second.")]
    public Vector3 windVelocity;

    [Header("Controls")]
    [Tooltip("Airspeed at which the control surfaces have full authority.")]
    [Min(1f)] public float fullControlSpeed = 35f;
    [Min(0f)] public float pitchAcceleration = 1.5f;
    [Min(0f)] public float rollAcceleration = 3f;
    [Min(0f)] public float yawAcceleration = 1.5f;
    [Tooltip("Aerodynamic resistance to angular motion.")]
    [Min(0f)] public float angularDamping = 2f;
    [Tooltip("Weathercock stability: the nose tends to follow the airflow.")]
    [Min(0f)] public float noseStability = 0.8f;

    [Header("Wing flaps")]
    public GameObject WingFlapLeft;
    public GameObject WingFlapRight;
    [Tooltip("Local X rotation in degrees at full roll input. Negate to reverse both flaps.")]
    [Range(-45f, 45f)] public float wingFlapAngle = 20f;
    [Tooltip("Local X deflection for S (pull up); W deflects the opposite way. Negate to reverse.")]
    [Range(-45f, 45f)] public float wingFlapPitchAngle = 20f;
    [Tooltip("Time in seconds for flaps to smoothly approach their target angle.")]
    [Min(0.01f)] public float wingFlapSmoothTime = 0.15f;

    [Header("Tail flaps")]
    public GameObject TailFlapLeft;
    public GameObject TailFlapRight;

    [Header("Mouse rudder")]
    public GameObject TailRudder;
    [Tooltip("Change in steering per pixel of horizontal mouse movement.")]
    [Min(0f)] public float mouseSensitivity = 0.002f;
    [Tooltip("Local Z deflection at full steering input. Negate to reverse the animation.")]
    [Range(-45f, 45f)] public float rudderAngle = 20f;
    [Min(0.01f)] public float rudderSmoothTime = 0.15f;
    [Tooltip("Seconds to hold steering after the last horizontal mouse movement before returning to neutral.")]
    [Min(0f)] public float rudderHoldDuration = 3f;

    [Header("Follow camera")]
    [Tooltip("Camera to follow the plane with a level horizon.")]
    public Camera camera;
    [Min(1f)] public float cameraDistance = 15f;
    [Min(0f)] public float cameraHeight = 5f;
    [Min(0.01f)] public float cameraSmoothTime = 0.5f;
    [Tooltip("Maximum camera heading change in degrees per second.")]
    [Min(1f)] public float cameraTurnSpeed = 60f;

    [Header("Landing gear")]
    public GameObject LeftWheel;
    public GameObject LeftWheelDoor;
    public GameObject RightWheel;
    public GameObject RightWheelDoor;
    [Tooltip("Deploy gear when ground is within this many metres below the plane's origin.")]
    [Min(0.1f)] public float gearDeployDistance = 10f;
    [Tooltip("Extra clearance before retracting, to prevent toggling near the deployment distance.")]
    [Min(0f)] public float gearRetractClearance = 2f;
    public LayerMask landingGroundLayers = ~0;
    [Tooltip("Retraction around local Z relative to the original open pose.")]
    public float leftGearRetractAngle = -90f;
    [Tooltip("Retraction around local Z relative to the original open pose.")]
    public float rightGearRetractAngle = 90f;
    [Tooltip("Landing gear rotation speed in degrees per second.")]
    [Min(1f)] public float gearRotationSpeed = 90f;

    [Header("Ground sliding")]
    [Tooltip("Apply a low-friction material to this Rigidbody's colliders at runtime.")]
    public bool useSlidingMaterial = true;
    [Range(0f, 1f)] public float groundFriction = 0.02f;
    [Tooltip("Ground braking deceleration in metres per second squared while holding W.")]
    [Min(0f)] public float groundBrakeAcceleration = 25f;

    public float Airspeed { get; private set; }
    public float AngleOfAttack { get; private set; }

    private Rigidbody body;
    private float throttle;
    private float propellerSpinRate;
    private float propellerShutdownSpinRate;
    private float propellerStopTimeRemaining;
    private float pitchInput;
    private float rollInput;
    private float yawInput;
    private float targetYawInput;
    private float yawInputVelocity;
    private float rudderHoldTimeRemaining;
    private Quaternion rudderRestRotation;
    private Quaternion leftFlapRestRotation;
    private Quaternion rightFlapRestRotation;
    private float currentFlapAngle;
    private float flapAngleVelocity;
    private float currentFlapPitchAngle;
    private float flapPitchAngleVelocity;
    private Quaternion leftTailFlapRestRotation;
    private Quaternion rightTailFlapRestRotation;
    private Camera activeFollowCamera;
    private Vector3 cameraPositionVelocity;
    private float cameraHeading;
    private float cameraHeadingVelocity;
    private float cameraPitchVelocity;
    private float cameraYawVelocity;
    private PhysicsMaterial slidingMaterial;
    private Collider[] slidingColliders;
    private PhysicsMaterial[] originalMaterials;
    private Quaternion leftWheelOpenRotation;
    private Quaternion leftDoorOpenRotation;
    private Quaternion rightWheelOpenRotation;
    private Quaternion rightDoorOpenRotation;
    private bool gearDeployed = true;
    private float leftGearAngle;
    private float rightGearAngle;
    private GUIStyle statusPanelStyle;
    private bool brakeInput;
    private readonly HashSet<Collider> groundContacts = new HashSet<Collider>();

    private void Reset()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.mass = 1000f;
        rb.useGravity = true;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        leftWheelOpenRotation = LeftWheel != null ? LeftWheel.transform.localRotation : Quaternion.identity;
        leftDoorOpenRotation = LeftWheelDoor != null ? LeftWheelDoor.transform.localRotation : Quaternion.identity;
        rightWheelOpenRotation = RightWheel != null ? RightWheel.transform.localRotation : Quaternion.identity;
        rightDoorOpenRotation = RightWheelDoor != null ? RightWheelDoor.transform.localRotation : Quaternion.identity;
        if (TailRudder != null)
            rudderRestRotation = TailRudder.transform.localRotation;
        if (WingFlapLeft != null)
            leftFlapRestRotation = WingFlapLeft.transform.localRotation;
        if (WingFlapRight != null)
            rightFlapRestRotation = WingFlapRight.transform.localRotation;
        if (TailFlapLeft != null)
            leftTailFlapRestRotation = TailFlapLeft.transform.localRotation;
        if (TailFlapRight != null)
            rightTailFlapRestRotation = TailFlapRight.transform.localRotation;
        body.useGravity = true;

        // Air resistance is calculated below rather than with uniform damping.
        body.linearDamping = 0f;
        body.angularDamping = 0f;

        if (!useSlidingMaterial)
            return;

        slidingMaterial = new PhysicsMaterial("Fighter plane sliding")
        {
            staticFriction = groundFriction,
            dynamicFriction = groundFriction,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };

        slidingColliders = GetComponentsInChildren<Collider>();
        originalMaterials = new PhysicsMaterial[slidingColliders.Length];
        for (int i = 0; i < slidingColliders.Length; i++)
        {
            Collider planeCollider = slidingColliders[i];
            if (planeCollider.attachedRigidbody != body || planeCollider.isTrigger)
                continue;

            originalMaterials[i] = planeCollider.sharedMaterial;
            planeCollider.sharedMaterial = slidingMaterial;
        }
    }

    private void Start()
    {
        SetCursorLocked(true);
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool justLocked = false;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            SetCursorLocked(false);
        else if (mouse != null && (mouse.leftButton.wasPressedThisFrame
            || mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame))
        {
            justLocked = Cursor.lockState != CursorLockMode.Locked;
            SetCursorLocked(true);
        }

        float mouseX = mouse != null ? mouse.delta.ReadValue().x : 0f;
        if (mouse != null && Cursor.lockState == CursorLockMode.Locked
            && Application.isFocused && !justLocked && Mathf.Abs(mouseX) > 0.01f)
        {
            // Accumulate steering while moving; reverse movement adjusts the held turn.
            targetYawInput = Mathf.Clamp(targetYawInput + mouseX * mouseSensitivity, -1f, 1f);
            rudderHoldTimeRemaining = Mathf.Max(0f, rudderHoldDuration);
        }
        else
        {
            // Releasing the cursor or losing focus cancels steering immediately.
            if (mouse == null || Cursor.lockState != CursorLockMode.Locked || !Application.isFocused)
                rudderHoldTimeRemaining = 0f;
            else
                rudderHoldTimeRemaining = Mathf.Max(0f, rudderHoldTimeRemaining - Time.deltaTime);

            if (rudderHoldTimeRemaining <= 0f)
                targetYawInput = 0f;
        }

        yawInput = Mathf.SmoothDamp(yawInput, targetYawInput, ref yawInputVelocity,
            rudderSmoothTime, Mathf.Infinity, Time.deltaTime);
        UpdateRudder();
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
        {
            throttle = throttle > 0f ? 0f : 1f;
            if (throttle == 0f)
            {
                propellerShutdownSpinRate = propellerSpinRate;
                propellerStopTimeRemaining = Mathf.Max(0f, propellerStopDuration);
            }
        }
        brakeInput = keyboard != null && keyboard.wKey.isPressed;
        pitchInput = keyboard == null ? 0f :
            (brakeInput && groundContacts.Count == 0 ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
        rollInput = keyboard == null ? 0f :
            (keyboard.aKey.isPressed ? 1f : 0f) - (keyboard.dKey.isPressed ? 1f : 0f);
        UpdateWingFlaps();
        UpdateTailFlaps();

        float gearStep = Mathf.Max(1f, gearRotationSpeed) * Time.deltaTime;
        leftGearAngle = Mathf.MoveTowards(leftGearAngle, gearDeployed ? 0f : leftGearRetractAngle, gearStep);
        rightGearAngle = Mathf.MoveTowards(rightGearAngle, gearDeployed ? 0f : rightGearRetractAngle, gearStep);
        ApplyGearRotation(LeftWheel, leftWheelOpenRotation, Vector3.forward, leftGearAngle);
        ApplyGearRotation(LeftWheelDoor, leftDoorOpenRotation, Vector3.forward, leftGearAngle);
        ApplyGearRotation(RightWheel, rightWheelOpenRotation, Vector3.forward, rightGearAngle);
        ApplyGearRotation(RightWheelDoor, rightDoorOpenRotation, Vector3.forward, rightGearAngle);

        if (throttle > 0f)
        {
            propellerSpinRate = body.linearVelocity.magnitude * propellerSpinPerSpeed;
        }
        else
        {
            propellerStopTimeRemaining = Mathf.Max(0f, propellerStopTimeRemaining - Time.deltaTime);
            propellerSpinRate = propellerStopDuration > 0f
                ? propellerShutdownSpinRate * Mathf.Clamp01(propellerStopTimeRemaining / propellerStopDuration)
                : 0f;
        }

        if (propeller != null)
            propeller.transform.Rotate(0f, 0f, -propellerSpinRate * Time.deltaTime, Space.Self);
    }

    private void OnGUI()
    {
        if (statusPanelStyle == null)
        {
            statusPanelStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            statusPanelStyle.normal.textColor = Color.white;
        }

        const float panelHeight = 112f;
        const float bottomMargin = 24f;
        float panelWidth = Mathf.Min(280f, Screen.width);
        Rect panelRect = new Rect((Screen.width - panelWidth) * 0.5f,
            Screen.height - panelHeight - bottomMargin, panelWidth, panelHeight);
        string engineStatus = throttle > 0f ? "ON" : "OFF";
        string gearStatus = gearDeployed ? "DOWN" : "UP";
        GUI.Box(panelRect, "Engine: " + engineStatus + "\nLanding gear: " + gearStatus
            + "\nSpeed: " + (body.linearVelocity.magnitude * 3.6f).ToString("F1") + " km/h"
            + "\nAltitude: " + transform.position.y.ToString("F1") + " m",
            statusPanelStyle);
    }

    private void OnCollisionEnter(Collision collision)
    {
        UpdateGroundContact(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        UpdateGroundContact(collision);
    }

    private void UpdateGroundContact(Collision collision)
    {
        Collider other = collision.collider;
        if ((landingGroundLayers.value & (1 << other.gameObject.layer)) != 0
            && other.attachedRigidbody != body && !other.transform.IsChildOf(transform))
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                // Supporting surfaces count as ground; walls do not.
                if (Vector3.Dot(collision.GetContact(i).normal, Vector3.up) > 0.5f)
                {
                    groundContacts.Add(other);
                    return;
                }
            }
        }
        groundContacts.Remove(other);
    }

    private void OnCollisionExit(Collision collision)
    {
        groundContacts.Remove(collision.collider);
    }

    private static void ApplyGearRotation(GameObject gear, Quaternion openRotation, Vector3 localAxis, float angle)
    {
        if (gear != null)
            gear.transform.localRotation = openRotation * Quaternion.AngleAxis(-angle, localAxis);
    }

    private void CheckLandingGround()
    {
        float checkDistance = Mathf.Max(0.1f, gearDeployDistance)
            + (gearDeployed ? Mathf.Max(0f, gearRetractClearance) : 0f);
        RaycastHit[] hits = Physics.RaycastAll(transform.position, Vector3.down,
            checkDistance, landingGroundLayers, QueryTriggerInteraction.Ignore);
        gearDeployed = false;
        foreach (RaycastHit hit in hits)
        {
            // Ignore every collider belonging to this plane, including the moving gear.
            if (hit.collider.attachedRigidbody == body || hit.collider.transform.IsChildOf(transform))
                continue;
            gearDeployed = true;
            break;
        }
    }

    private void UpdateRudder()
    {
        if (TailRudder != null)
            TailRudder.transform.localRotation = rudderRestRotation
                * Quaternion.Euler(0f, 0f, yawInput * rudderAngle);
    }

    private void UpdateWingFlaps()
    {
        // Deflect around each flap's local X axis relative to its authored pose.
        float angle = Mathf.SmoothDamp(currentFlapAngle, -rollInput * wingFlapAngle,
            ref flapAngleVelocity, wingFlapSmoothTime, Mathf.Infinity, Time.deltaTime);
        currentFlapAngle = angle;
        currentFlapPitchAngle = Mathf.SmoothDamp(currentFlapPitchAngle,
            -pitchInput * wingFlapPitchAngle, ref flapPitchAngleVelocity,
            wingFlapSmoothTime, Mathf.Infinity, Time.deltaTime);
        if (WingFlapLeft != null)
            WingFlapLeft.transform.localRotation = leftFlapRestRotation
                * Quaternion.Euler(currentFlapPitchAngle + angle, 0f, 0f);
        if (WingFlapRight != null)
            WingFlapRight.transform.localRotation = rightFlapRestRotation
                * Quaternion.Euler(currentFlapPitchAngle - angle, 0f, 0f);
    }

    private void UpdateTailFlaps()
    {
        if (TailFlapLeft != null)
            TailFlapLeft.transform.localRotation = leftTailFlapRestRotation
                * Quaternion.Euler(currentFlapPitchAngle + currentFlapAngle, 0f, 0f);
        if (TailFlapRight != null)
            TailFlapRight.transform.localRotation = rightTailFlapRestRotation
                * Quaternion.Euler(currentFlapPitchAngle - currentFlapAngle, 0f, 0f);
    }

    private void LateUpdate()
    {
        if (camera == null)
        {
            activeFollowCamera = null;
            return;
        }

        Transform cameraTransform = camera.transform;
        if (activeFollowCamera != camera)
        {
            activeFollowCamera = camera;
            // A child camera would otherwise inherit every roll and flip.
            if (cameraTransform.IsChildOf(transform))
                cameraTransform.SetParent(null, true);
            cameraHeading = cameraTransform.eulerAngles.y;
            cameraPositionVelocity = Vector3.zero;
            cameraHeadingVelocity = cameraPitchVelocity = cameraYawVelocity = 0f;
        }

        // Follow the travel direction, keeping the last heading during vertical flight.
        Vector3 heading = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
        if (body.linearVelocity.sqrMagnitude < 1f)
            heading = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (heading.sqrMagnitude > 0.01f)
        {
            float targetHeading = Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg;
            cameraHeading = Mathf.SmoothDampAngle(cameraHeading, targetHeading,
                ref cameraHeadingVelocity, cameraSmoothTime, cameraTurnSpeed, Time.deltaTime);
        }

        Vector3 followDirection = Quaternion.Euler(0f, cameraHeading, 0f) * Vector3.forward;
        Vector3 targetPosition = transform.position - followDirection * cameraDistance
            + Vector3.up * cameraHeight;
        cameraTransform.position = Vector3.SmoothDamp(cameraTransform.position, targetPosition,
            ref cameraPositionVelocity, cameraSmoothTime, Mathf.Infinity, Time.deltaTime);

        Vector3 lookDirection = transform.position - cameraTransform.position;
        if (lookDirection.sqrMagnitude < 0.001f)
            return;

        // Smooth pitch and yaw independently so the horizon never banks.
        float horizontalDistance = new Vector2(lookDirection.x, lookDirection.z).magnitude;
        float targetPitch = -Mathf.Atan2(lookDirection.y, horizontalDistance) * Mathf.Rad2Deg;
        float targetYaw = horizontalDistance > 0.01f
            ? Mathf.Atan2(lookDirection.x, lookDirection.z) * Mathf.Rad2Deg
            : cameraTransform.eulerAngles.y;
        float pitch = Mathf.SmoothDampAngle(cameraTransform.eulerAngles.x,
            Mathf.Clamp(targetPitch, -80f, 80f), ref cameraPitchVelocity,
            cameraSmoothTime, cameraTurnSpeed, Time.deltaTime);
        float yaw = Mathf.SmoothDampAngle(cameraTransform.eulerAngles.y, targetYaw,
            ref cameraYawVelocity, cameraSmoothTime, cameraTurnSpeed, Time.deltaTime);
        cameraTransform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void FixedUpdate()
    {
        CheckLandingGround();
        float speedLimit = Mathf.Max(1f, TopSpeed);
        // Preserve the direction of travel while limiting total world-space speed.
        body.linearVelocity = Vector3.ClampMagnitude(body.linearVelocity, speedLimit);

        // Reduce thrust near the limit to avoid repeatedly accelerating past it.
        float remainingSpeed = Mathf.Max(0f, speedLimit - body.linearVelocity.magnitude);
        float availableThrust = remainingSpeed * body.mass / Time.fixedDeltaTime;
        float thrust = Mathf.Min(throttle * engineThrust, availableThrust);
        body.AddForce(transform.forward * thrust, ForceMode.Force);

        groundContacts.RemoveWhere(contact => contact == null || !contact.enabled
            || !contact.gameObject.activeInHierarchy);
        if (brakeInput && groundContacts.Count > 0)
        {
            // Cancel horizontal motion and this step's thrust without reversing or affecting gravity.
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
            Vector3 horizontalThrust = Vector3.ProjectOnPlane(transform.forward * (thrust / body.mass), Vector3.up);
            Vector3 brakeAcceleration = -horizontalVelocity / Time.fixedDeltaTime - horizontalThrust;
            body.AddForce(Vector3.ClampMagnitude(brakeAcceleration, Mathf.Max(0f, groundBrakeAcceleration)),
                ForceMode.Acceleration);
        }

        Vector3 airVelocity = body.linearVelocity - windVelocity;
        Vector3 localVelocity = transform.InverseTransformDirection(airVelocity);
        Airspeed = airVelocity.magnitude;
        AngleOfAttack = Mathf.Atan2(-localVelocity.y, localVelocity.z) * Mathf.Rad2Deg;

        if (Airspeed < 0.1f)
            return;

        // Wing lift depends on airflow in the wing's forward/up plane, not sideslip.
        float wingSpeedSquared = localVelocity.y * localVelocity.y + localVelocity.z * localVelocity.z;
        float pressureArea = 0.5f * airDensity * wingArea * wingSpeedSquared;
        float liftCoefficient = Mathf.Clamp(
            levelLiftCoefficient + liftSlope * AngleOfAttack * Mathf.Deg2Rad,
            -maximumLiftCoefficient, maximumLiftCoefficient);

        // Beyond the stall angle, progressively lose lift and gain drag.
        float stall = Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(stallAngle, stallAngle + 20f, Mathf.Abs(AngleOfAttack)));
        liftCoefficient *= 1f - stall;
        if (localVelocity.z <= 0f)
            liftCoefficient = 0f;

        Vector3 wingVelocity = airVelocity - transform.right * localVelocity.x;
        Vector3 liftDirection = Vector3.Cross(wingVelocity.normalized, transform.right);
        body.AddForce(liftDirection * (pressureArea * liftCoefficient), ForceMode.Force);

        float dragCoefficient = parasiticDrag + inducedDrag * liftCoefficient * liftCoefficient
            + stalledDrag * stall;
        float dragForce = 0.5f * airDensity * wingArea * Airspeed * Airspeed * dragCoefficient;
        body.AddForce(-airVelocity.normalized * dragForce, ForceMode.Force);
        body.AddForce(-transform.right * (0.5f * airDensity * wingArea * sidewaysDrag
            * localVelocity.x * Mathf.Abs(localVelocity.x)), ForceMode.Force);

        float authority = Mathf.Clamp01(wingSpeedSquared / (fullControlSpeed * fullControlSpeed));
        Vector3 localAngularVelocity = transform.InverseTransformDirection(body.angularVelocity);
        Vector3 controlTorque = new Vector3(pitchInput * pitchAcceleration, yawInput * yawAcceleration,
            rollInput * rollAcceleration);
        body.AddRelativeTorque((controlTorque - localAngularVelocity * angularDamping) * authority,
            ForceMode.Acceleration);

        // Gentle pitch/yaw stability, with no automatic levelling of the bank.
        body.AddTorque(Vector3.Cross(transform.forward, airVelocity.normalized)
            * (noseStability * authority), ForceMode.Acceleration);
    }

    private void OnDisable()
    {
        brakeInput = false;
        groundContacts.Clear();
        throttle = pitchInput = rollInput = 0f;
        yawInput = targetYawInput = yawInputVelocity = 0f;
        rudderHoldTimeRemaining = 0f;
        UpdateRudder();
        SetCursorLocked(false);
        currentFlapAngle = flapAngleVelocity = 0f;
        currentFlapPitchAngle = flapPitchAngleVelocity = 0f;
        UpdateWingFlaps();
        UpdateTailFlaps();
        propellerSpinRate = propellerShutdownSpinRate = propellerStopTimeRemaining = 0f;
    }

    private void OnDestroy()
    {
        if (slidingMaterial == null)
            return;

        for (int i = 0; i < slidingColliders.Length; i++)
        {
            if (slidingColliders[i] != null && slidingColliders[i].sharedMaterial == slidingMaterial)
                slidingColliders[i].sharedMaterial = originalMaterials[i];
        }
        Destroy(slidingMaterial);
    }
}
