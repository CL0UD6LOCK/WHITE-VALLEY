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
    }

    void Update()
    {
        // 冷却计时
        if (expressionTimer > 0)
            expressionTimer -= Time.deltaTime;

        // 1. 旋转
        Quaternion targetRotation = Quaternion.Euler(0, camTransform.eulerAngles.y, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 20f);

        // 2. 移动
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        controller.Move(move * currentSpeed * Time.deltaTime);

        // ===== 地面检测 =====

        if (controller.isGrounded)
        {
            coyoteCounter = coyoteTime;

            if (velocity.y < 0)
                velocity.y = -2f;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        // ===== 跳跃缓存 =====

        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBuffer;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // ===== 执行跳跃 =====

        if (jumpBufferCounter > 0 && coyoteCounter > 0)
        {
            velocity.y = jumpPower;

            jumpBufferCounter = 0;
            coyoteCounter = 0;

            animator.SetTrigger("Jump");

            StopAllCoroutines();
            StartCoroutine(JumpVisualEffect());
        }

        // ===== 重力 =====

        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);

        // ===== 动画 =====

        animator.SetFloat("Horizontal", x, 0.1f, Time.deltaTime);
        animator.SetFloat("Vertical", z, 0.1f, Time.deltaTime);

        // ===== 表情 =====

        HandleExpressions();
    }

    void HandleExpressions()
    {
        float horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;

        if (controller.isGrounded && horizontalVelocity <= walkSpeed + 0.5f && expressionTimer <= 0)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                animator.SetTrigger("Action1");
                expressionTimer = expressionCooldown;
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                animator.SetTrigger("Action2");
                expressionTimer = expressionCooldown;
            }
        }
    }

    IEnumerator JumpVisualEffect()
    {
        transform.localScale = new Vector3(0.8f, 1.4f, 0.8f);

        yield return new WaitForSeconds(0.2f);

        float elapsed = 0;

        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;

            transform.localScale = Vector3.Lerp(
                transform.localScale,
                Vector3.one,
                elapsed / 0.2f
            );

            yield return null;
        }

        transform.localScale = Vector3.one;
    }
}