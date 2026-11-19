using TMPro;
using UnityEngine;

public class DamageNumber : MonoBehaviour
{
    [SerializeField] private float riseSpeed = 1.5f;
    [SerializeField] private float lifetime = 0.8f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color finisherColor = new Color(1f, 0.82f, 0.25f);
    [SerializeField] private float finisherScale = 1.4f;

    private TMP_Text text;
    private float age;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    public void Show(int damage, bool isFinisher)
    {
        text.text = damage.ToString();
        text.color = isFinisher ? finisherColor : normalColor;
        if (isFinisher) transform.localScale *= finisherScale;
    }

    private void Update()
    {
        age += Time.deltaTime;

        // Float up, face the camera and fade out
        transform.position += Vector3.up * riseSpeed * Time.deltaTime;
        if (Camera.main != null)
            transform.rotation = Camera.main.transform.rotation;

        Color color = text.color;
        color.a = 1f - age / lifetime;
        text.color = color;

        if (age >= lifetime)
            Destroy(gameObject);
    }
}