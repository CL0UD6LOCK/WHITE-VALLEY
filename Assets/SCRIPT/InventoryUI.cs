using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

/// <summary>
/// 背包 UI — PUBG 式操作
/// 
/// 操作方式：
///   - 左键拖拽武器图标 → 拖到另一槽交换 / 拖到左右手格子装备
///   - 右键点击武器图标 → 弹出菜单（装备右手 / 装备左手 / 丢弃）
///   - Tab 开关背包
/// 
/// 【Canvas 层级】
///   Canvas (Screen Space Overlay)
///   └── InventoryPanel  ← 挂此脚本
///       ├── Overlay          (全屏半透明黑，Button 组件用于点击空白关闭)
///       ├── Window           (居中面板)
///       │   ├── Title
///       │   ├── SlotsArea
///       │   │   ├── Slot_0   ← SlotEntry 挂 ItemSlotUI
///       │   │   └── Slot_1   ← SlotEntry 挂 ItemSlotUI
///       │   ├── EquipRow
///       │   │   ├── RightHandSlot  ← ItemSlotUI (isEquipSlot=true)
///       │   │   └── LeftHandSlot   ← ItemSlotUI (isEquipSlot=true)
///       │   └── HintText
///       └── ContextMenu      ← 挂 ContextMenuUI
///           ├── BtnEquipRight
///           ├── BtnEquipLeft
///           └── BtnDrop
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; } = false;

    [Header("背包面板（挂在 Canvas 上时需要指定面板物体）")]
    [Tooltip("把 InventoryPanel 拖进来，脚本本身挂在 Canvas 上")]
    public GameObject inventoryPanel;

    [Header("背包格子（顺序对应 slot 0, 1）")]
    public ItemSlotUI[] bagSlots      = new ItemSlotUI[2];

    [Header("装备栏格子")]
    public ItemSlotUI   rightHandSlot;
    public ItemSlotUI   leftHandSlot;

    [Header("右键菜单")]
    public ContextMenuUI contextMenu;

    [Header("提示文字")]
    public TMP_Text hintText;

    [Header("全屏遮罩（点击关闭）")]
    public Button overlay;

    // ── 拖拽状态
    private ItemSlotUI _dragSource;
    private GameObject _dragIcon;         // 跟随鼠标的浮动图标

    private InventorySystem _inv;

    void Awake()
    {
        _inv = FindFirstObjectByType<InventorySystem>();
        if (_inv) _inv.onInventoryChanged.AddListener(Refresh);

        // 初始化所有槽位回调
        for (int i = 0; i < bagSlots.Length; i++)
        {
            int idx = i;
            if (bagSlots[i] == null) continue;
            bagSlots[i].slotIndex    = idx;
            bagSlots[i].isEquipSlot  = false;
            bagSlots[i].onRightClick = (slot) => ShowContextMenu(slot);
            bagSlots[i].onDrop       = (src, dst) => HandleDrop(src, dst);
        }

        if (rightHandSlot)
        {
            rightHandSlot.slotIndex   = 0;   // 右手
            rightHandSlot.isEquipSlot = true;
            rightHandSlot.equipIsRight = true;
            rightHandSlot.onDrop      = (src, dst) => HandleDrop(src, dst);
        }
        if (leftHandSlot)
        {
            leftHandSlot.slotIndex    = 1;   // 左手
            leftHandSlot.isEquipSlot  = true;
            leftHandSlot.equipIsRight = false;
            leftHandSlot.onDrop       = (src, dst) => HandleDrop(src, dst);
        }

        if (overlay) overlay.onClick.AddListener(() => SetOpen(false));

        // 控制面板显示（脚本挂 Canvas，面板是子物体）
        if (inventoryPanel) inventoryPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) SetOpen(!IsOpen);

        // 拖拽图标跟随鼠标
        if (_dragIcon) _dragIcon.transform.position = Input.mousePosition;

        // Esc 关闭右键菜单
        if (Input.GetKeyDown(KeyCode.Escape) && contextMenu != null && contextMenu.gameObject.activeSelf)
            contextMenu.Hide();
    }

    public void SetOpen(bool open)
    {
        IsOpen = open;
        if (inventoryPanel) inventoryPanel.SetActive(open);

        if (open)
        {
            // 打开背包：解锁鼠标，显示系统光标，游戏继续运行
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible   = true;
            Refresh();
        }
        else
        {
            // 关闭背包：隐藏光标（鼠标瞄准模式用自定义准星）
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible   = false;
        }
    }

    // ════════════════════════════════════════
    //  刷新所有槽位显示
    // ════════════════════════════════════════
    public void Refresh()
    {
        if (_inv == null) return;

        for (int i = 0; i < bagSlots.Length; i++)
        {
            if (bagSlots[i] == null) continue;
            var weapon = _inv.slots[i];
            bagSlots[i].SetWeapon(weapon, weapon != null ? _inv.ammoInSlot[i] : 0,
                                  _inv.rightHandSlot == i, _inv.leftHandSlot == i);
        }

        // 装备栏显示
        var rw = _inv.GetRightHandWeapon();
        rightHandSlot?.SetWeapon(rw,
            rw != null && _inv.rightHandSlot >= 0 ? _inv.ammoInSlot[_inv.rightHandSlot] : 0,
            true, false);

        var lw = _inv.GetLeftHandWeapon();
        leftHandSlot?.SetWeapon(lw,
            lw != null && _inv.leftHandSlot >= 0 ? _inv.ammoInSlot[_inv.leftHandSlot] : 0,
            false, true);

        if (hintText) hintText.text = "Drag to equip  |  Right-click for options  |  [ Tab ] Close";
    }

    // ════════════════════════════════════════
    //  右键菜单
    // ════════════════════════════════════════
    void ShowContextMenu(ItemSlotUI slot)
    {
        if (contextMenu == null || slot.currentWeapon == null) return;
        int idx = slot.slotIndex;   // ★ 捕获 index 避免 lambda 闭包问题
        contextMenu.Show(
            slot,
            onEquipRight: () => { _inv.EquipToRightHand(idx); contextMenu.Hide(); Refresh(); },
            onEquipLeft:  () => { _inv.EquipToLeftHand(idx);  contextMenu.Hide(); Refresh(); },
            onDrop:       () => { _inv.DropSlot(idx);         contextMenu.Hide(); Refresh(); }
        );
    }

    // ════════════════════════════════════════
    //  拖拽处理
    // ════════════════════════════════════════
    void HandleDrop(ItemSlotUI src, ItemSlotUI dst)
    {
        if (src == null || dst == null || src == dst) return;

        // 背包槽 → 装备槽
        if (!src.isEquipSlot && dst.isEquipSlot)
        {
            if (dst.equipIsRight) _inv.EquipToRightHand(src.slotIndex);
            else                  _inv.EquipToLeftHand(src.slotIndex);
            Refresh();
            return;
        }

        // 背包槽 → 背包槽（交换）
        if (!src.isEquipSlot && !dst.isEquipSlot)
        {
            // 交换槽位内容
            var tmpWeapon          = _inv.slots[src.slotIndex];
            var tmpAmmo            = _inv.ammoInSlot[src.slotIndex];
            _inv.slots[src.slotIndex]      = _inv.slots[dst.slotIndex];
            _inv.ammoInSlot[src.slotIndex] = _inv.ammoInSlot[dst.slotIndex];
            _inv.slots[dst.slotIndex]      = tmpWeapon;
            _inv.ammoInSlot[dst.slotIndex] = tmpAmmo;

            // 修正装备槽索引引用
            if (_inv.rightHandSlot == src.slotIndex)      _inv.rightHandSlot = dst.slotIndex;
            else if (_inv.rightHandSlot == dst.slotIndex) _inv.rightHandSlot = src.slotIndex;
            if (_inv.leftHandSlot == src.slotIndex)       _inv.leftHandSlot  = dst.slotIndex;
            else if (_inv.leftHandSlot == dst.slotIndex)  _inv.leftHandSlot  = src.slotIndex;

            _inv.onInventoryChanged?.Invoke();
            Refresh();
        }
    }
}