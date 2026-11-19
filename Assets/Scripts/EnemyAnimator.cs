using UnityEngine;

[RequireComponent(typeof(EnemyAI), typeof(Health), typeof(Rigidbody))]
public class EnemyAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float speedDamping = 0.1f;

    private static readonly int SpeedId = Animator.StringToHash("Speed");
    private static readonly int AttackId = Animator.StringToHash("Attack");
    private static readonly int HitId = Animator.StringToHash("Hit");

    private EnemyAI ai;
    private Health health;
    private Rigidbody body;

    private void Awake()
    {
        // Finds the soldier's Animator by itself if the field is empty
        if (animator == null) animator = GetComponentInChildren<Animator>();

        ai = GetComponent<EnemyAI>();
        health = GetComponent<Health>();
        body = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        ai.OnWindupStarted += HandleWindup;
        health.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        ai.OnWindupStarted -= HandleWindup;
        health.OnDamaged -= HandleDamaged;
    }

    private void Update()
    {
        if (!animator.enabled) return; // the ragdoll took over

        Vector3 velocity = body.linearVelocity;
        velocity.y = 0f;
        animator.SetFloat(SpeedId, velocity.magnitude, speedDamping, Time.deltaTime);
    }

    private void HandleWindup()
    {
        animator.SetTrigger(AttackId);
    }

    private void HandleDamaged(int amount, Vector3 attackerPosition)
    {
        animator.SetTrigger(HitId);
    }
}