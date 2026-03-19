using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 通用血量组件，挂在角色/敌人上
/// </summary>
public class HealthSystem : MonoBehaviour
{
    [Header("血量")]
    public float maxHealth  = 100f;
    public float currentHealth { get; private set; }

    [Header("事件")]
    public UnityEvent<float> onDamaged;   // 参数：受到的伤害值
    public UnityEvent        onDeath;

    public bool IsDead => currentHealth <= 0f;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        onDamaged?.Invoke(amount);

        if (currentHealth <= 0f)
            onDeath?.Invoke();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
    }

    /// <summary>血量百分比 0~1，供 UI 使用</summary>
    public float HealthPercent => currentHealth / maxHealth;
}
