using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 通用拾取物视觉效果
/// 悬浮上下移动 + Y轴旋转 + 稀有度点光源呼吸 + 描边
/// 由 WeaponPickup 和 PickupItem 自动添加
/// </summary>
public class PickupVisual : MonoBehaviour
{
    [Header("悬浮")]
    public float floatAmplitude = 0.3f;
    public float floatSpeed     = 0.8f;   // 慢速上下
    public float rotateSpeed    = 60f;

    [Header("点光源")]
    public float lightIntensity = 3f;
    public float lightRange     = 2.2f;
    public float pulseSpeed     = 1.8f;
    public float pulseMin       = 0.6f;
    public float pulseMax       = 1.4f;

    [Header("描边")]
    public float outlineThickness = 0.04f;

    // ── 私有
    private Vector3              _startPos;
    private Light                _light;
    private float                _baseIntensity;
    private Color                _rarityColor = Color.white;
    private List<GameObject>     _outlineObjects = new List<GameObject>();
    private bool                 _initialized;

    // ════════════════════════════════════════
    /// <summary>由 WeaponPickup / PickupItem 调用</summary>
    public void Init(Color rarityColor, GameObject modelPrefab = null)
    {
        _rarityColor  = rarityColor;
        _initialized  = true;

        // 如果有模型 Prefab 且场景里还没有，实例化出来
        if (modelPrefab != null && transform.childCount == 0)
        {
            var model = Instantiate(modelPrefab, transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
        }
    }

    void Start()
    {
        _startPos = transform.position;

        // 没被 Init 调用时自动读取
        if (!_initialized)
        {
            var wp = GetComponent<WeaponPickup>();
            if (wp?.weaponData != null)
                Init(wp.weaponData.RarityColor(), wp.weaponData.modelPrefab);

            var pi = GetComponent<PickupItem>();
            if (pi?.itemData != null)
                Init(pi.itemData.RarityColor(), pi.itemData.modelPrefab);
        }

        SetupLight();
        SetupOutline();
    }

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

    void SetupOutline()
    {
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

            var outlineMF        = outlineGO.AddComponent<MeshFilter>();
            outlineMF.sharedMesh = mf.sharedMesh;

            var outlineMR = outlineGO.AddComponent<MeshRenderer>();
            var mat       = new Material(Shader.Find("Universal Render Pipeline/Unlit")
                                      ?? Shader.Find("Unlit/Color"));
            mat.color = _rarityColor * 1.5f;
            mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Front);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", _rarityColor * 3f);
            }
            outlineMR.material          = mat;
            outlineMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outlineMR.receiveShadows    = false;
            _outlineObjects.Add(outlineGO);
        }
    }

    void Update()
    {
        // 上下浮动（慢速正弦）
        float y = _startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(_startPos.x, y, _startPos.z);

        // Y 轴旋转
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

        // 点光源呼吸
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        float pulse = Mathf.Lerp(pulseMin, pulseMax, t);

        if (_light != null)
            _light.intensity = _baseIntensity * pulse;

        // 描边呼吸
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

    public void OnPickedUp()
    {
        if (_light != null) Destroy(_light.gameObject);
        foreach (var go in _outlineObjects)
            if (go != null) Destroy(go);
        _outlineObjects.Clear();
    }
}
