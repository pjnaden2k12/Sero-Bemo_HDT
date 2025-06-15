using DG.Tweening;
using UnityEngine;
using System.Collections;
public class LevelManager : MonoBehaviour
{
    public MapDatabase mapDatabase;
    private int currentLevel = 1;
    private GameObject currentMap;
    public CloudScreenEffect cloudEffect;
    public UIManager uiManager;

    private const string LevelProgressKey = "LevelProgress";
    private const string LevelCompletedKey = "LevelCompleted";

    void Start()
    {
        uiManager = FindFirstObjectByType<UIManager>();

        if (uiManager != null)
        {
            uiManager.OnPlayPressed += OnPlayPressed;
            uiManager.OnHomePressed += OnHomePressed;
            uiManager.OnResetPressed += OnResetPressed;
            uiManager.OnResetPlayPrefsPressed += OnResetPlayPrefsPressed;

            LoadLevelProgress();
            uiManager.ShowHomePanel();
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
        StartCoroutine(LoadLevelRoutine(level));
    }

    private IEnumerator LoadLevelRoutine(int level)
    {
        if (cloudEffect != null)
            yield return cloudEffect.EnterScreenEffect();

        KillAllDOTween();
        KillDotweenAndDestroyMap();

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
    }

    private void OnHomePressed()
    {
        StartCoroutine(ShowCloudEffectThenReturnHome());
    }

    private IEnumerator ShowCloudEffectThenReturnHome()
    {
        if (cloudEffect != null)
        {
            yield return cloudEffect.EnterScreenEffect();
        }

        KillAllDOTween();
        KillDotweenAndDestroyMap();

        SaveLevelProgress();

        if (uiManager != null)
        {
            uiManager.ShowHomePanel();
        }

        if (cloudEffect != null)
        {
            yield return cloudEffect.ExitScreenEffect();
        }
    }

    private void OnResetPressed()
    {
        KillAllDOTween();
        LoadLevel(currentLevel);
        StartCoroutine(DelaySetFalseLoseUI());
    }

    public void OnLevelCompleted()
    {
        currentLevel++;
        SaveLevelProgress();
        LoadNextLevel();
    }

    public void LoadNextLevel()
    {
        LoadLevel(currentLevel);
        SaveLevelProgress();
    }

    private void KillDotweenAndDestroyMap()
    {
        if (currentMap != null)
        {
            DOTween.Kill(currentMap, complete: false);
            Destroy(currentMap);
            currentMap = null;
        }
    }

    private void KillAllDOTween()
    {
        if (DOTween.TotalPlayingTweens() > 0)
        {
            DOTween.KillAll(false);
            Debug.Log("All DOTween tweens have been killed.");
        }
    }

    private IEnumerator DelaySetFalseLoseUI()
    {
        yield return new WaitForSeconds(1f);
        uiManager?.groupLoseBt.SetActive(false);
        if (uiManager != null)
        {
            uiManager.ShowGroupSettingAndMove();
        }
    }

    private void SaveLevelProgress()
    {
        PlayerPrefs.SetInt(LevelProgressKey, currentLevel);
        PlayerPrefs.Save();
    }

    private void LoadLevelProgress()
    {
        currentLevel = PlayerPrefs.GetInt(LevelProgressKey, 1);
    }

    public bool IsLevelCompleted()
    {
        return currentLevel > 1;
    }

    public int GetCurrentLevel()
    {
        return currentLevel;
    }

    private void OnResetPlayPrefsPressed()
    {
        currentLevel = 1;
        SaveLevelProgress();
    }
}
