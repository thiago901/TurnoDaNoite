using System.Collections.Generic;
using UnityEngine;

// Something that flies forward and hurts what it touches: wind blade, Legion bolts
public class Projectile : MonoBehaviour
{
    [SerializeField] private Vector3 hitBoxSize = new Vector3(2.4f, 1.2f, 0.6f);
    [SerializeField] private bool pierce = true;       // keeps flying through targets
    [SerializeField] private LayerMask hitLayers;      // who it hurts
    [SerializeField] private LayerMask stopLayers = 1; // walls that stop it (Default)
    [SerializeField] private GameObject hitSpark;
    [SerializeField] private DamageNumber damageNumber;
    [SerializeField] private float fadeTime = 0.25f;   // lets the trails fade before it disappears

    private readonly HashSet<Health> alreadyHit = new HashSet<Health>();
    private int damage;
    private float speed;
    private float range;
    private float knockback;
    private float launch;
    private Vector3 attackerPosition;
    private float travelled;
    private bool launched;
    private bool finished;

    public void Launch(int damage, float speed, float range, float knockback, float launch, Vector3 attackerPosition)
    {
        this.damage = damage;
        this.speed = speed;
        this.range = range;
        this.knockback = knockback;
        this.launch = launch;
        this.attackerPosition = attackerPosition;
        launched = true;
    }

    private void Update()
    {
        if (!launched || finished) return;

        float step = speed * Time.deltaTime;
        transform.position += transform.forward * step;
        travelled += step;

        Vector3 half = hitBoxSize / 2f;
        Collider[] hits = Physics.OverlapBox(transform.position, half, transform.rotation, hitLayers);
        foreach (Collider hit in hits)
        {
            Health health = hit.GetComponentInParent<Health>();
            if (health == null || health.CurrentHealth <= 0 || !alreadyHit.Add(health))
                continue;

            Vector3 point = hit.ClosestPoint(transform.position);
            health.TakeDamage(damage, attackerPosition);
            ShowHit(point);

            Rigidbody body = hit.attachedRigidbody;
            if (body != null && !body.isKinematic)
                body.AddForce(transform.forward * knockback + Vector3.up * launch, ForceMode.Impulse);

            if (!pierce)
            {
                Finish();
                return;
            }
        }

        bool hitWall = Physics.CheckBox(transform.position, half * 0.5f, transform.rotation,
                                        stopLayers, QueryTriggerInteraction.Ignore);
        if (travelled >= range || hitWall)
            Finish();
    }

    private void ShowHit(Vector3 point)
    {
        if (hitSpark != null)
            Instantiate(hitSpark, point, hitSpark.transform.rotation);

        if (damageNumber != null)
        {
            DamageNumber number = Instantiate(damageNumber, point + Vector3.up * 0.5f, Quaternion.identity);
            number.Show(damage, false);
        }
    }

    private void Finish()
    {
        finished = true;

        // Hide the shape now, keep the trails a moment so they fade out
        foreach (MeshRenderer shape in GetComponentsInChildren<MeshRenderer>())
            shape.enabled = false;

        Destroy(gameObject, fadeTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, hitBoxSize);
    }
}