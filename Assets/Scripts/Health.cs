using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 20;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;

    // Set by the stone wall
    public bool IsBlocking { get; set; }

    // Set by the dodge: while true, every hit is ignored
    public bool IsInvulnerable { get; set; }

    // Set by the aura: hits hurt more while charging
    public float DamageTakenMultiplier { get; set; } = 1f;

    public event Action<int, Vector3> OnDamaged; // damage, attacker position
    public event Action<int, Vector3> OnBlocked; // damage stopped by the wall
    public event Action OnDied;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int amount, Vector3 attackerPosition)
    {
        if (CurrentHealth <= 0 || IsInvulnerable) return;

        // Blocking only works against attacks from the front
        if (IsBlocking)
        {
            Vector3 toAttacker = (attackerPosition - transform.position).normalized;
            if (Vector3.Dot(transform.forward, toAttacker) > 0f)
            {
                OnBlocked?.Invoke(amount, attackerPosition);
                return;
            }
        }

        amount = Mathf.RoundToInt(amount * DamageTakenMultiplier);
        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0);
        Debug.Log($"{name} took {amount} damage, health {CurrentHealth}/{maxHealth}");
        OnDamaged?.Invoke(amount, attackerPosition);

        if (CurrentHealth == 0)
            OnDied?.Invoke();
    }
}