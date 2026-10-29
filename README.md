# VR Village: Google Cardboard VR App

Assignment 2 for *Introduction to Virtual/Augmented Reality and Telepresence*: a mobile VR app that lets you explore a low-poly medieval village on an Android phone, both in normal (mono) mode with touch gestures and in stereo VR with a Google Cardboard headset.

**Download the app (APK):** [Release v1.0](https://github.com/JDaniel-H/VRVillage/releases/tag/v1.0)

## Features

### Part I: Mono mode (touch navigation)
The app starts in mono mode.

| Gesture | Action |
|---|---|
| One-finger drag | Look around |
| Pinch | Move forward / backward |
| Two-finger drag | Strafe left / right |
| Double-tap | Walk to the tapped spot |
| Slider (top-left) | Adjust the field of view |

### Part II: Cardboard VR mode
Tap **Enter VR**, then place the phone in the Cardboard headset.

| Input | Action |
|---|---|
| Head movement | Look around |
| Thumb-hole tap | Start / stop walking in the direction you're looking |
| Thumb-hole double-tap | Teleport to the aiming dot |
| Thumb-hole swipe left / right | Turn 45° |
| Tilt head left / right | Strafe |
| Hold thumb hole ~3 s | Recenter the view |
| X button | Return to mono mode |
| Gear button | Scan the headset's QR code |

### Additional features
- **Field of view:** adjustable FOV slider in mono mode, and a comfort vignette in VR that narrows the view while moving or turning to reduce motion sickness.
- **Navigation:** gaze teleport in VR, and collisions with buildings, fences, bridges and terrain in both modes.

## Installing the APK
1. Download `VRVillage.apk` from the [release page](https://github.com/JDaniel-H/VRVillage/releases/tag/v1.0) on an Android phone (Android 8.0 or newer).
2. Open it and allow installing apps from that source.
3. On first entering VR, scan the QR code on your Cardboard viewer when asked.

## Opening the project in Unity
**Requirements**
- Unity **6000.6.4f1** with **Android Build Support** (OpenJDK, Android SDK & NDK)
- **Git** installed and on the system PATH (the Google Cardboard XR Plugin is installed from its GitHub repository)

**Steps**
1. Clone the repository or use **Code → Download ZIP**.
2. In Unity Hub: **Add → Add project from disk**, and select the folder. The first open takes a while.
3. Open the scene `Assets/Low-Poly Medieval Village/Scenes/SampleScene`.
4. Connect an Android phone with USB debugging enabled, then **File → Build Profiles → Build And Run**.

Note: touch gestures and VR mode only work on a phone, not in the editor's Play mode.

## Scripts (`Assets/Scripts`)
| Script | Purpose |
|---|---|
| `TouchNavigation.cs` | Mono-mode touch gestures (Input System, EnhancedTouch) |
| `CardboardNavigation.cs` | VR controls: walking, turning, head-tilt strafe, teleport, comfort vignette |
| `VrModeManager.cs` | Switches between mono and VR mode, handles Cardboard buttons and recentering |
| `FenceBlocker.cs` | Gives fences tall invisible colliders so they can't be stepped over |
| `CollisionLogger.cs` | Debug tool that logs what blocks the player (disabled in the build) |

## Known limitations
- The test phone (Redmi 14C) has no gyroscope, so Android estimates head orientation from the accelerometer and compass, making head tracking slower and less precise. Phones with a gyroscope track smoothly.

## Credits
- **Low Poly Medieval Village** asset package (provided with the assignment)
- **[Google Cardboard XR Plugin for Unity](https://github.com/googlevr/cardboard-xr-plugin)**

## AI assistance
This project was developed with the help of **Claude**, an AI assistant by Anthropic.
