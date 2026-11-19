using UnityEngine;

[CreateAssetMenu(fileName = "NewAttack", menuName = "Game/Attack Data")]
public class AttackData : ScriptableObject
{
    [Header("Animation")]
    public string animationState = "Sword_L1"; // exact state name in the Animator
    public float blendIn = 0.08f;

    [Header("Timing (seconds from the start of the attack)")]
    public float hitDelay = 0.15f;   // the blade reaches the target
    public float cancelTime = 0.3f;  // next attack, dodge or walking allowed
    public float duration = 0.6f;    // attack over

    [Header("Damage")]
    public int damage = 8;
    public bool hitsAll = false;     // false = only the closest enemy
    public bool isFinisher = false;  // bigger effects and camera shake
    public float knockback = 1.5f;
    public float launch = 0f;        // upward push

    [Header("Hit box")]
    public Vector3 hitBoxSize = new Vector3(1.8f, 1.6f, 1.4f);
    public float hitBoxDistance = 1f;

    [Header("Movement")]
    public float lunge = 0.4f;       // step forward until the hit (meters)

    [Header("Combo")]
    public AttackData nextLight;
    public AttackData nextHeavy;
}