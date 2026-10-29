using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

// Part I: mono-mode touch navigation using the new Input System (EnhancedTouch API).
// Attach to a "Player" object that has a CharacterController and the Main Camera as a child.
//
// Gestures:
//   One-finger drag   -> look around (body turns left/right, camera tilts up/down)
//   Two-finger pinch  -> move forward (spread) / backward (pinch in)
//   Two-finger drag   -> strafe left/right
//   Double-tap        -> walk to the tapped spot
[RequireComponent(typeof(CharacterController))]
public class TouchNavigation : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;   // Leave empty to use the Main Camera

    [Header("Look (one-finger drag)")]
    public float lookSensitivity = 180f; // Degrees turned per full screen-width drag
    public float maxPitch = 80f;         // Limit so you can't flip upside down

    [Header("Move (two fingers)")]
    public float pinchMoveSpeed = 20f;   // Meters moved per full screen-width of pinch
    public float strafeSpeed = 10f;      // Meters moved per full screen-width drag

    [Header("Walk to tap (double-tap)")]
    public float walkSpeed = 3f;         // Meters per second
    public float stopDistance = 0.5f;    // How close counts as "arrived"

    [Header("Physics")]
    public float gravity = -9.81f;

    const float TapMaxDuration = 0.25f;  // A touch longer than this is a drag, not a tap
    const float DoubleTapWindow = 0.35f; // Max time between the two taps

    CharacterController controller;
    Camera cam;
    float pitch;
    float verticalVelocity;

    bool hasTarget;
    Vector3 target;

    float touchStartTime;
    Vector2 touchStartPos;
    float lastTapTime = -10f;
    Vector2 lastTapPos;

    // EnhancedTouch is off by default; it must be switched on before Touch.activeTouches works.
    void OnEnable()
    {
        EnhancedTouchSupport.Enable();

        // Coming back from VR: clear the leftover head pose so mono mode starts level.
        pitch = 0f;
        if (cameraTransform != null) cameraTransform.localRotation = Quaternion.identity;
    }
    void OnDisable() { EnhancedTouchSupport.Disable(); }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null) cameraTransform = Camera.main.transform;
        cam = cameraTransform.GetComponent<Camera>();
        cameraTransform.localRotation = Quaternion.identity;
    }

    void Update()
    {
        // "move" is this frame's displacement in meters, not a speed.
        Vector3 move = Vector3.zero;
        var touches = Touch.activeTouches;

        if (touches.Count == 1)
            HandleOneFinger(touches[0]);
        else if (touches.Count == 2)
            move += TwoFingerMove(touches[0], touches[1]);

        if (hasTarget)
            move += WalkTowardTarget();

        // Gravity: keep a small downward push while grounded so isGrounded stays reliable.
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity * Time.deltaTime;

        controller.Move(move); // Move() handles collisions with walls and slopes
    }

    void HandleOneFinger(Touch touch)
    {
        // A touch that started on a button or slider belongs to the UI, not to navigation.
        if (IsOverUI(touch.startScreenPosition)) return;

        float scale = 1f / Screen.width; // Same feel on any screen resolution

        switch (touch.phase)
        {
            case TouchPhase.Began:
                hasTarget = false; // Touching the screen cancels walk-to-tap
                touchStartTime = Time.time;
                touchStartPos = touch.screenPosition;
                break;

            case TouchPhase.Moved:
                float yaw = touch.delta.x * scale * lookSensitivity;
                pitch -= touch.delta.y * scale * lookSensitivity;
                pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

                transform.Rotate(0f, yaw, 0f);                                 // Body turns left/right
                cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f); // Head tilts up/down
                break;

            case TouchPhase.Ended:
                bool shortTouch = Time.time - touchStartTime < TapMaxDuration;
                bool barelyMoved = (touch.screenPosition - touchStartPos).magnitude < Screen.width * 0.03f;
                if (shortTouch && barelyMoved) RegisterTap(touch.screenPosition);
                break;
        }
    }

    void RegisterTap(Vector2 pos)
    {
        bool isDoubleTap = Time.time - lastTapTime < DoubleTapWindow &&
                           (pos - lastTapPos).magnitude < Screen.width * 0.05f;

        if (isDoubleTap)
        {
            SetWalkTarget(pos);
            lastTapTime = -10f; // Reset so a third tap doesn't count as another double-tap
        }
        else
        {
            lastTapTime = Time.time;
            lastTapPos = pos;
        }
    }

    void SetWalkTarget(Vector2 screenPos)
    {
        // Shoot a ray from the camera through the tapped pixel into the scene.
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            target = hit.point;
            hasTarget = true;
        }
    }

    Vector3 WalkTowardTarget()
    {
        Vector3 toTarget = target - transform.position;
        toTarget.y = 0f; // Walk on the ground plane; gravity handles height

        if (toTarget.magnitude < stopDistance)
        {
            hasTarget = false;
            return Vector3.zero;
        }
        return toTarget.normalized * walkSpeed * Time.deltaTime;
    }

    static readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    static bool IsOverUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;
        var pointer = new PointerEventData(EventSystem.current) { position = screenPos };
        EventSystem.current.RaycastAll(pointer, uiHits);
        return uiHits.Count > 0;
    }

    Vector3 TwoFingerMove(Touch a, Touch b)
    {
        hasTarget = false;
        float scale = 1f / Screen.width;

        // Pinch: compare finger distance now vs. last frame (position minus delta = last position).
        float prevDistance = ((a.screenPosition - a.delta) - (b.screenPosition - b.delta)).magnitude;
        float currDistance = (a.screenPosition - b.screenPosition).magnitude;
        float pinch = (currDistance - prevDistance) * scale; // Positive = fingers spreading

        // Strafe: average horizontal movement of both fingers.
        float strafe = (a.delta.x + b.delta.x) * 0.5f * scale;

        return transform.forward * pinch * pinchMoveSpeed
             + transform.right * strafe * strafeSpeed;
    }
}
