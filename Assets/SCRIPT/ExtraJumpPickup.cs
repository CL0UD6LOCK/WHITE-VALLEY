using UnityEngine;

/// <summary>
/// 永久额外跳跃道具（以撒式）
/// 
/// 【挂载方式】
///   1. 创建一个道具 GameObject（放个模型或 Sprite）
///   2. 添加一个 Collider，勾选 Is Trigger
///   3. 把这个脚本挂上去
///   4. 做成 Prefab，在场景里随意摆放
/// 
/// 【效果】
///   角色碰到后永久 +1 空中跳次数，可叠加多个
///   落地后自动恢复所有次数（类似以撒）
/// </summary>
public class ExtraJumpPickup : MonoBehaviour
{
    [Header("道具设置")]
    [Tooltip("拾取后增加几次空中跳（通常填 1）")]
    public int jumpsToAdd = 1;

    [Tooltip("空中跳的力度，0 = 沿用角色基础跳力")]
    public float jumpPower = 0f;

    [Header("拾取效果")]
    [Tooltip("拾取时播放的粒子特效 Prefab（可选）")]
    public GameObject pickupFX;

    [Tooltip("拾取音效（可选）")]
    public AudioClip pickupSound;

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<FPS_PlayerController>();
        if (player == null) return;

        // 永久叠加跳跃次数
        player.AddPermanentJump(jumpsToAdd, jumpPower);

        // 播放特效
        if (pickupFX != null)
            Instantiate(pickupFX, transform.position, Quaternion.identity);

        // 播放音效
        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        // 道具消失
        Destroy(gameObject);
    }
}