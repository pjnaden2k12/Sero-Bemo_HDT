using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SortingLayerController : MonoBehaviour
{
    private List<Renderer> allItemRenderers = new List<Renderer>();
    private float checkInterval = 2f; // quét lại mỗi 2 giây
    private float timer = 0f;

    [Header("Debug Options")]
    public bool enableDebug = false;

    void Update()
    {
        // Cập nhật thứ tự layer theo Y
        foreach (Renderer rend in allItemRenderers)
        {
            if (rend != null)
            {
                rend.sortingOrder = Mathf.RoundToInt(-rend.transform.position.y * 100);
            }
        }

        // Kiểm tra input debug
        if (enableDebug && Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
        {
            DebugLogRenderersInfo();
        }

        // Quét lại danh sách Renderer theo chu kỳ
        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            UpdateRendererList();
            timer = 0f;
        }
    }

    private void UpdateRendererList()
    {
        Renderer[] allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);

        allItemRenderers.Clear();
        foreach (Renderer rend in allRenderers)
        {
            if (rend != null && rend.sortingLayerName == "All_Item")
            {
                allItemRenderers.Add(rend);
            }
        }

        if (enableDebug)
        {
            Debug.Log($"[SortingLayerController] Renderer list updated: {allItemRenderers.Count} items found.");
        }
    }

    private void DebugLogRenderersInfo()
    {
        Debug.Log("=== Debug Info: All_Item Renderers ===");

        foreach (Renderer rend in allItemRenderers)
        {
            if (rend != null)
            {
                Debug.Log($"Object: {rend.gameObject.name} | Y: {rend.transform.position.y:F2} | SortingOrder: {rend.sortingOrder}");
            }
        }

        Debug.Log("======================================");
    }
}
