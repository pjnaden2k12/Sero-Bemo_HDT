using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundScaler : MonoBehaviour
{
    public Camera mainCamera;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        ScaleToFitCamera();
    }

    void ScaleToFitCamera()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr.sprite == null)
        {
            Debug.LogError("Background has no sprite.");
            return;
        }

        // Kích thước camera
        float screenHeight = 1.4f * mainCamera.orthographicSize;
        float screenWidth = screenHeight * mainCamera.aspect;

        // Kích thước sprite
        float spriteHeight = sr.sprite.bounds.size.y;
        float spriteWidth = sr.sprite.bounds.size.x;

        // Tính toán scale
        Vector3 scale = transform.localScale;
        scale.x = screenWidth / spriteWidth;
        scale.y = screenHeight / spriteHeight;
        transform.localScale = scale;
    }

    void LateUpdate()
    {
        // Luôn scale lại nếu orthographicSize có thể thay đổi
        ScaleToFitCamera();
    }
}
