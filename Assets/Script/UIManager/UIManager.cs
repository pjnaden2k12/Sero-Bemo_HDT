using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Button btnUp;
    public Button btnDown;
    public Button btnLeft;
    public Button btnRight;

    public Button[] homeButtons;
    public Button[] resetButtons;
    public Button playBt;

    public GameObject groupMoveBt;
    public GameObject groupSettingBt;
    public GameObject groupLoseBt;
    public GameObject homePanel;

    public event Action<string> OnDirectionButtonPressed;
    public event Action OnPlayPressed;
    public event Action OnResetPressed;
    public event Action OnHomePressed;

    private bool levelCompleted = false;
    public int currentLevel = 1;

    void Start()
    {
        // Tự động tìm và gán nếu chưa gán trong Inspector
        if (btnUp == null) btnUp = FindButton("UpBt");
        if (btnDown == null) btnDown = FindButton("DownBt");
        if (btnLeft == null) btnLeft = FindButton("LeftBt");
        if (btnRight == null) btnRight = FindButton("RightBt");
        if (playBt == null) playBt = FindButton("PlayBt");

        // Gắn sự kiện cho các nút hướng
        btnUp?.onClick.AddListener(() => OnDirectionButtonPressed?.Invoke("Up"));
        btnDown?.onClick.AddListener(() => OnDirectionButtonPressed?.Invoke("Down"));
        btnLeft?.onClick.AddListener(() => OnDirectionButtonPressed?.Invoke("Left"));
        btnRight?.onClick.AddListener(() => OnDirectionButtonPressed?.Invoke("Right"));

        // Gắn sự kiện cho tất cả nút Home
        foreach (var btn in homeButtons)
        {
            if (btn != null)
                btn.onClick.AddListener(OnHomeButtonClicked);
        }

        // Gắn sự kiện cho tất cả nút Reset
        foreach (var btn in resetButtons)
        {
            if (btn != null)
                btn.onClick.AddListener(() => OnResetPressed?.Invoke());
        }

        playBt?.onClick.AddListener(OnPlayButtonClicked);

        ShowHomePanel();
    }

    private Button FindButton(string name)
    {
        return GameObject.Find(name)?.GetComponent<Button>();
    }

    public void SetLevelCompleted(bool completed)
    {
        levelCompleted = completed;
    }

    private void OnHomeButtonClicked()
    {
        OnHomePressed?.Invoke();
        ShowHomePanel();
    }

    private void OnPlayButtonClicked()
    {
        Debug.Log(">> Play button clicked");
        OnPlayPressed?.Invoke();

        homePanel?.SetActive(false);
        groupLoseBt?.SetActive(false);

        if (levelCompleted || currentLevel == 1)
            ShowGroupSettingAndMove();
        else
            HideGroupSettingAndMove();
    }

    public void ShowHomePanel()
    {
        homePanel?.SetActive(true);
        groupMoveBt?.SetActive(false);
        groupSettingBt?.SetActive(false);
        groupLoseBt?.SetActive(false);

        ResetScale(homePanel);
        ResetScale(groupMoveBt);
        ResetScale(groupSettingBt);
        ResetScale(groupLoseBt);
    }

    private void ShowGroupSettingAndMove()
    {
        groupSettingBt?.SetActive(true);
        groupMoveBt?.SetActive(true);

        ResetScale(groupSettingBt);
        ResetScale(groupMoveBt);
    }

    private void HideGroupSettingAndMove()
    {
        groupSettingBt?.SetActive(false);
        groupMoveBt?.SetActive(false);

        ResetScale(groupSettingBt);
        ResetScale(groupMoveBt);
    }

    private void ResetScale(GameObject obj)
    {
        if (obj != null)
            obj.transform.localScale = Vector3.one;
    }

    public void ShowLoseUI()
    {
        if (groupMoveBt != null)
            StartCoroutine(ScaleHide(groupMoveBt));
        if (groupSettingBt != null)
            StartCoroutine(ScaleHide(groupSettingBt));
        if (groupLoseBt != null)
            StartCoroutine(ScaleShow(groupLoseBt));
    }

    private IEnumerator ScaleHide(GameObject target, float duration = 0.3f)
    {
        if (target == null) yield break;

        float timer = 0f;
        while (timer < duration)
        {
            float scale = Mathf.Lerp(1f, 0f, timer / duration);
            target.transform.localScale = new Vector3(scale, scale, scale);
            timer += Time.deltaTime;
            yield return null;
        }

        target.transform.localScale = Vector3.zero;
        target.SetActive(false);
    }

    private IEnumerator ScaleShow(GameObject target, float duration = 0.3f)
    {
        if (target == null) yield break;

        target.SetActive(true);

        float timer = 0f;
        while (timer < duration * 0.7f)
        {
            float scale = Mathf.Lerp(0f, 1.2f, timer / (duration * 0.7f));
            target.transform.localScale = new Vector3(scale, scale, scale);
            timer += Time.deltaTime;
            yield return null;
        }

        timer = 0f;
        while (timer < duration * 0.3f)
        {
            float scale = Mathf.Lerp(1.2f, 1f, timer / (duration * 0.3f));
            target.transform.localScale = new Vector3(scale, scale, scale);
            timer += Time.deltaTime;
            yield return null;
        }

        target.transform.localScale = Vector3.one;
    }
}
