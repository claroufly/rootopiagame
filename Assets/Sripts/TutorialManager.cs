using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Tutorial UI")]
    public GameObject tutorialPanel;
    public Image npcIcon;
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI dialogueText;
    public Button nextButton;

    [Header("NPC Data")]
    public Sprite npcSprite;
    public string npcName = "Eco Guide";

    [Header("Game References")]
    public InventoryManager inventoryManager;
    public Land[] landTiles;
    public Transform inventoryButtonTransform;

    [Header("Visual Indicators")]
    public GameObject arrowIndicatorPrefab;
    public Material highlightMaterial;

    [Header("Tutorial Overrides")]
    [Tooltip("Pest forced to appear once the tutorial crop reaches Seedling, guaranteeing the pest step happens instead of relying on Land's random daily chance.")]
    public PestData tutorialPest;
    public bool forcePestDuringTutorial = true;

    [Tooltip("If true, TutorialManager ticks the tutorial crop's growth directly and faster than the normal day/night clock, independent of watered status or soil drying.")]
    public bool speedUpTutorialGrowth = true;
    public float extraGrowthTicksPerSecond = 5f;

    // Private state
    private int currentStep = 0;
    private bool waitingForCondition = false;
    private bool tutorialComplete = false;
    private GameObject currentArrow;
    private Renderer lastHighlightedRenderer;
    private Material lastOriginalMaterial;

    private bool pestForced = false;
    private float growthTickTimer = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    void Start()
    {
        // Set up NPC visuals
        if (npcIcon != null && npcSprite != null)
            npcIcon.sprite = npcSprite;

        if (npcNameText != null)
            npcNameText.text = npcName;

        nextButton.onClick.AddListener(OnNextClicked);

        // Hide panel at start, then show step 0
        tutorialPanel.SetActive(false);
        ShowStep(0);
    }

    void Update()
    {
        // Speed up crop growth during the planting-to-harvest portion of the
        // tutorial (steps 6 through 9), independent of the normal day/night
        // clock and watered-status gating.
        if (speedUpTutorialGrowth && !tutorialComplete && currentStep >= 6 && currentStep <= 9)
        {
            TickTutorialGrowth();
        }

        if (!waitingForCondition || tutorialComplete) return;

        switch (currentStep)
        {
            // Step 1: Wait for Hoe to be equipped
            case 1:
                if (inventoryManager.equippedTool != null)
                {
                    EquipmentData eq = inventoryManager.equippedTool as EquipmentData;
                    if (eq != null && eq.toolType == EquipmentData.ToolType.Hoe)
                        AdvanceStep();
                }
                break;

            // Step 2: Wait for any land tile to become Farmland
            case 2:
                if (AnyLandHasStatus(Land.LandStatus.Farmland))
                    AdvanceStep();
                break;

            // Step 3: Wait for Watering Can to be equipped
            case 3:
                if (inventoryManager.equippedTool != null)
                {
                    EquipmentData eq = inventoryManager.equippedTool as EquipmentData;
                    if (eq != null && eq.toolType == EquipmentData.ToolType.WateringCan)
                        AdvanceStep();
                }
                break;

            // Step 4: Wait for any land tile to become Watered
            case 4:
                if (AnyLandHasStatus(Land.LandStatus.Watered))
                    AdvanceStep();
                break;

            // Step 5: Wait for Seeds to be equipped
            case 5:
                if (inventoryManager.equippedTool is SeedData)
                    AdvanceStep();
                break;

            // Step 6: Wait for a crop to be planted
            case 6:
                if (AnyLandHasCrop())
                    AdvanceStep();
                break;

            // Step 7: Wait for crop to reach Seedling
            case 7:
                if (AnyLandCropAtState(CropBehaviour.CropState.Seedling))
                {
                    // Force the pest event here instead of waiting on Land's
                    // random daily roll, so the tutorial reliably teaches
                    // pest resolution every time.
                    if (forcePestDuringTutorial && tutorialPest != null && !pestForced)
                    {
                        Land seedlingLand = GetFirstLandCropAtState(CropBehaviour.CropState.Seedling);
                        if (seedlingLand != null && seedlingLand.cropPlanted != null
                            && !seedlingLand.cropPlanted.isInfested)
                        {
                            pestForced = true;
                            // Infest() -> Land.OnCropInfested() -> TutorialManager.OnPestAppeared()
                            // already jumps us straight to ShowStep(9), so skip AdvanceStep() below.
                            seedlingLand.cropPlanted.Infest(tutorialPest);
                            break;
                        }
                    }

                    AdvanceStep();
                }
                break;

            // Step 8: Wait for crop to become Harvestable
            case 8:
                if (AnyLandCropAtState(CropBehaviour.CropState.Harvestable))
                    AdvanceStep();
                break;

            // Step 9 (Pest): Wait for pest to be resolved
            case 9:
                if (!AnyLandHasInfestedCrop())
                    AdvanceStep();
                break;
        }
    }

    // ── Tutorial-only Growth Acceleration ───────────────────────────────

    void TickTutorialGrowth()
    {
        Land land = GetFirstLandWithCrop();
        if (land == null || land.cropPlanted == null) return;

        growthTickTimer += Time.deltaTime;
        float interval = 1f / Mathf.Max(extraGrowthTicksPerSecond, 0.01f);

        while (growthTickTimer >= interval)
        {
            growthTickTimer -= interval;
            land.cropPlanted.Grow();
        }
    }

    // ── Core Tutorial Flow ────────────────────────────────────────────

    void ShowStep(int step)
    {
        tutorialPanel.SetActive(true);
        currentStep = step;
        waitingForCondition = false;

        ClearArrow();
        ClearHighlight();

        switch (step)
        {
            case 0:
                ShowDialogue(
                    "Welcome to ROOTOPIA!\n" +
                    "This valley needs your help. Use the joystick to " +
                    "walk around and explore your surroundings.",
                    showNext: true);
                break;

            case 1:
                ShowDialogue(
                    "Open your inventory and equip the Hoe.\n" +
                    "You will need it to prepare the soil for planting.",
                    showNext: false);

                // Point arrow at inventory button
                PointArrowAt(inventoryButtonTransform);
                waitingForCondition = true;
                break;

            case 2:
                ShowDialogue(
                    "Great! Now walk to a soil tile and interact with it " +
                    "to till the land. Tilled land becomes Farmland.",
                    showNext: false);

                // Highlight and point arrow at the first soil tile
                Land firstSoil = GetFirstLandWithStatus(Land.LandStatus.Soil);
                if (firstSoil != null)
                {
                    PointArrowAt(firstSoil.transform);
                    HighlightObject(firstSoil.GetComponent<Renderer>());
                }
                waitingForCondition = true;
                break;

            case 3:
                ShowDialogue(
                    "Good job! The soil is tilled.\n" +
                    "Now equip the Watering Can from your inventory.",
                    showNext: false);

                PointArrowAt(inventoryButtonTransform);
                waitingForCondition = true;
                break;

            case 4:
                ShowDialogue(
                    "Water the tilled land. Crops can only grow on " +
                    "watered soil.\n" +
                    "Remember — soil dries up after 24 in-game hours!",
                    showNext: false);

                Land tilledLand = GetFirstLandWithStatus(Land.LandStatus.Farmland);
                if (tilledLand != null)
                {
                    PointArrowAt(tilledLand.transform);
                    HighlightObject(tilledLand.GetComponent<Renderer>());
                }
                waitingForCondition = true;
                break;

            case 5:
                ShowDialogue(
                    "Perfect! Now equip your Seeds from the inventory " +
                    "and get ready to plant.",
                    showNext: false);

                PointArrowAt(inventoryButtonTransform);
                waitingForCondition = true;
                break;

            case 6:
                ShowDialogue(
                    "Walk to the watered land and interact with it " +
                    "to plant your seed!",
                    showNext: false);

                Land wateredLand = GetFirstLandWithStatus(Land.LandStatus.Watered);
                if (wateredLand != null)
                {
                    PointArrowAt(wateredLand.transform);
                    HighlightObject(wateredLand.GetComponent<Renderer>());
                }
                waitingForCondition = true;
                break;

            case 7:
                ShowDialogue(
                    "Your seed is planted!\n" +
                    "Keep the soil watered every day. Watch it sprout " +
                    "into a seedling as it grows.",
                    showNext: false);

                waitingForCondition = true;
                break;

            case 8:
                ShowDialogue(
                    "Your crop is almost ready!\n" +
                    "Keep watering and wait for it to become " +
                    "fully harvestable.",
                    showNext: false);

                waitingForCondition = true;
                break;

            case 9: // PEST STEP
                Land infestedLand = GetFirstInfestedLand();

                if (inventoryManager.equippedTool is EquipmentData equippedEq
                    && equippedEq.toolType == EquipmentData.ToolType.Pesticide)
                {
                    // Pesticide already equipped — point them at the tile
                    ShowDialogue(
                        "Now walk to the affected tile and interact with it " +
                        "to apply the Pesticide.",
                        showNext: false);

                    if (infestedLand != null)
                    {
                        PointArrowAt(infestedLand.transform);
                        HighlightObject(infestedLand.GetComponent<Renderer>());
                    }
                }
                else
                {
                    // Not equipped yet — point them at the inventory first
                    ShowDialogue(
                        "Watch out! A pest has appeared on your crop!\n" +
                        "Open your inventory and equip the Pesticide.",
                        showNext: false);

                    PointArrowAt(inventoryButtonTransform);
                }

                waitingForCondition = true;
                break;

            case 10:
                ShowDialogue(
                    "Your crop is ready to harvest!\n" +
                    "Walk up to it and interact to collect your food.",
                    showNext: true);
                break;

            case 11:
                // Tutorial done
                tutorialPanel.SetActive(false);
                ClearArrow();
                ClearHighlight();
                tutorialComplete = true;
                Debug.Log("[TutorialManager] Tutorial complete!");
                break;
        }
    }

    void ShowDialogue(string message, bool showNext)
    {
        dialogueText.text = message;
        nextButton.gameObject.SetActive(showNext);
    }

    void OnNextClicked()
    {
        AdvanceStep();
    }

    void AdvanceStep()
    {
        waitingForCondition = false;
        ShowStep(currentStep + 1);
    }

    // Called by Land.cs when a pest appears — hooks pest into tutorial
    public void OnPestAppeared()
    {
        if (tutorialComplete) return;

        // Only show pest step if player has already planted
        if (currentStep >= 6)
            ShowStep(9);
    }

    // ── Visual Indicators ─────────────────────────────────────────────

    void PointArrowAt(Transform target)
    {
        ClearArrow();

        if (arrowIndicatorPrefab == null || target == null) return;

        currentArrow = Instantiate(
            arrowIndicatorPrefab,
            target.position + Vector3.up * 1.5f,
            Quaternion.identity);
    }

    void ClearArrow()
    {
        if (currentArrow != null)
        {
            Destroy(currentArrow);
            currentArrow = null;
        }
    }

    void HighlightObject(Renderer rend)
    {
        ClearHighlight();

        if (rend == null || highlightMaterial == null) return;

        lastHighlightedRenderer = rend;
        lastOriginalMaterial = rend.material;
        rend.material = highlightMaterial;
    }

    void ClearHighlight()
    {
        if (lastHighlightedRenderer != null && lastOriginalMaterial != null)
        {
            lastHighlightedRenderer.material = lastOriginalMaterial;
            lastHighlightedRenderer = null;
            lastOriginalMaterial = null;
        }
    }

    // ── Helper Methods ────────────────────────────────────────────────

    bool AnyLandHasStatus(Land.LandStatus status)
    {
        foreach (Land land in landTiles)
            if (land.landStatus == status) return true;
        return false;
    }

    bool AnyLandHasCrop()
    {
        foreach (Land land in landTiles)
            if (land.cropPlanted != null) return true;
        return false;
    }

    bool AnyLandCropAtState(CropBehaviour.CropState state)
    {
        foreach (Land land in landTiles)
            if (land.cropPlanted != null && land.cropPlanted.cropState == state)
                return true;
        return false;
    }

    bool AnyLandHasInfestedCrop()
    {
        foreach (Land land in landTiles)
            if (land.cropPlanted != null && land.cropPlanted.isInfested)
                return true;
        return false;
    }

    Land GetFirstLandWithStatus(Land.LandStatus status)
    {
        foreach (Land land in landTiles)
            if (land.landStatus == status) return land;
        return null;
    }

    Land GetFirstLandWithCrop()
    {
        foreach (Land land in landTiles)
            if (land.cropPlanted != null) return land;
        return null;
    }

    Land GetFirstLandCropAtState(CropBehaviour.CropState state)
    {
        foreach (Land land in landTiles)
            if (land.cropPlanted != null && land.cropPlanted.cropState == state)
                return land;
        return null;
    }

    Land GetFirstInfestedLand()
    {
        foreach (Land land in landTiles)
            if (land.cropPlanted != null && land.cropPlanted.isInfested)
                return land;
        return null;
    }
}