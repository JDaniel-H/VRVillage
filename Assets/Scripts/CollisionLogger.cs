using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

// Debug helper: logs which object blocks the Player, so we can find what's causing "getting stuck".
// Attach to Player. Remove or disable it once the problem is found.
public class CollisionLogger : MonoBehaviour
{
    Collider lastLogged;

    void Start()
    {
#if UNITY_EDITOR
        // Lets the mouse act as a finger in the editor's Game view (one finger only),
        // so we can test without building to the phone.
        TouchSimulation.Enable();
#endif
    }

    // Called by the CharacterController whenever it bumps into a collider during Move().
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y > 0.7f) return;          // Mostly-flat surface under us = normal ground, ignore
        if (hit.collider == lastLogged) return;   // Don't spam the same object every frame
        lastLogged = hit.collider;

        // Passing the collider as the second argument makes clicking the log line highlight it in the Hierarchy.
        Debug.Log($"Blocked by: {hit.collider.name} ({hit.collider.GetType().Name}), " +
                  $"surface angle {Vector3.Angle(hit.normal, Vector3.up):F0} deg, " +
                  $"height above feet {hit.point.y - transform.position.y:F2} m", hit.collider);
    }
}
