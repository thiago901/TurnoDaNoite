using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerEnergy), typeof(PlayerMovement), typeof(PlayerAnimator))]
[RequireComponent(typeof(Health))]
public class PlayerPower : MonoBehaviour
{
    [Header("Power")]
    [SerializeField] private PowerData power;

    [Header("UI")]
    [SerializeField] private Image cooldownIcon;
    [SerializeField] private Image superIcon;
    [SerializeField] private Color readyColor = Color.white;
    [SerializeField] private Color notReadyColor = new Color(0.35f, 0.35f, 0.45f);
    [SerializeField] private Color superReadyColor = new Color(1f, 0.82f, 0.3f);

    [Header("Super")]
    [SerializeField] private string superName = "TEMPESTADE";
    [SerializeField] private string superState = "Super";
    [SerializeField] private float superImpactDelay = 1f; // game seconds until the hit
    [SerializeField] private float superSlowScale = 0.3f;
    [SerializeField] private float superSlowTime = 0.6f;  // real seconds of slow motion and close camera
    [SerializeField] private float superRecover = 0.6f;
    [SerializeField] private float superRadius = 12f;
    [SerializeField] private int superDamage = 40;
    [SerializeField] private float superKnockback = 10f;
    [SerializeField] private float superLaunch = 6f;
    [SerializeField] private float superShakeForce = 3f;
    [SerializeField] private float superHitStop = 0.25f;
    [SerializeField] private float feetOffset = 0.95f;
    [SerializeField] private LayerMask superHitLayers;

    [Header("Super look")]
    [SerializeField] private GameObject superCamera;
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private GameObject superStartEffect;
    [SerializeField] private SuperWave superWave;
    [SerializeField] private GameObject superHitSpark;
    [SerializeField] private DamageNumber damageNumber;
    [SerializeField] private Image screenFlash;
    [SerializeField] private TMP_Text superText;

    // True while a power or the super is being used (combat, aura and dodge wait for it)
    public bool IsCasting { get; private set; }

    private InputAction powerAction;
    private InputAction superAction;
    private InputAction moveAction;
    private PlayerEnergy energy;
    private PlayerMovement movement;
    private PlayerAnimator playerAnimator;
    private PlayerCombat combat;
    private CharacterController controller;
    private Health health;
    private float readyAt;

    private void Awake()
    {
        energy = GetComponent<PlayerEnergy>();
        movement = GetComponent<PlayerMovement>();
        playerAnimator = GetComponent<PlayerAnimator>();
        combat = GetComponent<PlayerCombat>();
        controller = GetComponent<CharacterController>();
        health = GetComponent<Health>();

        if (superCamera != null) superCamera.SetActive(false);
        if (superText != null) superText.gameObject.SetActive(false);
    }

    private void Start()
    {
        powerAction = InputSystem.actions.FindAction("Player/Power");
        superAction = InputSystem.actions.FindAction("Player/Super");
        moveAction = InputSystem.actions.FindAction("Player/Move");
    }

    private void Update()
    {
        if (powerAction.WasPressedThisFrame())
            TryCast();

        if (superAction.WasPressedThisFrame())
            TrySuper();

        UpdateIcons();
    }

    // ---------- Power ----------

    private void TryCast()
    {
        if (power == null || IsCasting || !controller.isGrounded) return;
        if (Time.time < readyAt) return;
        if (!energy.TrySpend(power.manaCost)) return;

        readyAt = Time.time + power.cooldown;
        StartCoroutine(CastRoutine(power));
    }

    private IEnumerator CastRoutine(PowerData data)
    {
        BeginAction();
        playerAnimator.Play(data.animationState, data.blendIn);

        yield return new WaitForSeconds(data.castDelay);
        SpawnProjectile(data);

        yield return new WaitForSeconds(Mathf.Max(data.duration - data.castDelay, 0f));
        EndAction();
    }

    private void SpawnProjectile(PowerData data)
    {
        if (data.projectile == null) return;

        Vector3 position = transform.TransformPoint(data.spawnOffset);
        Projectile shot = Instantiate(data.projectile, position, transform.rotation);
        shot.Launch(data.damage, data.speed, data.range, data.knockback, data.launch, transform.position);
    }

    // ---------- Super ----------

    private void TrySuper()
    {
        if (IsCasting || !controller.isGrounded || !energy.IsFull) return;
        StartCoroutine(SuperRoutine());
    }

    private IEnumerator SuperRoutine()
    {
        BeginAction();
        health.IsInvulnerable = true;
        energy.Spend(energy.Max);

        // The show: close camera, slow motion and the name on screen
        if (superCamera != null) superCamera.SetActive(true);
        if (superText != null)
        {
            superText.text = superName;
            superText.gameObject.SetActive(true);
        }
        if (superStartEffect != null)
            Instantiate(superStartEffect, transform.position, Quaternion.identity);
        playerAnimator.Play(superState, 0.05f);

        float start = Time.time;
        Time.timeScale = superSlowScale;
        yield return new WaitForSecondsRealtime(superSlowTime);
        Time.timeScale = 1f;

        if (superCamera != null) superCamera.SetActive(false);
        if (superText != null) superText.gameObject.SetActive(false);

        float left = superImpactDelay - (Time.time - start);
        if (left > 0f)
            yield return new WaitForSeconds(left);

        SuperImpact();
        StartCoroutine(FlashRoutine());

        // Long pause on the impact
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(superHitStop);
        Time.timeScale = 1f;

        yield return new WaitForSeconds(superRecover);
        health.IsInvulnerable = false;
        EndAction();
    }

    private void SuperImpact()
    {
        Vector3 center = transform.position;
        var done = new HashSet<Health>();

        foreach (Collider hit in Physics.OverlapSphere(center, superRadius, superHitLayers))
        {
            Health target = hit.GetComponentInParent<Health>();
            if (target == null || target.CurrentHealth <= 0 || !done.Add(target))
                continue;

            target.TakeDamage(superDamage, center);

            Vector3 point = hit.ClosestPoint(center + Vector3.up);
            if (superHitSpark != null)
                Instantiate(superHitSpark, point, superHitSpark.transform.rotation);
            if (damageNumber != null)
            {
                DamageNumber number = Instantiate(damageNumber, point + Vector3.up * 0.5f, Quaternion.identity);
                number.Show(superDamage, true);
            }

            Rigidbody body = hit.attachedRigidbody;
            if (body != null && !body.isKinematic)
            {
                Vector3 away = hit.transform.position - center;
                away.y = 0f;
                body.AddForce(away.normalized * superKnockback + Vector3.up * superLaunch, ForceMode.Impulse);
            }
        }

        Breakable.BreakAllWithin(center, superRadius);

        if (superWave != null)
        {
            SuperWave wave = Instantiate(superWave, center + Vector3.down * feetOffset, Quaternion.identity);
            wave.SetSize(superRadius * 2f);
        }

        if (impulseSource != null)
            impulseSource.GenerateImpulse(superShakeForce);
    }

    private IEnumerator FlashRoutine()
    {
        if (screenFlash == null) yield break;

        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.unscaledDeltaTime;
            Color color = screenFlash.color;
            color.a = Mathf.Lerp(0.85f, 0f, t / 0.5f);
            screenFlash.color = color;
            yield return null;
        }
    }

    // ---------- Shared ----------

    private void BeginAction()
    {
        IsCasting = true;

        // Powers cut the combo, like a skill in Genshin
        if (combat != null) combat.CancelAttack();
        movement.SpeedMultiplier = 0f;
        movement.LockRotation = true;

        // Face the direction the player is pushing
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 direction = new Vector3(input.x, 0f, input.y);
        if (direction.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    private void EndAction()
    {
        movement.SpeedMultiplier = 1f;
        movement.LockRotation = false;
        IsCasting = false;
        playerAnimator.ReturnToLocomotion();
    }

    private void UpdateIcons()
    {
        if (cooldownIcon != null && power != null)
        {
            float left = Mathf.Max(readyAt - Time.time, 0f);
            cooldownIcon.fillAmount = power.cooldown > 0f ? 1f - left / power.cooldown : 1f;
            bool ready = left <= 0f && energy.Current >= power.manaCost;
            cooldownIcon.color = ready ? readyColor : notReadyColor;
        }

        if (superIcon != null)
        {
            superIcon.fillAmount = energy.Max > 0f ? energy.Current / energy.Max : 0f;
            superIcon.color = energy.IsFull ? superReadyColor : notReadyColor;
        }
    }
}