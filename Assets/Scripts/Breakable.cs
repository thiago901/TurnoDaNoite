using System.Collections.Generic;
using UnityEngine;

// A prop the super (or a captain) can smash into pieces
public class Breakable : MonoBehaviour
{
    private static readonly List<Breakable> all = new List<Breakable>();

    [SerializeField] private GameObject breakEffect;
    [SerializeField] private Material debrisMaterial; // empty = same material as the prop
    [SerializeField] private int debrisCount = 8;
    [SerializeField] private float debrisSize = 0.4f;
    [SerializeField] private float debrisForce = 7f;
    [SerializeField] private float debrisLifetime = 4f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetAll()
    {
        all.Clear();
    }

    private void OnEnable()
    {
        all.Add(this);
    }

    private void OnDisable()
    {
        all.Remove(this);
    }

    public static void BreakAllWithin(Vector3 center, float radius)
    {
        // A copy of the list, because breaking removes the prop from it
        foreach (Breakable prop in all.ToArray())
        {
            if (Vector3.Distance(prop.transform.position, center) <= radius)
                prop.Break(center);
        }
    }

    public void Break(Vector3 from)
    {
        Renderer shape = GetComponent<Renderer>();
        Bounds bounds = shape != null ? shape.bounds : new Bounds(transform.position, Vector3.one);
        Material material = debrisMaterial != null ? debrisMaterial : (shape != null ? shape.sharedMaterial : null);

        if (breakEffect != null)
            Instantiate(breakEffect, bounds.center, Quaternion.identity);

        for (int i = 0; i < debrisCount; i++)
        {
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.transform.position = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z));
            piece.transform.rotation = Random.rotation;
            piece.transform.localScale = Vector3.one * debrisSize * Random.Range(0.6f, 1.4f);
            if (material != null)
                piece.GetComponent<Renderer>().sharedMaterial = material;

            // Fly away from the hit, and a bit up
            Rigidbody body = piece.AddComponent<Rigidbody>();
            Vector3 away = piece.transform.position - from;
            away.y = 0f;
            body.AddForce((away.normalized + Vector3.up) * debrisForce, ForceMode.VelocityChange);

            Destroy(piece, debrisLifetime);
        }

        Destroy(gameObject);
    }
}