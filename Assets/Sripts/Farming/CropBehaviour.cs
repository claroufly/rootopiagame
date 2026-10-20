using UnityEngine;

public class CropBehaviour : MonoBehaviour
{
    SeedData seedToGrow;

    [Header("Stages of Life")]

    public GameObject seed;
    private GameObject seedling;
    private GameObject harvestable;

    int growth;

    int maxGrowth;

    public CropState cropState;

    public enum CropState
    {
        Seed, Seedling, Harvestable
    }

    // Every pickable fruit child under `harvestable` (e.g. 3 tomatoes on one plant).
    private RegrowableHarvestBehaviour[] regrowableFruits;
    private int remainingFruits;

    [Header("Crop Health")]
    [Range(0f, 1f)]
    public float cropHealth = 1f; // 1 = healthy, 0 = dead

    [Header("Pest")]
    public bool isInfested = false;
    private PestData currentPest;
    private int daysSinceInfestation = 0;

    public void Plant(SeedData seedToGrow)
    {
        this.seedToGrow = seedToGrow;

        seedling = Instantiate(seedToGrow.seedling, transform);

        // Access the crop item data
        ItemData cropToYield = seedToGrow.cropToYield;

        // Instantiate the harvestable crop
        harvestable = Instantiate(cropToYield.gameModel, transform);

        int hoursToGrow = GameTimestamp.DaysToHours(seedToGrow.daysToGrow);
        // Convert it to minutes
        maxGrowth = GameTimestamp.HoursToMinutes(hoursToGrow);

        if (seedToGrow.regrowable)
        {
            // FIX: a harvestable can contain multiple pickable fruit children
            // (e.g. 3 tomatoes on one plant), not just one on the root, so we
            // need GetComponentsInChildren rather than GetComponent.
            regrowableFruits = harvestable.GetComponentsInChildren<RegrowableHarvestBehaviour>(true);

            if (regrowableFruits.Length == 0)
            {
                Debug.LogError($"[CropBehaviour] '{seedToGrow.name}' is marked regrowable but no " +
                    $"RegrowableHarvestBehaviour was found on '{harvestable.name}' or its children. " +
                    $"Check the prefab assigned to cropToYield.gameModel.");
            }
            else
            {
                foreach (RegrowableHarvestBehaviour fruit in regrowableFruits)
                {
                    fruit.SetParent(this);
                }

                remainingFruits = regrowableFruits.Length;
            }
        }

        // Set the current crop state to the state we're switching to
        SwitchState(CropState.Seed);
    }

    public void Grow()
    {
        // Stop ticking once fully grown and waiting to be harvested.
        if (cropState == CropState.Harvestable)
            return;

        Debug.Log($"Growing {growth}/{maxGrowth}");
        growth++;

        if (growth >= maxGrowth / 2 && cropState == CropState.Seed)
        {
            SwitchState(CropState.Seedling);
        }

        if (growth >= maxGrowth)
        {
            SwitchState(CropState.Harvestable);
        }
    }

    void SwitchState(CropState stateToSwitch)
    {
        // Reset everything and set all GameObjects to inactive
        seed.SetActive(false);
        seedling.SetActive(false);
        harvestable.SetActive(false);

        switch (stateToSwitch)
        {
            case CropState.Seed:
                seed.SetActive(true);
                break;

            case CropState.Seedling:
                seedling.SetActive(true);
                break;

            case CropState.Harvestable:
                harvestable.SetActive(true);

                // Re-activate every fruit on the plant when it becomes
                // harvestable again after regrowing. Individual fruits are
                // deactivated (not destroyed) when picked - see
                // RegrowableHarvestBehaviour.Pickup() - so reactivating the
                // parent alone won't bring them back into view.
                if (seedToGrow.regrowable && regrowableFruits != null)
                {
                    foreach (RegrowableHarvestBehaviour fruit in regrowableFruits)
                    {
                        if (fruit != null)
                            fruit.gameObject.SetActive(true);
                    }

                    remainingFruits = regrowableFruits.Length;
                }

                // NOTE: no auto-destroy here anymore. Non-regrowable crops
                // now wait for the player to actually harvest them via
                // Harvest() (triggered from Land.Interact()), instead of
                // being destroyed the instant they finish growing.
                break;
        }

        cropState = stateToSwitch;
    }

    // Called when the player interacts with a Harvestable crop (bare hand,
    // no tool equipped - see Land.Interact()).
    public void Harvest()
    {
        if (cropState != CropState.Harvestable) return;

        if (!seedToGrow.regrowable)
        {
            // Unparent it from the crop so it survives independently, then
            // clean up the CropBehaviour/seed/seedling objects.
            harvestable.transform.parent = null;
            Destroy(gameObject);
        }
        // Regrowable crops are harvested fruit-by-fruit via
        // RegrowableHarvestBehaviour.Pickup() -> NotifyFruitHarvested(),
        // not through this method.
    }

    public void Regrow()
    {
        Debug.Log("Regrow called");

        // Get the regrowth time in hours, reset growth accordingly
        int hoursToRegrow = GameTimestamp.DaysToHours(seedToGrow.daysToRegrow);
        growth = maxGrowth - GameTimestamp.HoursToMinutes(hoursToRegrow);

        // Switch the state back to seedling
        SwitchState(CropState.Seedling);
    }

    // Called by each individual fruit when it's picked. Regrow only actually
    // happens once every fruit on the plant has been harvested.
    public void NotifyFruitHarvested()
    {
        remainingFruits--;
        Debug.Log($"[CropBehaviour] Fruit harvested, {remainingFruits} remaining on plant");

        if (remainingFruits <= 0)
        {
            Regrow();
        }
    }

    // ── Pest System ──────────────────────────────────────────────────

    // Call this from PestManager/Land when a pest event triggers on this crop
    public void Infest(PestData pest)
    {
        if (isInfested) return;

        isInfested = true;
        currentPest = pest;
        daysSinceInfestation = 0;

        Debug.Log($"[CropBehaviour] Crop infested by {pest.pestName}");

        // TEMP DEBUG — remove once the indicator issue is confirmed fixed
        Land parentLand = GetComponentInParent<Land>();
        Debug.Log($"[CropBehaviour] Parent Land found on '{gameObject.name}'? {(parentLand != null ? parentLand.gameObject.name : "NULL - not found!")}");

        // Notify the Land tile to show the pest indicator, passing along
        // which pest it is so the correct icon sprite can be shown.
        parentLand?.OnCropInfested(pest);
    }

    // Call this from Land.ClockUpdate once per in-game day
    public void TickPestDamage()
    {
        if (!isInfested || currentPest == null) return;

        daysSinceInfestation++;

        // Grace period — warn but no damage yet
        if (daysSinceInfestation <= currentPest.daysBeforeFirstDamage)
        {
            Debug.Log($"[CropBehaviour] Pest grace period: {daysSinceInfestation}" +
                      $"/{currentPest.daysBeforeFirstDamage} days");
            return;
        }

        // Damage ticks every damageIntervalDays after grace period
        if ((daysSinceInfestation - currentPest.daysBeforeFirstDamage)
            % currentPest.damageIntervalDays == 0)
        {
            cropHealth -= currentPest.damagePerTick;
            cropHealth = Mathf.Clamp01(cropHealth);

            Debug.Log($"[CropBehaviour] Pest damage tick. Health: {cropHealth}");

            if (cropHealth <= 0f)
                KillCrop();
        }
    }

    // Resolve the pest — called when player uses pesticide on this tile
    public void ResolvePest()
    {
        isInfested = false;
        currentPest = null;
        daysSinceInfestation = 0;

        Debug.Log("[CropBehaviour] Pest resolved.");

        GetComponentInParent<Land>()?.OnPestResolved();
    }

    private void KillCrop()
    {
        Debug.Log("[CropBehaviour] Crop died from pest damage.");

        // Notify land to clean up
        GetComponentInParent<Land>()?.OnCropDied();

        Destroy(gameObject);
    }
}