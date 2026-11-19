using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health), typeof(Rigidbody))]
public class EnemyRagdoll : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float launchSpeed = 4f;

    private Health health;
    private Rigidbody mainBody;
    private Collider mainCollider;
    private Rigidbody[] boneBodies;
    private Collider[] boneColliders;
    private Vector3 lastAttackerPosition;

    private void Awake()
    {
        health = GetComponent<Health>();
        mainBody = GetComponent<Rigidbody>();
        mainCollider = GetComponent<Collider>();

        // Finds the soldier's Animator by itself if the field is empty
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Bones created by the Ragdoll wizard
        boneBodies = animator.GetComponentsInChildren<Rigidbody>();
        boneColliders = animator.GetComponentsInChildren<Collider>();
        SetRagdoll(false);
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
        lastAttackerPosition = attackerPosition;
    }

    private void HandleDied()
    {
        // Turn off everything that keeps the enemy "alive"
        if (TryGetComponent(out EnemyAI ai)) ai.enabled = false;
        if (TryGetComponent(out NavMeshAgent agent)) agent.enabled = false;
        animator.enabled = false;
        mainBody.isKinematic = true;
        mainCollider.enabled = false;

        SetRagdoll(true);

        // Throw the body away from whoever hit it
        Vector3 away = transform.position - lastAttackerPosition;
        away.y = 0f;
        Vector3 launch = away.normalized * launchSpeed + Vector3.up * launchSpeed * 0.5f;
        foreach (Rigidbody bone in boneBodies)
            bone.AddForce(launch, ForceMode.VelocityChange);
    }

    private void SetRagdoll(bool active)
    {
        foreach (Rigidbody bone in boneBodies) bone.isKinematic = !active;
        foreach (Collider bone in boneColliders) bone.enabled = active;
    }
}