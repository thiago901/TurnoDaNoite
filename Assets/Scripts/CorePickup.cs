using UnityEngine;

// A Nucleo crystal: touching it makes the mana bar bigger
public class CorePickup : MonoBehaviour
{
    [SerializeField] private float coreBonus = 25f;
    [SerializeField] private float pickupRadius = 1.3f;
    [SerializeField] private float spinSpeed = 90f;
    [SerializeField] private float bobHeight = 0.25f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private Transform visual;
    [SerializeField] private GameObject pickupEffect;

    private Transform player;
    private PlayerEnergy energy;
    private Vector3 visualStart;

    private void Start()
    {
        GameObject found = GameObject.FindWithTag("Player");
        if (found != null)
        {
            player = found.transform;
            energy = found.GetComponent<PlayerEnergy>();
        }

        if (visual == null) visual = transform;
        visualStart = visual.localPosition;
    }

    private void Update()
    {
        // Spin and float so it catches the eye
        visual.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        visual.localPosition = visualStart + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;

        if (player == null) return;

        Vector3 offset = player.position - transform.position;
        offset.y = 0f;
        if (offset.magnitude <= pickupRadius)
            Collect();
    }

    private void Collect()
    {
        if (energy != null) energy.ExpandCore(coreBonus);
        if (pickupEffect != null) Instantiate(pickupEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}