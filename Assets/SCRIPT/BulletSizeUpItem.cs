using UnityEngine;

/// <summary>
/// 子弹增幅道具
/// 每拾取一个：子弹碰撞体积 +5%，伤害 +7%
/// 效果永久叠加，类似以撒
/// 
/// 挂在 PickupItem.cs 同一 GameObject 上
/// 在 PickupItem 的 TryPickup 里调用 Apply()
/// </summary>
public class BulletSizeUpItem : MonoBehaviour
{
    // ── 静态数据，全局唯一，所有子弹共享
    public static float SizeMultiplier   = 1f;   // 碰撞体积倍率
    public static float DamageMultiplier = 1f;   // 伤害倍率

    [Header("每次拾取的增幅")]
    public float sizeIncreasePercent   = 5f;    // +5%
    public float damageIncreasePercent = 7f;    // +7%

    /// <summary>拾取时调用</summary>
    public void Apply()
    {
        SizeMultiplier   *= 1f + sizeIncreasePercent   / 100f;
        DamageMultiplier *= 1f + damageIncreasePercent / 100f;

        Debug.Log($"[BulletSizeUp] 当前倍率 - 体积: {SizeMultiplier:F2}x  伤害: {DamageMultiplier:F2}x");
    }

    /// <summary>换图/死亡时重置（可选）</summary>
    public static void Reset()
    {
        SizeMultiplier   = 1f;
        DamageMultiplier = 1f;
    }
}
