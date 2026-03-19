using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 静态 ^ 形准星
/// 白色（平时）→ 橙色（击中敌人）→ 红色（换弹中）
/// </summary>
public class CrosshairUI : MonoBehaviour
{
    [Header("形状")]
    public float armLength = 28f;
    public float thickness = 5f;
    public float angle     = 90f;   // 开口角度90度

    [Header("颜色")]
    public Color normalColor = new Color(1f,   1f,   1f,   0.92f);  // 白
    public Color hitColor    = new Color(1f,   0.55f,0.1f, 0.92f);  // 橙（击中）
    public Color reloadColor = new Color(1f,   0.15f,0.15f,0.92f);  // 红（换弹）

    [Header("击中橙色持续时间")]
    public float hitColorDuration = 0.12f;

    // ── 私有
    private RectTransform _rt;
    private Image         _leftImg;
    private Image         _rightImg;
    private bool          _isHit     = false;

    private WeaponController _wc;

    void Start()
    {
        _wc = FindFirstObjectByType<WeaponController>();

        _rt = GetComponent<RectTransform>();
        _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);
        _rt.pivot     = new Vector2(0.5f, 0.5f);
        _rt.anchoredPosition = Vector2.zero;

        Cursor.visible   = false;
        Cursor.lockState = CursorLockMode.Confined;

        BuildCaret();
    }

    void BuildCaret()
    {
        // 左臂（从顶点向左下延伸）
        var leftGO  = new GameObject("LeftArm", typeof(RectTransform), typeof(Image));
        leftGO.transform.SetParent(transform, false);
        _leftImg = leftGO.GetComponent<Image>();
        _leftImg.raycastTarget = false;
        _leftImg.sprite = CreateRoundedSprite();
        _leftImg.type   = Image.Type.Sliced;
        var leftRT = leftGO.GetComponent<RectTransform>();
        leftRT.anchorMin = leftRT.anchorMax = new Vector2(0.5f, 0.5f);
        leftRT.pivot     = new Vector2(0.5f, 1f);   // pivot 在顶部，从顶点往下延伸
        leftRT.sizeDelta = new Vector2(thickness, armLength);
        leftRT.anchoredPosition  = new Vector2(0f, armLength * 0.3f);  // 顶点在中心偏上
        leftRT.localRotation     = Quaternion.Euler(0f, 0f, -(angle * 0.5f));

        // 右臂（从顶点向右下延伸）
        var rightGO = new GameObject("RightArm", typeof(RectTransform), typeof(Image));
        rightGO.transform.SetParent(transform, false);
        _rightImg = rightGO.GetComponent<Image>();
        _rightImg.raycastTarget = false;
        _rightImg.sprite = CreateRoundedSprite();
        _rightImg.type   = Image.Type.Sliced;
        var rightRT = rightGO.GetComponent<RectTransform>();
        rightRT.anchorMin = rightRT.anchorMax = new Vector2(0.5f, 0.5f);
        rightRT.pivot     = new Vector2(0.5f, 1f);
        rightRT.sizeDelta = new Vector2(thickness, armLength);
        rightRT.anchoredPosition = new Vector2(0f, armLength * 0.3f);
        rightRT.localRotation    = Quaternion.Euler(0f, 0f, angle * 0.5f);

        SetColor(normalColor);
    }

    void Update()
    {
        bool hide = InventoryUI.IsOpen;
        _leftImg.enabled  = !hide;
        _rightImg.enabled = !hide;
        if (hide) return;

        // 换弹优先级最高
        if (_wc != null && (_wc.IsRightReloading || _wc.IsLeftReloading))
        {
            SetColor(reloadColor);
            return;
        }

        // 击中橙色由协程控制，这里不覆盖
        if (!_isHit)
            SetColor(normalColor);
    }

    /// <summary>子弹打到有血量目标时由 Bullet.cs 调用</summary>
    public void ShowHit()
    {
        StopCoroutine("HitFlash");
        StartCoroutine("HitFlash");
    }

    IEnumerator HitFlash()
    {
        _isHit = true;
        SetColor(hitColor);
        yield return new WaitForSeconds(hitColorDuration);
        _isHit = false;
    }

    void SetColor(Color c)
    {
        if (_leftImg)  _leftImg.color  = c;
        if (_rightImg) _rightImg.color = c;
    }

    // 生成一个两端圆形的胶囊形贴图，消除线条锯齿
    static Sprite CreateRoundedSprite()
    {
        int size   = 32;
        int radius = size / 2;
        var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 胶囊形：中间矩形 + 两端半圆
                float cx   = x - radius + 0.5f;
                float cy   = y - radius + 0.5f;
                float dist = Mathf.Sqrt(cx * cx + cy * cy);
                // 边缘 1px 软化
                float alpha = Mathf.Clamp01((radius - 0.5f - dist + 1f));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            size,
            0,
            SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius)  // border for sliced
        );
    }
}