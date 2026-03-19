using UnityEngine;

/// <summary>
/// 道具数据（ScriptableObject）
/// 右键 Project → Create → Game → PickupItemData
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Game/PickupItemData")]
public class PickupItemData : ScriptableObject
{
    [Header("基础信息")]
    public string itemName   = "Item";
    public Sprite icon;
    public GameObject modelPrefab;

    [Header("稀有度")]
    public WeaponRarity rarity = WeaponRarity.Common;

    [Header("描述")]
    [TextArea] public string description;

    /// <summary>根据稀有度返回颜色（复用 WeaponData 的逻辑）</summary>
    public Color RarityColor()
    {
        switch (rarity)
        {
            case WeaponRarity.Common:    return new Color(0.85f, 0.85f, 0.85f);
            case WeaponRarity.Uncommon:  return new Color(0.2f,  0.85f, 0.2f);
            case WeaponRarity.Rare:      return new Color(0.2f,  0.5f,  1f);
            case WeaponRarity.Epic:      return new Color(0.6f,  0.1f,  0.9f);
            case WeaponRarity.Legendary: return new Color(1f,    0.15f, 0.15f);
            case WeaponRarity.Mythic:    return new Color(1f,    0.85f, 0.1f);
            default: return Color.white;
        }
    }
}
