using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 单个武器槽位 UI
/// 挂在 Slot_0 和 Slot_1 上
/// </summary>
public class ItemSlotUI : MonoBehaviour,
    IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("子组件引用")]
    public Image    iconImage;
    public TMP_Text nameText;
    public TMP_Text ammoText;
    public Image    rightHandBadge;
    public Image    leftHandBadge;
    public Image    emptyOverlay;

    [HideInInspector] public int        slotIndex;
    [HideInInspector] public bool       isEquipSlot;
    [HideInInspector] public bool       equipIsRight;
    [HideInInspector] public WeaponData currentWeapon;

    [HideInInspector] public System.Action<ItemSlotUI, ItemSlotUI> onDrop;
    [HideInInspector] public System.Action<ItemSlotUI>             onDropOutside;

    private static GameObject _floatingIcon;
    private static ItemSlotUI _dragSource;
    private CanvasGroup       _canvasGroup;
    private RectTransform     _panelRect;

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        var inv = FindFirstObjectByType<InventoryUI>();
        if (inv && inv.inventoryPanel)
            _panelRect = inv.inventoryPanel.GetComponent<RectTransform>();
    }

    public void SetWeapon(WeaponData weapon, int ammo, bool inRight, bool inLeft)
    {
        currentWeapon = weapon;
        bool has = weapon != null;

        if (iconImage)
        {
            iconImage.enabled = has && weapon.icon != null;
            if (has && weapon.icon != null)
            {
                iconImage.sprite = weapon.icon;
                iconImage.color  = Color.white;
            }
        }
        if (nameText) nameText.text = has ? weapon.weaponName : (inRight ? "右手 — 空" : "左手 — 空");
        if (ammoText) ammoText.text = has ? $"{ammo} / {weapon.maxAmmo}" : "";
        if (rightHandBadge) rightHandBadge.enabled = inRight && has;
        if (leftHandBadge)  leftHandBadge.enabled  = inLeft  && has;
        if (emptyOverlay)   emptyOverlay.enabled   = !has;
    }

    public void OnPointerClick(PointerEventData e) { }

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

        if (_dragSource == this && _panelRect != null)
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(_panelRect, e.position, e.pressEventCamera))
                onDropOutside?.Invoke(this);
        }

        _dragSource = null;
    }

    public void OnDrop(PointerEventData e)
    {
        if (_dragSource == null || _dragSource == this) return;
        onDrop?.Invoke(_dragSource, this);
    }

}
