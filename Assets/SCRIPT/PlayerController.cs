using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class FPS_PlayerController : MonoBehaviour
{
    [Header("移动设置")]
    public float walkSpeed = 3.0f;
    public float runSpeed = 6.0f;
    public float jumpPower = 7.0f;
    public float gravity = -20f;

    [Header("跳跃容错")]
    public float coyoteTime = 0.15f;
    public float jumpBuffer = 0.15f;

    [Header("自定义骨架 & 头部控制")]
    public Transform headBone; 
    [Range(0, 120)] public float maxHeadYaw = 100f;   // 观察时左右扭头极限
    [Range(0, 90)] public float maxHeadPitch = 60f;  // 观察时上下转头极限
    private Quaternion headInitialLocalRotation;

    [Header("自由观察 (Alt)")]
    public KeyCode altKey = KeyCode.LeftAlt;
    private bool isFreeLooking = false;
    private float bodyRotationAtAltStart; // 记录按下Alt瞬间身体的角度

    [Header("表情冷却")]
    public float expressionCooldown = 2f;
    private float expressionTimer = 0f;

    private float coyoteCounter;
    private float jumpBufferCounter;
    private CharacterController controller;
    private Animator animator;
    private Transform camTransform;
    private Vector3 velocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        camTransform = Camera.main.transform;

        if (headBone != null)
        {
            headInitialLocalRotation = headBone.localRotation;
        }
        
        // 隐藏并锁定鼠标
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (expressionTimer > 0)
            expressionTimer -= Time.deltaTime;

        // ===== 1. 自由观察状态检测 (PUBG 逻辑) =====
        if (Input.GetKeyDown(altKey))
        {
            isFreeLooking = true;
            // 按下瞬间记录身体朝向，作为头扭动的静态参考系
            bodyRotationAtAltStart = transform.eulerAngles.y;
        }
        
        if (Input.GetKeyUp(altKey))
        {
            isFreeLooking = false;
        }

        if (!isFreeLooking)
        {
            // 非观察模式：身体跟随相机
            Quaternion targetRotation = Quaternion.Euler(0, camTransform.eulerAngles.y, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 20f);
        }

        // 2. 移动
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // ===== 地面检测 & 重力 =====
        if (controller.isGrounded)
        {
            coyoteCounter = coyoteTime;
            if (velocity.y < 0) velocity.y = -2f;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        if (Input.GetButtonDown("Jump")) jumpBufferCounter = jumpBuffer;
        else jumpBufferCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0 && coyoteCounter > 0)
        {
            velocity.y = jumpPower;
            jumpBufferCounter = 0;
            coyoteCounter = 0;
            animator.SetTrigger("Jump");
            StopCoroutine("JumpVisualEffect");
            StartCoroutine("JumpVisualEffect");
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        animator.SetFloat("Horizontal", x, 0.1f, Time.deltaTime);
        animator.SetFloat("Vertical", z, 0.1f, Time.deltaTime);

        HandleExpressions();
    }

    void LateUpdate()
    {
        if (headBone == null) return;

        float yawDiff;
        float pitchDiff;

        if (isFreeLooking)
        {
            // 【观察模式】：头相对于“按下Alt那一刻”的身体方向扭动
            yawDiff = Mathf.DeltaAngle(bodyRotationAtAltStart, camTransform.eulerAngles.y);
            // 这里根据你之前的反馈，加入了负号修复上下反向
            pitchDiff = -Mathf.DeltaAngle(camTransform.eulerAngles.x, 0); 
        }
        else
        {
            // 【普通模式】：头基本正对前方（相机方向），DeltaAngle 会趋近于 0
            yawDiff = Mathf.DeltaAngle(transform.eulerAngles.y, camTransform.eulerAngles.y);
            pitchDiff = -Mathf.DeltaAngle(transform.eulerAngles.x, camTransform.eulerAngles.x);
        }

        // 限制角度
        float clampedYaw = Mathf.Clamp(yawDiff, -maxHeadYaw, maxHeadYaw);
        float clampedPitch = Mathf.Clamp(pitchDiff, -maxHeadPitch, maxHeadPitch);

        // 应用旋转
        Quaternion headRotation = Quaternion.Euler(clampedPitch, clampedYaw, 0);
        headBone.localRotation = headInitialLocalRotation * headRotation;
    }

    void HandleExpressions()
    {
        float horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;
        if (controller.isGrounded && horizontalVelocity <= walkSpeed + 0.5f && expressionTimer <= 0)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) { animator.SetTrigger("Action1"); expressionTimer = expressionCooldown; }
            if (Input.GetKeyDown(KeyCode.Alpha2)) { animator.SetTrigger("Action2"); expressionTimer = expressionCooldown; }
        }
    }

    IEnumerator JumpVisualEffect()
    {
        transform.localScale = new Vector3(0.8f, 1.4f, 0.8f);
        yield return new WaitForSeconds(0.1f);
        float elapsed = 0;
        float duration = 0.2f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, elapsed / duration);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }
}