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

    void Update()
    {
        if (_playerInRange && Input.GetKeyDown(KeyCode.E))
            TryPickup();
    }

    void OnTriggerEnter(Collider other)
    {
        var inv = other.GetComponent<InventorySystem>();
        if (inv == null) return;
        _player        = inv;
        _playerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<InventorySystem>() == null) return;
        _player        = null;
        _playerInRange = false;
    }

    void TryPickup()
    {
        if (_player == null || itemData == null) return;

        // 这里接入你的道具效果逻辑
        // 例如：extra jump pickup
        var player = _player.GetComponent<FPS_PlayerController>();
        if (player != null)
        {
            // 示例：额外跳跃道具
            // player.AddPermanentJump(1);
        }

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