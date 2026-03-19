using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 右键菜单
/// 挂在 ContextMenu GameObject 上
/// </summary>
public class ContextMenuUI : MonoBehaviour
{
    public Button   btnEquipRight;
    public Button   btnEquipLeft;
    public Button   btnDrop;
    public TMP_Text titleText;

    void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Show(ItemSlotUI slot,
                     System.Action onEquipRight,
                     System.Action onEquipLeft,
                     System.Action onDrop)
    {
        gameObject.SetActive(true);
        transform.position = Input.mousePosition + new Vector3(5, -5, 0);

        if (titleText) titleText.text = slot.currentWeapon?.weaponName ?? "";

        btnEquipRight.onClick.RemoveAllListeners();
        btnEquipLeft.onClick.RemoveAllListeners();
        btnDrop.onClick.RemoveAllListeners();

        btnEquipRight.onClick.AddListener(() => onEquipRight());
        btnEquipLeft.onClick.AddListener(()  => onEquipLeft());
        btnDrop.onClick.AddListener(()       => onDrop());
    }

    public void Hide() => gameObject.SetActive(false);
}
