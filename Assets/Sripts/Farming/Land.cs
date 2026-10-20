using UnityEngine;

public class Land : MonoBehaviour, ITimeTracker
{

    public enum LandStatus
    {
        Soil, Farmland, Watered
    }

    public LandStatus landStatus;

    public Material soilMat, farmlandMat, wateredMat;
    new Renderer renderer;
    public GameObject select;

    GameTimestamp timeWatered;

    [Header("Crops")]
    //The crop prefab to instantiate
    public GameObject cropPrefab;

    // Public so TutorialManager can read whether a crop has been planted
    // on this tile from outside the class.
    public CropBehaviour cropPlanted = null;

    [Header("Pest System")]
    public GameObject pestIndicator;     // Assign a floating pest icon prefab in Inspector
    public PestData[] possiblePests;     // Assign PestData ScriptableObjects in Inspector
    [Range(0f, 1f)]
    public float pestChancePerDay = 0.1f; // 10% chance per day once crop is planted

    private bool pestIndicatorActive = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        renderer= GetComponent<Renderer>();
        SwitchLandStatus(LandStatus.Soil);
         TimeManager.Instance.RegisterTracker(this);
    }

    public void SwitchLandStatus(LandStatus statusToSwitch)
    {
        landStatus = statusToSwitch;
        Material materialToSwitch = soilMat;

        switch (statusToSwitch)
        {
            case LandStatus.Soil:
                materialToSwitch = soilMat;
                break;
            case LandStatus.Farmland:
                materialToSwitch = farmlandMat;

                break;
            case LandStatus.Watered:
                materialToSwitch = wateredMat;
               timeWatered = TimeManager.Instance.GetGameTimeStamp();
                break;
        }

        renderer.material = materialToSwitch;
    }
    public void Select (bool toggle)
    {
        select.SetActive(toggle);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Interact()
    {
        ItemData toolSlot = InventoryManager.Instance.equippedTool;
        EquipmentData equipmentTool = toolSlot as EquipmentData;

        if (toolSlot == null)
        {
            // No tool equipped - try to harvest instead of just bailing out.
            if (cropPlanted != null && cropPlanted.cropState == CropBehaviour.CropState.Harvestable)
            {
                cropPlanted.Harvest();
                cropPlanted = null;
            }
            return;
        }

        if(equipmentTool != null)
        
        {

            EquipmentData.ToolType toolType = equipmentTool.toolType;

            switch (toolType)
            {

                case EquipmentData.ToolType.Hoe:
                 SwitchLandStatus(LandStatus.Farmland);

                break;

                case EquipmentData.ToolType.WateringCan:

                 SwitchLandStatus(LandStatus.Watered);

                 break;

                case EquipmentData.ToolType.Pesticide:
                    if (cropPlanted != null && cropPlanted.isInfested)
                    {
                        cropPlanted.ResolvePest();
                        Debug.Log("[Land] Pesticide applied. Pest cleared.");
                    }
                    break;
            }

            return;


        }

        SeedData seedTool = toolSlot as SeedData;

        if(seedTool != null && landStatus != LandStatus.Soil && cropPlanted == null)
        {
                GameObject cropObject = Instantiate(cropPrefab, transform);

                cropObject.transform.position = new Vector3(transform.position.x, .35f, transform.position.z);

                cropPlanted = cropObject.GetComponent<CropBehaviour>();

                cropPlanted.Plant(seedTool);
        }

    }

    public void ClockUpdate(GameTimestamp timestamp)
    {
        if (landStatus == LandStatus.Watered)
        {
            int hoursElapsed = GameTimestamp.CompareTimestamps(timeWatered, timestamp);
            Debug.Log(hoursElapsed + " hours since this was watered");

            if (cropPlanted != null)
            {
                cropPlanted.Grow();

                // ── PEST SYSTEM ──────────────────────────
                // Roll for pest infestation once per in-game day
                if (hoursElapsed % 24 == 0 && !cropPlanted.isInfested
                    && cropPlanted.cropState != CropBehaviour.CropState.Seed)
                {
                    if (Random.value < pestChancePerDay && possiblePests.Length > 0)
                    {
                        PestData randomPest = possiblePests[
                            Random.Range(0, possiblePests.Length)];
                        cropPlanted.Infest(randomPest);
                    }
                }

                // Tick pest damage once per in-game day
                if (hoursElapsed % 24 == 0 && cropPlanted.isInfested)
                {
                    cropPlanted.TickPestDamage();
                }
                // ─────────────────────────────────────────
            }

            if (hoursElapsed > 24)
            {
                //Dry up (Switch back to farmland)
                SwitchLandStatus(LandStatus.Farmland);
            }
        }
    }

    // Called by CropBehaviour.Infest()
    public void OnCropInfested(PestData pest)
    {
        // TEMP DEBUG — remove once the indicator issue is confirmed fixed
        Debug.Log($"[Land] OnCropInfested called on '{gameObject.name}'. pestIndicator assigned? {(pestIndicator != null ? pestIndicator.name : "NULL - not assigned on this instance!")}");

        if (pestIndicator != null)
        {
            // Set the icon sprite for this specific pest before showing it,
            // so different pest types can display different icons.
            SpriteRenderer indicatorSprite = pestIndicator.GetComponent<SpriteRenderer>();
            if (indicatorSprite != null && pest != null && pest.pestIcon != null)
                indicatorSprite.sprite = pest.pestIcon;

            pestIndicator.SetActive(true);
        }

        pestIndicatorActive = true;

        // Notify tutorial system if active
        TutorialManager.Instance?.OnPestAppeared();
    }

    // Called by CropBehaviour.ResolvePest()
    public void OnPestResolved()
    {
        if (pestIndicator != null)
            pestIndicator.SetActive(false);

        pestIndicatorActive = false;
    }

    // Called by CropBehaviour.KillCrop()
    public void OnCropDied()
    {
        if (pestIndicator != null)
            pestIndicator.SetActive(false);

        pestIndicatorActive = false;
        cropPlanted = null;
    }
}