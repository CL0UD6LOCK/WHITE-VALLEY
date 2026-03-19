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

        // ★ 减伤接口：以后做减伤道具/护甲直接改这里
        float finalDamage = Mathf.Max(0f, amount * damageMultiplier - flatDefense);

        currentHealth = Mathf.Max(0f, currentHealth - finalDamage);
        onDamaged?.Invoke(finalDamage);

        if (currentHealth <= 0f)
            onDeath?.Invoke();
    }

    // ── 减伤参数（供道具/Buff系统修改）
    [Header("减伤（供道具系统使用）")]
    [Tooltip("伤害倍率，1=无减伤，0.5=减伤50%")]
    public float damageMultiplier = 1f;
    [Tooltip("固定减伤值，每次伤害直接减去此数值")]
    public float flatDefense      = 0f;

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