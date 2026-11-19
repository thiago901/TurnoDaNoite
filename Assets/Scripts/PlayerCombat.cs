using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(PlayerMovement))]
public class PlayerCombat : MonoBehaviour
{
    private enum AttackInput { None, Light, Heavy }

    [Header("Combo start")]
    [SerializeField] private AttackData firstLight;
    [SerializeField] private AttackData firstHeavy;

    [Header("Input")]
    [SerializeField] private float holdTime = 0.35f;   // hold this long = heavy attack
    [SerializeField] private float inputBuffer = 0.5f; // early presses are remembered this long

    [Header("Counter")]
    [SerializeField] private float counterMultiplier = 2f; // damage after a perfect dodge

    [SerializeField] private LayerMask hitLayers;

    public event Action<AttackData> OnAttack;            // every attack (PlayerAnimator)
    public event Action OnAttackCancelled;               // recovery cut by walking
    public event Action<AttackData> OnHitLanded;         // attack hit at least one enemy
    public event Action<Vector3, int, bool> OnTargetHit; // hit point, damage, is finisher

    public bool IsAttacking => current != null;

    private InputAction attackAction;
    private InputAction moveAction;
    private CharacterController controller;
    private PlayerMovement movement;
    private PlayerDodge dodge;
    private PlayerPower power;

    private AttackData current;
    private float attackStart;
    private float pressStart;
    private bool heavyFired;
    private AttackInput buffered = AttackInput.None;
    private float bufferedUntil;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PlayerMovement>();
        dodge = GetComponent<PlayerDodge>();
        power = GetComponent<PlayerPower>();
    }

    private void Start()
    {
        attackAction = InputSystem.actions.FindAction("Player/Attack");
        moveAction = InputSystem.actions.FindAction("Player/Move");
    }

    private void Update()
    {
        ReadInput();

        if (current != null)
        {
            float elapsed = Time.time - attackStart;

            // Walking after the cancel point cuts the recovery, like in Genshin
            bool moving = moveAction.ReadValue<Vector2>().sqrMagnitude > 0.01f;
            if (elapsed >= current.cancelTime && moving && buffered == AttackInput.None)
            {
                EndAttack();
                OnAttackCancelled?.Invoke();
            }
            else if (elapsed >= current.duration)
            {
                EndAttack();
            }
        }

        // While rolling, presses wait in the buffer and fire when the roll ends
        bool dodging = (dodge != null && dodge.IsDodging) || (power != null && power.IsCasting);
        bool canChain = !dodging && (current == null || Time.time - attackStart >= current.cancelTime);
        if (canChain && buffered != AttackInput.None && Time.time <= bufferedUntil)
        {
            AttackData next = PickNext(buffered);
            buffered = AttackInput.None;
            if (next != null) StartAttack(next);
        }
    }

    // Called by PlayerDodge: stops the attack right away, even before the hit
    public void CancelAttack()
    {
        StopAllCoroutines();
        buffered = AttackInput.None;
        if (current != null) EndAttack();
    }

    private void ReadInput()
    {
        if (attackAction.WasPressedThisFrame())
        {
            pressStart = Time.time;
            heavyFired = false;
        }

        // Held long enough: heavy attack, fired while still holding
        if (attackAction.IsPressed() && !heavyFired && Time.time - pressStart >= holdTime)
        {
            heavyFired = true;
            Buffer(AttackInput.Heavy);
        }

        // Released before the hold time: light attack
        if (attackAction.WasReleasedThisFrame() && !heavyFired)
            Buffer(AttackInput.Light);
    }

    private void Buffer(AttackInput input)
    {
        buffered = input;
        bufferedUntil = Time.time + inputBuffer;
    }

    private AttackData PickNext(AttackInput input)
    {
        // Follow the combo tree; at the end of a branch, start again
        if (current != null)
        {
            AttackData chained = input == AttackInput.Light ? current.nextLight : current.nextHeavy;
            if (chained != null) return chained;
        }
        return input == AttackInput.Light ? firstLight : firstHeavy;
    }

    private void StartAttack(AttackData attack)
    {
        StopAllCoroutines();
        current = attack;
        attackStart = Time.time;

        // Face the direction the player is pushing
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 direction = new Vector3(input.x, 0f, input.y);
        if (direction.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(direction);

        movement.SpeedMultiplier = 0f;
        movement.LockRotation = true;

        OnAttack?.Invoke(attack);
        StartCoroutine(AttackRoutine(attack));
    }

    private IEnumerator AttackRoutine(AttackData attack)
    {
        // Small step forward until the blade lands
        float elapsed = 0f;
        float stepTime = Mathf.Max(attack.hitDelay, 0.01f);
        while (elapsed < attack.hitDelay)
        {
            controller.Move(transform.forward * (attack.lunge / stepTime) * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        DoHit(attack);
    }

    private void EndAttack()
    {
        current = null;
        movement.SpeedMultiplier = 1f;
        movement.LockRotation = false;
    }

    private void DoHit(AttackData attack)
    {
        Vector3 center = HitBoxCenter(attack);
        Collider[] hits = Physics.OverlapBox(center, attack.hitBoxSize / 2f, transform.rotation, hitLayers);
        bool counter = dodge != null && dodge.CounterReady;

        bool hitSomething = false;
        if (attack.hitsAll)
        {
            foreach (Collider hit in hits)
                hitSomething |= HitTarget(hit, attack, center, counter);
        }
        else
        {
            Collider closest = FindClosest(hits);
            if (closest != null)
                hitSomething = HitTarget(closest, attack, center, counter);
        }

        if (hitSomething)
        {
            if (counter) dodge.ConsumeCounter();
            OnHitLanded?.Invoke(attack);
        }
    }

    private bool HitTarget(Collider hit, AttackData attack, Vector3 center, bool counter)
    {
        if (!hit.TryGetComponent(out Health health) || health.CurrentHealth <= 0)
            return false;

        // A counter after a perfect dodge hits harder and shows the big number
        int damage = counter ? Mathf.RoundToInt(attack.damage * counterMultiplier) : attack.damage;
        bool finisher = attack.isFinisher || counter;

        health.TakeDamage(damage, transform.position);
        OnTargetHit?.Invoke(hit.ClosestPoint(center), damage, finisher);

        if (hit.attachedRigidbody != null)
        {
            Vector3 push = transform.forward * attack.knockback + Vector3.up * attack.launch;
            hit.attachedRigidbody.AddForce(push, ForceMode.Impulse);
        }
        return true;
    }

    private Collider FindClosest(Collider[] hits)
    {
        Collider closest = null;
        float bestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            if (!hit.TryGetComponent(out Health health) || health.CurrentHealth <= 0)
                continue;

            float d = (hit.transform.position - transform.position).sqrMagnitude;
            if (d < bestDistance)
            {
                bestDistance = d;
                closest = hit;
            }
        }
        return closest;
    }

    private Vector3 HitBoxCenter(AttackData attack)
    {
        return transform.position + transform.forward * attack.hitBoxDistance;
    }

    private void OnDrawGizmos()
    {
        AttackData shown = current != null ? current : firstLight;
        if (shown == null) return;

        Gizmos.color = Color.red;
        Gizmos.matrix = Matrix4x4.TRS(HitBoxCenter(shown), transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, shown.hitBoxSize);
    }
}