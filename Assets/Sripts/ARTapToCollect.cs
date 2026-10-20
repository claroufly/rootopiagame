using UnityEngine;

// Put this on the AR Camera (or any persistent object in the AR scene).
// Detects taps on AR-spawned collectible items and triggers pickup.
public class ARTapToCollect : MonoBehaviour
{
    public Camera arCamera;

    void Update()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase != TouchPhase.Began) return;

        Ray ray = arCamera.ScreenPointToRay(touch.position);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // GetComponentInParent in case the Collider sits on a child mesh
            // rather than the root of the prefab.
            ARCollectibleItem collectible = hit.collider.GetComponentInParent<ARCollectibleItem>();

            if (collectible != null)
            {
                collectible.Collect();
            }
        }
    }
}