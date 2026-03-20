using UnityEngine;

/// <summary>
/// 物理子弹 — 自动生成发光拖尾 + 击中爆散粒子
/// 
/// 【Prefab 只需要这些组件，视觉全部由代码生成】
///   Rigidbody:     Interpolate=Interpolate, Collision Detection=Continuous
///   SphereCollider: Radius=0.05, Is Trigger=true
///   Bullet.cs（本脚本）
/// 
/// 不需要手动加 Trail 或粒子，Awake 里自动创建。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    // ── 由 WeaponController 注入
    [HideInInspector] public float  damage   = 10f;
    [HideInInspector] public float  lifetime = 3f;

    [Header("子弹外观颜色（WeaponController 可按武器类型覆盖）")]
    public Color bulletColor     = new Color(1f, 0.85f, 0.2f, 1f);   // 亮黄
    public Color trailStartColor = new Color(1f, 0.6f,  0.1f, 0.9f); // 橙
    public Color trailEndColor   = new Color(1f, 0.2f,  0f,   0f);   // 透明红

    [Header("弹头大小")]
    public float coreRadius = 0.06f;

    [Header("拖尾")]
    public float trailTime  = 0.25f;   // 拖尾持续时间，越大越长
    public float trailWidth = 0.06f;   // 拖尾最宽处

    // ── 私有
    private Rigidbody  _rb;
    private TrailRenderer _trail;
    private Light      _light;
    private static CrosshairUI _crosshair;  // 全局缓存，所有子弹共用

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        BuildVisuals();
    }

    void Start()
    {
        Destroy(gameObject, lifetime);

        // ── 应用道具倍率
        damage *= BulletSizeUpItem.DamageMultiplier;

        // 碰撞体积缩放
        var col = GetComponent<SphereCollider>();
        if (col != null)
            col.radius *= BulletSizeUpItem.SizeMultiplier;

        // 弹头视觉也跟着缩放
        transform.localScale = Vector3.one * BulletSizeUpItem.SizeMultiplier;
    }

    public void Launch(Vector3 velocity)
    {
        _rb.linearVelocity = velocity;
    }

    // ════════════════════════════════════════
    //  自动生成视觉组件
    // ════════════════════════════════════════
    void BuildVisuals()
    {
        // ── 1. 弹头球体
        var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.transform.SetParent(transform, false);
        core.transform.localScale = Vector3.one * coreRadius * 2f;

        // 删掉子物体上多余的 Collider（碰撞由父物体处理）
        Destroy(core.GetComponent<Collider>());

        // 发光材质 — 用 Unlit 系列，打包时不会丢失
        Material mat;
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");
        if (unlitShader == null) unlitShader = Shader.Find("Standard");

        mat = new Material(unlitShader);
        mat.color = bulletColor;

        // HDR Emission（Bloom 发光）
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", bulletColor * 3f);
        }

        core.GetComponent<Renderer>().material = mat;

        // ── 2. 拖尾
        _trail = gameObject.AddComponent<TrailRenderer>();
        _trail.time             = trailTime;
        _trail.minVertexDistance = 0.001f;
        _trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _trail.receiveShadows   = false;

        // 宽度曲线：头宽尾细
        AnimationCurve widthCurve = new AnimationCurve(
            new Keyframe(0f, trailWidth),
            new Keyframe(1f, 0f)
        );
        _trail.widthCurve = widthCurve;

        // 颜色渐变：橙→红→透明
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(trailStartColor, 0f),
                new GradientColorKey(trailEndColor,   1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f,  0f),
                new GradientAlphaKey(0.6f,0.4f),
                new GradientAlphaKey(0f,  1f)
            }
        );
        _trail.colorGradient = grad;

        // 拖尾材质（用内置 Sprites/Default 最稳定）
        var trailMat = new Material(Shader.Find("Sprites/Default"));
        _trail.material = trailMat;

        // ── 3. 点光源（飞过时照亮周围）
        var lightGO = new GameObject("BulletLight");
        lightGO.transform.SetParent(transform, false);
        _light = lightGO.AddComponent<Light>();
        _light.type      = LightType.Point;
        _light.color     = bulletColor;
        _light.intensity = 3f;
        _light.range     = 1.5f;
        _light.shadows   = LightShadows.None;   // 子弹不投影，省性能
    }

    // ════════════════════════════════════════
    //  碰撞
    // ════════════════════════════════════════
    void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;

        // 无视 Player tag 的物体
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player")) return;

        var health = other.GetComponentInParent<HealthSystem>();
        if (health != null)
        {
            float hpBefore  = health.currentHealth;
            health.TakeDamage(damage);
            float actualDmg = hpBefore - health.currentHealth;

            if (actualDmg > 0f)
            {
                DamageNumberSpawner.Spawn(transform.position, actualDmg);

                // 通知准星变橙（缓存引用，不每次查找）
                if (_crosshair == null)
                    _crosshair = FindFirstObjectByType<CrosshairUI>();
                if (_crosshair != null) _crosshair.ShowHit();
            }
        }

        SpawnHitFX();

        if (_trail != null)
        {
            _trail.transform.SetParent(null);
            Destroy(_trail.gameObject, _trail.time + 0.1f);
        }

        Destroy(gameObject);
    }

    void SpawnHitFX()
    {
        var fxGO = new GameObject("HitFX");
        fxGO.transform.position = transform.position;
        fxGO.transform.rotation = Quaternion.LookRotation(-transform.forward);

        var ps = fxGO.AddComponent<ParticleSystem>();

        // ★ 必须先 Stop 再配置，否则 Unity 会报 duration 不能在播放时修改
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main             = ps.main;
        main.duration        = 0.5f;
        main.loop            = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(1.5f, 4f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.03f, 0.1f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.8f, 0.2f, 1f),
            new Color(1f, 0.3f, 0f,   1f)
        );
        main.gravityModifier = 0.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake     = false;   // 禁止自动播放，由我们手动 Play

        var emission         = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 8, 14)
        });

        var shape       = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle     = 35f;
        shape.radius    = 0.01f;

        // 击中闪光点光源
        var flashLight       = fxGO.AddComponent<Light>();
        flashLight.type      = LightType.Point;
        flashLight.color     = new Color(1f, 0.7f, 0.2f);
        flashLight.intensity = 8f;
        flashLight.range     = 2f;
        flashLight.shadows   = LightShadows.None;

        ps.Play();
        Destroy(fxGO, 0.6f);

    }
}