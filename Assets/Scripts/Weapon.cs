using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private WeaponData data;

    public WeaponData Data => data;
    public int Durability { get; private set; }

    private void Awake()
    {
        Durability = data.durability;
    }

    // Returns true when the weapon breaks
    public bool UseOnce()
    {
        Durability--;
        return Durability <= 0;
    }
}