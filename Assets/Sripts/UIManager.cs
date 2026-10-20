using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour, ITimeTracker
{
    public static UIManager Instance { get; private set; }
    [Header("Status Bar")]
    public Image toolEquipSlot;


    public TMP_Text timeText;
    public TMP_Text dateText;
    [Header("Inventory System")]
    // The inventory panel
    public GameObject inventoryPanel;


    public HandInventorySlot toolHandSlot;

    // The tool slot UIs
    public InventorySlot[] toolSlots;

    public HandInventorySlot itemHandSlot;

    // The item slot UIs
    public InventorySlot[] itemSlots;

    // Item info box
    public TMP_Text itemNameText;
    public TMP_Text itemDescriptionText;

    [Header("Livestock Info")]
    public GameObject livestockPanel;
    public TMP_Text livestockNameText;
    public Slider livestockHealthBar;
    public Slider livestockHungerBar;
    public Slider livestockHappinessBar;

    [Header("Livestock Panel Positioning")]
    [SerializeField] private Camera mainCamera; // assign your gameplay camera in Inspector; falls back to Camera.main
    [SerializeField] private Vector2 panelOffset = new Vector2(300f, 0f); // pixels to the side of the chicken
    private Chicken trackedChicken; // chicken the panel is currently following, if any

    [Header("River Info")]
    public GameObject riverPanel;
    public TMP_Text riverNameText;
    public TMP_Text riverHealthText;
    public TMP_Text riverStatusText;
    public Slider riverHealthBar;       // optional
    public Image riverHealthBarFill;    // optional, tinted by status
    private RiverHealthZone currentRiver; // river the player is currently standing in, if any

    [Header("Forest Info")]
    public GameObject forestPanel;
    public TMP_Text forestNameText;
    public TMP_Text forestHealthText;
    public TMP_Text forestStatusText;
    public TMP_Text forestTreeCountText;
    public TMP_Text forestWildlifeCountText;
    public Slider forestHealthBar;       // optional
    public Image forestHealthBarFill;    // optional, tinted by status
    private ForestHealthZone currentForest; // forest the player is currently standing in, if any

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        RenderInventory();
        AssignSlotIndexes();

        // FIX: This was the main bug causing "TMP not updating" - UIManager
        // never told TimeManager it wanted to listen for clock updates, so
        // ClockUpdate() was never being called. Registering here in Start()
        // (not Awake()) because TimeManager.Instance is only guaranteed to
        // be set once TimeManager's own Awake() has run, and Unity doesn't
        // guarantee execution order between different scripts' Awake() calls.
        TimeManager.Instance.RegisterTracker(this);

        if (livestockPanel != null)
            livestockPanel.SetActive(false);

        if (riverPanel != null)
            riverPanel.SetActive(false);

        if (forestPanel != null)
            forestPanel.SetActive(false);

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    // FIX: Added OnDestroy to unregister from TimeManager when this object
    // is destroyed (e.g. scene change). Without this, TimeManager would keep
    // a dangling reference in its listeners list and throw a
    // MissingReferenceException the next time it tries to call ClockUpdate
    // on a destroyed UIManager.
    private void OnDestroy()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.UnregistTracker(this);
    }

    // Keeps the panel glued beside the chicken if the camera or chicken moves while it's open.
    private void LateUpdate()
    {
        if (trackedChicken != null && livestockPanel != null && livestockPanel.activeSelf)
        {
            PositionPanelNearWorldPoint(trackedChicken.transform.position);
        }

        // Keep the river panel live while the player is standing in a river zone.
        if (currentRiver != null)
        {
            RefreshRiverUI();
        }

        // Same for the forest panel.
        if (currentForest != null)
        {
            RefreshForestUI();
        }
    }

    public void AssignSlotIndexes()
    {
        for (int i = 0; i < toolSlots.Length; i++)
        {
            // FIX: Was toolSlots[1] / itemSlots[1] - hardcoded index 1 instead
            // of the loop variable i. This meant every slot except index 1
            // never got assigned an index at all, and slot 1 kept getting
            // overwritten on every iteration. Also would throw
            // IndexOutOfRangeException if either array had fewer than 2 elements.
            toolSlots[i].AssignIndex(i);
            itemSlots[i].AssignIndex(i);
        }
    }

    // Render the inventory screen
    public void RenderInventory()
    {
        ItemData[] inventoryToolSlots = InventoryManager.Instance.tools;
        ItemData[] inventoryItemSlots = InventoryManager.Instance.items;

        RenderInventoryPanel(inventoryToolSlots, toolSlots);
        RenderInventoryPanel(inventoryItemSlots, itemSlots);
    

        toolHandSlot.Display(InventoryManager.Instance.equippedTool);
        itemHandSlot.Display(InventoryManager.Instance.equippedItem);

       ItemData equippedTool = InventoryManager.Instance.equippedTool;

            if(equippedTool != null)
        {
            //Switch the thumbnail over
            toolEquipSlot.sprite = equippedTool.thumbnail;
         
           toolEquipSlot.gameObject.SetActive(true);

            return; 
        }

        toolEquipSlot.gameObject.SetActive(false);
    }

    void RenderInventoryPanel(ItemData[] slots, InventorySlot[] uiSlots)
    {
        for (int i = 0; i < uiSlots.Length; i++)
        {
            uiSlots[i].AssignIndex(i);
            uiSlots[i].Display(slots[i]);
        }
    }

    public void ToggleInventoryPanel()
    {
        inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        RenderInventory();
    }

    public void DisplayItemInfo(ItemData data)
    {
        if (data == null)
        {
            itemNameText.text = "";
            itemDescriptionText.text = "";
            return;
        }

        itemNameText.text = data.name;
        itemDescriptionText.text = data.description;
    }

    // Opens the livestock panel and fills it with the given chicken's stats.
    public void DisplayLivestockInfo(Chicken chicken)
    {
        Debug.Log($"[UIManager] DisplayLivestockInfo called for: {(chicken != null ? chicken.chickenName : "null")}");

        if (chicken == null)
        {
            Debug.LogWarning("[UIManager] DisplayLivestockInfo called with a null chicken.");
            return;
        }

        if (livestockPanel == null)
        {
            Debug.LogWarning("[UIManager] livestockPanel is not assigned in the Inspector.");
            return;
        }

        livestockPanel.SetActive(true);
        Debug.Log($"[UIManager] livestockPanel set active. activeSelf = {livestockPanel.activeSelf}, activeInHierarchy = {livestockPanel.activeInHierarchy}");

        if (livestockNameText != null)
            livestockNameText.text = chicken.chickenName;

        if (livestockHealthBar != null)
        {
            livestockHealthBar.maxValue = chicken.MaxHealth;
            livestockHealthBar.value = chicken.CurrentHealth;
        }

        if (livestockHungerBar != null)
            livestockHungerBar.value = chicken.hunger;

        if (livestockHappinessBar != null)
            livestockHappinessBar.value = chicken.happiness;

        // Remember which chicken to follow, and snap the panel beside it immediately.
        trackedChicken = chicken;
        PositionPanelNearWorldPoint(chicken.transform.position);
    }

    // Converts a world position (the chicken) into a screen position and moves
    // the livestock panel's RectTransform there, offset to the side.
    // NOTE: assumes the Canvas is set to Screen Space - Overlay. If your Canvas
    // uses Screen Space - Camera or World Space, this conversion needs to change.
    private void PositionPanelNearWorldPoint(Vector3 worldPos)
    {
        if (livestockPanel == null) return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogWarning("[UIManager] No camera assigned or found (Camera.main is null) - cannot position livestock panel.");
            return;
        }

        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

        // If the chicken is behind the camera, WorldToScreenPoint gives a
        // meaningless position - bail out rather than snapping the panel somewhere odd.
        if (screenPos.z < 0f)
            return;

        RectTransform panelRect = livestockPanel.GetComponent<RectTransform>();
        if (panelRect == null) return;

        panelRect.position = screenPos + (Vector3)panelOffset;
    }

    public void CloseLivestockPanel()
    {
        if (livestockPanel != null)
            livestockPanel.SetActive(false);

        trackedChicken = null;
    }

    // Called by RiverHealthZone when the player enters its trigger.
    public void ShowRiverInfo(RiverHealthZone river)
    {
        if (river == null) return;

        if (riverPanel == null)
        {
            Debug.LogWarning("[UIManager] riverPanel is not assigned in the Inspector.");
            return;
        }

        currentRiver = river;
        riverPanel.SetActive(true);
        RefreshRiverUI();
    }

    // Called by RiverHealthZone when the player leaves its trigger.
    // Only hides the panel if the zone leaving is the one currently shown, so
    // overlapping zones don't close the panel on a player who already entered the next one.
    public void HideRiverInfo(RiverHealthZone river)
    {
        if (currentRiver != river) return;

        currentRiver = null;

        if (riverPanel != null)
            riverPanel.SetActive(false);
    }

    private void RefreshRiverUI()
    {
        if (currentRiver == null) return;

        if (riverNameText != null)
            riverNameText.text = currentRiver.riverName;

        if (riverHealthText != null)
            riverHealthText.text = $"Health: {Mathf.RoundToInt(currentRiver.HealthPercent * 100f)}%";

        if (riverStatusText != null)
        {
            riverStatusText.text = currentRiver.Status;
            riverStatusText.color = currentRiver.StatusColor;
        }

        if (riverHealthBar != null)
        {
            riverHealthBar.minValue = 0f;
            riverHealthBar.maxValue = 1f;
            riverHealthBar.value = currentRiver.HealthPercent;
        }

        if (riverHealthBarFill != null)
            riverHealthBarFill.color = currentRiver.StatusColor;
    }

    // Called by ForestHealthZone when the player enters its trigger.
    public void ShowForestInfo(ForestHealthZone forest)
    {
        if (forest == null) return;

        if (forestPanel == null)
        {
            Debug.LogWarning("[UIManager] forestPanel is not assigned in the Inspector.");
            return;
        }

        currentForest = forest;
        forestPanel.SetActive(true);
        RefreshForestUI();
    }

    // Called by ForestHealthZone when the player leaves its trigger.
    // Only hides the panel if the zone leaving is the one currently shown.
    public void HideForestInfo(ForestHealthZone forest)
    {
        if (currentForest != forest) return;

        currentForest = null;

        if (forestPanel != null)
            forestPanel.SetActive(false);
    }

    private void RefreshForestUI()
    {
        if (currentForest == null) return;

        if (forestNameText != null)
            forestNameText.text = currentForest.forestName;

        if (forestHealthText != null)
            forestHealthText.text = $"Health: {Mathf.RoundToInt(currentForest.HealthPercent * 100f)}%";

        if (forestStatusText != null)
        {
            forestStatusText.text = currentForest.Status;
            forestStatusText.color = currentForest.StatusColor;
        }

        if (forestTreeCountText != null)
            forestTreeCountText.text = $"Trees: {currentForest.treeCount}";

        if (forestWildlifeCountText != null)
            forestWildlifeCountText.text = $"Wildlife: {currentForest.wildlifeCount}";

        if (forestHealthBar != null)
        {
            forestHealthBar.minValue = 0f;
            forestHealthBar.maxValue = 1f;
            forestHealthBar.value = currentForest.HealthPercent;
        }

        if (forestHealthBarFill != null)
            forestHealthBarFill.color = currentForest.StatusColor;
    }

    public void ClockUpdate(GameTimestamp timestamp)
    {
        int hours = timestamp.hour;
        int minutes = timestamp.minute;
        string prefix = "AM";

        // FIX: Was "if (hours > 12)", which meant exactly 12:xx (noon) fell
        // through as AM instead of PM. Changed to >= 12 so noon correctly
        // triggers the PM branch. Also wrapped the "-12" conversion in its
        // own check so that hour 12 (noon) stays displayed as 12, not 0.
        if (hours >= 12)
        {
            prefix = "PM";
            if (hours > 12) hours -= 12;
        }

        // FIX: Added midnight handling. Previously hour 0 (midnight) had no
        // conversion at all and would display as "0", when 12-hour clocks
        // should show midnight as 12 AM.
        if (hours == 0) hours = 12;

        // FIX: Was "prefix + hours + minutes", which just concatenated the
        // raw numbers with no separator or zero-padding - e.g. 9:05 AM would
        // render as "AM95" instead of "9:05 AM". Using string interpolation
        // with ":00" format specifier to zero-pad minutes below 10.
        timeText.text = $"{hours}:{minutes:00} {prefix}";


        int day = timestamp.day;
        string season = timestamp.season.ToString();
        string DayOfTheWeek = timestamp.GetDayofTheWeek().ToString();

        dateText.text = season + " " + " (" + DayOfTheWeek + ")";

    }
}