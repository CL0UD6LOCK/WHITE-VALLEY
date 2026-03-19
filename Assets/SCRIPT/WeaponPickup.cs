using UnityEngine;

/// <summary>
/// 场景武器拾取物
/// 挂在武器道具 GameObject 上，Collider 勾 Is Trigger
/// </summary>
public class WeaponPickup : MonoBehaviour
{
    [Header("武器数据")]
    public WeaponData weaponData;

    [Header("拾取提示（可选 UI Text）")]
    public string pickupHint = "按 F 拾取";

    private bool _playerInRange = false;
    private InventorySystem _player;

    void Update()
    {
        if (_playerInRange && Input.GetKeyDown(KeyCode.F))
            TryPickup();
    }

    void OnTriggerEnter(Collider other)
    {
        var inv = other.GetComponent<InventorySystem>();
        if (inv == null) return;
        _player       = inv;
        _playerInRange = true;
        // 可在此显示拾取提示 UI
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
            Destroy(gameObject);
        else
            Debug.Log("背包已满！");   // 可改成 UI 提示
    }

    // 编辑器里显示拾取范围
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }
}
