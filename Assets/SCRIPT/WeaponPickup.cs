using UnityEngine;

/// <summary>
/// 场景武器拾取物
/// 挂在武器道具 GameObject 上，Collider 勾 Is Trigger
/// 自动添加 PickupVisual 实现悬浮旋转发光
/// </summary>
public class WeaponPickup : MonoBehaviour
{
    [Header("武器数据")]
    public WeaponData weaponData;

    [Header("拾取提示")]
    public string pickupHint = "按 E 拾取";

    [Header("拾取范围")]
    public float pickupRadius = 1.5f;

    private bool             _playerInRange = false;
    private InventorySystem  _player;
    private PickupVisual     _visual;

    void Start()
    {
        // 自动设置 Collider 范围
        var col = GetComponent<SphereCollider>();
        if (col != null) { col.radius = pickupRadius; col.isTrigger = true; }

        // 自动添加视觉效果
        _visual = GetComponent<PickupVisual>();
        if (_visual == null)
            _visual = gameObject.AddComponent<PickupVisual>();
        if (weaponData != null)
            _visual.Init(weaponData.RarityColor(), weaponData.modelPrefab);
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
        if (_player == null || weaponData == null) return;
        if (_player.TryPickup(weaponData))
        {
            _visual?.OnPickedUp();
            Destroy(gameObject);
        }
        else
            Debug.Log("背包已满！");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }
    void OnValidate()
{
    var col = GetComponent<SphereCollider>();
    if (col != null) col.radius = pickupRadius;
}
}