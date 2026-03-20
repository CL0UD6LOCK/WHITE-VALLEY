using UnityEngine;

/// <summary>
/// 场景道具拾取物
/// 挂在道具 GameObject 上，Collider 勾 Is Trigger，Tag 设为 ITEM
/// 自动添加悬浮旋转发光效果
/// </summary>
public class PickupItem : MonoBehaviour
{
    [Header("道具数据")]
    public PickupItemData itemData;

    [Header("拾取范围")]
    public float pickupRadius = 1.5f;

    private bool            _playerInRange;
    private InventorySystem _player;
    private PickupVisual    _visual;

    void Start()
    {
        // 自动添加视觉效果
        _visual = GetComponent<PickupVisual>();
        if (_visual == null)
            _visual = gameObject.AddComponent<PickupVisual>();

        if (itemData != null)
            _visual.Init(itemData.RarityColor(), itemData.modelPrefab);
    }

    void OnTriggerEnter(Collider other)
    {
        var inv = other.GetComponent<InventorySystem>();
        if (inv == null) return;
        _player = inv;
        TryPickup();
    }

    void TryPickup()
    {
        if (_player == null || itemData == null) return;

        var player = _player.GetComponent<FPS_PlayerController>();

        // 检测并应用道具效果
        var bulletSizeUp = GetComponent<BulletSizeUpItem>();
        if (bulletSizeUp != null) bulletSizeUp.Apply();

        var magSizeUp = GetComponent<MagSizeUpItem>();
        if (magSizeUp != null) magSizeUp.Apply();

        // 额外跳跃道具示例
        // if (player != null) player.AddPermanentJump(1);

        Debug.Log($"拾取道具: {itemData.itemName}");
        _visual?.OnPickedUp();
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}