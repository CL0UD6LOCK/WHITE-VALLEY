using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// URP 材质工具类
/// 集中处理透明模式切换，避免各脚本重复写 Shader 字符串
/// </summary>
public static class URPMaterialUtil
{
    /// <summary>将材质切换为 URP 透明模式</summary>
    public static void SetTransparent(Material mat)
    {
        // URP Lit / Unlit 通用属性
        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);   // 0=Opaque 1=Transparent
        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 0f);     // Alpha blend

        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite",   0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
    }

    /// <summary>设置材质 Alpha 值</summary>
    public static void SetAlpha(Material mat, float a)
    {
        Color c = mat.color;
        c.a = Mathf.Clamp01(a);
        mat.color = c;
    }

    /// <summary>
    /// 创建一个指定颜色的透明 URP 材质
    /// 自动回退：URP Lit → URP Unlit → Standard → 纯色
    /// </summary>
    public static Material CreateTransparentMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Standard");

        Material mat;
        if (shader != null)
        {
            mat = new Material(shader);
        }
        else
        {
            // 最后回退：用任意已存在的材质复制
            mat = new Material(Shader.Find("Hidden/InternalErrorShader"));
            Debug.LogWarning("[URPMaterialUtil] 找不到合适的 Shader，使用错误 Shader");
        }

        SetTransparent(mat);
        mat.color = color;
        return mat;
    }
}
