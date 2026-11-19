using UnityEngine;

[RequireComponent(typeof(Health), typeof(BodyGlow))]
public class HitReaction : MonoBehaviour
{
    [ColorUsage(false, true)]
    [SerializeField] private Color flashColor = new Color(3f, 3f, 3f);
    [SerializeField] private float flashTime = 0.1f;
    [SerializeField] private float destroyDelay = 3f; // time the body stays on the ground

    private Health health;
    private BodyGlow glow;

    private void Awake()
    {
        health = GetComponent<Health>();
        glow = GetComponent<BodyGlow>();
    }

    private void OnEnable()
    {
        health.OnDamaged += HandleDamaged;
        health.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        health.OnDamaged -= HandleDamaged;
        health.OnDied -= HandleDied;
    }

    private void HandleDamaged(int amount, Vector3 attackerPosition)
    {
        glow.Flash(flashColor, flashTime);
    }

    private void HandleDied()
    {
        Destroy(gameObject, destroyDelay);
    }
}