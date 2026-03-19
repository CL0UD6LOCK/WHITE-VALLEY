using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 武器拾取物视觉效果
/// - 悬浮旋转
/// - 稀有度点光源（呼吸脉冲）
/// - 模型描边发光（用稍大的反向法线外壳实现，URP 兼容）
/// 
/// 由 WeaponPickup.Start() 自动添加，不需要手动挂
/// </summary>
public class WeaponPickupVisual : MonoBehaviour
{
    [Header("悬浮旋转")]
    public float floatAmplitude = 0.18f;
    public float floatSpeed     = 1.8f;
    public float rotateSpeed    = 80f;

    [Header("点光源")]
    public float lightIntensity = 3f;
    public float lightRange     = 2.2f;
    public float pulseSpeed     = 1.8f;
    public float pulseMin       = 0.6f;
    public float pulseMax       = 1.4f;

    [Header("描边")]
    [Tooltip("描边厚度，值越大越粗")]
    public float outlineThickness = 0.04f;

    // ── 私有
    private Vector3  _startPos;
    private Light    _light;
    private float    _baseIntensity;
    private Color    _rarityColor = Color.white;
    private List<GameObject> _outlineObjects = new List<GameObject>();

    // ════════════════════════════════════════
    public void Init(WeaponData data)
    {
        if (data == null) return;
        _rarityColor = data.RarityColor();
    }

    void Start()
    {
        _startPos = transform.position;

        // 如果 Init 未被调用，从 WeaponPickup 读取
        if (_rarityColor == Color.white)
        {
            var pickup = GetComponent<WeaponPickup>();
            if (pickup?.weaponData != null)
                _rarityColor = pickup.weaponData.RarityColor();
        }

        SetupLight();
        SetupOutline();
    }

    // ════════════════════════════════════════
    //  点光源
    // ════════════════════════════════════════
    void SetupLight()
    {
        var go       = new GameObject("RarityLight");
        go.transform.SetParent(transform, false);

        _light           = go.AddComponent<Light>();
        _light.type      = LightType.Point;
        _light.color     = _rarityColor;
        _light.intensity = lightIntensity;
        _light.range     = lightRange;
        _light.shadows   = LightShadows.None;
        _baseIntensity   = lightIntensity;
    }

    // ════════════════════════════════════════
    //  描边（反向法线外壳法，URP 通用）
    // ════════════════════════════════════════
    void SetupOutline()
    {
        // 找到所有 MeshRenderer 子物体，每个复制一个稍大的外壳
        var renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var r in renderers)
        {
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            var outlineGO = new GameObject("Outline_" + r.gameObject.name);
            outlineGO.transform.SetParent(r.transform, false);
            outlineGO.transform.localPosition = Vector3.zero;
            outlineGO.transform.localRotation = Quaternion.identity;
            outlineGO.transform.localScale    = Vector3.one * (1f + outlineThickness);

            var outlineMF   = outlineGO.AddComponent<MeshFilter>();
            outlineMF.sharedMesh = mf.sharedMesh;

            var outlineMR   = outlineGO.AddComponent<MeshRenderer>();

            // 描边材质：Unlit 纯色，只渲染背面
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")
                               ?? Shader.Find("Unlit/Color"));
            mat.color = _rarityColor * 1.5f;   // 稍亮于稀有度颜色

            // 只渲染背面，形成描边效果
            mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Front);

            // HDR 颜色让 Bloom 发光
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", _rarityColor * 3f);
            }

            outlineMR.material           = mat;
            outlineMR.shadowCastingMode  = UnityEngine.Rendering.ShadowCastingMode.Off;
            outlineMR.receiveShadows     = false;

            _outlineObjects.Add(outlineGO);
        }
    }

    // ════════════════════════════════════════
    //  Update
    // ════════════════════════════════════════
    void Update()
    {
        // 悬浮
        float y = _startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(_startPos.x, y, _startPos.z);

        // 旋转
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

        // 点光源呼吸
        if (_light != null)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            _light.intensity = _baseIntensity * Mathf.Lerp(pulseMin, pulseMax, t);
        }

        // 描边亮度也随光源脉冲
        if (_outlineObjects.Count > 0)
        {
            float t     = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float pulse = Mathf.Lerp(pulseMin, pulseMax, t);
            foreach (var go in _outlineObjects)
            {
                if (go == null) continue;
                var mr = go.GetComponent<MeshRenderer>();
                if (mr == null) continue;
                Color c = _rarityColor * (1.5f * pulse);
                c.a = 1f;
                mr.material.color = c;
                if (mr.material.HasProperty("_EmissionColor"))
                    mr.material.SetColor("_EmissionColor", _rarityColor * (3f * pulse));
            }
        }
    }

    // ════════════════════════════════════════
    /// <summary>拾取时清理</summary>
    public void OnPickedUp()
    {
        if (_light != null) Destroy(_light.gameObject);
        foreach (var go in _outlineObjects)
            if (go != null) Destroy(go);
        _outlineObjects.Clear();
    }
}
