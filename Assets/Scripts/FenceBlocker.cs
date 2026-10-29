using UnityEngine;

// Makes a fence solid and too tall to step over.
// On start it adds a Box Collider that fits the fence, then stretches it upward
// to at least minHeight meters, so the CharacterController's Step Offset can't climb it.
// Add to any fence piece that has a mesh (works with many selected at once).
[RequireComponent(typeof(MeshFilter))]
public class FenceBlocker : MonoBehaviour
{
    public float minHeight = 2f; // Meters; must be taller than the Player's Step Offset

    void Awake()
    {
        // A new BoxCollider automatically sizes itself to the mesh.
        BoxCollider box = gameObject.AddComponent<BoxCollider>();

        // Box sizes are in local units, so convert using the object's scale.
        float scaleY = transform.lossyScale.y;
        float worldHeight = box.size.y * scaleY;
        if (worldHeight >= minHeight) return;

        float newLocalHeight = minHeight / scaleY;
        Vector3 bottom = box.center - Vector3.up * box.size.y / 2f; // Keep the bottom where it is, grow upward
        box.size = new Vector3(box.size.x, newLocalHeight, box.size.z);
        box.center = bottom + Vector3.up * newLocalHeight / 2f;
    }
}
