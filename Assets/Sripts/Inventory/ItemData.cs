using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Items/Item")]
public class ItemData : ScriptableObject
{
    public string description;

    public Sprite thumbnail;

    public GameObject gameModel;

}
