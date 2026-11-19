using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;

public class HitFeedback : MonoBehaviour
{
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private TMP_Text comboText;

    [SerializeField] private float hitStopTime = 0.05f;
    [SerializeField] private float finisherHitStopTime = 0.12f;
    [SerializeField] private float finisherShakeForce = 1.2f;
    [SerializeField] private float comboDisplayTime = 1.5f;

    private int hitCount;
    private float lastHitTime;
    private bool isStopped;

    private void OnEnable()
    {
        playerCombat.OnHitLanded += HandleHit;
    }

    private void OnDisable()
    {
        playerCombat.OnHitLanded -= HandleHit;
    }

    private void Start()
    {
        comboText.text = "";
    }

    private void Update()
    {
        if (hitCount > 0 && Time.unscaledTime - lastHitTime > comboDisplayTime)
        {
            hitCount = 0;
            comboText.text = "";
        }
    }

    private void HandleHit(AttackData attack)
    {
        hitCount++;
        lastHitTime = Time.unscaledTime;
        comboText.text = $"{hitCount} HITS";

        // Camera shake only on finishers
        if (attack.isFinisher)
            impulseSource.GenerateImpulse(finisherShakeForce);

        if (!isStopped)
            StartCoroutine(HitStop(attack.isFinisher ? finisherHitStopTime : hitStopTime));
    }

    private IEnumerator HitStop(float duration)
    {
        isStopped = true;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
        isStopped = false;
    }
}