using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 背包 UI
/// 操作：拖拽交换槽位 / 拖出面板外丢弃武器 / Tab 开关
/// Slot_0 = 右手，Slot_1 = 左手，固定绑定
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; } = false;

    [Header("背包面板")]
    public GameObject inventoryPanel;

    [Header("背包格子（Slot_0=右手 Slot_1=左手）")]
    public ItemSlotUI[] bagSlots = new ItemSlotUI[2];

    [Header("提示文字")]
    public TMP_Text hintText;

    [Header("全屏遮罩（点击空白关闭）")]
    public Button overlay;

    [Header("丢弃武器生成位置（留空则在角色前方生成）")]
    public float dropDistance = 1.5f;

    [HideInInspector] public ItemSlotUI rightHandSlot;
    [HideInInspector] public ItemSlotUI leftHandSlot;
    [HideInInspector] public ContextMenuUI contextMenu;

    private InventorySystem _inv;

    void Awake()
    {
        _inv = FindFirstObjectByType<InventorySystem>();
        if (_inv) _inv.onInventoryChanged.AddListener(Refresh);

        if (bagSlots.Length > 0 && bagSlots[0] != null)
        {
            bagSlots[0].slotIndex    = 0;
            bagSlots[0].isEquipSlot  = true;
            bagSlots[0].equipIsRight = true;
            bagSlots[0].onDrop       = (src, dst) => HandleDrop(src, dst);
            bagSlots[0].onDropOutside = (slot) => DropWeapon(slot.slotIndex);
        }
        if (bagSlots.Length > 1 && bagSlots[1] != null)
        {
            bagSlots[1].slotIndex    = 1;
            bagSlots[1].isEquipSlot  = true;
            bagSlots[1].equipIsRight = false;
            bagSlots[1].onDrop       = (src, dst) => HandleDrop(src, dst);
            bagSlots[1].onDropOutside = (slot) => DropWeapon(slot.slotIndex);
        }

        if (overlay) overlay.onClick.AddListener(() => SetOpen(false));
        if (inventoryPanel) inventoryPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) SetOpen(!IsOpen);
    }

    public void SetOpen(bool open)
    {
        IsOpen = open;
        if (inventoryPanel) inventoryPanel.SetActive(open);
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible   = open;
        if (open) Refresh();
    }

    public void Refresh()
    {
        if (_inv == null) { Debug.LogWarning("[InventoryUI] _inv 是 null，找不到 InventorySystem"); return; }
        Debug.Log($"[InventoryUI] Refresh 被调用，slot0={_inv.slots[0]?.weaponName ?? "空"} slot1={_inv.slots[1]?.weaponName ?? "空"}");
        for (int i = 0; i < bagSlots.Length; i++)
        {
            if (bagSlots[i] == null) continue;
            var weapon = _inv.slots[i];
            bagSlots[i].SetWeapon(weapon,
                weapon != null ? _inv.ammoInSlot[i] : 0,
                i == 0, i == 1);
        }
        if (hintText) hintText.text = "拖拽交换槽位  |  拖出背包外丢弃武器  |  [ Tab ] 关闭";
    }

    // ── 丢弃武器（在角色前方生成拾取物）
    void DropWeapon(int slotIndex)
    {
        if (_inv == null || _inv.slots[slotIndex] == null) return;
        var weapon = _inv.slots[slotIndex];

        // 找角色位置
        var player = FindFirstObjectByType<FPS_PlayerController>();
        Vector3 spawnPos = player != null
            ? player.transform.position + player.transform.forward * dropDistance + Vector3.up * 0.5f
            : Vector3.zero;

        // 生成拾取物
        var go  = new GameObject($"Pickup_{weapon.weaponName}");
        go.transform.position = spawnPos;
        go.transform.rotation = Quaternion.identity;
        var col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius    = 0.5f;
        var pickup    = go.AddComponent<WeaponPickup>();
        pickup.weaponData = weapon;

        // 模型直立放置
        if (weapon.modelPrefab != null)
        {
            var model = Instantiate(weapon.modelPrefab, go.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0f, 0f, 0f); // 调这里改朝向
        }

        _inv.DropSlot(slotIndex);
        Refresh();
    }

    // ── 拖拽换位
    void HandleDrop(ItemSlotUI src, ItemSlotUI dst)
    {
        if (src == null || dst == null || src == dst) return;
        if (src.isEquipSlot && dst.isEquipSlot)
        {
            // 两个装备槽之间交换
            int tmp               = _inv.rightHandSlot;
            _inv.rightHandSlot    = _inv.leftHandSlot;
            _inv.leftHandSlot     = tmp;
            _inv.onInventoryChanged?.Invoke();
            Refresh();
            return;
        }
        // 交换槽位内容
        var tmpWeapon          = _inv.slots[src.slotIndex];
        var tmpAmmo            = _inv.ammoInSlot[src.slotIndex];
        _inv.slots[src.slotIndex]      = _inv.slots[dst.slotIndex];
        _inv.ammoInSlot[src.slotIndex] = _inv.ammoInSlot[dst.slotIndex];
        _inv.slots[dst.slotIndex]      = tmpWeapon;
        _inv.ammoInSlot[dst.slotIndex] = tmpAmmo;

        if (_inv.rightHandSlot == src.slotIndex)      _inv.rightHandSlot = dst.slotIndex;
        else if (_inv.rightHandSlot == dst.slotIndex) _inv.rightHandSlot = src.slotIndex;
        if (_inv.leftHandSlot == src.slotIndex)       _inv.leftHandSlot  = dst.slotIndex;
        else if (_inv.leftHandSlot == dst.slotIndex)  _inv.leftHandSlot  = src.slotIndex;

        _inv.onInventoryChanged?.Invoke();
        Refresh();
    }
}

// ════════════════════════════════════════════════════════
//  右键菜单（保留类定义避免编译报错，但不再使用）
// ════════════════════════════════════════════════════════
public class ContextMenuUI : MonoBehaviour
{
    public Button   btnEquipRight;
    public Button   btnEquipLeft;
    public Button   btnDrop;
    public TMP_Text titleText;
    void Awake() { gameObject.SetActive(false); }
    public void Show(ItemSlotUI s, System.Action r, System.Action l, System.Action d) { }
    public void Hide() { gameObject.SetActive(false); }
}