using UnityEngine;

// Attach this to the AR-spawned collectible prefab (the same GameObject you
// pass into AutoPlaceOnPlane.objectPrefab), and assign the ItemData it
// represents in the Inspector. The prefab also needs a Collider on it (or a
// child) so ARTapToCollect's raycast can hit it.
public class ARCollectibleItem : MonoBehaviour
{
    [Tooltip("The ItemData this AR object represents.")]
    public ItemData itemData;

    private bool collected = false;

    public void Collect()
    {
        if (collected) return;

        if (itemData == null)
        {
            Debug.LogWarning("[ARCollectibleItem] No ItemData assigned, can't add to inventory.");
            return;
        }

        bool added = InventoryManager.Instance.AddItem(itemData);

        if (added)
        {
            collected = true;
            Debug.Log($"[ARCollectibleItem] Collected {itemData.name}");
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("[ARCollectibleItem] Inventory full — item stays in AR scene.");
        }
    }
}