using UnityEngine;

/// <summary>
/// 脚底粒子控制器 v2
/// 
/// 效果：
///   - 奔跑时：拖尾残影式尘土（少量持续粒子 + 随速度拉长）
///   - 落地时：爆发尘土（根据下落速度决定数量，轻落不触发）
///   - 起跳时：轻微爆发
/// 
/// 【推荐 ParticleSystem 设置】
///   Duration        : 1  Loop: true
///   Start Lifetime  : 0.2 ~ 0.5
///   Start Speed     : 0.5 ~ 1.5
///   Start Size      : 0.05 ~ 0.15
///   Start Color     : 米白/浅灰，Alpha 80~140
///   Gravity Modifier: 0.1（轻微上浮）
///   Simulation Space: World          ← 重要！
///   Renderer Mode   : Billboard
///   Shape           : Circle, Radius 0.12, 旋转 X=-90（朝上喷）
///   Trails 模块     : 开启，Lifetime 0.15，Width 0.03~0.06（拖尾效果来源）
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class FootDustController : MonoBehaviour
{
    [Header("速度阈值")]
    [Tooltip("超过此速度才开始发射跑步尘")]
    public float runThreshold = 4.0f;

    [Header("跑步拖尾粒子")]
    [Tooltip("奔跑时每秒发射的粒子数（建议 5~12，不要太多）")]
    public float runEmissionRate = 8f;

    [Header("落地爆发")]
    [Tooltip("触发落地效果所需的最低下落速度（m/s）")]
    public float landMinFallSpeed  = 4f;
    [Tooltip("下落速度达到此值时爆发粒子数量最多")]
    public float landMaxFallSpeed  = 18f;
    [Tooltip("最少爆发粒子数（刚超过阈值时）")]
    public int   landMinBurst      = 4;
    [Tooltip("最多爆发粒子数（高速落地时）")]
    public int   landMaxBurst      = 18;

    [Header("起跳爆发")]
    public int jumpBurstCount = 5;

    // ── 私有
    private ParticleSystem                  ps;
    private ParticleSystem.EmissionModule   emission;

    void Awake()
    {
        ps       = GetComponent<ParticleSystem>();
        emission = ps.emission;
        emission.enabled = true;
        SetRate(0f);

        // 强制 Simulation Space = World，防止粒子跟着角色旋转偏移
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Shape 朝上
        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius    = 0.12f;
        shape.rotation  = new Vector3(-90f, 0f, 0f);
    }

    /// <summary>每帧由 PlayerController 调用</summary>
    public void UpdateState(bool isGrounded, float horizontalSpeed, bool isRunning)
    {
        if (!isGrounded || horizontalSpeed < runThreshold)
        {
            SetRate(0f);
            return;
        }

        // 速度越快粒子略多，但上限较低（拖尾感靠 Trails 模块，不靠堆粒子数）
        float speedFactor = Mathf.Clamp01((horizontalSpeed - runThreshold) / 4f);
        SetRate(runEmissionRate * speedFactor);
    }

    /// <summary>
    /// 落地爆发，传入落地瞬间的下落速度（取绝对值）
    /// 由 PlayerController.OnLanded 调用，需传入下落速度
    /// </summary>
    public void PlayLandBurst(float fallSpeed)
    {
        if (fallSpeed < landMinFallSpeed) return;   // 轻落不触发

        float t     = Mathf.InverseLerp(landMinFallSpeed, landMaxFallSpeed, fallSpeed);
        int   count = Mathf.RoundToInt(Mathf.Lerp(landMinBurst, landMaxBurst, t));
        ps.Emit(count);
    }

    /// <summary>起跳爆发</summary>
    public void PlayJumpBurst()
    {
        SetRate(0f);
        ps.Emit(jumpBurstCount);
    }

    private void SetRate(float rate)
    {
        var r = emission.rateOverTime;
        r.constant          = rate;
        emission.rateOverTime = r;
    }
}