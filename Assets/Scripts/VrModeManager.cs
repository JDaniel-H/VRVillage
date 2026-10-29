using System.Collections;
using Google.XR.Cardboard;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Management;

// Switches between mono mode (Part I touch controls) and Cardboard VR mode (Part II).
// Also handles the Cardboard housekeeping: close (X), settings (gear), recenter, screen updates.
public class VrModeManager : MonoBehaviour
{
    [Header("References")]
    public TouchNavigation monoNavigation;    // Part I script on Player
    public CardboardNavigation vrNavigation;  // Part II script on Player
    public TrackedPoseDriver headTracker;     // On Main Camera, applies head rotation in VR
    public GameObject enterVrButton;          // On-screen button shown in mono mode

    Camera cam;
    float defaultFieldOfView;
    bool isVrMode;

    void Start()
    {
        cam = Camera.main;
        Screen.sleepTimeout = SleepTimeout.NeverSleep; // Don't let the screen dim inside the headset
        SetMode(false); // App always starts in mono mode
    }

    // Called by the Enter VR button's OnClick.
    public void EnterVR()
    {
        defaultFieldOfView = cam.fieldOfView; // Remember the mono FOV (it may have been changed by the slider)
        StartCoroutine(StartXR());
    }

    // Starting XR takes more than one frame, so it runs as a coroutine.
    IEnumerator StartXR()
    {
        var manager = XRGeneralSettings.Instance.Manager;
        yield return manager.InitializeLoader();

        if (manager.activeLoader == null)
        {
            Debug.LogError("Cardboard XR failed to initialize.");
            yield break;
        }

        manager.StartSubsystems();
        if (!Api.HasDeviceParams()) Api.ScanDeviceParams(); // First time: scan the headset QR code
        SetMode(true);
    }

    void ExitVR()
    {
        var manager = XRGeneralSettings.Instance.Manager;
        manager.StopSubsystems();
        manager.DeinitializeLoader();

        // XR changes the camera's projection; restore the normal single-screen view.
        cam.ResetAspect();
        cam.fieldOfView = defaultFieldOfView;
        SetMode(false);
    }

    // Only one controller may drive the camera at a time, so each mode turns the other off.
    void SetMode(bool vr)
    {
        isVrMode = vr;
        headTracker.enabled = vr;
        monoNavigation.enabled = !vr;
        vrNavigation.enabled = vr;
        enterVrButton.SetActive(!vr);
    }

    void Update()
    {
        if (!isVrMode) return;

        if (Api.IsCloseButtonPressed)
        {
            ExitVR();
            return;
        }
        if (Api.IsGearButtonPressed) Api.ScanDeviceParams();   // Re-scan headset QR code
        if (Api.IsTriggerHeldPressed) Api.Recenter();          // Hold thumb hole ~3 s
        if (Api.HasNewDeviceParams()) Api.ReloadDeviceParams();
        Api.UpdateScreenParams();
    }
}
