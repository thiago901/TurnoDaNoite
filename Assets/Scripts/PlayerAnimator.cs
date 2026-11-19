using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(PlayerCombat), typeof(Health))]
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float speedDamping = 0.1f;
    [SerializeField] private string locomotionState = "Locomotion";
    [SerializeField] private float returnBlend = 0.15f;
    [SerializeField] private string dodgeState = "Dodge";
    [SerializeField] private float dodgeBlend = 0.05f;
    [SerializeField] private string chargeState = "Charge";
    [SerializeField] private float chargeBlend = 0.15f;

    private static readonly int SpeedId = Animator.StringToHash("Speed");
    private static readonly int GroundedId = Animator.StringToHash("Grounded");
    private static readonly int BlockingId = Animator.StringToHash("Blocking");
    private static readonly int HitId = Animator.StringToHash("Hit");

    private CharacterController controller;
    private PlayerCombat combat;
    private PlayerDodge dodge;
    private PlayerAura aura;
    private Health health;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        combat = GetComponent<PlayerCombat>();
        dodge = GetComponent<PlayerDodge>();
        aura = GetComponent<PlayerAura>();
        health = GetComponent<Health>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        combat.OnAttack += HandleAttack;
        combat.OnAttackCancelled += ReturnToLocomotion;
        health.OnDamaged += HandleDamaged;
        if (dodge != null)
        {
            dodge.OnDodge += HandleDodge;
            dodge.OnDodgeEnd += ReturnToLocomotion;
        }
        if (aura != null)
        {
            aura.OnChargeStart += HandleCharge;
            aura.OnChargeEnd += ReturnToLocomotion;
        }
    }

    private void OnDisable()
    {
        combat.OnAttack -= HandleAttack;
        combat.OnAttackCancelled -= ReturnToLocomotion;
        health.OnDamaged -= HandleDamaged;
        if (dodge != null)
        {
            dodge.OnDodge -= HandleDodge;
            dodge.OnDodgeEnd -= ReturnToLocomotion;
        }
        if (aura != null)
        {
            aura.OnChargeStart -= HandleCharge;
            aura.OnChargeEnd -= ReturnToLocomotion;
        }
    }

    private void Update()
    {
        Vector3 velocity = controller.velocity;
        velocity.y = 0f;

        animator.SetFloat(SpeedId, velocity.magnitude, speedDamping, Time.deltaTime);
        animator.SetBool(GroundedId, controller.isGrounded);
        animator.SetBool(BlockingId, health.IsBlocking);
    }

    // Powers and the super call these to play their own states
    public void Play(string state, float blend)
    {
        animator.CrossFadeInFixedTime(state, blend);
    }

    public void ReturnToLocomotion()
    {
        animator.CrossFadeInFixedTime(locomotionState, returnBlend);
    }

    private void HandleAttack(AttackData attack)
    {
        // Jump straight to the attack's state, blending from the current pose
        animator.CrossFadeInFixedTime(attack.animationState, attack.blendIn);
    }

    private void HandleDodge()
    {
        animator.CrossFadeInFixedTime(dodgeState, dodgeBlend);
    }

    private void HandleCharge()
    {
        animator.CrossFadeInFixedTime(chargeState, chargeBlend);
    }

    private void HandleDamaged(int amount, Vector3 attackerPosition)
    {
        animator.SetTrigger(HitId);
    }
}