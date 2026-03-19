using UnityEngine;
using System.Collections;

/// <summary>
/// 测试用敌人（无血条版）
/// 挂载：Enemy.cs + HealthSystem.cs + Collider（不勾 Is Trigger）
/// </summary>
[RequireComponent(typeof(HealthSystem))]
public class Enemy : MonoBehaviour
{
    [Header("受击闪红")]
    public float hitFlashTime = 0.12f;

    [Header("死亡")]
    public float deathFadeTime = 0.8f;
    public float decalSize     = 1.4f;

    [Header("伤害数字弹出偏移")]
    public Vector3 damageNumberOffset = new Vector3(0f, 1.8f, 0f);

    private HealthSystem _health;
    private Renderer[]   _renderers;
    private Color[]      _originalColors;
    private bool         _isDead;

    void Awake()
    {
        _health = GetComponent<HealthSystem>();
        _health.onDamaged.AddListener(OnDamaged);
        _health.onDeath.AddListener(OnDeath);

        _renderers      = GetComponentsInChildren<Renderer>();
        _originalColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
            _originalColors[i] = _renderers[i].material.color;
    }

    void OnDamaged(float amount)
    {
        if (_isDead) return;
        Debug.Log($"[Enemy] 受到伤害: {amount}，剩余血量: {_health.currentHealth}");
        StopCoroutine("HitFlash");
        StartCoroutine("HitFlash");
    }

    IEnumerator HitFlash()
    {
        foreach (var r in _renderers)
            r.material.color = new Color(1f, 0.15f, 0.15f);
        yield return new WaitForSeconds(hitFlashTime);
        for (int i = 0; i < _renderers.Length; i++)
            _renderers[i].material.color = _originalColors[i];
    }

    void OnDeath()
    {
        if (_isDead) return;
        _isDead = true;
        StopAllCoroutines();

        // 生成血迹
        Vector3 origin = transform.position + Vector3.up * 0.3f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 3f))
        {
            Debug.Log($"[Enemy] 血迹射线打到: {hit.collider.gameObject.name}，位置: {hit.point}");
            var decalGO = new GameObject("BloodDecal");
            decalGO.transform.position = hit.point + hit.normal * 0.015f;
            decalGO.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal)
                                       * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            decalGO.transform.localScale = Vector3.one * decalSize;
            decalGO.AddComponent<BloodDecal>().Init();
        }
        else
        {
            Debug.LogWarning("[Enemy] 血迹射线没打到地面，检查地面是否有 Collider");
        }

        // 禁用碰撞
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;

        StartCoroutine(DeathFade());
    }

    IEnumerator DeathFade()
    {
        // 把所有材质切换为透明模式
        foreach (var r in _renderers)
            URPMaterialUtil.SetTransparent(r.material);

        float elapsed = 0f;
        while (elapsed < deathFadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - elapsed / deathFadeTime;
            foreach (var r in _renderers)
                URPMaterialUtil.SetAlpha(r.material, alpha);
            yield return null;
        }
        Destroy(gameObject);
    }
}
