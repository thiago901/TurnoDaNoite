using UnityEngine;

[CreateAssetMenu(fileName = "NewPower", menuName = "Game/Power Data")]
public class PowerData : ScriptableObject
{
    [Header("Info")]
    public string powerName = "Lamina de Vento";

    [Header("Cost")]
    public float manaCost = 25f;
    public float cooldown = 3f;      // seconds before it can be used again

    [Header("Animation")]
    public string animationState = "Power_Wind"; // exact state name in the Animator
    public float blendIn = 0.05f;
    public float castDelay = 0.37f;  // the projectile leaves at this moment
    public float duration = 0.75f;   // the player is free again

    [Header("Projectile")]
    public Projectile projectile;
    public Vector3 spawnOffset = new Vector3(0f, 0f, 0.8f); // from the player's center
    public float speed = 18f;
    public float range = 12f;
    public int damage = 14;
    public float knockback = 4f;
    public float launch = 1f;
}