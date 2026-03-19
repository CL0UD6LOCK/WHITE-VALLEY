using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 背包系统（两格武器槽 + 左右手装备）
/// 挂在角色上
/// </summary>
public class InventorySystem : MonoBehaviour
{
    // ── 背包槽（最多2把枪）
    public WeaponData[] slots      = new WeaponData[2];  // 槽位存数据
    public int[]        ammoInSlot = new int[2];         // 每把枪当前弹药

    // ── 左右手装备（存的是槽位索引，-1=空手）
    public int leftHandSlot  = -1;
    public int rightHandSlot = -1;

    // ── 事件（UI 订阅）
    public UnityEvent onInventoryChanged;

    // ── 单例式快速访问（同场景只有一个角色）
    public static InventorySystem Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    // ════════════════════════════════════════
    //  拾取武器
    // ════════════════════════════════════════
    /// <summary>
    /// 返回 true = 拾取成功，false = 背包满
    /// </summary>
    public bool TryPickup(WeaponData weapon)
    {
        // 先找空槽
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i]      = weapon;
                ammoInSlot[i] = weapon.maxAmmo;

                // 自动装备到空手
                if (rightHandSlot == -1)      rightHandSlot = i;
                else if (leftHandSlot == -1)  leftHandSlot  = i;

                onInventoryChanged?.Invoke();
                return true;
            }
        }
        return false;   // 背包满
    }

    // ════════════════════════════════════════
    //  丢弃武器
    // ════════════════════════════════════════
    public void DropSlot(int slotIndex)
    {
        if (slots[slotIndex] == null) return;

        slots[slotIndex]      = null;
        ammoInSlot[slotIndex] = 0;

        if (rightHandSlot == slotIndex) rightHandSlot = -1;
        if (leftHandSlot  == slotIndex) leftHandSlot  = -1;

        onInventoryChanged?.Invoke();
    }

    // ════════════════════════════════════════
    //  切换装备（左右手互换 / 指定哪只手拿哪槽）
    // ════════════════════════════════════════
    public void EquipToRightHand(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length) return;
        // 如果左手拿的是同一把，先换手
        if (leftHandSlot == slotIndex) leftHandSlot = rightHandSlot;
        rightHandSlot = slotIndex;
        onInventoryChanged?.Invoke();
    }

    public void EquipToLeftHand(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length) return;
        if (rightHandSlot == slotIndex) rightHandSlot = leftHandSlot;
        leftHandSlot = slotIndex;
        onInventoryChanged?.Invoke();
    }

    public void SwapHands()
    {
        (leftHandSlot, rightHandSlot) = (rightHandSlot, leftHandSlot);
        onInventoryChanged?.Invoke();
    }

    // ════════════════════════════════════════
    //  弹药操作
    // ════════════════════════════════════════
    public WeaponData GetRightHandWeapon() => rightHandSlot >= 0 ? slots[rightHandSlot] : null;
    public WeaponData GetLeftHandWeapon()  => leftHandSlot  >= 0 ? slots[leftHandSlot]  : null;

    public int  GetRightHandAmmo()  => rightHandSlot >= 0 ? ammoInSlot[rightHandSlot] : 0;
    public int  GetLeftHandAmmo()   => leftHandSlot  >= 0 ? ammoInSlot[leftHandSlot]  : 0;

    public bool ConsumeRightAmmo()
    {
        if (rightHandSlot < 0 || ammoInSlot[rightHandSlot] <= 0) return false;
        ammoInSlot[rightHandSlot]--;
        onInventoryChanged?.Invoke();
        return true;
    }

    public bool ConsumeLeftAmmo()
    {
        if (leftHandSlot < 0 || ammoInSlot[leftHandSlot] <= 0) return false;
        ammoInSlot[leftHandSlot]--;
        onInventoryChanged?.Invoke();
        return true;
    }

    public void ReloadRightHand()
    {
        if (rightHandSlot >= 0 && slots[rightHandSlot] != null)
        {
            ammoInSlot[rightHandSlot] = slots[rightHandSlot].maxAmmo;
            onInventoryChanged?.Invoke();
        }
    }

    public void ReloadLeftHand()
    {
        if (leftHandSlot >= 0 && slots[leftHandSlot] != null)
        {
            ammoInSlot[leftHandSlot] = slots[leftHandSlot].maxAmmo;
            onInventoryChanged?.Invoke();
        }
    }
}