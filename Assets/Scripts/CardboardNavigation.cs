using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

// Part II: navigation inside the Cardboard headset.
// Attach to Player (next to TouchNavigation). VrModeManager enables it only in VR mode.
//
// Controls:
//   Head movement               -> look around (done by the Tracked Pose Driver on the camera)
//   Thumb-hole tap              -> start / stop walking where you're looking
//   Thumb-hole double-tap       -> teleport to the spot you're looking at
//   Thumb-hole swipe L/R        -> turn 45 degrees
//   Head tilt (ear to shoulder) -> strafe left/right
//   Hold thumb hole ~3 s        -> recenter (handled in VrModeManager)
//
// Comfort vignette: while moving, the edges of the view darken (narrower effective FOV)
// to reduce motion sickness, then fade back when you stop.
[RequireComponent(typeof(CharacterController))]
public class CardboardNavigation : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;    // Leave empty to use the Main Camera

    [Header("Walk (thumb-hole tap)")]
    public float walkSpeed = 2f;         // Meters per second

    [Header("Turn (thumb-hole swipe)")]
    public float turnAngle = 45f;        // Degrees per swipe

    [Header("Strafe (head tilt)")]
    public float strafeSpeed = 1.5f;     // Meters per second at full tilt (make negative to flip direction)
    public float tiltDeadZone = 10f;     // Degrees of tilt ignored, so normal head wobble doesn't move you
    public float tiltForFullSpeed = 30f; // Degrees of tilt that give full strafe speed

    [Header("Teleport (thumb-hole double-tap)")]
    public float teleportRange = 30f;    // Max distance in meters
    public float maxGroundSlope = 45f;   // Steeper surfaces (walls) are not valid teleport targets

    [Header("Comfort vignette (FOV restriction while moving)")]
    public Volume comfortVolume;         // A Global Volume in the scene
    public float vignetteWhileMoving = 0.45f; // 0 = off, 1 = very dark edges
    public float vignetteFadeSpeed = 1.5f;    // Intensity change per second

    [Header("Physics")]
    public float gravity = -9.81f;

    const float TapMaxDuration = 0.3f;
    const float DoubleTapWindow = 0.3f;  // A single tap waits this long to see if a second tap follows

    CharacterController controller;
    bool isWalking;
    float verticalVelocity;
    float touchStartTime;
    Vector2 touchStartPos;

    bool tapPending;
    float lastTapTime;

    Vignette vignette;
    float vignetteAmount;

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        isWalking = false; // Always enter VR standing still
        tapPending = false;
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        if (vignette != null) vignette.intensity.value = 0f; // No vignette in mono mode
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null) cameraTransform = Camera.main.transform;

        if (comfortVolume != null)
        {
            // .profile gives this Volume its own copy, so the profile asset on disk isn't changed.
            VolumeProfile profile = comfortVolume.profile;
            if (!profile.TryGet(out vignette)) vignette = profile.Add<Vignette>();
            vignette.intensity.overrideState = true;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 1f;   // Soft edge instead of a hard circle
            vignette.intensity.value = 0f;
        }
    }

    void Update()
    {
        var touches = Touch.activeTouches;
        if (touches.Count > 0) HandleTouch(touches[0]);

        // No second tap arrived in time, so it was a single tap.
        if (tapPending && Time.time - lastTapTime > DoubleTapWindow)
        {
            tapPending = false;
            isWalking = !isWalking;
        }

        // Move relative to where the head is looking, flattened so looking down doesn't dig into the ground.
        Vector3 forward = Flatten(cameraTransform.forward);
        Vector3 right = Flatten(cameraTransform.right);
        float strafe = TiltStrafe();

        Vector3 velocity = Vector3.zero;
        if (isWalking) velocity += forward * walkSpeed;
        velocity += right * strafe * strafeSpeed;

        Vector3 move = velocity * Time.deltaTime;

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity * Time.deltaTime;

        controller.Move(move);

        UpdateVignette(isWalking || strafe != 0f);
    }

    void HandleTouch(Touch touch)
    {
        if (touch.phase == TouchPhase.Began)
        {
            touchStartTime = Time.time;
            touchStartPos = touch.screenPosition;
        }
        else if (touch.phase == TouchPhase.Ended)
        {
            Vector2 drag = touch.screenPosition - touchStartPos;
            bool isSwipe = Mathf.Abs(drag.x) > Screen.width * 0.1f && Mathf.Abs(drag.x) > Mathf.Abs(drag.y);
            bool isTap = Time.time - touchStartTime < TapMaxDuration && drag.magnitude < Screen.width * 0.03f;

            if (isSwipe)
            {
                transform.Rotate(0f, Mathf.Sign(drag.x) * turnAngle, 0f); // Swipe right = turn right
                vignetteAmount = vignetteWhileMoving;                      // Snap turns are jarring, so pulse the vignette
            }
            else if (isTap)
            {
                if (tapPending)
                {
                    tapPending = false;
                    TeleportToGaze();
                }
                else
                {
                    tapPending = true;
                    lastTapTime = Time.time;
                }
            }
            // A long press is neither, so holding to recenter doesn't toggle walking.
        }
    }

    void TeleportToGaze()
    {
        // Ray from the center of the view, straight ahead.
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, teleportRange)) return;
        if (Vector3.Angle(hit.normal, Vector3.up) > maxGroundSlope) return; // Looking at a wall, not the ground

        // A CharacterController overrides direct position changes, so switch it off while moving the Player.
        controller.enabled = false;
        transform.position = hit.point;
        controller.enabled = true;

        isWalking = false;
        verticalVelocity = 0f;
        vignetteAmount = vignetteWhileMoving; // Brief vignette softens the jump
    }

    void UpdateVignette(bool moving)
    {
        if (vignette == null) return;
        float target = moving ? vignetteWhileMoving : 0f;
        vignetteAmount = Mathf.MoveTowards(vignetteAmount, target, vignetteFadeSpeed * Time.deltaTime);
        vignette.intensity.value = vignetteAmount;
    }

    // Returns -1..1: how hard the head is tilted sideways (0 inside the dead zone).
    float TiltStrafe()
    {
        Vector3 angles = cameraTransform.localEulerAngles;
        float pitch = Mathf.DeltaAngle(0f, angles.x); // Convert 0..360 to -180..180
        float roll = Mathf.DeltaAngle(0f, angles.z);

        // Roll readings get unreliable when looking almost straight up or down.
        if (Mathf.Abs(pitch) > 60f) return 0f;

        float amount = Mathf.InverseLerp(tiltDeadZone, tiltForFullSpeed, Mathf.Abs(roll));
        return -Mathf.Sign(roll) * amount; // Positive roll = tilting left, so move left
    }

    static Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;
        return v.normalized;
    }
}
