using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody), typeof(NavMeshAgent), typeof(Health))]
[RequireComponent(typeof(BodyGlow))]
public class EnemyAI : MonoBehaviour
{
    private enum State { Idle, Alert, Chase, Wait, Windup, Recover, Stunned }

    // Shared by all enemies: only a few can attack at the same time
    private static readonly List<EnemyAI> allEnemies = new List<EnemyAI>();
    private static int attackersNow;

    // Fired when the enemy starts preparing a punch (used by EnemyAnimator)
    public event Action OnWindupStarted;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float waitDistance = 3f;

    [Header("Perception")]
    [SerializeField] private float viewDistance = 8f;
    [SerializeField] private float viewAngle = 120f;
    [SerializeField] private float hearingDistance = 2.5f;
    [SerializeField] private float alertTime = 0.6f;
    [SerializeField] private float callForHelpRadius = 6f;
    [SerializeField] private LayerMask obstacleMask = 1; // Default
    [SerializeField] private GameObject alertIcon;

    [Header("Attack")]
    [SerializeField] private int maxAttackers = 2;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float maxHeightDifference = 1f;
    [SerializeField] private float windupTime = 0.5f;
    [SerializeField] private float recoverTime = 1f;
    [SerializeField] private float stunTime = 0.5f;
    [SerializeField] private int damage = 10;
    [ColorUsage(false, true)]
    [SerializeField] private Color windupColor = new Color(3f, 0.2f, 0.2f);

    private State state = State.Idle;
    private Transform player;
    private Rigidbody body;
    private NavMeshAgent agent;
    private Health health;
    private BodyGlow glow;
    private float stateEndTime;
    private bool hasToken;

    // Seconds until the punch lands, or -1 when not preparing one (used by PlayerDodge)
    public float WindupTimeLeft => state == State.Windup ? windupEndsAt - Time.time : -1f;
    private float windupEndsAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetShared()
    {
        allEnemies.Clear();
        attackersNow = 0;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        glow = GetComponent<BodyGlow>();

        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.speed = moveSpeed;

        if (alertIcon != null) alertIcon.SetActive(false);
    }

    private void OnEnable()
    {
        allEnemies.Add(this);
        health.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        allEnemies.Remove(this);
        health.OnDamaged -= HandleDamaged;
        ReleaseToken();
        HideAlert();
        if (glow != null) glow.SetHold(null);
    }

    private void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
    }

    private void FixedUpdate()
    {
        agent.nextPosition = body.position;
        if (player == null) return;

        switch (state)
        {
            case State.Idle:
                StopMoving();
                if (CanSeePlayer()) BecomeAlert(true);
                break;

            case State.Alert:
                StopMoving();
                FaceDirection(player.position - body.position);
                if (Time.time >= stateEndTime)
                {
                    HideAlert();
                    state = State.Chase;
                }
                break;

            case State.Stunned:
                // Don't touch the velocity, so the knockback plays
                if (Time.time >= stateEndTime) state = State.Chase;
                break;

            case State.Chase:
            case State.Wait:
                UpdateChase();
                break;

            default: // Windup and Recover are run by AttackRoutine
                StopMoving();
                break;
        }
    }

    private void LateUpdate()
    {
        // Keep the "!" facing the camera
        if (alertIcon != null && alertIcon.activeSelf && Camera.main != null)
            alertIcon.transform.rotation = Camera.main.transform.rotation;
    }

    private void UpdateChase()
    {
        Vector3 toPlayer = player.position - body.position;
        float heightDifference = Mathf.Abs(toPlayer.y);
        toPlayer.y = 0f;
        FaceDirection(toPlayer);

        bool inRange = toPlayer.magnitude <= attackRange && heightDifference <= maxHeightDifference;
        if (inRange && TryTakeToken())
        {
            StopMoving();
            StartCoroutine(AttackRoutine());
            return;
        }

        if (hasToken || attackersNow < maxAttackers)
        {
            state = State.Chase;
            MoveTo(player.position);
        }
        else
        {
            // Too many attackers already: wait in a ring around the player
            state = State.Wait;
            Vector3 away = body.position - player.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = Vector3.right;

            Vector3 spot = player.position + away.normalized * waitDistance;
            if (Vector3.Distance(body.position, spot) > 0.3f)
                MoveTo(spot);
            else
                StopMoving();
        }
    }

    private bool CanSeePlayer()
    {
        Vector3 eye = body.position + Vector3.up * 0.5f;
        Vector3 target = player.position + Vector3.up * 0.5f;
        Vector3 toPlayer = target - eye;
        float distance = toPlayer.magnitude;

        if (distance <= hearingDistance) return true; // too close to miss
        if (distance > viewDistance) return false;

        Vector3 flat = new Vector3(toPlayer.x, 0f, toPlayer.z);
        if (Vector3.Angle(transform.forward, flat) > viewAngle * 0.5f) return false;

        // Is a wall or object in the way?
        return !Physics.Linecast(eye, target, obstacleMask);
    }

    private void BecomeAlert(bool callOthers)
    {
        if (state != State.Idle) return;

        state = State.Alert;
        stateEndTime = Time.time + alertTime;
        if (alertIcon != null) alertIcon.SetActive(true);

        if (callOthers) AlertNearby();
    }

    private void AlertNearby()
    {
        foreach (EnemyAI other in allEnemies)
        {
            if (other != this && Vector3.Distance(other.transform.position, transform.position) <= callForHelpRadius)
                other.BecomeAlert(false);
        }
    }

    private void HideAlert()
    {
        if (alertIcon != null) alertIcon.SetActive(false);
    }

    private bool TryTakeToken()
    {
        if (hasToken) return true;
        if (attackersNow >= maxAttackers) return false;

        attackersNow++;
        hasToken = true;
        return true;
    }

    private void ReleaseToken()
    {
        if (!hasToken) return;
        attackersNow = Mathf.Max(attackersNow - 1, 0);
        hasToken = false;
    }

    private void MoveTo(Vector3 target)
    {
        agent.SetDestination(target);
        Vector3 desired = agent.desiredVelocity;
        float verticalSpeed = desired.y > 0f ? desired.y : body.linearVelocity.y;
        body.linearVelocity = new Vector3(desired.x, verticalSpeed, desired.z);
    }

    private void StopMoving()
    {
        body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);
    }

    private void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    private IEnumerator AttackRoutine()
    {
        // Warning: glow red while the punch is being prepared
        state = State.Windup;
        windupEndsAt = Time.time + windupTime;
        OnWindupStarted?.Invoke();
        glow.SetHold(windupColor);
        yield return new WaitForSeconds(windupTime);
        glow.SetHold(null);

        Vector3 toPlayer = player.position - transform.position;
        bool closeEnough = new Vector3(toPlayer.x, 0f, toPlayer.z).magnitude <= attackRange + 0.3f
                           && Mathf.Abs(toPlayer.y) <= maxHeightDifference;

        if (closeEnough && player.TryGetComponent(out Health playerHealth))
            playerHealth.TakeDamage(damage, transform.position);

        state = State.Recover;
        yield return new WaitForSeconds(recoverTime);
        ReleaseToken();
        state = State.Chase;
    }

    private void HandleDamaged(int amount, Vector3 attackerPosition)
    {
        // Getting hit cancels the attack and wakes up the group
        StopAllCoroutines();
        glow.SetHold(null);
        ReleaseToken();
        HideAlert();
        AlertNearby();

        state = State.Stunned;
        stateEndTime = Time.time + stunTime;
    }
}