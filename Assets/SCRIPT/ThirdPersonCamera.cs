using UnityEngine;

public class FPS_Camera : MonoBehaviour
{
    public Transform target;        
    public float mouseSensitivity = 2.0f;
    public Vector3 offset = new Vector3(0.5f, 1.8f, -3.0f); 
    public KeyCode altKey = KeyCode.LeftAlt; // 保持按键一致

    private float xRotation = 0f;
    private float yRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        if (target) yRotation = target.eulerAngles.y;
    }

    void LateUpdate()
    {
        if (!target) return;

        // 读取鼠标输入
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -40f, 60f);

        // 计算旋转
        Quaternion rotation = Quaternion.Euler(xRotation, yRotation, 0);
        
        // 关键逻辑：松开 Alt 的瞬间，身体会同步相机的 yRotation（在玩家脚本处理）
        // 这里相机只负责根据 yRotation 计算自己的位置
        transform.position = target.position + (rotation * offset);
        transform.LookAt(target.position + rotation * Vector3.forward * 10f);
    }
}