using System;
using UnityEngine;
using UnityEngine.UI;

// The player's mana. Max is the Nucleo: it grows during the game
public class PlayerEnergy : MonoBehaviour
{
    [SerializeField] private float maxEnergy = 100f;    // Nucleo size at the start
    [SerializeField] private float regenPerSecond = 2f; // slow refill on its own
    [SerializeField] private float regenDelay = 1f;     // wait after spending before refilling
    [SerializeField] private Slider energyBar;
    [SerializeField] private bool growBarWithCore = true;

    public float Current { get; private set; }
    public float Max => maxEnergy;
    public bool IsFull => Current >= maxEnergy;

    public event Action OnCoreExpanded;

    private float lastSpendTime = -10f;
    private RectTransform barRect;
    private float barBaseWidth;
    private float barBaseMax;

    private void Awake()
    {
        Current = maxEnergy;
    }

    private void Start()
    {
        if (energyBar != null)
        {
            barRect = energyBar.GetComponent<RectTransform>();
            barBaseWidth = barRect.rect.width;
            barBaseMax = maxEnergy;
            energyBar.maxValue = maxEnergy;
            energyBar.value = Current;
        }
    }

    private void Update()
    {
        if (Time.time - lastSpendTime >= regenDelay && Current < maxEnergy)
            Current = Mathf.Min(Current + regenPerSecond * Time.deltaTime, maxEnergy);

        if (energyBar != null)
            energyBar.value = Current;
    }

    public void Spend(float amount)
    {
        Current = Mathf.Max(Current - amount, 0f);
        lastSpendTime = Time.time;
    }

    // For powers: only spends when there is enough mana
    public bool TrySpend(float amount)
    {
        if (Current < amount) return false;
        Spend(amount);
        return true;
    }

    public void Gain(float amount)
    {
        Current = Mathf.Min(Current + amount, maxEnergy);
    }

    // A bigger Nucleo: more mana, a longer bar, and it comes full
    public void ExpandCore(float amount)
    {
        maxEnergy += amount;
        Current = maxEnergy;

        if (energyBar != null)
        {
            energyBar.maxValue = maxEnergy;
            if (growBarWithCore)
                barRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                    barBaseWidth * maxEnergy / barBaseMax);
        }

        OnCoreExpanded?.Invoke();
    }

    [ContextMenu("Test: expand core (+25)")]
    private void TestExpandCore()
    {
        ExpandCore(25f);
    }
}