using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private Land selectedLand;
    private InteractableObject selectedInteractable = null;
    private Chicken selectedLivestock = null; // currently in-range chicken (or other livestock later)

    [SerializeField] private LayerMask landLayer;
    [SerializeField] private float rayDistance = 2f;

    void Update()
    {
        RaycastHit hit;

        if (Physics.Raycast(transform.position, Vector3.down, out hit, rayDistance, landLayer))
        {
            Land land = hit.collider.GetComponent<Land>();

            if (land != null && land != selectedLand)
            {
                SelectLand(land);
            }
        }
        else
        {
            DeselectLand();
        }

        // TEMP TEST
        if (Input.touchCount > 0)
        {
            Debug.Log("Touch detected");
            Interact();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[PlayerInteraction] OnTriggerEnter by: {other.name}, tag: {other.tag}");

        if (other.CompareTag("Item"))
        {
            selectedInteractable = other.GetComponent<InteractableObject>();
            Debug.Log($"[PlayerInteraction] selectedInteractable set: {(selectedInteractable != null ? selectedInteractable.name : "null")}");
        }
        else if (other.CompareTag("Livestock"))
        {
            selectedLivestock = other.GetComponent<Chicken>();
            Debug.Log($"[PlayerInteraction] selectedLivestock set: {(selectedLivestock != null ? selectedLivestock.name : "null")}");

            // Auto-open the popup as soon as the chicken is in range
            LivestockInteract();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"[PlayerInteraction] OnTriggerExit by: {other.name}, tag: {other.tag}");

        // Only clear if the object leaving is the one we actually have selected -
        // avoids wiping selectedInteractable when an unrelated collider exits.
        if (other.CompareTag("Item") && selectedInteractable == other.GetComponent<InteractableObject>())
        {
            selectedInteractable = null;
            Debug.Log("[PlayerInteraction] selectedInteractable cleared (exit)");
        }
        else if (other.CompareTag("Livestock") && selectedLivestock == other.GetComponent<Chicken>())
        {
            selectedLivestock = null;
            Debug.Log("[PlayerInteraction] selectedLivestock cleared (exit)");

            // Auto-close the popup when the player walks away
            UIManager.Instance.CloseLivestockPanel();
        }
    }

    private void SelectLand(Land land)
    {
        if (selectedLand != null)
            selectedLand.Select(false);

        selectedLand = land;
        selectedLand.Select(true);
    }

    private void DeselectLand()
    {
        if (selectedLand == null)
            return;

        selectedLand.Select(false);
        selectedLand = null;
    }

    public void Interact()
    {

         if (InventoryManager.Instance.equippedItem != null)
        {
            return;
        }
        if (selectedLand != null)
        {
            selectedLand.Interact();
        }
    }

    public void ItemInteract()
    {
        Debug.Log("[PlayerInteraction] ItemInteract called");

        if (InventoryManager.Instance.equippedItem != null)
        {
            Debug.Log("[PlayerInteraction] Hand occupied -> returning equipped item to inventory");
            InventoryManager.Instance.HandToInventory(InventorySlot.InventoryType.Item);
            return;
        }

        if (selectedInteractable != null)
        {
            Debug.Log($"[PlayerInteraction] Picking up: {selectedInteractable.name}");
            selectedInteractable.Pickup();
            selectedInteractable = null; // FIX: clear reference immediately after pickup,
                                          // since the object gets destroyed and OnTriggerExit
                                          // may not fire cleanly for a destroyed collider.
        }
        else
        {
            Debug.Log("[PlayerInteraction] ItemInteract called but nothing to pick up and hand is empty");
        }
    }

    // call this from your interact button, same way ItemInteract() is wired up.
    public void LivestockInteract()
    {
        Debug.Log("[PlayerInteraction] LivestockInteract called");

        if (selectedLivestock != null)
        {
            Debug.Log($"[PlayerInteraction] Interacting with: {selectedLivestock.name}");
            selectedLivestock.Interact();
        }
        else
        {
            Debug.Log("[PlayerInteraction] LivestockInteract called but no livestock in range");
        }
    }
}