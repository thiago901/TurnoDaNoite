using System.Collections;
using UnityEngine;

public class HitEffects : MonoBehaviour
{
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private GameObject hitSpark;
    [SerializeField] private GameObject finisherSpark;
    [SerializeField] private GameObject dustRing;
    [SerializeField] private DamageNumber damageNumber;
    [SerializeField] private TrailRenderer punchTrail;
    [SerializeField] private float trailTime = 0.4f;
    [SerializeField] private float feetOffset = 0.95f;

    private void OnEnable()
    {
        playerCombat.OnAttack += HandleAttack;
        playerCombat.OnTargetHit += HandleTargetHit;
        playerCombat.OnHitLanded += HandleHitLanded;
    }

    private void OnDisable()
    {
        playerCombat.OnAttack -= HandleAttack;
        playerCombat.OnTargetHit -= HandleTargetHit;
        playerCombat.OnHitLanded -= HandleHitLanded;
    }

    private void Start()
    {
        if (punchTrail != null) punchTrail.emitting = false;
    }

    private void HandleAttack(AttackData attack)
    {
        // Trail only on finishers
        if (attack.isFinisher && punchTrail != null)
        {
            StopAllCoroutines();
            StartCoroutine(TrailRoutine());
        }
    }

    private IEnumerator TrailRoutine()
    {
        punchTrail.emitting = true;
        yield return new WaitForSeconds(trailTime);
        punchTrail.emitting = false;
    }

    private void HandleTargetHit(Vector3 point, int damage, bool isFinisher)
    {
        GameObject spark = isFinisher ? finisherSpark : hitSpark;
        if (spark != null)
            Instantiate(spark, point, spark.transform.rotation);

        if (damageNumber != null)
        {
            DamageNumber number = Instantiate(damageNumber, point + Vector3.up * 0.5f, Quaternion.identity);
            number.Show(damage, isFinisher);
        }
    }

    private void HandleHitLanded(AttackData attack)
    {
        if (attack.isFinisher && dustRing != null)
        {
            Vector3 feet = transform.position + Vector3.down * feetOffset;
            Instantiate(dustRing, feet, dustRing.transform.rotation);
        }
    }
}