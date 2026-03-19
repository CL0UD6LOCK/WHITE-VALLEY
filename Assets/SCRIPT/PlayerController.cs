using UnityEngine;
using System.Collections;

/// <summary>
/// FPS 人物控制器 v3
/// 
/// ★ 道具扩展接口（二段跳/弹跳鞋等）：
///   不要改这个脚本，另写道具脚本调用：
///     player.GiveExtraJump(次数, 力度);
///     player.RemoveExtraJump();
///   详见脚本底部注释示例。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FPS_PlayerController : MonoBehaviour
{
    // ════════════════════════════════════════════════════
    [Header("移动设置")]
    public float walkSpeed = 3.0f;
    public float runSpeed  = 6.0f;
    public float jumpPower = 7.0f;
    public float gravity   = -20f;

    [Header("跳跃容错")]
    [Tooltip("离地后仍可起跳的时间窗（解决下坡/边缘漏检）")]
    public float coyoteTime          = 0.2f;
    [Tooltip("提前按跳跃键的缓冲时间")]
    public float jumpBuffer          = 0.2f;
    [Tooltip("SphereCast 向下检测额外距离（解决下坡 isGrounded 失灵）")]
    public float groundCheckDistance = 0.3f;
    [Tooltip("起跳后屏蔽地面检测的时间（防止同一点无法二次起跳 bug）")]
    public float jumpCooldown        = 0.15f;

    [Header("头部骨骼控制")]
    public Transform headBone;
    [Range(0, 120)] public float maxHeadYaw   = 21f;
    [Range(0, 90)]  public float maxHeadPitch = 35f;
    private Quaternion headInitialLocalRotation;

    [Header("自由观察 (Alt)")]
    public KeyCode altKey = KeyCode.LeftAlt;
    private bool   isFreeLooking          = false;
    private float  bodyRotationAtAltStart = 0f;
    private bool   isSnapBack             = false;  // 松开Alt后正在对齐
    private float  snapTargetYaw          = 0f;     // 对齐目标角度

    [Header("表情冷却")]
    public float expressionCooldown = 2f;
    private float expressionTimer   = 0f;

    // ── 永久额外跳系统（以撒式：拾取永久叠加，落地恢复）
    private int   extraJumpMax   = 0;   // 永久上限，每拾取一个道具 +1
    private int   extraJumpsLeft = 0;   // 当前剩余次数，落地后自动恢复到上限
    private float extraJumpPower = 0f;  // 0 = 沿用基础跳力

    // ── 内部状态
    private float coyoteCounter;
    private float jumpBufferCounter;
    private float jumpCooldownCounter;
    private bool  wasGrounded;
    private bool  isGroundedExtended;
    private float lastVelocityY = 0f;   // 记录上帧 Y 速度，落地时传给粒子

    private CharacterController controller;
    private Animator             animator;
    private Transform            camTransform;
    private Vector3              velocity;

    [Header("脚底粒子（自动查找子物体 FootDustFX）")]
    public FootDustController footDust;

    // ════════════════════════════════════════════════════
    //  公开只读属性（供相机脚本、UI 等读取）
    // ════════════════════════════════════════════════════
    public bool  IsGrounded        => isGroundedExtended;
    public float HorizontalSpeed   => new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;
    public int   ExtraJumpsMax       => extraJumpMax;
    public int   ExtraJumpsLeft      => extraJumpsLeft;

    // ════════════════════════════════════════════════════
    void Start()
    {
        controller   = GetComponent<CharacterController>();
        animator     = GetComponent<Animator>();
        camTransform = Camera.main.transform;

        if (headBone != null)
            headInitialLocalRotation = headBone.localRotation;

        if (footDust == null)
            footDust = GetComponentInChildren<FootDustController>();

        Cursor.lockState = CursorLockMode.Confined;
    }

    bool CheckGrounded()
    {
        // 起跳冷却期内强制腾空，防止原地无法二次起跳
        if (jumpCooldownCounter > 0) return false;

        if (controller.isGrounded) return true;

        Vector3 origin = transform.position + Vector3.up * controller.radius;
        return Physics.SphereCast(
            origin,
            controller.radius * 0.9f,
            Vector3.down,
            out _,
            groundCheckDistance + controller.skinWidth
        );
    }

    void Update()
    {
        if (expressionTimer     > 0) expressionTimer     -= Time.deltaTime;
        if (jumpCooldownCounter > 0) jumpCooldownCounter -= Time.deltaTime;

        isGroundedExtended = CheckGrounded();

        if (isGroundedExtended)
        {
            coyoteCounter = coyoteTime;
            extraJumpsLeft = extraJumpMax;   // 落地恢复所有额外跳次数
            if (velocity.y < 0) velocity.y = -2f;
        }
        else
        {
            if (coyoteCounter > 0) coyoteCounter -= Time.deltaTime;
        }

        if (!wasGrounded && isGroundedExtended) OnLanded(Mathf.Abs(lastVelocityY));
        wasGrounded   = isGroundedExtended;
        lastVelocityY = velocity.y;

        // ── 自由观察
        if (Input.GetKeyDown(altKey))
        {
            isFreeLooking        = true;
            isSnapBack           = false;
            bodyRotationAtAltStart = transform.eulerAngles.y;
        }

        if (Input.GetKeyUp(altKey))
        {
            isFreeLooking  = false;
            isSnapBack     = true;   // 启动平滑对齐
            snapTargetYaw  = camTransform.eulerAngles.y;  // 记录松开时相机朝向
        }

        // ── 松开Alt后平滑对齐到相机方向（有旋转过程，不瞬变）
        if (isSnapBack)
        {
            float snapSpeed = 600f;   // 度/秒，调大更快，调小更慢
            Quaternion target  = Quaternion.Euler(0, snapTargetYaw, 0);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, snapSpeed * Time.deltaTime);

            // 对齐完成后切换回正常跟随
            if (Quaternion.Angle(transform.rotation, target) < 0.5f)
            {
                transform.rotation = target;
                isSnapBack         = false;
            }
        }
        else if (!isFreeLooking)
        {
            // 正常模式：身体跟随武器瞄准方向（由 WeaponController 驱动）
            // 这里不再强制 Slerp 到相机，避免和武器瞄准转向冲突
        }

        // ── 移动
        float   x            = Input.GetAxis("Horizontal");
        float   z            = Input.GetAxis("Vertical");
        Vector3 move         = transform.right * x + transform.forward * z;
        bool    isRunning    = Input.GetKey(KeyCode.LeftShift);
        float   currentSpeed = isRunning ? runSpeed : walkSpeed;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // ── 跳跃 Buffer
        if (Input.GetButtonDown("Jump"))
            jumpBufferCounter = jumpBuffer;
        else if (jumpBufferCounter > 0)
            jumpBufferCounter -= Time.deltaTime;

        // ── 跳跃执行
        if (jumpBufferCounter > 0)
        {
            if (coyoteCounter > 0)
            {
                PerformJump(jumpPower);
            }
            else if (!isGroundedExtended && extraJumpsLeft > 0)
            {
                extraJumpsLeft--;
                PerformJump(extraJumpPower > 0 ? extraJumpPower : jumpPower);
            }
        }

        // ── 重力
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // ── 动画
        animator.SetFloat("Horizontal", x, 0.1f, Time.deltaTime);
        animator.SetFloat("Vertical",   z, 0.1f, Time.deltaTime);
        animator.SetBool ("IsGrounded", isGroundedExtended);

        // ── 粒子
        if (footDust != null)
            footDust.UpdateState(isGroundedExtended, HorizontalSpeed, isRunning);

        HandleExpressions();
    }

    void PerformJump(float power)
    {
        velocity.y          = power;
        jumpBufferCounter   = 0;
        coyoteCounter       = 0;
        jumpCooldownCounter = jumpCooldown;

        animator.SetTrigger("Jump");
        StopCoroutine("JumpVisualEffect");
        StartCoroutine("JumpVisualEffect");

        if (footDust != null) footDust.PlayJumpBurst();
    }

    void OnLanded(float fallSpeed)
    {
        if (footDust != null) footDust.PlayLandBurst(fallSpeed);
    }

    // ════════════════════════════════════════════════════
    //  道具系统公开接口
    // ════════════════════════════════════════════════════
    /// <summary>
    /// 永久增加空中跳跃次数上限（以撒式拾取）
    /// 每次拾取调用一次，效果永久叠加
    /// </summary>
    public void AddPermanentJump(int count = 1, float power = 0f)
    {
        extraJumpMax  += count;
        extraJumpsLeft = extraJumpMax;   // 立即刷新当前可用次数
        if (power > 0f) extraJumpPower = power;
    }

    /// <summary>重置所有额外跳（换图/死亡时调用）</summary>
    public void ResetExtraJumps()
    {
        extraJumpMax   = 0;
        extraJumpsLeft = 0;
        extraJumpPower = 0f;
    }

    /// <summary>相机松开Alt时调用，触发角色平滑转向目标角度</summary>
    public void StartSnapBack(float targetYaw)
    {
        isSnapBack    = true;
        snapTargetYaw = targetYaw;
    }

    // ════════════════════════════════════════════════════
    void LateUpdate()
    {
        if (headBone == null) return;

        float yawDiff, pitchDiff;
        if (isFreeLooking)
        {
            yawDiff   =  Mathf.DeltaAngle(bodyRotationAtAltStart, camTransform.eulerAngles.y);
            pitchDiff = -Mathf.DeltaAngle(camTransform.eulerAngles.x, 0);
        }
        else
        {
            yawDiff   =  Mathf.DeltaAngle(transform.eulerAngles.y, camTransform.eulerAngles.y);
            pitchDiff = -Mathf.DeltaAngle(transform.eulerAngles.x, camTransform.eulerAngles.x);
        }

        float clampedYaw   = Mathf.Clamp(yawDiff,   -maxHeadYaw,   maxHeadYaw);
        float clampedPitch = Mathf.Clamp(pitchDiff, -maxHeadPitch, maxHeadPitch);
        headBone.localRotation = headInitialLocalRotation * Quaternion.Euler(clampedPitch, clampedYaw, 0);
    }

    void HandleExpressions()
    {
        if (isGroundedExtended && HorizontalSpeed <= walkSpeed + 0.5f && expressionTimer <= 0)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) { animator.SetTrigger("Action1"); expressionTimer = expressionCooldown; }
            if (Input.GetKeyDown(KeyCode.Alpha2)) { animator.SetTrigger("Action2"); expressionTimer = expressionCooldown; }
        }
    }

    IEnumerator JumpVisualEffect()
    {
        transform.localScale = new Vector3(0.8f, 1.4f, 0.8f);
        yield return new WaitForSeconds(0.1f);
        float elapsed = 0f, duration = 0.2f;
        while (elapsed < duration)
        {
            elapsed             += Time.deltaTime;
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, elapsed / duration);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }
}

/* ══════════════════════════════════════════════════════════════════
   【道具扩展示例 — 独立文件，完全不修改角色脚本】

   ── A：一次性拾取道具（弹跳蘑菇、气垫）
   新建 DoubleJumpPickup.cs：

       public class DoubleJumpPickup : MonoBehaviour
       {
           public int   extraCount = 1;
           public float extraPower = 8f;   // 0 = 沿用角色基础跳力

           void OnTriggerEnter(Collider other)
           {
               var player = other.GetComponent<FPS_PlayerController>();
               if (player == null) return;
               player.GiveExtraJump(extraCount, extraPower);
               Destroy(gameObject);
           }
       }

   ── B：持续装备（弹簧鞋 Buff）
   新建 SpringBootBuff.cs：

       public class SpringBootBuff : MonoBehaviour
       {
           public float extraPower = 8f;
           private FPS_PlayerController _player;

           public void Equip(FPS_PlayerController target)
           {
               _player = target;
               InvokeRepeating(nameof(Refresh), 0f, 0.05f);
           }

           public void Unequip()
           {
               CancelInvoke(nameof(Refresh));
               _player?.RemoveExtraJump();
           }

           void Refresh()
           {
               if (_player != null && _player.ExtraJumpsRemaining == 0)
                   _player.GiveExtraJump(1, extraPower);
           }
       }
══════════════════════════════════════════════════════════════════ */