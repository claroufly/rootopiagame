using UnityEngine;

public class RegrowableHarvestBehaviour : InteractableObject
{
    CropBehaviour parentCrop;

    public void SetParent(CropBehaviour parentCrop)
    {
        this.parentCrop = parentCrop;
    }

    public override void Pickup()
    {
        Debug.Log($"[RegrowableHarvestBehaviour] Pickup called on {gameObject.name}, item assigned: {(item != null ? item.name : "NULL - assign ItemData in Inspector!")}");

        InventoryManager.Instance.equippedItem = item;
        InventoryManager.Instance.RenderHand();

        // FIX: deactivate instead of destroy. This specific fruit needs to
        // exist again once the whole plant regrows, so it can't be
        // permanently removed from the scene like a normal InteractableObject.
        gameObject.SetActive(false);

        // FIX: notify the parent plant instead of calling Regrow() directly.
        // Regrow should only happen once ALL fruits on the plant are picked,
        // not the instant a single one is.
        if (parentCrop != null)
        {
            parentCrop.NotifyFruitHarvested();
        }
        else
        {
            Debug.LogWarning($"[RegrowableHarvestBehaviour] {gameObject.name} has no parentCrop assigned - was SetParent() called on it from CropBehaviour.Plant()?");
        }
    }
}