using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeapon : MonoBehaviour
{
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private float pickupRadius = 1.5f;
    [SerializeField] private LayerMask weaponLayers;

    public Weapon CurrentWeapon { get; private set; }

    private InputAction interactAction;

    private void Start()
    {
        interactAction = InputSystem.actions.FindAction("Player/Interact");
    }

    private void Update()
    {
        if (CurrentWeapon == null && interactAction.WasPerformedThisFrame())
            TryPickUp();
    }

    private void TryPickUp()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRadius, weaponLayers);
        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent(out Weapon weapon))
            {
                Equip(weapon);
                return;
            }
        }
    }

    private void Equip(Weapon weapon)
    {
        CurrentWeapon = weapon;
        weapon.GetComponent<Collider>().enabled = false;
        weapon.transform.SetParent(weaponHolder);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
    }

    // Called by PlayerCombat after an attack that hit something
    public void RegisterHit()
    {
        if (CurrentWeapon == null) return;

        if (CurrentWeapon.UseOnce())
        {
            Destroy(CurrentWeapon.gameObject);
            CurrentWeapon = null;
        }
    }
}