using UnityEngine;

public class FPS_Camera : MonoBehaviour
{
    public Transform target;        // PlayerRoot 下的 CameraTarget
    public float mouseSensitivity = 2.0f;
    // 确保 Z 是负数！比如 -3 表示在背后 3 米
    public Vector3 offset = new Vector3(0.5f, 1.8f, -3.0f); 

    private float xRotation = 0f;
    private float yRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        // 初始化 yRotation，防止一进游戏视角瞬间跳变
        yRotation = target.eulerAngles.y;
    }

    void LateUpdate()
    {
        if (!target) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -40f, 60f);

        // 核心修正：旋转叠加
        Quaternion rotation = Quaternion.Euler(xRotation, yRotation, 0);
        
        // 计算位置：角色位置 + 旋转后的偏移
        // 这样当你转鼠标时，offset 也会绕着角色转
        transform.position = target.position + (rotation * offset);
        
        // 看着角色前方的一个点（比如准星位置）
        transform.LookAt(target.position + rotation * Vector3.forward * 10f);
    }
}