using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private Slider healthBar;

    private void OnEnable()
    {
        playerHealth.OnDamaged += HandleDamaged;
        playerHealth.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        playerHealth.OnDamaged -= HandleDamaged;
        playerHealth.OnDied -= HandleDied;
    }

    private void Start()
    {
        healthBar.maxValue = playerHealth.MaxHealth;
        healthBar.value = playerHealth.CurrentHealth;
    }

    private void HandleDamaged(int amount, Vector3 attackerPosition)
    {
        healthBar.value = playerHealth.CurrentHealth;
    }

    private void HandleDied()
    {
        Time.timeScale = 1f; // just in case the game was frozen by a hit stop
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}