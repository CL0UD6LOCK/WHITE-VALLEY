using UnityEngine;

/// <summary>
/// 武器数据（ScriptableObject）
/// 右键 Project → Create → Game → WeaponData
/// </summary>
[CreateAssetMenu(fileName = "NewWeapon", menuName = "Game/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("基础信息")]
    public string weaponName = "Pistol";
    public Sprite icon;
    [Tooltip("武器的 3D 模型 Prefab，拾取后显示在手上")]
    public GameObject modelPrefab;

    [Header("射击参数")]
    public float damage       = 25f;
    public float fireRate     = 0.2f;      // 射击间隔（秒）
    public int   maxAmmo      = 12;
    public float reloadTime   = 1.5f;

    [Header("子弹设置")]
    public GameObject bulletPrefab;        // 子弹 Prefab（挂 Bullet.cs）
    public float      bulletSpeed  = 30f;  // 子弹初速度
    public float      bulletLifetime = 3f; // 子弹存活时间（超时自动销毁）

    [Header("枪口偏移（子弹从哪里飞出，相对于手部锚点）")]
    public Vector3 muzzleOffset = new Vector3(0f, 0f, 0.3f);

    [Header("视觉/音效")]
    public GameObject muzzleFlashFX;
    public AudioClip  shootSound;
    public AudioClip  reloadSound;
    public AudioClip  emptySound;

    [Header("持枪姿态偏移（相对于手部骨骼）")]
    public Vector3 holdPositionOffset = Vector3.zero;
    public Vector3 holdRotationOffset = Vector3.zero;
}