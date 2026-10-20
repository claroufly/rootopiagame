using UnityEngine;

public class InventoryManager : MonoBehaviour
{

    public static InventoryManager Instance { get; private set; }

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else{
            Instance = this;

            // If your AR scene is a separate Unity scene loaded on top of
            // or instead of the farm scene, uncomment this so the singleton
            // (and anything collected in AR) survives the scene transition
            // back to the farm. If AR is additively loaded into the same
            // scene as the farm, you likely don't need this.
            // DontDestroyOnLoad(gameObject);
        }

    }
    [Header("TOOLS")]
    public ItemData[] tools = new ItemData[8];
    public ItemData equippedTool = null;

    [Header("ITEMS")]
    public ItemData[] items = new ItemData[8];
    public ItemData equippedItem = null;

    public Transform handPoint;

    public void InventoryToHand(int slotIndex, InventorySlot.InventoryType inventoryType)
    {

        if(inventoryType == InventorySlot.InventoryType.Item)
        {

            ItemData itemToEquip = items[slotIndex];

            items[slotIndex] = equippedItem;

            equippedItem = itemToEquip;

             RenderHand();

        }else
        {
            ItemData toolToEquip = tools[slotIndex];

            tools[slotIndex] = equippedTool;

            equippedTool = toolToEquip;
        }

        UIManager.Instance.RenderInventory();

    }

    public void HandToInventory(InventorySlot.InventoryType inventoryType)
    {
            if(inventoryType == InventorySlot.InventoryType.Item)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    if(items[i] == null)
                    {
                        items[i] = equippedItem;
                          equippedItem = null;
                        break;
                    }
                }

             
                RenderHand();
            } else
            {
                 for (int i = 0; i < tools.Length; i++)
                {
                    if(tools[i] == null)
                    {
                        tools[i] = equippedTool;
                          equippedTool = null;
                        break;
                    }
                }
               
            }
            UIManager.Instance.RenderInventory();
    }

    // Adds a brand-new item to the first free inventory slot — for picking
    // up items from the world (e.g. AR-collected items), as opposed to
    // InventoryToHand/HandToInventory which only move items you already have
    // between hand and inventory.
    public bool AddItem(ItemData newItem)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
            {
                items[i] = newItem;
                UIManager.Instance.RenderInventory();
                return true;
            }
        }

        Debug.Log("[InventoryManager] Inventory full, couldn't add " + newItem.name);
        return false;
    }

    public void RenderHand()
    {
        if(handPoint.childCount > 0)
        {
            Destroy(handPoint.GetChild(0).gameObject);
        }
        
        if(equippedItem != null)
        {
            //Instantiate the game model on the player's hand and put it on the scene
            Instantiate(equippedItem.gameModel, handPoint);
        }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}