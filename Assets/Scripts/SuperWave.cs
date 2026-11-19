using UnityEngine;

// A flat ring that grows fast and fades out (super impact, captain's area attack)
public class SuperWave : MonoBehaviour
{
    [SerializeField] private float maxSize = 24f;
    [SerializeField] private float growTime = 0.35f;
    [SerializeField] private float fadeTime = 0.3f;
    [ColorUsage(true, true)]
    [SerializeField] private Color color = new Color(0.5f, 0.85f, 1f, 1f);

    private Material material;
    private float age;

    private void Awake()
    {
        material = GetComponentInChildren<Renderer>().material;
        transform.localScale = new Vector3(0.1f, 1f, 0.1f);
    }

    public void SetSize(float size)
    {
        maxSize = size;
    }

    public void SetColor(Color newColor)
    {
        color = newColor;
    }

    private void Update()
    {
        // Real time, so it keeps growing during the hit stop
        age += Time.unscaledDeltaTime;

        float t = Mathf.Clamp01(age / growTime);
        float size = Mathf.Lerp(0.1f, maxSize, 1f - (1f - t) * (1f - t));
        transform.localScale = new Vector3(size, 1f, size);

        float fade = age <= growTime ? 1f : 1f - (age - growTime) / fadeTime;
        Color current = color;
        current.a = Mathf.Clamp01(fade);
        material.SetColor("_BaseColor", current);

        if (fade <= 0f)
            Destroy(gameObject);
    }
}