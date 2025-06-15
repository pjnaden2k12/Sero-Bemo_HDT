using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;  // Import DOTween
using System.Collections;
public class UIManager : MonoBehaviour
{
    public Button resetPlayPrefs;

    public Button btnUp;
    public Button btnDown;
    public Button btnLeft;
    public Button btnRight;

    public Button[] homeButtons;
    public Button[] resetButtons;
    public Button[] undoBts;
    public Button playBt;
    public Button guideBt;
    public Button exitBt;
    

    public GameObject groupMoveBt;
    public GameObject groupSettingBt;
    public GameObject groupLoseBt;
    public GameObject homePanel;
    public GameObject guidePanel;
    public GameObject logo;  // Logo

    public event Action<string> OnDirectionButtonPressed;
    public event Action OnPlayPressed;
    public event Action OnResetPressed;
    public event Action OnHomePressed;
    public event Action OnResetPlayPrefsPressed;
    public event Action OnUndoPressed;

    private LevelManager levelManager;
    private CloudScreenEffect cloudEffect;

    void Start()
    {
        levelManager = FindFirstObjectByType<LevelManager>();

        btnUp?.onClick.AddListener(() => OnDirectionButtonPressed?.Invoke("Up"));
        btnDown?.onClick.AddListener(() => OnDirectionButtonPressed?.Invoke("Down"));
        btnLeft?.onClick.AddListener(() => OnDirectionButtonPressed?.Invoke("Left"));
        btnRight?.onClick.AddListener(() => OnDirectionButtonPressed?.Invoke("Right"));

        foreach (var btn in homeButtons)
        {
            if (btn != null)
                btn.onClick.AddListener(() => OnHomePressed?.Invoke());
        }

        foreach (var btn in resetButtons)
        {
            if (btn != null)
                btn.onClick.AddListener(() => OnResetPressed?.Invoke());
        }
        foreach (var btn in undoBts)
        {
            if(btn != null)
                btn.onClick.AddListener(()=> OnUndoPressed?.Invoke());
        }
        resetPlayPrefs?.onClick.AddListener(() => OnResetPlayPrefsPressed.Invoke());
        
        playBt?.onClick.AddListener(OnPlayButtonClicked);
        guideBt?.onClick.AddListener(OnPlayGuidePanel);
        exitBt?.onClick.AddListener(ExitGuidePanel);
        ShowHomePanel();
    }

    private void OnPlayGuidePanel()
    {
        CanvasGroup canvasGroup = guidePanel.GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            // Đảm bảo rằng alpha được reset về 0 trước khi thực hiện fade-in
            canvasGroup.alpha = 0f;
        }

        guidePanel.SetActive(true);  // Bật guidePanel

        // Thực hiện hiệu ứng fade-in
        canvasGroup?.DOFade(1f, 0.5f);
    }

    private void ExitGuidePanel()
    {
        guidePanel.GetComponent<CanvasGroup>().DOFade(0f, 0.5f).OnComplete(() =>
        {
            guidePanel?.SetActive(false);
        });
    }


    private void OnPlayButtonClicked()
    {
        OnPlayPressed?.Invoke();
        StartCoroutine(DelaySetFalseHome());
    }

    public void ShowHomePanel()
    {
        guideBt?.gameObject.SetActive(false );
        playBt?.gameObject.SetActive(false );
        homePanel?.SetActive(true);
        groupMoveBt?.SetActive(false);
        groupSettingBt?.SetActive(false);
        groupLoseBt?.SetActive(false);

        ResetScale(homePanel);
        ResetScale(groupMoveBt);
        ResetScale(groupSettingBt);
        ResetScale(groupLoseBt);

        StartCoroutine(ShowUIElementsWithDelay()); 
    }

    private IEnumerator ShowUIElementsWithDelay()
    {
        // Hiển thị logo trượt từ trên xuống
        logo?.SetActive(true);
        RectTransform logoRect = logo.GetComponent<RectTransform>();
        logoRect.anchoredPosition = new Vector2(logoRect.anchoredPosition.x, 1500);  // Đặt logo ở trên ngoài màn hình
        logoRect.DOAnchorPosY(0, 1.5f, false);  // Trượt logo từ trên xuống trong 1 giây
        yield return new WaitForSeconds(1f);  // Chờ logo trượt xuống xong

        // Hiển thị playBt với hiệu ứng scale-up
        playBt?.gameObject.SetActive(true);
        playBt.transform.localScale = Vector3.zero;  // Đặt kích thước ban đầu là 0
        playBt.transform.DOScale(Vector3.one, 0.8f);  // Scale-up từ 0 đến 1 trong 0.5 giây
        yield return new WaitForSeconds(0.8f);  // Chờ playBt xuất hiện

        // Hiển thị guideBt với hiệu ứng fade-in
        guideBt?.gameObject.SetActive(true);
        guideBt.transform.localScale = Vector3.zero;  // Đặt kích thước ban đầu là 0
       guideBt.transform.DOScale(Vector3.one, 0.8f);  // Fade-in guideBt trong 0.5 giây
    }

    private void ResetScale(GameObject obj)
    {
        if (obj != null)
            obj.transform.localScale = Vector3.one;
    }

    public void ShowGroupSettingAndMove()
    {
        groupSettingBt?.SetActive(true);
        groupMoveBt?.SetActive(true);

        ResetScale(groupSettingBt);
        ResetScale(groupMoveBt);
    }

    public void HideGroupSettingAndMove()
    {
        groupSettingBt?.SetActive(false);
        groupMoveBt?.SetActive(false);

        ResetScale(groupSettingBt);
        ResetScale(groupMoveBt);
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

    private IEnumerator DelaySetFalseHome()
    {
        yield return new WaitForSeconds(1f);
        homePanel?.SetActive(false);
        groupLoseBt?.SetActive(false);

        if (levelManager != null)
        {
            if (levelManager.IsLevelCompleted() || levelManager.GetCurrentLevel() == 1)
                ShowGroupSettingAndMove();
            else
                HideGroupSettingAndMove();
        }
    }
}
