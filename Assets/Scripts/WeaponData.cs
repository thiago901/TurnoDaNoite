using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Game/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public int damage = 12;
    public float range = 1.4f;  // distance of the hit box in front of the player
    public int durability = 8;  // number of hits before it breaks
}