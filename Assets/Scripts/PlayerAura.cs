using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerEnergy), typeof(PlayerMovement), typeof(Health))]
public class PlayerAura : MonoBehaviour
{
    [Header("Charge")]
    [SerializeField] private float chargePerSecond = 12f;           // mana per second away from Fontes
    [SerializeField] private float damageTakenWhileCharging = 1.5f; // the risk: hits hurt more
    [SerializeField] private Element affinity = Element.Wind;       // the player's own element

    [Header("Look")]
    [SerializeField] private ParticleSystem auraParticles;
    [SerializeField] private Light auraLight;
    [SerializeField] private float lightIntensity = 3f;
    [SerializeField] private float lightFadeSpeed = 10f;
    [ColorUsage(false, true)]
    [SerializeField] private Color elementColor = new Color(0.4f, 0.9f, 2.5f); // wind

    public event Action OnChargeStart; // PlayerAnimator plays the charge pose
    public event Action OnChargeEnd;   // PlayerAnimator goes back to Locomotion

    public bool IsCharging { get; private set; }

    private InputAction chargeAction;
    private PlayerEnergy energy;
    private PlayerMovement movement;
    private Health health;
    private CharacterController controller;
    private PlayerCombat combat;
    private PlayerDodge dodge;
    private PlayerPower power;
    private bool lockedOut; // hit while charging: wait until the button is pressed again

    private void Awake()
    {
        energy = GetComponent<PlayerEnergy>();
        movement = GetComponent<PlayerMovement>();
        health = GetComponent<Health>();
        controller = GetComponent<CharacterController>();
        combat = GetComponent<PlayerCombat>();
        dodge = GetComponent<PlayerDodge>();
        power = GetComponent<PlayerPower>();

        if (auraLight != null) auraLight.intensity = 0f;
        SetElementColor(elementColor);
    }

    private void OnEnable()
    {
        health.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        health.OnDamaged -= HandleDamaged;
    }

    private void Start()
    {
        chargeAction = InputSystem.actions.FindAction("Player/Charge");
    }

    private void Update()
    {
        if (chargeAction.WasReleasedThisFrame())
            lockedOut = false;

        // Attacking, rolling or blocking takes over and ends the charge
        bool busy = (combat != null && combat.IsAttacking)
                    || (dodge != null && dodge.IsDodging)
                    || health.IsBlocking
                    || (power != null && power.IsCasting);

        bool wantsCharge = chargeAction.IsPressed() && !lockedOut && !busy && controller.isGrounded;

        if (wantsCharge && !IsCharging)
            StartCharging();
        else if (!wantsCharge && IsCharging)
            StopCharging(!busy, !busy);

        if (IsCharging)
            Charge();

        UpdateLight();
    }

    // The player's own color, used away from the Fontes
    public void SetElementColor(Color color)
    {
        elementColor = color;
        ApplyColor(color);
    }

    private void Charge()
    {
        float amount = chargePerSecond * Time.deltaTime;
        ManaSource source = ManaSource.FindAt(transform.position);

        // Inside a Fonte: it gives the extra mana and paints the aura with its element
        if (source != null && source.Current > 0f)
        {
            ApplyColor(source.Color);
            if (!energy.IsFull)
            {
                float extra = amount * (source.ChargeMultiplier(affinity) - 1f);
                amount += source.Draw(extra, transform);
            }
        }
        else
        {
            ApplyColor(elementColor);
        }

        energy.Gain(amount);
    }

    private void ApplyColor(Color color)
    {
        if (auraParticles != null)
        {
            ParticleSystem.MainModule main = auraParticles.main;
            main.startColor = color;
        }

        if (auraLight != null)
        {
            float brightest = Mathf.Max(color.maxColorComponent, 0.01f);
            auraLight.color = color / brightest;
        }
    }

    private void StartCharging()
    {
        IsCharging = true;
        movement.SpeedMultiplier = 0f;
        movement.LockRotation = true;
        health.DamageTakenMultiplier = damageTakenWhileCharging;

        if (auraParticles != null) auraParticles.Play();
        OnChargeStart?.Invoke();
    }

    private void StopCharging(bool restoreMovement, bool returnAnimation)
    {
        IsCharging = false;
        health.DamageTakenMultiplier = 1f;

        if (auraParticles != null)
            auraParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // When an attack, a roll or the wall took over, they already control movement
        if (restoreMovement)
        {
            movement.SpeedMultiplier = 1f;
            movement.LockRotation = false;
        }

        if (returnAnimation)
            OnChargeEnd?.Invoke();
    }

    private void HandleDamaged(int amount, Vector3 attackerPosition)
    {
        if (!IsCharging) return;

        StopCharging(true, false); // the hit reaction plays instead
        lockedOut = true;
    }

    private void UpdateLight()
    {
        if (auraLight == null) return;

        float pulse = 1f + 0.2f * Mathf.Sin(Time.time * 14f);
        float target = IsCharging ? lightIntensity * pulse : 0f;
        auraLight.intensity = Mathf.MoveTowards(auraLight.intensity, target, lightFadeSpeed * Time.deltaTime);
    }
}