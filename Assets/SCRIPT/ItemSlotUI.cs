using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 单个武器槽位 UI
/// 挂在每个槽位 GameObject 上（Slot_0, Slot_1, RightHandSlot, LeftHandSlot）
/// </summary>
public class ItemSlotUI : MonoBehaviour,
    IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("子组件引用")]
    public Image    iconImage;
    public TMP_Text nameText;
    public TMP_Text ammoText;
    public Image    rightHandBadge;   // "右手"标记图片（可选）
    public Image    leftHandBadge;    // "左手"标记图片（可选）
    public Image    emptyOverlay;     // 空槽遮罩（可选）

    // ── 状态
    [HideInInspector] public int        slotIndex;
    [HideInInspector] public bool       isEquipSlot;
    [HideInInspector] public bool       equipIsRight;
    [HideInInspector] public WeaponData currentWeapon;

    // ── 回调（由 InventoryUI 在 Awake 里绑定）
    [HideInInspector] public System.Action<ItemSlotUI>             onRightClick;
    [HideInInspector] public System.Action<ItemSlotUI, ItemSlotUI> onDrop;

    // ── 拖拽（全局共享，同时只有一个拖拽源）
    private static GameObject _floatingIcon;
    private static ItemSlotUI _dragSource;
    private CanvasGroup _canvasGroup;

    void Awake()
    {
        // 自动添加 CanvasGroup（拖拽半透明效果需要）
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 关闭所有直接子物体的 Raycast Target，避免遮挡 OnDrop 检测
        // 只有槽位根物体的 Image 需要接收射线
        foreach (var img in GetComponentsInChildren<UnityEngine.UI.Image>())
        {
            if (img.gameObject != gameObject)
                img.raycastTarget = false;
        }
        foreach (var txt in GetComponentsInChildren<TMPro.TMP_Text>())
            txt.raycastTarget = false;
    }

    public void SetWeapon(WeaponData weapon, int ammo, bool inRight, bool inLeft)
    {
        currentWeapon = weapon;
        bool has = weapon != null;

        if (iconImage)
        {
            iconImage.enabled = has && weapon.icon != null;
            if (has && weapon.icon) iconImage.sprite = weapon.icon;
        }
        if (nameText)       nameText.text = has ? weapon.weaponName : "Empty";
        if (ammoText)       ammoText.text = has ? $"{ammo}/{weapon.maxAmmo}" : "";
        if (rightHandBadge) rightHandBadge.enabled = inRight;
        if (leftHandBadge)  leftHandBadge.enabled  = inLeft;
        if (emptyOverlay)   emptyOverlay.enabled   = !has;
    }

    // ── 右键点击 → 弹出菜单
    public void OnPointerClick(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Right)
            onRightClick?.Invoke(this);
    }

    // ── 拖拽开始
    public void OnBeginDrag(PointerEventData e)
    {
        if (currentWeapon == null) return;
        _dragSource = this;

        if (_floatingIcon) Destroy(_floatingIcon);
        _floatingIcon = new GameObject("DragIcon", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        _floatingIcon.transform.SetParent(GetComponentInParent<Canvas>().transform, false);
        _floatingIcon.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 60);

        var img = _floatingIcon.GetComponent<Image>();
        img.sprite        = currentWeapon.icon;
        img.raycastTarget = false;
        _floatingIcon.GetComponent<CanvasGroup>().blocksRaycasts = false;

        if (_canvasGroup) _canvasGroup.alpha = 0.4f;
    }

    public void OnDrag(PointerEventData e)
    {
        if (_floatingIcon) _floatingIcon.transform.position = e.position;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (_canvasGroup) _canvasGroup.alpha = 1f;
        if (_floatingIcon) { Destroy(_floatingIcon); _floatingIcon = null; }
        _dragSource = null;
    }

    // ── 放下到此槽位
    public void OnDrop(PointerEventData e)
    {
        if (_dragSource == null || _dragSource == this) return;
        onDrop?.Invoke(_dragSource, this);
    }
}