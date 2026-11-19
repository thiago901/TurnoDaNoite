using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Changes the glow (Emission) of every visible part of the body
public class BodyGlow : MonoBehaviour
{
    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    private readonly List<Material> materials = new List<Material>();
    private readonly List<Color> originalColors = new List<Color>();
    private Color? holdColor;
    private bool isFlashing;

    private void Awake()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (!r.enabled || r is ParticleSystemRenderer) continue;

            // .materials makes a copy for this enemy only
            foreach (Material m in r.materials)
            {
                if (!m.HasProperty(EmissionId)) continue;
                m.EnableKeyword("_EMISSION");
                materials.Add(m);
                originalColors.Add(m.GetColor(EmissionId));
            }
        }
    }

    // Keeps a glow until cleared with null (red while preparing an attack)
    public void SetHold(Color? color)
    {
        holdColor = color;
        if (!isFlashing) Restore();
    }

    // Short glow, then back to the held or normal color
    public void Flash(Color color, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine(color, duration));
    }

    private IEnumerator FlashRoutine(Color color, float duration)
    {
        isFlashing = true;
        Apply(color);
        yield return new WaitForSeconds(duration);
        isFlashing = false;
        Restore();
    }

    private void Restore()
    {
        if (holdColor.HasValue)
        {
            Apply(holdColor.Value);
            return;
        }

        for (int i = 0; i < materials.Count; i++)
            materials[i].SetColor(EmissionId, originalColors[i]);
    }

    private void Apply(Color color)
    {
        foreach (Material m in materials)
            m.SetColor(EmissionId, color);
    }
}