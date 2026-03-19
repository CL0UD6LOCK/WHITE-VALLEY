using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 鼠标跟随准星
/// 替代系统鼠标光标，跟随鼠标位置显示
/// 背包打开时隐藏，换弹时变色，射击时有扩散动画
/// 
/// 【Setup】
///   Canvas (Screen Space Overlay) 下新建空物体 "Crosshair"，挂此脚本
///   Canvas 的 Render Mode 必须是 Screen Space - Overlay
/// </summary>
public class CrosshairUI : MonoBehaviour
{
    [Header("外观")]
    public float lineLength    = 10f;
    public float lineThickness = 2f;
    public float gap           = 5f;          // 中心空隙
    public Color normalColor   = new Color(1f, 1f, 1f, 0.9f);
    public Color reloadColor   = new Color(1f, 0.55f, 0.1f, 0.9f);
    public Color noAmmoColor   = new Color(1f, 0.2f, 0.2f, 0.9f);

    [Header("射击扩散")]
    public float shootSpread      = 8f;       // 开枪时瞬间扩散量
    public float spreadDecaySpeed = 12f;      // 恢复速度

    [Header("移动扩散（叠加在射击扩散上）")]
    public bool  enableMoveSpread = true;
    public float maxMoveSpread    = 14f;
    public float moveSpreadSpeed  = 8f;

    // ── 私有
    private RectTransform   _rt;              // 准星根节点（跟随鼠标）
    private RectTransform[] _lines   = new RectTransform[4];
    private Image[]         _imgs    = new Image[4];

    private float _shootSpread    = 0f;
    private float _moveSpread     = 0f;

    private WeaponController     _wc;
    private FPS_PlayerController _player;
    private InventorySystem      _inv;

    void Start()
    {
        _wc     = FindFirstObjectByType<WeaponController>();
        _player = FindFirstObjectByType<FPS_PlayerController>();
        _inv    = FindFirstObjectByType<InventorySystem>();
        _rt     = GetComponent<RectTransform>();

        // 固定屏幕正中心
        _rt.anchorMin        = new Vector2(0.5f, 0.5f);
        _rt.anchorMax        = new Vector2(0.5f, 0.5f);
        _rt.pivot            = new Vector2(0.5f, 0.5f);
        _rt.anchoredPosition = Vector2.zero;

        // Locked 模式：鼠标固定屏幕中心不可见，Mouse X/Y 正常工作
        // 准星用 UI 画在屏幕中心替代系统光标
        Cursor.visible   = false;
        Cursor.lockState = CursorLockMode.Locked;

        // 生成四条线
        string[] names = { "Top", "Bottom", "Left", "Right" };
        for (int i = 0; i < 4; i++)
        {
            var go  = new GameObject(names[i], typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            _imgs[i]  = go.GetComponent<Image>();
            _imgs[i].raycastTarget = false;
            _lines[i] = go.GetComponent<RectTransform>();
            _lines[i].anchorMin = _lines[i].anchorMax = new Vector2(0.5f, 0.5f);
            _lines[i].pivot     = new Vector2(0.5f, 0.5f);
        }
    }

    void Update()
    {
        // 背包打开时隐藏准星线段（不禁用自身，否则 Update 停止）
        bool bagOpen = InventoryUI.IsOpen;
        foreach (var img in _imgs) if (img) img.enabled = !bagOpen;
        if (bagOpen) return;

        // 准星固定屏幕中心
        _rt.anchoredPosition = Vector2.zero;

        // ── 扩散衰减
        _shootSpread = Mathf.MoveTowards(_shootSpread, 0f, spreadDecaySpeed * Time.deltaTime);

        // ── 移动扩散
        if (enableMoveSpread && _player != null)
        {
            float t = Mathf.Clamp01(_player.HorizontalSpeed / _player.runSpeed);
            float target = t * maxMoveSpread;
            _moveSpread = Mathf.Lerp(_moveSpread, target, Time.deltaTime * moveSpreadSpeed);
        }

        float totalSpread = _shootSpread + _moveSpread;

        // ── 颜色
        Color c = normalColor;
        if (_wc != null && (_wc.IsRightReloading || _wc.IsLeftReloading))
            c = reloadColor;
        else if (_inv != null && _inv.GetRightHandAmmo() == 0 && _inv.GetLeftHandAmmo() == 0)
            c = noAmmoColor;

        foreach (var img in _imgs) if (img) img.color = c;

        RefreshLines(totalSpread);
    }

    /// <summary>WeaponController 射击时调用，触发扩散</summary>
    public void TriggerShootSpread()
    {
        _shootSpread = Mathf.Min(_shootSpread + shootSpread, maxMoveSpread * 2f);
    }

    void RefreshLines(float spread)
    {
        if (_lines[0] == null) return;
        float o = gap + spread;

        // Top
        _lines[0].sizeDelta        = new Vector2(lineThickness, lineLength);
        _lines[0].anchoredPosition = new Vector2(0,  o + lineLength * 0.5f);
        // Bottom
        _lines[1].sizeDelta        = new Vector2(lineThickness, lineLength);
        _lines[1].anchoredPosition = new Vector2(0, -(o + lineLength * 0.5f));
        // Left
        _lines[2].sizeDelta        = new Vector2(lineLength, lineThickness);
        _lines[2].anchoredPosition = new Vector2(-(o + lineLength * 0.5f), 0);
        // Right
        _lines[3].sizeDelta        = new Vector2(lineLength, lineThickness);
        _lines[3].anchoredPosition = new Vector2( (o + lineLength * 0.5f), 0);
    }
}