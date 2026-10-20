using UnityEngine;

[CreateAssetMenu(fileName = "PestData", menuName = "Disasters/Pest")]
public class PestData : ScriptableObject
{
    [Header("Pest Info")]
    public string pestName;
    public string description;
    public Sprite pestIcon; // shown as floating indicator above the tile

    [Header("Timing")]
    public int daysBeforeFirstDamage = 2; // grace period before crop takes damage
    public int damageIntervalDays = 1;    // how often damage ticks after grace period

    [Header("Damage")]
    [Range(0f, 1f)]
    public float damagePerTick = 0.25f;   // fraction of crop health lost per tick
}