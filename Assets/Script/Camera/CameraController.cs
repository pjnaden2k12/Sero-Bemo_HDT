using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Camera mainCamera;

    public void AdjustCameraToZone(GameObject zone)
    {
       
        var collider = zone.GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogError("Zone không có Collider2D");
            return;
        }

        Bounds bounds = collider.bounds;

        
        Vector3 center = bounds.center;
        mainCamera.transform.position = new Vector3(center.x, center.y, mainCamera.transform.position.z);

    
        float height = bounds.size.y;
        float width = bounds.size.x;

       
        float screenAspect = (float)Screen.width / Screen.height;

     
        float cameraSize = Mathf.Max(height / 2, width / (2 * screenAspect));
        mainCamera.orthographicSize = cameraSize;
    }
}
