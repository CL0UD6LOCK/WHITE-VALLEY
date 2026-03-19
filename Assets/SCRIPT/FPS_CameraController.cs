using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 第三人称相机（雨中冒险2风格）
/// 挂在 Main Camera 上，target 拖入角色身上的 CameraAnchor（胸口高度空物体）
/// 
/// 【相机控制模式】
///   MouseAim 模式（默认）：鼠标控制准星瞄准，相机跟随角色身体朝向
///   FreeLook 模式：按住 Alt 自由转动相机视角（类 PUBG）
///   两种模式下鼠标都不锁定，保持 Confined 状态
/// </summary>
public class FPS_Camera : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target;

    [Header("鼠标灵敏度")]
    public float mouseSensitivity  = 2.0f;
    [Tooltip("平滑值，0=无平滑即时响应，建议 0")]
    public float rotationSmoothing = 0f;

    [Header("相机偏移（肩膀视角）")]
    public Vector3 offset = new Vector3(0.5f, 1.8f, -3.0f);

    [Header("俯仰角限制")]
    public float minPitch = -40f;
    public float maxPitch =  60f;

    [Header("遮挡检测（防穿墙）")]
    [Tooltip("开启后相机会在遇到墙壁时自动拉近")]
    public bool  enableCollision   = true;
    public float collisionRadius   = 0.2f;
    public LayerMask collisionMask = ~0;     // 默认检测所有层，可排除 Player 层

    [Header("落地/起跳震动")]
    [Tooltip("触发落地震动所需的最低下落速度（m/s），防止小跳也抖")]
    public float landShakeMinSpeed   = 5f;
    [Tooltip("最大震动幅度对应的下落速度，超过此值幅度不再增加")]
    public float landShakeMaxSpeed   = 20f;
    public float landShakeMaxAmount  = 0.15f;
    public float landShakeDuration   = 0.25f;
    public float jumpShakeAmount     = 0.04f;
    public float jumpShakeDuration   = 0.12f;

    [Header("走路视野摇晃 (Bob)")]
    [Tooltip("走路/跑步时相机轻微上下摆动，模拟头部运动感")]
    public bool  enableBob     = true;
    public float bobFrequency  = 7f;    // 摆动频率，对应步频
    public float bobAmplitudeY = 0.05f; // 上下幅度
    public float bobAmplitudeX = 0.025f;// 左右幅度

    [Header("自由观察 (Alt)")]
    public KeyCode altKey = KeyCode.LeftAlt;

    // ── 私有：旋转
    private float xRotation = 0f;
    private float yRotation = 0f;
    private float currentX  = 0f;
    private float currentY  = 0f;

    // ── 私有：自由视角
    private bool  _isFreeLooking      = false;
    private float _freeLookYaw        = 0f;
    private float _freeLookPitch      = 0f;
    private float _savedYaw           = 0f;
    private float _savedPitch         = 0f;
    public  float freeLookReturnSpeed = 35f;  // 回正速度，Inspector 可调
    private bool  _isSnapReturning    = false;  // 松开Alt后的平滑回正状态

    // ── 私有：震动（本地空间偏移，不受 LookAt 影响）
    private float   shakeTimer     = 0f;
    private float   shakeDuration  = 0f;
    private float   shakeMagnitude = 0f;
    private Vector3 shakeLocalOffset = Vector3.zero;

    // ── 私有：Bob
    private float   bobTimer    = 0f;
    private Vector3 bobLocalOffset = Vector3.zero;

    // ── 私有：角色引用
    private FPS_PlayerController _player;
    private CharacterController  _cc;
    private bool  _wasGrounded   = true;
    private float _lastVelocityY = 0f;

    /// <summary>当前是否处于 Alt 自由视角模式</summary>
    public bool IsFreeLooking => _isFreeLooking;

    void Start()
    {
        // 锁定鼠标到屏幕中心（准星固定中心模式）
        // 鼠标瞄准模式：Confined + 隐藏，由 InventoryUI 控制显隐
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible   = false;
        if (target) yRotation = target.eulerAngles.y;
        currentX = xRotation;
        currentY = yRotation;

        _player = FindFirstObjectByType<FPS_PlayerController>();
        if (_player != null) _cc = _player.GetComponent<CharacterController>();
    }

    void Update()
    {
        if (_player != null)
        {
            float velY    = _cc ? _cc.velocity.y : 0f;
            bool grounded = _player.IsGrounded;

            // 落地震动：按下落速度决定幅度
            if (!_wasGrounded && grounded)
            {
                float fallSpeed = Mathf.Abs(_lastVelocityY);
                if (fallSpeed >= landShakeMinSpeed)
                {
                    float t      = Mathf.InverseLerp(landShakeMinSpeed, landShakeMaxSpeed, fallSpeed);
                    float amount = Mathf.Lerp(0f, landShakeMaxAmount, t);
                    TriggerShake(amount, landShakeDuration);
                }
            }

            // 起跳震动
            if (_wasGrounded && !grounded)
                TriggerShake(jumpShakeAmount, jumpShakeDuration);

            _wasGrounded   = grounded;
            _lastVelocityY = velY;
        }

        UpdateShake();
        UpdateBob();
        UpdateRecoil();
    }

    void LateUpdate()
    {
        if (!target) return;

        // ── 背包打开时暂停鼠标旋转输入
        if (!InventoryUI.IsOpen)
        {
            bool altHeld = Input.GetKey(altKey);

            if (altHeld && !_isFreeLooking)
            {
                // 刚按下 Alt：保存当前相机角度，自由视角从这里开始
                _isFreeLooking = true;
                _savedYaw      = yRotation;
                _savedPitch    = xRotation;
                _freeLookYaw   = yRotation;
                _freeLookPitch = xRotation;
            }
            else if (!altHeld && _isFreeLooking)
            {
                // 松开 Alt：
                // currentX/Y 保持在自由视角当前位置（不赋值，让 Lerp 从这里开始）
                // xRotation/yRotation 设为目标（普通相机跟随角色的角度）
                _isFreeLooking   = false;
                _isSnapReturning = true;   // 启动平滑回正
                // currentX/currentY 故意不动，保留自由视角最后的角度
                // 下方 Lerp 会从这里平滑过渡到 xRotation/yRotation
                xRotation = _freeLookPitch;   // 普通模式目标 = 自由视角结束角度
                yRotation = _freeLookYaw;     // 两者此刻相同，之后普通模式输入会改变 xRotation/yRotation

                // 触发角色转身（平滑，由 PlayerController 的 snapBack 处理）
                if (_player != null)
                    _player.StartSnapBack(_freeLookYaw);
            }

            if (_isFreeLooking)
            {
                // ── 自由视角：鼠标控制独立的FreeLook角度，完全不动 yRotation/xRotation
                _freeLookYaw   += Input.GetAxis("Mouse X") * mouseSensitivity;
                _freeLookPitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
                _freeLookPitch  = Mathf.Clamp(_freeLookPitch, minPitch, maxPitch);
                // 水平360度不限制
            }
            else
            {
                // ── 普通模式：鼠标直接控制相机
                yRotation += Input.GetAxis("Mouse X") * mouseSensitivity;
                xRotation -= Input.GetAxis("Mouse Y") * mouseSensitivity;
                xRotation  = Mathf.Clamp(xRotation, minPitch, maxPitch);
            }
        }

        if (_isFreeLooking)
        {
            currentX = _freeLookPitch;
            currentY = _freeLookYaw;
        }
        else if (_isSnapReturning)
        {
            // 松开Alt后平滑回正——只有这段时间才用 Lerp
            currentX = Mathf.LerpAngle(currentX, xRotation, Time.deltaTime * 25f);
            currentY = Mathf.LerpAngle(currentY, yRotation, Time.deltaTime * 25f);

            // 足够近就结束 snap，切回即时模式
            if (Mathf.Abs(Mathf.DeltaAngle(currentX, xRotation)) < 0.3f &&
                Mathf.Abs(Mathf.DeltaAngle(currentY, yRotation)) < 0.3f)
            {
                currentX         = xRotation;
                currentY         = yRotation;
                _isSnapReturning = false;
            }
        }
        else
        {
            // ★ 正常瞄准模式：直接赋值，零延迟，无滑动感
            currentX     = xRotation;
            currentY     = yRotation;
        }

        // ── 计算基础位置（后坐偏移叠加到 pitch）
        Quaternion rotation  = Quaternion.Euler(currentX - _recoilPitch, currentY, 0f);
        Vector3    desiredPos = target.position + rotation * offset;
        Vector3    finalPos   = desiredPos;

        if (enableCollision)
        {
            // ── 指定 Tag 物体：半透明处理，不推开相机
            FadeNearbyObjects(target.position, desiredPos);

            // ── 其他物体：正常相机回避推开
            Vector3 dir     = desiredPos - target.position;
            float   maxDist = dir.magnitude;
            if (Physics.SphereCast(target.position, collisionRadius, dir.normalized,
                                   out RaycastHit hit, maxDist, collisionMask))
            {
                // 如果打到的是指定 Tag 物体就不推开
                if (!IsTransparentTag(hit.collider.gameObject.tag))
                    finalPos = hit.point + hit.normal * collisionRadius;
            }
        }

        transform.position = finalPos;
        transform.rotation = rotation;  // 先设好旋转……

        // ── Bob 和震动在相机本地空间叠加（这样无论朝哪都能看到效果）
        transform.position += transform.TransformDirection(bobLocalOffset + shakeLocalOffset);

        // ── 朝向角色（保持偏移感，不完全盯后脑勺）
        Vector3 lookTarget = target.position + rotation * new Vector3(offset.x * 0.3f, 0f, 10f);
        transform.LookAt(lookTarget);
    }

    // ════════════════════════════════════════════════════
    //  Bob
    // ════════════════════════════════════════════════════
    void UpdateBob()
    {
        if (!enableBob || _player == null)
        {
            bobLocalOffset = Vector3.Lerp(bobLocalOffset, Vector3.zero, Time.deltaTime * 10f);
            return;
        }

        float hSpeed  = _player.HorizontalSpeed;
        bool  onGround = _player.IsGrounded;

        if (onGround && hSpeed > 0.5f)
        {
            float speedFactor = Mathf.Clamp01(hSpeed / _player.runSpeed);
            // 速度越快频率越高、幅度越大
            bobTimer += Time.deltaTime * bobFrequency * (0.6f + speedFactor * 0.4f);

            bobLocalOffset = new Vector3(
                Mathf.Sin(bobTimer * 0.5f)     * bobAmplitudeX * speedFactor,  // 左右
                Mathf.Abs(Mathf.Sin(bobTimer)) * bobAmplitudeY * speedFactor,  // 上下（始终向上弹）
                0f
            );
        }
        else
        {
            bobTimer       = 0f;
            bobLocalOffset = Vector3.Lerp(bobLocalOffset, Vector3.zero, Time.deltaTime * 8f);
        }
    }


    // ════════════════════════════════════════════════════
    //  近距离半透明（仅对指定 Tag 生效）
    // ════════════════════════════════════════════════════
    [Header("半透明 Tag 列表（这些 Tag 的物体靠近相机会半透明而不是推开相机）")]
    public string[] transparentTags = { "Player", "Pickup", "Weapon" };

    [Header("半透明距离")]
    public float fadeStartDist = 1.8f;
    public float fadeEndDist   = 0.4f;

    private Dictionary<Renderer, Material[]> _originalMats  = new Dictionary<Renderer, Material[]>();
    private HashSet<Renderer>                _fadingNow      = new HashSet<Renderer>();

    bool IsTransparentTag(string tag)
    {
        foreach (var t in transparentTags)
            if (t == tag) return true;
        return false;
    }

    void FadeNearbyObjects(Vector3 playerPos, Vector3 camPos)
    {
        Vector3 dir  = camPos - playerPos;
        float   dist = dir.magnitude;
        var     hits = Physics.SphereCastAll(playerPos, 0.3f, dir.normalized, dist, collisionMask);

        var currentlyFading = new HashSet<Renderer>();

        foreach (var hit in hits)
        {
            if (hit.collider.isTrigger) continue;

            // 只处理指定 Tag 的物体
            if (!IsTransparentTag(hit.collider.gameObject.tag))
            {
                // 找根物体的 Tag
                var root = hit.collider.transform.root;
                if (!IsTransparentTag(root.gameObject.tag)) continue;
            }

            var rends = hit.collider.GetComponentsInChildren<Renderer>();
            foreach (var r in rends)
            {
                if (r == null) continue;
                float objDist = Vector3.Distance(r.transform.position, camPos);
                float alpha   = Mathf.Clamp01(Mathf.InverseLerp(fadeEndDist, fadeStartDist, objDist));

                // 第一次淡出时保存原始材质实例
                if (!_originalMats.ContainsKey(r))
                    _originalMats[r] = r.materials;

                SetRendererAlpha(r, alpha);
                currentlyFading.Add(r);
            }
        }

        // 恢复不再需要淡出的物体
        var toRestore = new List<Renderer>();
        foreach (var r in _originalMats.Keys)
            if (!currentlyFading.Contains(r))
                toRestore.Add(r);

        foreach (var r in toRestore)
        {
            if (r != null) SetRendererAlpha(r, 1f);
            _originalMats.Remove(r);
        }

        _fadingNow = currentlyFading;
    }

    void SetRendererAlpha(Renderer r, float alpha)
    {
        foreach (var mat in r.materials)
        {
            if (alpha < 0.99f) URPMaterialUtil.SetTransparent(mat);
            URPMaterialUtil.SetAlpha(mat, alpha);
        }
    }

    // ════════════════════════════════════════════════════
    //  震动
    // ════════════════════════════════════════════════════
    public void TriggerShake(float magnitude, float duration)
    {
        shakeMagnitude = magnitude;
        shakeDuration  = duration;
        shakeTimer     = duration;
    }

    // ── 开火后坐：pitch 向上偏然后弹回
    private float _recoilPitch     = 0f;   // 当前后坐偏移量
    private float _recoilTarget    = 0f;   // 目标偏移（开火时设置）
    private float _recoilSpeed     = 20f;  // 恢复速度

    /// <summary>WeaponController 每次开火时调用</summary>
    public void TriggerRecoil(float amount = 1.5f)
    {
        _recoilTarget += amount;   // 叠加，连射时后坐累积
    }

    void UpdateRecoil()
    {
        // 目标值衰减
        _recoilTarget  = Mathf.Lerp(_recoilTarget, 0f, Time.deltaTime * 8f);
        // 实际 pitch 跟随目标
        _recoilPitch   = Mathf.Lerp(_recoilPitch, _recoilTarget, Time.deltaTime * _recoilSpeed);
    }

    void UpdateShake()
    {
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float t     = shakeTimer / shakeDuration;           // 1→0 衰减
            // 本地空间：X左右 Y上下，不在Z（前后）方向抖
            shakeLocalOffset = new Vector3(
                Random.Range(-1f, 1f) * shakeMagnitude * t,
                Random.Range(-1f, 1f) * shakeMagnitude * t,
                0f
            );
        }
        else
        {
            shakeLocalOffset = Vector3.Lerp(shakeLocalOffset, Vector3.zero, Time.deltaTime * 20f);
        }
    }
}