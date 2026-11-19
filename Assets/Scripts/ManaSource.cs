using System.Collections.Generic;
using UnityEngine;

// A Fonte: a place in the world that holds mana of one element
public class ManaSource : MonoBehaviour
{
    private static readonly List<ManaSource> all = new List<ManaSource>();

    [Header("Mana")]
    [SerializeField] private Element element = Element.Wind;
    [ColorUsage(false, true)]
    [SerializeField] private Color color = new Color(0.4f, 0.9f, 2.5f);
    [SerializeField] private float radius = 5f;
    [SerializeField] private float capacity = 200f;       // how rich this Fonte is
    [SerializeField] private float refillPerSecond = 4f;  // fills back slowly when nobody draws
    [SerializeField] private float chargeMultiplier = 4f; // charging inside is this many times faster
    [SerializeField] private float sameElementBonus = 1.5f;

    [Header("Look")]
    [SerializeField] private Renderer crystal;
    [SerializeField] private Light sourceLight;
    [SerializeField] private Renderer area;
    [SerializeField] private ParticleSystem stream;
    [SerializeField] private float idleGlow = 1.5f;
    [SerializeField] private float drawGlow = 6f;
    [SerializeField] private float idleLight = 1f;
    [SerializeField] private float drawLight = 5f;
    [SerializeField] private float areaAlpha = 0.12f;

    public Element Element => element;
    public Color Color => color;
    public float Current { get; private set; }

    private Material crystalMaterial;
    private Color baseColor; // the color with its brightest channel at 1
    private float glow;
    private float lastDrawTime = -10f;
    private Transform drawer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetAll()
    {
        all.Clear();
    }

    // The closest Fonte whose circle contains this position, or null
    public static ManaSource FindAt(Vector3 position)
    {
        ManaSource best = null;
        float bestDistance = float.MaxValue;

        foreach (ManaSource source in all)
        {
            Vector3 offset = position - source.transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (distance <= source.radius && distance < bestDistance)
            {
                best = source;
                bestDistance = distance;
            }
        }
        return best;
    }

    private void Awake()
    {
        Current = capacity;
        baseColor = color / Mathf.Max(color.maxColorComponent, 0.01f);
        baseColor.a = 1f;
        glow = idleGlow;

        if (crystal != null)
        {
            crystalMaterial = crystal.material;
            crystalMaterial.EnableKeyword("_EMISSION");
        }

        if (sourceLight != null)
            sourceLight.color = baseColor;

        if (area != null)
        {
            area.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
            Color areaColor = baseColor;
            areaColor.a = areaAlpha;
            area.material.SetColor("_BaseColor", areaColor);
        }
    }

    private void OnEnable()
    {
        all.Add(this);
    }

    private void OnDisable()
    {
        all.Remove(this);
    }

    public float ChargeMultiplier(Element affinity)
    {
        return chargeMultiplier * (affinity == element ? sameElementBonus : 1f);
    }

    // Takes up to "amount" mana out of the Fonte and returns how much it really gave
    public float Draw(float amount, Transform who)
    {
        float given = Mathf.Min(amount, Current);
        Current -= given;
        lastDrawTime = Time.time;
        drawer = who;
        return given;
    }

    private void Update()
    {
        bool drawing = Time.time - lastDrawTime < 0.1f && Current > 0f;

        if (!drawing)
            Current = Mathf.Min(Current + refillPerSecond * Time.deltaTime, capacity);

        // Glows less as it runs dry, pulses hard while someone draws from it
        float fullness = capacity > 0f ? Current / capacity : 0f;
        float strength = Mathf.Lerp(0.15f, 1f, fullness);
        float pulse = drawing
            ? 1f + 0.35f * Mathf.Sin(Time.time * 18f)
            : 1f + 0.1f * Mathf.Sin(Time.time * 2f);

        float targetGlow = (drawing ? drawGlow : idleGlow) * strength * pulse;
        glow = Mathf.Lerp(glow, targetGlow, 10f * Time.deltaTime);
        if (crystalMaterial != null)
            crystalMaterial.SetColor("_EmissionColor", baseColor * glow);

        if (sourceLight != null)
            sourceLight.intensity = (drawing ? drawLight : idleLight) * strength * pulse;

        UpdateStream(drawing);
    }

    private void UpdateStream(bool on)
    {
        if (stream == null) return;

        if (on && drawer != null)
        {
            // Aim the river of particles at the player's chest
            Vector3 target = drawer.position + Vector3.up;
            stream.transform.LookAt(target);

            ParticleSystem.MainModule main = stream.main;
            main.startColor = baseColor;
            main.startSpeed = Vector3.Distance(stream.transform.position, target) / main.startLifetime.constant;

            if (!stream.isPlaying) stream.Play();
        }
        else if (stream.isPlaying)
        {
            stream.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}