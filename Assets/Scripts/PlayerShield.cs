using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Health), typeof(PlayerMovement), typeof(PlayerEnergy))]
public class PlayerShield : MonoBehaviour
{
    [SerializeField] private GameObject stoneWall;
    [SerializeField] private GameObject breakEffect;
    [SerializeField] private float blockSpeedMultiplier = 0.4f;
    [SerializeField] private float tapTime = 0.2f;

    [Header("Energy")]
    [SerializeField] private float minEnergyToBlock = 20f;
    [SerializeField] private float holdDrainPerSecond = 8f;
    [SerializeField] private float energyPerDamage = 2f;
    [SerializeField] private float breakCooldown = 2f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashForce = 10f;
    [SerializeField] private Vector3 dashBoxSize = new Vector3(1.2f, 1.5f, 1.2f);
    [SerializeField] private LayerMask enemyLayers;

    private InputAction blockAction;
    private Health health;
    private PlayerMovement movement;
    private PlayerEnergy energy;
    private CharacterController controller;
    private float pressTime;
    private float brokenUntil;
    private bool blockCancelled;
    private bool isBlocking;
    private bool isDashing;

    private void Awake()
    {
        health = GetComponent<Health>();
        movement = GetComponent<PlayerMovement>();
        energy = GetComponent<PlayerEnergy>();
        controller = GetComponent<CharacterController>();
        stoneWall.SetActive(false);
    }

    private void OnEnable()
    {
        health.OnBlocked += HandleBlocked;
    }

    private void OnDisable()
    {
        health.OnBlocked -= HandleBlocked;
    }

    private void Start()
    {
        blockAction = InputSystem.actions.FindAction("Player/Block");
    }

    private void Update()
    {
        if (blockAction.WasPressedThisFrame())
        {
            pressTime = Time.time;
            blockCancelled = false;
        }

        // Jumping drops the wall until the button is pressed again
        if (!controller.isGrounded)
            blockCancelled = true;

        bool wantsBlock = blockAction.IsPressed()
                          && !blockCancelled
                          && Time.time - pressTime >= tapTime
                          && Time.time >= brokenUntil;

        // Raising the wall needs a minimum of energy
        if (wantsBlock && !isBlocking && energy.Current < minEnergyToBlock)
            wantsBlock = false;

        SetBlocking(wantsBlock);

        // Holding the wall slowly drains energy
        if (isBlocking)
        {
            energy.Spend(holdDrainPerSecond * Time.deltaTime);
            if (energy.Current <= 0f)
                BreakWall();
        }

        // Released quickly: it was a tap, so dash
        if (blockAction.WasReleasedThisFrame() && Time.time - pressTime < tapTime
            && !isDashing && controller.isGrounded)
            StartCoroutine(Dash());
    }

    private void HandleBlocked(int amount, Vector3 attackerPosition)
    {
        energy.Spend(amount * energyPerDamage);
        if (energy.Current <= 0f)
            BreakWall();
    }

    private void BreakWall()
    {
        if (breakEffect != null)
            Instantiate(breakEffect, stoneWall.transform.position, stoneWall.transform.rotation);

        SetBlocking(false);
        brokenUntil = Time.time + breakCooldown;
        blockCancelled = true;
    }

    private void SetBlocking(bool value)
    {
        if (value == isBlocking) return;

        isBlocking = value;
        health.IsBlocking = value;
        stoneWall.SetActive(value);
        movement.SpeedMultiplier = value ? blockSpeedMultiplier : 1f;
        movement.LockRotation = value;
    }

    private IEnumerator Dash()
    {
        isDashing = true;
        float endTime = Time.time + dashDuration;
        var pushed = new HashSet<Rigidbody>();

        while (Time.time < endTime)
        {
            controller.Move(transform.forward * dashSpeed * Time.deltaTime);

            Vector3 center = transform.position + transform.forward;
            Collider[] hits = Physics.OverlapBox(center, dashBoxSize / 2f, transform.rotation, enemyLayers);
            foreach (Collider hit in hits)
            {
                Rigidbody enemyBody = hit.attachedRigidbody;
                if (enemyBody != null && pushed.Add(enemyBody))
                    enemyBody.AddForce(transform.forward * dashForce, ForceMode.Impulse);
            }

            yield return null;
        }

        isDashing = false;
    }
}