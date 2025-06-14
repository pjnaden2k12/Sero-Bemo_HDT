using System.Collections;
using UnityEngine;
using DG.Tweening;

public class LevelManager : MonoBehaviour
{
    public MapDatabase mapDatabase;
    public int currentLevel = 1;
    private GameObject currentMap;
    public CloudScreenEffect cloudEffect;
    public UIManager uiManager;

    void Start()
    {
        if (uiManager != null)
        {
            uiManager.OnPlayPressed += OnPlayPressed;
            uiManager.OnHomePressed += OnHomePressed;
            uiManager.OnResetPressed += OnResetPressed;

            uiManager.currentLevel = currentLevel;
            uiManager.SetLevelCompleted(false);
        }
    }

    void OnDestroy()
    {
        if (uiManager != null)
        {
            uiManager.OnPlayPressed -= OnPlayPressed;
            uiManager.OnHomePressed -= OnHomePressed;
            uiManager.OnResetPressed -= OnResetPressed;
        }
    }

    public void LoadLevel(int level)
    {
        if (uiManager != null)
            uiManager.currentLevel = level;

        StartCoroutine(LoadLevelRoutine(level));
    }

    private IEnumerator LoadLevelRoutine(int level)
    {
        if (cloudEffect != null)
            yield return cloudEffect.EnterScreenEffect();

        KillAllDOTween();           // <<< Kill toàn bộ tween trước khi làm gì đó
        KillDotweenAndDestroyMap(); // <<< Sau đó mới destroy map

        yield return null;

        var prefab = mapDatabase.GetMapPrefabByLevel(level);
        if (prefab != null)
        {
            currentMap = Instantiate(prefab, Vector3.zero, Quaternion.identity);

            var zone = currentMap.transform.Find("ZoneBounds");
            FindFirstObjectByType<CameraController>()?.AdjustCameraToZone(zone?.gameObject);

            var worm = currentMap.GetComponentInChildren<WormController>();
            var data = mapDatabase.GetMapDataByLevel(level);
            if (worm != null && data != null)
                worm.SetupLevelData(data.initialBodyCount, data.moveSequence);
        }

        if (cloudEffect != null)
            yield return cloudEffect.ExitScreenEffect();

        currentLevel = level;
    }

    private void OnPlayPressed()
    {
        LoadLevel(currentLevel);
        uiManager?.SetLevelCompleted(false);
    }

    private void OnHomePressed()
    {
        KillAllDOTween();           // <<< Kill tween toàn cục (quan trọng)
        KillDotweenAndDestroyMap(); // <<< Destroy map sau khi kill tween

        currentLevel = 1;

        if (uiManager != null)
        {
            uiManager.currentLevel = currentLevel;
            uiManager.SetLevelCompleted(false);
            uiManager.ShowHomePanel();
        }
    }

    private void OnResetPressed()
    {
        KillAllDOTween();           // <<< Kill tween trước khi reload
        LoadLevel(currentLevel);
        uiManager?.SetLevelCompleted(false);
    }

    public void OnLevelCompleted()
    {
        currentLevel++;
        uiManager?.SetLevelCompleted(true);
    }

    public void NextLevel()
    {
        LoadLevel(currentLevel + 1);
    }

    /// <summary>
    /// Kill DOTween trong currentMap và destroy map.
    /// </summary>
    private void KillDotweenAndDestroyMap()
    {
        if (currentMap != null)
        {
            DOTween.Kill(currentMap, complete: false); // Chỉ kill tween liên quan currentMap và con
            Destroy(currentMap);
            currentMap = null;
        }
    }

    /// <summary>
    /// Kill toàn bộ tween đang hoạt động (nếu có tween không ràng buộc target).
    /// </summary>
    private void KillAllDOTween()
    {
        if (DOTween.TotalPlayingTweens() > 0)
        {
            DOTween.KillAll(false);
            Debug.Log("All DOTween tweens have been killed.");
        }
    }
}
