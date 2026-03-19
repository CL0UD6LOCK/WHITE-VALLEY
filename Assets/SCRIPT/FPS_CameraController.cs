using UnityEngine;

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

    void Start()
    {
        // 锁定鼠标到屏幕中心（准星固定中心模式）
        Cursor.lockState = CursorLockMode.Locked;
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
    }

    void LateUpdate()
    {
        if (!target) return;

        // ── 背包打开时暂停鼠标旋转输入，但相机继续跟随角色位置
        if (!InventoryUI.IsOpen)
        {
            yRotation += Input.GetAxis("Mouse X") * mouseSensitivity;
            xRotation -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            xRotation  = Mathf.Clamp(xRotation, minPitch, maxPitch);
        }

        if (rotationSmoothing > 0f)
        {
            float spd = 1f / Mathf.Max(rotationSmoothing, 0.001f);
            currentX  = Mathf.LerpAngle(currentX, xRotation, Time.deltaTime * spd);
            currentY  = Mathf.LerpAngle(currentY, yRotation, Time.deltaTime * spd);
        }
        else
        {
            currentX = xRotation;
            currentY = yRotation;
        }

        // ── 计算基础位置
        Quaternion rotation      = Quaternion.Euler(currentX, currentY, 0f);
        Vector3    desiredPos    = target.position + rotation * offset;

        // ── 防穿墙
        Vector3 finalPos = desiredPos;
        if (enableCollision)
        {
            Vector3 dir     = desiredPos - target.position;
            float   maxDist = dir.magnitude;
            if (Physics.SphereCast(target.position, collisionRadius, dir.normalized,
                                   out RaycastHit hit, maxDist, collisionMask))
                finalPos = hit.point + hit.normal * collisionRadius;
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
    //  震动
    // ════════════════════════════════════════════════════
    void TriggerShake(float magnitude, float duration)
    {
        shakeMagnitude = magnitude;
        shakeDuration  = duration;
        shakeTimer     = duration;
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