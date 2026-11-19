using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(PlayerMovement), typeof(Health))]
public class PlayerDodge : MonoBehaviour
{
    [Header("Dodge")]
    [SerializeField] private float distance = 4f;
    [SerializeField] private float duration = 0.45f;
    [SerializeField] private float cooldown = 0.25f;
    [SerializeField] private GameObject dustEffect;

    [Header("Invulnerability")]
    [SerializeField] private float invulnerableStart = 0.03f;
    [SerializeField] private float invulnerableTime = 0.3f;

    [Header("Perfect dodge")]
    [SerializeField] private float perfectWindow = 0.25f; // the enemy hit lands within this time
    [SerializeField] private float perfectRadius = 3f;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private float slowScale = 0.3f;
    [SerializeField] private float slowTime = 0.6f;       // real seconds
    [SerializeField] private float counterTime = 1.5f;    // real seconds to land the counter
    [SerializeField] private GameObject perfectEffect;

    public event Action OnDodge;        // PlayerAnimator plays the roll
    public event Action OnDodgeEnd;     // PlayerAnimator goes back to Locomotion
    public event Action OnPerfectDodge;

    public bool IsDodging { get; private set; }
    public bool CounterReady => Time.unscaledTime <= counterUntil;

    private InputAction dodgeAction;
    private InputAction moveAction;
    private CharacterController controller;
    private PlayerMovement movement;
    private PlayerCombat combat;
    private Health health;
    private PlayerPower power;
    private float readyAt;
    private float counterUntil = -1f;
    private bool slowActive;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PlayerMovement>();
        combat = GetComponent<PlayerCombat>();
        health = GetComponent<Health>();
        power = GetComponent<PlayerPower>();
    }

    private void Start()
    {
        dodgeAction = InputSystem.actions.FindAction("Player/Dodge");
        moveAction = InputSystem.actions.FindAction("Player/Move");
    }

    private void Update()
    {
        bool canDodge = !IsDodging && Time.time >= readyAt
                        && controller.isGrounded && !health.IsBlocking
                        && (power == null || !power.IsCasting); ;

        if (dodgeAction.WasPressedThisFrame() && canDodge)
            StartCoroutine(DodgeRoutine());
    }

    public void ConsumeCounter()
    {
        counterUntil = -1f;
    }

    private IEnumerator DodgeRoutine()
    {
        IsDodging = true;

        // The dodge cuts any attack, at any moment
        if (combat != null) combat.CancelAttack();
        movement.SpeedMultiplier = 0f;
        movement.LockRotation = true;

        // Roll where the player is pushing, or forward if nothing is pressed
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 direction = new Vector3(input.x, 0f, input.y);
        direction = direction.sqrMagnitude > 0.01f ? direction.normalized : transform.forward;
        transform.rotation = Quaternion.LookRotation(direction);

        if (dustEffect != null)
            Instantiate(dustEffect, transform.position, Quaternion.identity);

        CheckPerfectDodge();
        OnDodge?.Invoke();

        float elapsed = 0f;
        float done = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - (1f - t) * (1f - t); // fast start, soft stop
            controller.Move(direction * distance * (eased - done));
            done = eased;

            health.IsInvulnerable = elapsed >= invulnerableStart
                                    && elapsed <= invulnerableStart + invulnerableTime;
            yield return null;
        }

        health.IsInvulnerable = false;
        movement.SpeedMultiplier = 1f;
        movement.LockRotation = false;
        IsDodging = false;
        readyAt = Time.time + cooldown;
        OnDodgeEnd?.Invoke();
    }

    private void CheckPerfectDodge()
    {
        // Perfect: a nearby enemy is about to land its punch
        Collider[] hits = Physics.OverlapSphere(transform.position, perfectRadius, enemyLayers);
        foreach (Collider hit in hits)
        {
            EnemyAI enemy = hit.GetComponentInParent<EnemyAI>();
            if (enemy == null) continue;

            float timeLeft = enemy.WindupTimeLeft;
            if (timeLeft < 0f || timeLeft > perfectWindow) continue;

            counterUntil = Time.unscaledTime + counterTime;
            if (perfectEffect != null)
                Instantiate(perfectEffect, transform.position + Vector3.up, Quaternion.identity);
            if (!slowActive)
                StartCoroutine(SlowMotion());
            OnPerfectDodge?.Invoke();
            return;
        }
    }

    private IEnumerator SlowMotion()
    {
        slowActive = true;
        Time.timeScale = slowScale;
        yield return new WaitForSecondsRealtime(slowTime);
        Time.timeScale = 1f;
        slowActive = false;
    }
}