using UnityEngine;
using System.Collections;

/// <summary>
/// 武器控制器 v3 — 鼠标指哪打哪（雨中冒险2风格）
/// 
/// 【射击逻辑】
///   1. 从相机向鼠标屏幕位置发射线，打到世界中的"瞄准平面"（角色高度的水平面）
///   2. 角色身体平滑转向那个世界坐标点
///   3. 子弹从枪口飞向该点
///   4. 准星图标跟随鼠标（解锁鼠标，但相机仍可用键盘/摇杆控制）
/// 
/// 注意：使用此模式时相机脚本的鼠标输入需要屏蔽，
///       或改为键盘/手柄控制相机，鼠标只控制瞄准方向。
///       详见下方 AimMode 枚举。
/// </summary>
public class WeaponController : MonoBehaviour
{
    [Header("手部锚点")]
    public Transform rightHandAnchor;
    public Transform leftHandAnchor;

    [Header("瞄准设置")]
    [Tooltip("瞄准平面高度偏移（相对于角色脚底，通常设为腰部高度）")]
    public float aimPlaneHeight = 0.8f;
    [Tooltip("角色转向瞄准点的速度")]
    public float bodyTurnSpeed  = 720f;   // 度/秒
    [Tooltip("瞄准射线检测层（地面/墙壁等）")]
    public LayerMask aimMask    = ~0;

    // ── 私有
    private InventorySystem _inv;
    private AudioSource     _audio;
    private Camera          _cam;
    private CharacterController _cc;

    private float _rightCooldown  = 0f;
    private float _leftCooldown   = 0f;
    private bool  _rightReloading = false;
    private bool  _leftReloading  = false;

    private GameObject _rightModel;
    private GameObject _leftModel;

    // 当前瞄准的世界坐标（供准星 UI 使用）
    public Vector3 AimWorldPoint { get; private set; }
    public bool    HasAimPoint   { get; private set; }

    // ── 公开状态
    public bool IsRightReloading => _rightReloading;
    public bool IsLeftReloading  => _leftReloading;

    void Awake()
    {
        _inv   = GetComponent<InventorySystem>();
        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
        _cam   = Camera.main;
        _cc    = GetComponent<CharacterController>();
    }

    void Start()
    {
        _inv.onInventoryChanged.AddListener(RefreshHandModels);
        RefreshHandModels();

        // 鼠标由 CrosshairUI 统一管理
    }

    void Update()
    {
        if (InventoryUI.IsOpen) return;

        if (_rightCooldown > 0) _rightCooldown -= Time.deltaTime;
        if (_leftCooldown  > 0) _leftCooldown  -= Time.deltaTime;

        // ── 计算瞄准世界坐标
        UpdateAimPoint();

        // ── 角色身体转向瞄准点
        RotateBodyTowardAim();

        // ── 射击输入
        if (Input.GetMouseButton(0))      TryShoot(true);
        if (Input.GetMouseButton(1))      TryShoot(false);
        if (Input.GetKeyDown(KeyCode.R)) { StartReload(true); StartReload(false); }
    }

    // ════════════════════════════════════════
    //  瞄准点计算
    // ════════════════════════════════════════
    void UpdateAimPoint()
    {
        // 从屏幕正中心（准星位置）向前发射线
        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, 200f, aimMask))
        {
            AimWorldPoint = hit.point;
            HasAimPoint   = true;
            return;
        }

        // 没打到任何东西，取射线上一个远处的点
        AimWorldPoint = ray.origin + ray.direction * 200f;
        HasAimPoint   = true;
    }

    // ════════════════════════════════════════
    //  角色转向
    // ════════════════════════════════════════
    void RotateBodyTowardAim()
    {
        if (!HasAimPoint) return;

        Vector3 dir = AimWorldPoint - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation   = Quaternion.RotateTowards(
            transform.rotation, targetRot, bodyTurnSpeed * Time.deltaTime);
    }

    // ════════════════════════════════════════
    //  射击
    // ════════════════════════════════════════
    void TryShoot(bool isRight)
    {
        var weapon = isRight ? _inv.GetRightHandWeapon() : _inv.GetLeftHandWeapon();
        if (weapon == null || weapon.bulletPrefab == null) return;
        if (isRight ? _rightReloading : _leftReloading)   return;
        if ((isRight ? _rightCooldown : _leftCooldown) > 0) return;

        int ammo = isRight ? _inv.GetRightHandAmmo() : _inv.GetLeftHandAmmo();
        if (ammo <= 0)
        {
            PlaySound(weapon.emptySound);
            StartReload(isRight);
            return;
        }

        if (isRight) _inv.ConsumeRightAmmo(); else _inv.ConsumeLeftAmmo();
        if (isRight) _rightCooldown = weapon.fireRate; else _leftCooldown = weapon.fireRate;

        FireBullet(weapon, isRight);
        PlaySound(weapon.shootSound);
        SpawnMuzzleFlash(weapon, isRight);
    }

    void FireBullet(WeaponData weapon, bool isRight)
    {
        if (!HasAimPoint) return;

        // ── 枪口位置：优先用手部锚点，没有锚点则用角色胸口高度
        Vector3 muzzlePos;
        Transform anchor = isRight ? rightHandAnchor : leftHandAnchor;
        if (anchor != null)
        {
            muzzlePos = anchor.TransformPoint(weapon.muzzleOffset);
        }
        else
        {
            // 备用：角色位置 + 胸口高度 + 前方一点
            muzzlePos = transform.position
                      + Vector3.up * 1.2f
                      + transform.forward * 0.3f;
        }

        // ── 朝向：从枪口飞向瞄准点
        Vector3 dir = (AimWorldPoint - muzzlePos).normalized;
        if (dir == Vector3.zero) dir = _cam.transform.forward;

        Quaternion rot = Quaternion.LookRotation(dir);
        var bulletGO   = Instantiate(weapon.bulletPrefab, muzzlePos, rot);
        var bullet     = bulletGO.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.damage   = weapon.damage;
            bullet.lifetime = weapon.bulletLifetime;
            bullet.Launch(dir * weapon.bulletSpeed);
        }
    }

    void SpawnMuzzleFlash(WeaponData weapon, bool isRight)
    {
        if (weapon.muzzleFlashFX == null) return;
        Transform anchor = isRight ? rightHandAnchor : leftHandAnchor;
        if (anchor == null) return;
        var fx = Instantiate(weapon.muzzleFlashFX,
                             anchor.TransformPoint(weapon.muzzleOffset),
                             anchor.rotation);
        Destroy(fx, 0.08f);
    }

    // ════════════════════════════════════════
    //  换弹
    // ════════════════════════════════════════
    void StartReload(bool isRight)
    {
        var weapon = isRight ? _inv.GetRightHandWeapon() : _inv.GetLeftHandWeapon();
        if (weapon == null) return;
        if (isRight ? _rightReloading : _leftReloading) return;
        int ammo = isRight ? _inv.GetRightHandAmmo() : _inv.GetLeftHandAmmo();
        if (ammo >= weapon.maxAmmo) return;
        StartCoroutine(ReloadCo(weapon, isRight));
    }

    IEnumerator ReloadCo(WeaponData weapon, bool isRight)
    {
        if (isRight) _rightReloading = true; else _leftReloading = true;
        PlaySound(weapon.reloadSound);
        yield return new WaitForSeconds(weapon.reloadTime);
        if (isRight) _inv.ReloadRightHand(); else _inv.ReloadLeftHand();
        if (isRight) _rightReloading = false; else _leftReloading = false;
    }

    // ════════════════════════════════════════
    //  手部模型
    // ════════════════════════════════════════
    void RefreshHandModels()
    {
        if (_rightModel) Destroy(_rightModel);
        if (_leftModel)  Destroy(_leftModel);

        var rw = _inv.GetRightHandWeapon();
        if (rw != null && rw.modelPrefab != null && rightHandAnchor != null)
        {
            _rightModel = Instantiate(rw.modelPrefab, rightHandAnchor);
            _rightModel.transform.localPosition    = rw.holdPositionOffset;
            _rightModel.transform.localEulerAngles = rw.holdRotationOffset;
        }

        var lw = _inv.GetLeftHandWeapon();
        if (lw != null && lw.modelPrefab != null && leftHandAnchor != null)
        {
            _leftModel = Instantiate(lw.modelPrefab, leftHandAnchor);
            _leftModel.transform.localPosition    = lw.holdPositionOffset;
            _leftModel.transform.localEulerAngles = lw.holdRotationOffset;
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (clip && _audio) _audio.PlayOneShot(clip);
    }
}