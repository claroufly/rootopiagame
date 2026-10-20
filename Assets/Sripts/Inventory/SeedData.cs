using UnityEngine;

[CreateAssetMenu(fileName = "SeedData", menuName = "Items/Seed")]
public class SeedData : ItemData
{
    public int daysToGrow;

    public ItemData cropToYield;

    public GameObject seedling;

    [Header("Regrowable")]
    //Is the plant able to regrow the crop after being harvested?
    public bool regrowable;
    //Time taken before the plant yields another crop
    public int daysToRegrow;
}
