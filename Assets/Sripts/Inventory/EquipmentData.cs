using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Items/Equipment")]
public class EquipmentData : ItemData
{
    public enum ToolType
    {
        Hoe, WateringCan, Axe, Pickaxe, Pesticide
    }

    public ToolType toolType;


}
