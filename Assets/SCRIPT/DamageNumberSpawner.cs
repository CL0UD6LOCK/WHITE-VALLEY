using UnityEngine;
using System.Collections;
using TMPro;

/// <summary>
/// 浮动伤害数字管理器
/// 
/// 【Setup — 必须做这一步！】
///   Hierarchy 新建空物体，命名 "DamageNumberSpawner"，挂上此脚本
///   全场景只需要一个，放哪里都行
/// </summary>
public class DamageNumberSpawner : MonoBehaviour
{
    public static DamageNumberSpawner Instance { get; private set; }

    [Header("外观")]
    public float fontSize    = 3f;                                    // 世界空间要大得多
    public Color damageColor = new Color(1f, 0.92f, 0.2f, 1f);

    [Header("动画")]
    public float floatHeight   = 1.4f;
    public float duration      = 0.75f;
    public float spreadRadius  = 0.25f;

    private Camera _cam;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _cam     = Camera.main;

        if (_cam == null)
            Debug.LogWarning("[DamageNumberSpawner] 找不到 Main Camera，请确认相机 Tag 是 MainCamera");
    }

    /// <summary>外部调用入口</summary>
    public static void Spawn(Vector3 worldPos, float damage, bool isCrit = false)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[DamageNumberSpawner] 场景里没有 DamageNumberSpawner 物体！");
            return;
        }
        Instance.DoSpawn(worldPos, Mathf.RoundToInt(damage), isCrit);
    }

    void DoSpawn(Vector3 pos, int damage, bool isCrit)
    {
        var go = new GameObject("DmgNum_" + damage);
        go.transform.position = pos + new Vector3(
            Random.Range(-spreadRadius, spreadRadius),
            0f,
            Random.Range(-spreadRadius, spreadRadius)
        );

        var tmp          = go.AddComponent<TextMeshPro>();
        tmp.text         = damage.ToString();
        tmp.fontSize     = isCrit ? fontSize * 1.5f : fontSize;
        tmp.color        = damageColor;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.fontStyle    = isCrit ? FontStyles.Bold : FontStyles.Normal;
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = Color.black;

        // 直接复制相机旋转，文字平面与相机屏幕平行
        if (_cam)
            go.transform.rotation = _cam.transform.rotation;

        StartCoroutine(Animate(go.transform, tmp));
    }

    IEnumerator Animate(Transform t, TextMeshPro tmp)
    {
        if (t == null) yield break;

        Vector3 startPos = t.position;
        float   elapsed  = 0f;
        Camera  cam      = _cam != null ? _cam : Camera.main;

        while (elapsed < duration && t != null)
        {
            elapsed += Time.deltaTime;
            float p     = elapsed / duration;
            float eased = 1f - Mathf.Pow(1f - p, 2f);

            t.position = startPos + Vector3.up * (floatHeight * eased);

            // 朝向相机（与相机同向，文字正面朝前）
            if (cam)
                t.rotation = cam.transform.rotation;

            // 后半段淡出
            float alpha = p < 0.55f ? 1f : 1f - (p - 0.55f) / 0.45f;
            Color c = tmp.color;
            c.a     = alpha;
            tmp.color = c;

            yield return null;
        }

        if (t != null) Destroy(t.gameObject);
    }
}
