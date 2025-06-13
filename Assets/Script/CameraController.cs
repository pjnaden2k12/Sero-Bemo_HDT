using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Camera mainCamera;

    public void AdjustCameraToZone(GameObject zone)
    {
        // Lấy bounds từ collider
        var collider = zone.GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogError("Zone không có Collider2D");
            return;
        }

        Bounds bounds = collider.bounds;

        // Tính toán vị trí trung tâm
        Vector3 center = bounds.center;
        mainCamera.transform.position = new Vector3(center.x, center.y, mainCamera.transform.position.z);

        // Tính size mới cho camera (orthographic)
        float height = bounds.size.y;
        float width = bounds.size.x;

        // Tính toán aspect ratio của màn hình
        float screenAspect = (float)Screen.width / Screen.height;

        // Tính toán size cần thiết để hiển thị toàn bộ vùng
        float cameraSize = Mathf.Max(height / 2, width / (2 * screenAspect));
        mainCamera.orthographicSize = cameraSize;
    }
}
