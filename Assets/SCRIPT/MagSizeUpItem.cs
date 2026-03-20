using UnityEngine;

/// <summary>
/// 弹夹扩容道具
/// 每拾取一个：当前所有装备的武器最大弹药 +5，且永久生效（换枪后新枪也享受加成）
/// 
/// 实现方式：在 InventorySystem 上存一个全局弹夹加成值
/// WeaponController 射击时用 (maxAmmo + bonusAmmo) 作为上限
/// 不修改 WeaponData，避免影响原始数据
/// 
/// 挂在 PickupItem 同一 GameObject 上
/// </summary>
public class MagSizeUpItem : MonoBehaviour
{
    [Header("每次拾取增加的弹药量")]
    public int ammoBonus = 5;

    /// <summary>拾取时调用</summary>
    public void Apply()
    {
        var inv = FindFirstObjectByType<InventorySystem>();
        if (inv == null) return;

        // 增加全局弹夹加成
        inv.globalMagBonus += ammoBonus;

        // 立即给当前装备的武器补满到新上限
        for (int i = 0; i < inv.slots.Length; i++)
        {
            if (inv.slots[i] == null) continue;
            int newMax = inv.slots[i].maxAmmo + inv.globalMagBonus;
            // 如果当前弹药已满（等于旧上限），顺带补到新上限
            if (inv.ammoInSlot[i] == inv.slots[i].maxAmmo)
                inv.ammoInSlot[i] = newMax;
        }

        inv.onInventoryChanged?.Invoke();
        Debug.Log($"[MagSizeUp] 全局弹夹加成: +{inv.globalMagBonus}");
    }
}
