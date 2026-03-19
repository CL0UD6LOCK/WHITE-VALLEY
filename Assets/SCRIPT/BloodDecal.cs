using UnityEngine;
using System.Collections;

/// <summary>
/// 死亡血迹（代码生成，无需贴图）
/// 由 Enemy.cs 死亡时自动调用 Init()
/// </summary>
public class BloodDecal : MonoBehaviour
{
    public float lifetime    = 15f;
    public float fadeOutTime = 3f;
    public float growTime    = 0.25f;

    private static readonly Color PoolColor  = new Color(0.38f, 0f,    0f,    0.9f);
    private static readonly Color SplatColor = new Color(0.5f,  0.04f, 0.04f, 0.75f);

    public void Init()
    {
        StartCoroutine(GrowPool());
        SpawnSplats();
        StartCoroutine(LifetimeFade());
    }

    // ── 血池主体
    IEnumerator GrowPool()
    {
        var pool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pool.transform.SetParent(transform);
        pool.transform.localPosition = Vector3.zero;
        pool.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        Destroy(pool.GetComponent<Collider>());

        var mat = URPMaterialUtil.CreateTransparentMaterial(PoolColor);
        pool.GetComponent<Renderer>().material = mat;

        float tx = transform.localScale.x;
        float tz = transform.localScale.z;
        float elapsed = 0f;

        while (elapsed < growTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / growTime);
            pool.transform.localScale = new Vector3(t * tx, 0.012f, t * tz);
            yield return null;
        }
        pool.transform.localScale = new Vector3(tx, 0.012f, tz);
    }

    // ── 溅点
    void SpawnSplats()
    {
        int count = Random.Range(5, 11);
        for (int i = 0; i < count; i++)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            s.transform.SetParent(transform);
            Destroy(s.GetComponent<Collider>());

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist  = Random.Range(0.25f, transform.localScale.x * 0.75f);
            s.transform.localPosition = new Vector3(Mathf.Cos(angle) * dist, 0.008f, Mathf.Sin(angle) * dist);

            float sz = Random.Range(0.04f, 0.18f);
            s.transform.localScale = new Vector3(sz, 0.012f, sz);

            s.GetComponent<Renderer>().material =
                URPMaterialUtil.CreateTransparentMaterial(SplatColor);
        }
    }

    // ── 淡出消失
    IEnumerator LifetimeFade()
    {
        yield return new WaitForSeconds(lifetime);

        float elapsed = 0f;
        var renderers = GetComponentsInChildren<Renderer>();

        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - elapsed / fadeOutTime;
            foreach (var r in renderers)
                URPMaterialUtil.SetAlpha(r.material, r.material.color.a * alpha);
            yield return null;
        }

        Destroy(gameObject);
    }
}
